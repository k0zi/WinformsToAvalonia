using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Mapping;
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

    /// <param name="resx">
    /// The form's paired .resx, when it has one. Only consulted for
    /// <c>resources.ApplyResources(...)</c> calls; passing null (or an empty document) keeps the
    /// walker's behaviour exactly as it was before resources were understood at all.
    /// </param>
    public DesignerWalkResult Walk(
        string designerFileContent,
        string designerFilePath,
        string className,
        string? @namespace,
        ResxDocument? resx = null)
    {
        var tree = CSharpSyntaxTree.ParseText(designerFileContent, path: designerFilePath);
        var root = tree.GetRoot();

        var formModel = new FormModel { ClassName = className, Namespace = @namespace };
        var edges = new List<ParentChildEdge>();
        var hostedAliases = new Dictionary<string, string>(StringComparer.Ordinal);
        var warnings = new List<string>();

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
                    HandleAssignment(formModel, assignment, hostedAliases);
                    break;
                case InvocationExpressionSyntax invocation:
                    HandleInvocation(formModel, invocation, edges, warnings);
                    HandleExtenderProviderInvocation(formModel, invocation, warnings);
                    HandleApplyResourcesInvocation(formModel, invocation, resx, className, warnings);
                    break;
            }
        }

        return new DesignerWalkResult(formModel, edges, warnings, hostedAliases);
    }

    /// <summary>
    /// `resources.ApplyResources(this.button1, "button1")` - the shape a Localizable form uses
    /// for *every* property, including Location/Size/Text. Resolves the matching .resx entries
    /// onto the target's own <see cref="ControlModel.Properties"/>, exactly as the equivalent
    /// `this.button1.Text = ...;` assignment would, so nothing downstream needs to know the
    /// value came from a resource file.
    /// </summary>
    /// <remarks>
    /// Applied at the call site rather than before or after the whole walk, which preserves the
    /// "statements processed once, in source order" rule: the designer emits ApplyResources
    /// first in a control's block, so a later explicit assignment still wins - as it does at
    /// run time.
    /// </remarks>
    private static void HandleApplyResourcesInvocation(
        FormModel formModel,
        InvocationExpressionSyntax invocation,
        ResxDocument? resx,
        string className,
        List<string> warnings)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ApplyResources" }
            || invocation.ArgumentList.Arguments.Count != 2
            || invocation.ArgumentList.Arguments[1].Expression is not LiteralExpressionSyntax { Token.Value: string resourceKey })
        {
            return;
        }

        var target = invocation.ArgumentList.Arguments[0].Expression;

        // `resources.ApplyResources(this, "$this")` configures the form itself.
        var properties = target is ThisExpressionSyntax
            ? formModel.FormProperties
            : TryGetControlFieldReference(target, out var fieldName) && formModel.Controls.TryGetValue(fieldName, out var control)
                ? control.Properties
                : null;

        if (properties is null)
        {
            return;
        }

        if (resx is null || ReferenceEquals(resx, ResxDocument.Empty))
        {
            // Once per form, not once per ApplyResources call: a localizable form makes one per
            // control, and the fact is the same every time.
            var message =
                $"'{className}' configures its controls through resources.ApplyResources(...) but no .resx file was " +
                "found next to it - every property that form sets through resources (Text, Location, Size, ...) is " +
                "missing from the generated view.";

            if (!warnings.Contains(message, StringComparer.Ordinal))
            {
                warnings.Add(message);
            }

            return;
        }

        foreach (var entry in resx.EntriesFor(resourceKey))
        {
            // A base64 payload has no XAML-attribute form; it becomes a copied asset instead,
            // resolved by ConversionPipeline once it knows the output project's layout.
            var value = entry.IsBinary
                ? new PropertyValue.ResourceReference(entry.Name)
                : ResxPropertyProvider.Convert(entry);

            // An entry this converter cannot express is left out entirely rather than written
            // as a guess - the same rule the rest of the pipeline follows.
            if (value is not null)
            {
                properties[entry.PropertyName] = value;
            }
        }
    }

    private static void HandleAssignment(
        FormModel formModel, AssignmentExpressionSyntax assignment, Dictionary<string, string> hostedAliases)
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
                HandleThisLevelAssignment(formModel, first, assignment.Right, hostedAliases);
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

    private static void HandleThisLevelAssignment(
        FormModel formModel, string name, ExpressionSyntax right, Dictionary<string, string> hostedAliases)
    {
        if (right is ObjectCreationExpressionSyntax creation
            && !InlineValueTypeNames.Contains(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type)))
        {
            var typeName = RoslynTypeNameHelper.GetSimpleTypeName(creation.Type);
            formModel.Controls[name] = new ControlModel { FieldName = name, ClrTypeName = typeName };

            // A host is plumbing around a control the designer names right here - the only shape
            // it can take, since the type has no parameterless constructor. Recorded only: the
            // host goes on collecting property assignments until the walk is over.
            if (HostedControlCatalog.TryGetHostedArgumentIndex(typeName, out var argumentIndex)
                && creation.ArgumentList is { } arguments
                && arguments.Arguments.Count > argumentIndex
                && TryGetControlFieldReference(arguments.Arguments[argumentIndex].Expression, out var hostedField))
            {
                hostedAliases[name] = hostedField;
            }
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

    private static void HandleInvocation(
        FormModel formModel, InvocationExpressionSyntax invocation, List<ParentChildEdge> edges, List<string> warnings)
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
                Name.Identifier.ValueText: var regionName,
            }
            && (regionName is "Panel1" or "Panel2" || ToolStripContainerRegionCatalog.IsRegion(regionName)))
        {
            // `this.splitContainer1.Panel1.Controls.Add(...)` /
            // `this.toolStripContainer1.ContentPanel.Controls.Add(...)` - a child of one of a
            // container's named sub-regions, not a real ControlModel of its own. Encoded as a
            // synthetic "field.Region" parent id - real WinForms field names can never contain
            // '.', so this can't collide with a real field or the FormOwner sentinel.
            // ControlGraphBuilder is what splits it back apart.
            parent = $"{containerField}.{regionName}";
        }
        else
        {
            // The same `this.container.Region.Controls.Add(...)` shape, but a region this
            // converter has no slot for. SplitContainer's two halves and ToolStripContainer's
            // five regions are handled above; anything else has nowhere to go. Saying nothing
            // is worse than warning: such a child used to vanish from the AXAML *and* from the
            // report, because a Direct-mapped child parked in FormModel.Components is not
            // something the pipeline warns about either.
            WarnAboutUnplacedRegionChildren(invocation, controlsAccess, warnings);
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

        // Only a real `Items` collection can hold literal entries; `Controls`/`DropDownItems`/
        // `Columns` always take objects, and a literal there would be a designer bug.
        var isItemsCollection = controlsAccess.Name.Identifier.ValueText == "Items";

        foreach (var childExpression in childExpressions)
        {
            if (TryGetControlFieldReference(childExpression, out var childField))
            {
                edges.Add(new ParentChildEdge(parent, childField));
                continue;
            }

            // `comboBox1.Items.AddRange(new object[] { "A", "B" })` - plain entries that are not
            // controls at all. Stored on the owner so AxamlEmitter can emit them as real items
            // instead of dropping the list.
            if (isItemsCollection
                && childExpression is LiteralExpressionSyntax { Token.Value: string literalItem }
                && formModel.Controls.TryGetValue(parent, out var owner))
            {
                owner.LiteralItems.Add(literalItem);
            }
        }
    }

    /// <summary>
    /// Reports children added to a named sub-region this converter cannot place.
    /// </summary>
    /// <remarks>
    /// Only for the two-level receiver shape (`this.container.Region.Controls.Add(x)`); anything
    /// else that reaches the same branch is not a container region at all and stays silent, as it
    /// always has. Named per child, because the whole point is that the user can find them.
    /// </remarks>
    private static void WarnAboutUnplacedRegionChildren(
        InvocationExpressionSyntax invocation,
        MemberAccessExpressionSyntax controlsAccess,
        List<string> warnings)
    {
        if (controlsAccess.Expression is not MemberAccessExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var containerField },
                Name.Identifier.ValueText: var regionName,
            }
            || invocation.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        var childExpressions = invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddRange" }
            ? GetArrayElements(invocation.ArgumentList.Arguments[0].Expression)
            : [invocation.ArgumentList.Arguments[0].Expression];

        var childNames = childExpressions
            .Select(e => TryGetControlFieldReference(e, out var field) ? field : null)
            .OfType<string>()
            .ToList();

        if (childNames.Count == 0)
        {
            return;
        }

        warnings.Add(
            $"'{containerField}.{regionName}' holds {string.Join(", ", childNames.Select(n => $"'{n}'"))}, "
            + "which is a nested container region this conversion has no slot for - a SplitContainer's "
            + "Panel1/Panel2 and a ToolStripContainer's five regions are the ones it places. Those "
            + $"controls are not emitted; add them to the generated '{containerField}' by hand.");
    }

    /// <summary>
    /// Recognizes `this.toolTip1.SetToolTip(this.someControl, "text")` - a ToolTip
    /// An extender provider's designer output is neither a `Controls.Add` nor a property
    /// assignment - it is a plain two-argument call on a non-visual field,
    /// `this.toolTip1.SetToolTip(this.button1, "text")`, and the value belongs to the argument
    /// rather than to the field it was called on. The resolved value is parked on the *target*
    /// control's Properties under the key `ExtenderProviderCatalog` names, exactly as an ordinary
    /// `this.someControl.SomeProperty = ...;` would - so everything downstream treats it
    /// identically, regardless of which provider field it came from.
    /// </summary>
    private static void HandleExtenderProviderInvocation(
        FormModel formModel, InvocationExpressionSyntax invocation, List<string> warnings)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: var methodName,
                Expression: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var ownerField },
            })
        {
            return;
        }

        if (!formModel.Controls.TryGetValue(ownerField, out var owner)
            || !ExtenderProviderCatalog.IsProvider(owner.ClrTypeName))
        {
            return;
        }

        if (!ExtenderProviderCatalog.TryGetSetter(owner.ClrTypeName, methodName, out var setter))
        {
            // A provider this converter knows, calling a setter it cannot translate
            // (SetShowHelp, SetHelpKeyword, SetError). Reported by name rather than dropped:
            // the value the designer recorded is real, and silence about it is what made
            // HelpProvider's whole contribution disappear without a trace.
            warnings.Add(
                $"'{ownerField}' ({owner.ClrTypeName}) calls '{methodName}(...)', which has no Avalonia " +
                "equivalent - that setting is not carried over.");
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

        target.Properties[setter.PropertyKey] = ExpressionEvaluator.Evaluate(invocation.ArgumentList.Arguments[1].Expression);
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
