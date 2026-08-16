using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Walks a WinForms Designer.cs InitializeComponent() method body and extracts the raw
/// facts of one Form/UserControl: control creations and their property assignments,
/// Form-level property assignments (`this.Text = ...`), event subscriptions (`+=`), and
/// `Controls.Add`/`AddRange` parent/child edges. Statements are processed once, in source
/// order, relying on designer-generated code always creating a control before it
/// configures/adds it. Assembling the edges into an actual tree (RootControls/Components)
/// is ControlGraphBuilder's job, not this class's.
/// </summary>
public sealed class DesignerSyntaxWalker
{
    // Value types designer code constructs inline for a Form/control PROPERTY value
    // (`this.ClientSize = new Size(w, h);`, `this.Font = new Font(...);`). Everything else
    // constructed via `this.field = new T(...);` is a control/component - including cases
    // like `this.refreshTimer = new Timer(this.components);` where the constructor does
    // take an argument, so argument count alone can't be the discriminator here.
    private static readonly HashSet<string> InlineValueTypeNames = new(StringComparer.Ordinal)
    {
        "Size", "Point", "Padding", "Font",
    };

    public DesignerWalkResult Walk(string designerFileContent, string designerFilePath, string className, string? @namespace)
    {
        var tree = CSharpSyntaxTree.ParseText(designerFileContent, path: designerFilePath);
        var root = tree.GetRoot();

        var formModel = new FormModel { ClassName = className, Namespace = @namespace };
        var edges = new List<ParentChildEdge>();

        var initializeComponent = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m =>
                m.Identifier.ValueText == "InitializeComponent"
                && m.Ancestors().OfType<ClassDeclarationSyntax>().Any(c => c.Identifier.ValueText == className));

        if (initializeComponent?.Body is null)
        {
            return new DesignerWalkResult(formModel, edges);
        }

        foreach (var statement in initializeComponent.Body.Statements)
        {
            if (statement is not ExpressionStatementSyntax expressionStatement)
            {
                continue;
            }

            switch (expressionStatement.Expression)
            {
                case AssignmentExpressionSyntax assignment:
                    HandleAssignment(formModel, assignment);
                    break;
                case InvocationExpressionSyntax invocation:
                    HandleInvocation(invocation, edges);
                    HandleSetToolTipInvocation(formModel, invocation);
                    break;
            }
        }

