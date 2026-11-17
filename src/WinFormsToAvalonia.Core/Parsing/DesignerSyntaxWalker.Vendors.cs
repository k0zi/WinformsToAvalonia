using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Mapping.Vendors;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

public sealed partial class DesignerSyntaxWalker
{
    /// <summary>
    /// The vendor half of one walk: which controls are vendor controls, the designer locals that
    /// stand for some of them, and the shapes only their designers write.
    /// </summary>
    /// <remarks>
    /// Nothing here runs for a control that is not a vendor control - that is the whole guarantee a
    /// WinForms project's conversion rests on, and every entry point checks it first.
    /// </remarks>
    private sealed class VendorWalk(
        VendorCatalog catalog,
        IReadOnlySet<string> usings,
        string className,
        bool vendorBaseForm,
        List<VendorLink> links)
    {
        /// <summary>Designer local name → model key (<c>col1</c> → <c>col1@MainForm</c>).</summary>
        private readonly Dictionary<string, string> _locals = new(StringComparer.Ordinal);

        private int _elementTreeAssignments;

        public bool VendorBaseForm => vendorBaseForm;

        /// <summary>Marks a freshly created control as the vendor control it is, when it is one.</summary>
        public void Recognise(ControlModel control, ObjectCreationExpressionSyntax creation)
        {
            if (control.WrittenTypeName is not { } written || catalog.Resolve(written, usings) is not { } entry)
            {
                return;
            }

            control.VendorTypeName = entry.FullName;
            ApplyConstructorText(control, entry, creation);
        }

        /// <summary>
        /// <c>Telerik.WinControls.UI.GridViewTextBoxColumn col1 = new ...();</c> - a designer local of
        /// a type the catalog allows as one. Keyed so that it can collide neither with a field nor
        /// with the same local name in a base Form's designer.
        /// </summary>
        public void DeclareLocals(FormModel formModel, LocalDeclarationStatementSyntax declaration)
        {
            foreach (var variable in declaration.Declaration.Variables)
            {
                if (variable.Initializer?.Value is not ObjectCreationExpressionSyntax creation)
                {
                    continue;
                }

                var written = creation.Type.ToString();
                if (catalog.Resolve(written, usings) is not { AllowedAsLocal: true } entry)
                {
                    continue;
                }

                var name = variable.Identifier.ValueText;
                var key = $"{name}@{className}";
                var control = new ControlModel
                {
                    FieldName = key,
                    ClrTypeName = RoslynTypeNameHelper.GetSimpleTypeName(creation.Type),
                    WrittenTypeName = written,
                    VendorTypeName = entry.FullName,
                    IsDesignerLocal = true,
                };
                ApplyConstructorText(control, entry, creation);
                formModel.Controls[key] = control;
                _locals[name] = key;
            }
        }

        /// <summary>
        /// An assignment the ordinary <c>this.a</c>/<c>this.a.b</c> parse refused: a deeper chain on a
        /// vendor control (<c>this.textEdit1.Properties.ReadOnly</c>), a vendor local
        /// (<c>col1.FieldName</c>), or a vendor Form's own nested setting.
        /// </summary>
        public void HandleDeepAssignment(FormModel formModel, AssignmentExpressionSyntax assignment, IReadOnlySet<string> declaredFields)
        {
            if (!TryParseChain(assignment.Left, out var owner, out var path))
            {
                // `((FillPrimitive)(this.radButton1.GetChildAt(0))).BackColor = ...` - Telerik's
                // element tree, which has nothing to map onto. Counted, and said once.
                if (assignment.Left.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                    .Any(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetChildAt" }))
                {
                    _elementTreeAssignments++;
                }

                return;
            }

            if (formModel.Controls.TryGetValue(owner, out var control))
            {
                if (control.VendorTypeName is null)
                {
                    return;
                }

                if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    control.Properties[path] = ExpressionEvaluator.Evaluate(assignment.Right);
                }
                else if (assignment.IsKind(SyntaxKind.AddAssignmentExpression)
                    && TryParseEventHandlerRhs(assignment.Right, out var methodName, out var inlineBody))
                {
                    control.Events.Add(new EventHandlerBinding(path, methodName, inlineBody));
                }

                return;
            }

            if (vendorBaseForm
                && !_locals.ContainsKey(owner)
                && !declaredFields.Contains(owner)
                && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                formModel.FormProperties[$"{owner}.{path}"] = ExpressionEvaluator.Evaluate(assignment.Right);
            }
        }

        /// <summary>
        /// <c>this.f.&lt;path&gt;.Add/AddRange(...)</c> on a vendor control whose catalog entry names
        /// the collection. True when the call was a vendor collection and has been accounted for -
        /// recorded, or reported - so the generic <c>Controls/Items/Columns</c> handling must not see
        /// it too. A vendor control's plain <c>Controls.Add</c> is not one of these and falls through.
        /// </summary>
        public bool TryHandleCollection(
            FormModel formModel, InvocationExpressionSyntax invocation, List<ParentChildEdge> edges, List<string> warnings)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add" or "AddRange" } add
                || !TryParseChain(add.Expression, out var owner, out var path)
                || !formModel.Controls.TryGetValue(owner, out var control)
                || control.VendorTypeName is null
                || !catalog.TryGet(control.VendorTypeName, out var entry))
            {
                return false;
            }

            if (entry.CollectionAt(path) is not { } collection)
            {
                // A plain collection (`Controls`) or a WinForms container region (`Panel1.Controls`)
                // is the generic handler's. Anything deeper is a vendor collection nothing here reads.
                if (path.Count(c => c == '.') < 1 || path.EndsWith(".Controls", StringComparison.Ordinal))
                {
                    return false;
                }

                warnings.Add(
                    $"'{DisplayName(owner)}.{path}' ({entry.SimpleName}) is filled in InitializeComponent, but this conversion "
                    + "does not read that collection - what it holds is not carried over.");
                return true;
            }

            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count == 0)
            {
                return true;
            }

