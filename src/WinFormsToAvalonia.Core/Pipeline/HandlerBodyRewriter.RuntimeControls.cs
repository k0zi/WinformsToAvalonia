using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Controls a handler creates itself: <c>new Button { Text = "Go" }</c>, given a position and a
/// parent with <c>Controls.Add</c> - or a status label added to a strip's <c>Items</c>.
/// </summary>
/// <remarks>
/// <para>
/// The creation is admissible only for a type whose undecorated mapping is the bare element
/// (<see cref="RuntimeControlSupport"/>), so <c>new Element()</c> is what the AXAML would have
/// declared. From then on the local is a control like any designer one - it joins the same alias
/// table a <c>(Button)sender</c> cast does - so every property it is given goes through the same
/// catalog, and anything the catalog does not know still stops the handler.
/// </para>
/// <para>
/// Only a control the body created may be handed to a parent: a designer control already has one,
/// and where WinForms silently re-parents, Avalonia throws. A runtime-control <em>field</em> may have
/// been added before, so it is detached first - which is the WinForms behaviour, spelled out.
/// </para>
/// <para>
/// Position and size are the Canvas the emitter lays every container out with: <c>Location</c>
/// becomes <c>Canvas.SetLeft</c>/<c>SetTop</c>, <c>Size</c> becomes Width/Height. That holds for a
/// designer control only where its parent really is such a Canvas.
/// </para>
/// </remarks>
public sealed partial class HandlerBodyRewriter
{
    /// <summary><c>var b = new Button { … };</c> or <c>ToolStripItem[] items = { label };</c>.</summary>
    private static bool TryRewriteRuntimeControlDeclaration(
        TypeSyntax declaredType, string name, ExpressionSyntax initializer, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (initializer is ObjectCreationExpressionSyntax creation)
        {
            var createdType = RoslynTypeNameHelper.GetSimpleTypeName(creation.Type);
            if (!declaredType.IsVar && RoslynTypeNameHelper.GetSimpleTypeName(declaredType) != createdType)
            {
                return false;
            }

            if (!TryRewriteRuntimeCreation(creation, target, out var element, out var winFormsType))
            {
                return false;
            }

            target.DeclareRuntimeControl(name, winFormsType);
            return TryAppendInitializer($"var {name} = new {element}();", name, creation.Initializer, target, out rewritten);
        }

        if (initializer is not (InitializerExpressionSyntax { RawKind: (int)SyntaxKind.ArrayInitializerExpression }
                or ArrayCreationExpressionSyntax { Initializer: not null }
                or ImplicitArrayCreationExpressionSyntax)
            || declaredType is not (ArrayTypeSyntax or IdentifierNameSyntax { IsVar: true })
            || !TryRewriteControlArrayElements(initializer, target, out var elements, out var detach, out var category))
        {
            return false;
        }

        // The element category goes with the local: a strip takes ToolStripItems, a Canvas the rest.
        target.Locals.Declare(name, LocalKind.ControlArray, string.Join(",", detach), category);
        rewritten = new RewrittenStatement($"Control[] {name} = {{ {string.Join(", ", elements)} }};");
        return true;
    }