        return new DesignerWalkResult(formModel, edges);
    }

    private static void HandleAssignment(FormModel formModel, AssignmentExpressionSyntax assignment)
    {
        if (!TryParseThisMemberAccess(assignment.Left, out var first, out var second))
        {
            return;
        }

        // `this.components` is the designer's IContainer field for non-visual components
        // (Timer, ToolTip, ...), not a control itself.
        if (first == "components")
        {
            return;
        }

        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            if (second is null)
            {
                HandleThisLevelAssignment(formModel, first, assignment.Right);
            }
            else if (formModel.Controls.TryGetValue(first, out var control))
            {
                control.Properties[second] = ExpressionEvaluator.Evaluate(assignment.Right);
            }

            return;
        }

        if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
        {
            HandleEventSubscription(formModel, first, second, assignment.Right);
        }
    }

    private static void HandleThisLevelAssignment(FormModel formModel, string name, ExpressionSyntax right)
    {
        if (right is ObjectCreationExpressionSyntax creation
            && !InlineValueTypeNames.Contains(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type)))
        {
            var typeName = RoslynTypeNameHelper.GetSimpleTypeName(creation.Type);
            formModel.Controls[name] = new ControlModel { FieldName = name, ClrTypeName = typeName };
        }
        else
        {
            formModel.FormProperties[name] = ExpressionEvaluator.Evaluate(right);
        }
    }

    private static void HandleEventSubscription(FormModel formModel, string first, string? second, ExpressionSyntax right)
    {
        if (!TryParseEventHandlerRhs(right, out var methodName, out var inlineBody))
        {
            return;
        }

        if (second is null)
        {
            // `this.EventName += ...` - a Form/UserControl-level event (e.g. Load).
            formModel.FormEvents.Add(new EventHandlerBinding(first, methodName, inlineBody));
        }
        else if (formModel.Controls.TryGetValue(first, out var control))
        {
            control.Events.Add(new EventHandlerBinding(second, methodName, inlineBody));
        }
    }

    private static bool TryParseEventHandlerRhs(ExpressionSyntax right, out string? methodName, out string? inlineBody)
    {
        if (right is LambdaExpressionSyntax lambda)
        {
            methodName = null;
            inlineBody = lambda.ToString();
            return true;
        }

        // `new System.EventHandler(this.button1_Click)` - the common designer form.
        if (right is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 1 } creation
            && TryGetMethodGroupName(creation.ArgumentList!.Arguments[0].Expression, out var wrappedName))
        {
            methodName = wrappedName;
            inlineBody = null;
            return true;
        }

        // `this.button1.Click += this.button1_Click;` - modern bare method-group form.
        if (TryGetMethodGroupName(right, out var directName))
        {
            methodName = directName;
            inlineBody = null;
            return true;
        }

        methodName = null;
        inlineBody = null;
        return false;
    }

    private static bool TryGetMethodGroupName(ExpressionSyntax expression, out string name)
    {
        if (expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var memberName })
        {
            name = memberName;
            return true;
        }

        if (expression is IdentifierNameSyntax identifier)
        {
            name = identifier.Identifier.ValueText;
            return true;
        }

        name = "";
        return false;
    }

    private static void HandleInvocation(InvocationExpressionSyntax invocation, List<ParentChildEdge> edges)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Add" or "AddRange",
                // "Controls" (a visual-tree child), "Items"/"DropDownItems" (a
                // MenuStrip/ToolStrip/StatusStrip/ContextMenuStrip/ToolStripMenuItem's owned
                // items - ToolStripItem doesn't derive from Control, but the shape is
                // identical), or "Columns" (a DataGridView's owned column definitions).
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Controls" or "Items" or "DropDownItems" or "Columns" } controlsAccess,
            } addMember)
        {
            return;
        }

        string parent;
        if (controlsAccess.Expression is ThisExpressionSyntax)
        {
            // `this.Controls.Add(...)` - a direct child of the Form/UserControl itself.
            parent = ParentChildEdge.FormOwner;
        }
        else if (controlsAccess.Expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var ownerField })
        {
            // `this.panel1.Controls.Add(...)` / `this.menuStrip1.Items.Add(...)` /
            // `this.fileMenuItem.DropDownItems.Add(...)` / `this.grid1.Columns.Add(...)` -
            // a child of the owning field, regardless of which of the four collection names
            // above was used.
            parent = ownerField;
        }
        else if (controlsAccess.Expression is MemberAccessExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var containerField },
                Name.Identifier.ValueText: ("Panel1" or "Panel2") and var panelName,
            })
        {
            // `this.splitContainer1.Panel1.Controls.Add(...)` - a child of one of
            // SplitContainer's two named sub-regions, not a real ControlModel of its own.
            // Encoded as a synthetic "field.PanelN" parent id - real WinForms field names
            // can never contain '.', so this can't collide with a real field or the FormOwner
            // sentinel. ControlGraphBuilder is what splits it back apart.
            parent = $"{containerField}.{panelName}";
        }
        else
        {
            return;
        }

        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        var isAddRange = addMember.Name.Identifier.ValueText == "AddRange";
        var childExpressions = isAddRange
            ? GetArrayElements(invocation.ArgumentList.Arguments[0].Expression)
            : [invocation.ArgumentList.Arguments[0].Expression];

        foreach (var childExpression in childExpressions)
        {
            if (TryGetControlFieldReference(childExpression, out var childField))
            {
                edges.Add(new ParentChildEdge(parent, childField));
            }
        }
    }

    /// <summary>
    /// Recognizes `this.toolTip1.SetToolTip(this.someControl, "text")` - a ToolTip
    /// component's tooltip assignment isn't a `Controls.Add`/property-assignment shape at
    /// all, it's a plain method call on the (non-visual) ToolTip field. Stores the resolved
    /// text on the *target* control's Properties under "ToolTipText", exactly like a normal
    /// `this.someControl.SomeProperty = ...;` assignment would - so AxamlEmitter can treat it
    /// identically regardless of which ToolTip field it came from.
    /// </summary>
    private static void HandleSetToolTipInvocation(FormModel formModel, InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "SetToolTip",
                Expression: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var ownerField },
            })
        {
            return;
        }

        if (!formModel.Controls.TryGetValue(ownerField, out var owner) || owner.ClrTypeName != "ToolTip")
        {
            return;
        }

        if (invocation.ArgumentList.Arguments.Count != 2)
        {
            return;
        }

        if (!TryGetControlFieldReference(invocation.ArgumentList.Arguments[0].Expression, out var targetField)
            || !formModel.Controls.TryGetValue(targetField, out var target))
        {
            return;
        }

        target.Properties["ToolTipText"] = ExpressionEvaluator.Evaluate(invocation.ArgumentList.Arguments[1].Expression);
    }

    private static IEnumerable<ExpressionSyntax> GetArrayElements(ExpressionSyntax arrayExpression) => arrayExpression switch
    {
        ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
        ImplicitArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
        _ => [],
    };

    private static bool TryGetControlFieldReference(ExpressionSyntax expression, out string fieldName)
    {
        if (expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name })
        {
            fieldName = name;
            return true;
        }

        if (expression is IdentifierNameSyntax identifier)
        {
            fieldName = identifier.Identifier.ValueText;
            return true;
        }

        fieldName = "";
        return false;
    }

    /// <summary>
    /// Recognizes `this.first` (second = null) and `this.first.second` target shapes.
    /// Any other assignment target (indexers, static members, deeper chains) is not
    /// understood by this walker and returns false so the caller skips the statement.
    /// </summary>
    private static bool TryParseThisMemberAccess(ExpressionSyntax expression, out string first, out string? second)
    {
        if (expression is MemberAccessExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var innerName },
                Name.Identifier.ValueText: var outerName,
            })
        {
            first = innerName;
            second = outerName;
            return true;
        }

        if (expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name })
        {
            first = name;
            second = null;
            return true;
        }

        first = "";
        second = null;
        return false;
    }
}