            var isAddRange = add.Name.Identifier.ValueText == "AddRange";
            var elements = isAddRange ? GetArrayElements(arguments[0].Expression) : [arguments[0].Expression];

            // `ItemLinks.Add(this.barButtonItem1, true)` - the second argument is BeginGroup.
            var addBeginsGroup = !isAddRange
                && arguments.Count > 1
                && arguments[1].Expression.IsKind(SyntaxKind.TrueLiteralExpression);

            foreach (var element in elements)
            {
                switch (collection.Kind)
                {
                    case VendorCollectionKind.Registration:
                        break;

                    case VendorCollectionKind.Children when TryResolveItem(element, out var child):
                        edges.Add(new ParentChildEdge(owner, child));
                        break;

                    case VendorCollectionKind.Items:
                        if (element is LiteralExpressionSyntax { Token.Value: string literal })
                        {
                            control.LiteralItems.Add(literal);
                        }
                        else if (TryResolveItem(element, out var item))
                        {
                            links.Add(new VendorLink(owner, path, item, BeginGroup: false));
                        }

                        break;

                    case VendorCollectionKind.ConstructedItems:
                        if (element is LiteralExpressionSyntax { Token.Value: string bare })
                        {
                            control.LiteralItems.Add(bare);
                        }
                        else if (element is ObjectCreationExpressionSyntax { ArgumentList.Arguments: var itemArguments }
                            && itemArguments.Count > collection.CaptionArgument
                            && itemArguments[collection.CaptionArgument].Expression is LiteralExpressionSyntax { Token.Value: string caption })
                        {
                            control.LiteralItems.Add(caption);
                        }

                        break;

                    case VendorCollectionKind.Links:
                        AddLink(owner, path, element, addBeginsGroup);
                        break;
                }
            }

            return true;
        }

        /// <summary>Says once what the walk saw and could not read.</summary>
        public void ReportUnreadStatements(List<string> warnings)
        {
            if (_elementTreeAssignments > 0)
            {
                warnings.Add(
                    $"'{className}' styles {_elementTreeAssignments} Telerik element(s) through the element tree "
                    + "(GetChildAt(...) in InitializeComponent). Avalonia has no element tree of that shape - those settings "
                    + "are not carried over; restyle the controls with Avalonia styles.");
            }
        }

        /// <summary>
        /// <c>new LinkPersistInfo(this.barButtonItem1, true)</c> or a bare <c>this.barButtonItem1</c>:
        /// the first argument that is an item, and BeginGroup when any argument is <c>true</c>.
        /// </summary>
        private void AddLink(string owner, string path, ExpressionSyntax element, bool addBeginsGroup)
        {
            if (TryResolveItem(element, out var direct))
            {
                links.Add(new VendorLink(owner, path, direct, addBeginsGroup));
                return;
            }

            if (element is not ObjectCreationExpressionSyntax { ArgumentList.Arguments: var arguments })
            {
                return;
            }

            string? item = null;
            foreach (var argument in arguments)
            {
                if (item is null && TryResolveItem(argument.Expression, out var resolved))
                {
                    item = resolved;
                }
            }

            if (item is not null)
            {
                var beginGroup = arguments.Any(a => a.Expression.IsKind(SyntaxKind.TrueLiteralExpression));
                links.Add(new VendorLink(owner, path, item, beginGroup));
            }
        }

        /// <summary><c>this.x</c> is the field x; a bare identifier is a vendor local if one was declared, else a field.</summary>
        private bool TryResolveItem(ExpressionSyntax expression, out string key)
        {
            switch (expression)
            {
                case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var field }:
                    key = field;
                    return true;
                case IdentifierNameSyntax { Identifier.ValueText: var name }:
                    key = _locals.GetValueOrDefault(name, name);
                    return true;
                default:
                    key = "";
                    return false;
            }
        }

        /// <summary>
        /// <c>this.f.A.B</c> → (<c>f</c>, <c>A.B</c>), and <c>local.A.B</c> → (the local's key,
        /// <c>A.B</c>). Only plain member access all the way down - a cast or a call in the chain is
        /// not a designer property.
        /// </summary>
        private bool TryParseChain(ExpressionSyntax expression, out string owner, out string path)
        {
            var segments = new List<string>();
            var current = expression;
            while (current is MemberAccessExpressionSyntax access)
            {
                segments.Add(access.Name.Identifier.ValueText);
                current = access.Expression;
            }

            segments.Reverse();
            owner = "";
            path = "";

            if (current is ThisExpressionSyntax && segments.Count >= 2)
            {
                owner = segments[0];
                path = string.Join('.', segments.Skip(1));
                return true;
            }

            if (current is IdentifierNameSyntax { Identifier.ValueText: var local }
                && segments.Count >= 1
                && _locals.TryGetValue(local, out var key))
            {
                owner = key;
                path = string.Join('.', segments);
                return true;
            }

            return false;
        }

        private static void ApplyConstructorText(ControlModel control, VendorControl entry, ObjectCreationExpressionSyntax creation)
        {
            if (entry.ConstructorTextArgument is { } index
                && creation.ArgumentList is { } arguments
                && arguments.Arguments.Count > index
                && arguments.Arguments[index].Expression is LiteralExpressionSyntax { Token.Value: string text })
            {
                control.Properties["Text"] = new PropertyValue.Literal(text);
            }
        }

        private static string DisplayName(string key) => key.Split('@')[0];
    }
}