    /// <summary><c>label = new ToolStripStatusLabel();</c> into a runtime-control field.</summary>
    private static bool TryRewriteRuntimeControlAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            || assignment.Right is not ObjectCreationExpressionSyntax creation
            || assignment.Left switch
            {
                IdentifierNameSyntax { Identifier.ValueText: var bare } when !target.Locals.TryGet(bare, out _) => bare,
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var member } => member,
                _ => null,
            } is not { } fieldName
            || !target.IsRuntimeControl(fieldName, out var isField)
            || !isField
            || !target.TryResolveControlTypeName(fieldName, out var fieldType)
            || RoslynTypeNameHelper.GetSimpleTypeName(creation.Type) != fieldType
            || !TryRewriteRuntimeCreation(creation, target, out var element, out _))
        {
            return false;
        }

        return TryAppendInitializer($"{fieldName} = new {element}();", fieldName, creation.Initializer, target, out rewritten);
    }

    /// <summary>
    /// <c>new T()</c> or <c>new T { … }</c> of a creatable control, with no constructor argument -
    /// a WinForms control constructor taking one is a different control (a hosted one, a caption).
    /// </summary>
    private static bool TryRewriteRuntimeCreation(
        ObjectCreationExpressionSyntax creation, IRewriteTarget target, out string element, out string winFormsType)
    {
        winFormsType = RoslynTypeNameHelper.GetSimpleTypeName(creation.Type);
        element = "";

        return creation.ArgumentList is null or { Arguments.Count: 0 }
            && creation.Initializer is null or { RawKind: (int)SyntaxKind.ObjectInitializerExpression }
            && target.TryResolveRuntimeControlType(winFormsType, out element);
    }

    /// <summary>
    /// An object initializer's members, each as the <c>name.Member = value;</c> it stands for -
    /// translated by the ordinary statement path, so a member the catalog does not know refuses
    /// the whole creation.
    /// </summary>
    private static bool TryAppendInitializer(
        string creationStatement, string name, InitializerExpressionSyntax? initializer, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;
        var parts = new List<RewrittenStatement> { new(creationStatement) };

        foreach (var member in initializer?.Expressions ?? [])
        {
            if (member is not AssignmentExpressionSyntax { Left: IdentifierNameSyntax property } memberAssignment
                || !memberAssignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                return false;
            }

            var statement = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(name), property.WithoutTrivia()),
                memberAssignment.Right.WithoutTrivia()));

            if (!TryRewriteStatement(statement, target, out var part))
            {
                return false;
            }

            parts.Add(part);
        }

        rewritten = Merge(string.Join("\n", parts.Select(p => p.Text)), parts);
        return true;
    }

    /// <summary>
    /// The elements of an array of controls the body created - all ToolStripItems or all
    /// controls, never a mix, since only one kind of parent can take the array.
    /// </summary>
    private static bool TryRewriteControlArrayElements(
        ExpressionSyntax arrayExpression, IRewriteTarget target, out List<string> elements, out List<string> detach, out string category)
    {
        elements = [];
        detach = [];
        category = "";

        var initializer = arrayExpression switch
        {
            InitializerExpressionSyntax direct => direct,
            ArrayCreationExpressionSyntax { Initializer: { } explicitInitializer } => explicitInitializer,
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer,
            _ => null,
        };

        if (initializer is not { Expressions.Count: > 0 })
        {
            return false;
        }

        foreach (var element in initializer.Expressions)
        {
            if (!target.TryResolveControlField(element, out var field)
                || !target.IsRuntimeControl(field, out var isField)
                || !target.TryResolveControlTypeName(field, out var type))
            {
                return false;
            }

            var elementCategory = ChildCategory(type);
            if (category.Length > 0 && category != elementCategory)
            {
                return false;
            }

            category = elementCategory;
            elements.Add(field);
            if (isField)
            {
                detach.Add(field);
            }
        }

        return true;
    }

    /// <summary>Which child collection can hold a control: a strip's Items, or a container's Controls.</summary>
    private static string ChildCategory(string winFormsTypeName) =>
        winFormsTypeName.StartsWith("ToolStrip", StringComparison.Ordinal) ? "Items" : "Controls";

    /// <summary>
    /// <c>Controls.Add(button)</c>, <c>panel1.Controls.Remove(button)</c>,
    /// <c>statusStrip1.Items.AddRange(items)</c> - for a control the body created.
    /// </summary>
    private static bool TryRewriteChildCollectionCall(
        ExpressionSyntax? receiver, string methodName, InvocationExpressionSyntax invocation, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (methodName is not ("Add" or "Remove" or "AddRange")
            || invocation.ArgumentList.Arguments is not [{ RefKindKeyword.RawKind: 0, Expression: var argument }])
        {
            return false;
        }

        string? owner;
        string collection;
        switch (receiver)
        {
            case IdentifierNameSyntax { Identifier.ValueText: "Controls" } when !target.Locals.TryGet("Controls", out _):
                (owner, collection) = (null, "Controls");
                break;
            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: "Controls" }:
                (owner, collection) = (null, "Controls");
                break;
            case MemberAccessExpressionSyntax { Name.Identifier.ValueText: ("Controls" or "Items") and var name } access
                when target.TryResolveControlField(access.Expression, out var ownerField):
                (owner, collection) = (ownerField, name);
                break;
            default:
                return false;
        }

        if (!target.TryResolveChildCollection(owner, collection, out var children))
        {
            return false;
        }

        if (methodName == "AddRange")
        {
            List<string> detach;
            string elementsText;
            if (argument is IdentifierNameSyntax { Identifier.ValueText: var arrayLocal }
                && target.Locals.TryGetBinding(arrayLocal, out var binding)
                && binding is { Kind: LocalKind.ControlArray, WinFormsTypeName: var arrayCategory })
            {
                if (arrayCategory != collection)
                {
                    return false;
                }

                detach = binding.ControlFieldName is { Length: > 0 } fields ? [.. fields.Split(',')] : [];
                elementsText = arrayLocal;
            }
            else if (TryRewriteControlArrayElements(argument, target, out var elements, out detach, out var category)
                && category == collection)
            {
                elementsText = $"new Control[] {{ {string.Join(", ", elements)} }}";
            }
            else
            {
                return false;
            }

            rewritten = new RewrittenStatement(string.Join(
                "\n",
                detach.Select(Detach).Append($"{children}.AddRange({elementsText});")));
            return true;
        }

        if (!target.TryResolveControlField(argument, out var child)
            || !target.IsRuntimeControl(child, out var childIsField)
            || !target.TryResolveControlTypeName(child, out var childType)
            || ChildCategory(childType) != collection)
        {
            return false;
        }

        rewritten = new RewrittenStatement(methodName == "Remove"
            ? $"{children}.Remove({child});"
            : (childIsField ? Detach(child) + "\n" : "") + $"{children}.Add({child});");
        return true;
    }

    /// <summary>WinForms moves a control that already has a parent; Avalonia throws instead.</summary>
    private static string Detach(string control) => $"({control}.Parent as Panel)?.Children.Remove({control});";

    /// <summary>
    /// <c>button.Location = new Point(x, y);</c>, <c>Size = new Size(w, h)</c>, <c>Left</c>,
    /// <c>Top</c>, <c>Width</c>, <c>Height</c> - on a control a Canvas positions.
    /// </summary>
    private static bool TryRewriteLayoutAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            || assignment.Left is not MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: ("Location" or "Size" or "Left" or "Top" or "Width" or "Height") and var property,
            } access
            || !target.TryResolveControlField(access.Expression, out var field)
            || !target.IsCanvasPositioned(field))
        {
            return false;
        }

        if (property is "Location" or "Size")
        {
            if (assignment.Right is not ObjectCreationExpressionSyntax
                {
                    ArgumentList.Arguments: [{ Expression: var first }, { Expression: var second }],
                    Initializer: null,
                } pair
                || RoslynTypeNameHelper.GetSimpleTypeName(pair.Type) != (property == "Location" ? "Point" : "Size")
                || !TryRewriteExpression(first, target, out var firstText)
                || !TryRewriteExpression(second, target, out var secondText))
            {
                return false;
            }

            rewritten = new RewrittenStatement(property == "Location"
                ? $"Canvas.SetLeft({field}, {firstText});\nCanvas.SetTop({field}, {secondText});"
                : $"{field}.Width = {firstText};\n{field}.Height = {secondText};");
            return true;
        }

        if (!TryRewriteExpression(assignment.Right, target, out var value))
        {
            return false;
        }

        rewritten = new RewrittenStatement(property switch
        {
            "Left" => $"Canvas.SetLeft({field}, {value});",
            "Top" => $"Canvas.SetTop({field}, {value});",
            _ => $"{field}.{property} = {value};",
        });
        return true;
    }

    private sealed partial class ViewTarget
    {
        private readonly HashSet<string> _runtimeLocals = new(StringComparer.Ordinal);

        /// <summary>The containers whose children the emitter lays out on a Canvas at their designer coordinates.</summary>
        private static readonly HashSet<string> CanvasContainers = new(StringComparer.Ordinal) { "Panel", "GroupBox", "TabPage" };

        public bool TryResolveRuntimeControlType(string winFormsTypeName, out string avaloniaElementName) =>
            RuntimeControlSupport.TryGetElement(controlMappings, winFormsTypeName, out avaloniaElementName);

        public void DeclareRuntimeControl(string localName, string winFormsTypeName)
        {
            _senderAliases[localName] = new ControlModel { FieldName = localName, ClrTypeName = winFormsTypeName };
            _runtimeLocals.Add(localName);
            Locals.Declare(localName, LocalKind.Control, localName);
        }

        public bool IsRuntimeControl(string fieldName, out bool isField)
        {
            isField = false;
            if (_runtimeLocals.Contains(fieldName) && Locals.TryGet(fieldName, out _))
            {
                return true;
            }

            isField = formModel.RuntimeControls.ContainsKey(fieldName) && !Locals.TryGet(fieldName, out _);
            return isField;
        }

        public bool TryResolveChildCollection(string? ownerField, string collectionName, out string childrenExpression)
        {
            childrenExpression = "";

            // Every View's content is the one root Canvas the emitter writes (see AxamlEmitter) -
            // a Window's and a UserControl's alike.
            if (ownerField is null)
            {
                childrenExpression = "((Canvas)Content!).Children";
                return collectionName == "Controls";
            }

            if (!TryGetControl(ownerField, out var owner))
            {
                return false;
            }

            var mapped = controlMappings.Map(owner);
            var hostsChildren = collectionName switch
            {
                // A Panel that stayed a Canvas - one that paints became a childless surface.
                "Controls" => owner.ClrTypeName == "Panel" && mapped is { Status: MappingStatus.Direct, AvaloniaElementName: "Canvas" },
                // A status strip is a StackPanel of its items (StatusStripFallback).
                "Items" => mapped is { Status: MappingStatus.Fallback, FallbackTemplateKey: "StatusStripFallback" },
                _ => false,
            };

            childrenExpression = $"{ownerField}.Children";
            return hostsChildren;
        }

        public bool IsCanvasPositioned(string fieldName)
        {
            if (IsRuntimeControl(fieldName, out _))
            {
                return true;
            }

            if (!formModel.Controls.TryGetValue(fieldName, out var control) || _senderAliases.ContainsKey(fieldName))
            {
                return false;
            }

            return formModel.RootControls.Contains(control)
                || formModel.Controls.Values.Any(c => CanvasContainers.Contains(c.ClrTypeName) && c.Children.Contains(control));
        }
    }
}
