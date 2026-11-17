using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// A <c>TableLayoutPanel</c> asked about its cells: <c>GetColumn(label)</c>, <c>GetRow(label)</c>,
/// <c>GetControlFromPosition(c, r)</c>, and its children walked by index.
/// </summary>
/// <remarks>
/// <para>
/// The panel became a Canvas, but each child carries its designer cell as <c>Grid.Column</c> /
/// <c>Grid.Row</c> (see <c>AxamlEmitter</c>), so the answers are still there to read:
/// <c>Grid.GetColumn(label)</c> is what the original's <c>GetColumn</c> returned for it.
/// </para>
/// <para>
/// Admissible only for a panel the designer filled completely and plainly: every child placed by
/// <c>Controls.Add(child, column, row)</c> and nothing after it, no two in one cell, all of one
/// WinForms type mapped to one element. Then the child list has the same order and length on both
/// sides - nothing is inserted, nothing dropped - so <c>Controls[i]</c> is <c>Children[i]</c>, a
/// cast to that type cannot fail where it did not, and the cell a child was put in is the one it
/// occupies, which is what <c>GetControlFromPosition</c> searches.
/// </para>
/// <para>
/// A control asked about has to be one of the panel's children - a field, or a local every
/// candidate of which is (a shared handler's <c>sender</c>, a child read by index). A control
/// outside the panel has no cell at all in WinForms; in Avalonia it would read 0.
/// </para>
/// </remarks>
public sealed partial class HandlerBodyRewriter
{
    /// <summary>A panel <see cref="IRewriteTarget.TryResolveTableLayout"/> admitted.</summary>
    private sealed record TableLayoutTarget(
        string FieldName, string ElementName, string ChildWinFormsTypeName, IReadOnlyList<string> ChildFieldNames);

    private const string CellLambdaParameter = "w2aCell";

    /// <summary><c>panel.GetColumn(x)</c>, <c>panel.GetRow(x)</c>, <c>panel.Controls.Count</c>.</summary>
    private static bool TryRewriteTableLayoutExpression(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        switch (expression)
        {
            case InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: ("GetColumn" or "GetRow") and var method } access,
                ArgumentList.Arguments: [{ NameColon: null, RefKindKeyword.RawKind: 0, Expression: var child }],
            } when target.TryResolveTableLayout(access.Expression, out var layout)
                && target.TryResolveControlCandidates(StripSuppression(child), out var childText, out var candidates)
                && candidates.All(layout.ChildFieldNames.Contains):
                text = $"Grid.{method}({childText})";
                return true;

            case MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Count",
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Controls" } controls,
            } when target.TryResolveTableLayout(controls.Expression, out var counted):
                text = $"{counted.FieldName}.Children.Count";
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// <c>var column = panel.GetColumn(label);</c>, <c>var label = (Label)panel.Controls[i];</c> and
    /// <c>var label = panel.GetControlFromPosition(c, r);</c>.
    /// </summary>
    private static bool TryRewriteTableLayoutDeclaration(
        TypeSyntax declaredType, string name, ExpressionSyntax initializer, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;
        initializer = StripSuppression(initializer);

        // The cell number. An int on both sides - and declared as one, so the semantic check can
        // take it as an argument although it cannot type the WinForms call it came from.
        if (initializer is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetColumn" or "GetRow" } }
            && (declaredType.IsVar || declaredType.ToString() == "int")
            && TryRewriteTableLayoutExpression(initializer, target, out var cell))
        {
            target.Locals.Declare(name, LocalKind.Value, vouchedType: SpecialType.System_Int32);
            rewritten = new RewrittenStatement($"{declaredType} {name} = {cell};");
            return true;
        }

        TypeSyntax? castType = null;
        if (initializer is CastExpressionSyntax cast)
        {
            castType = cast.Type;
            initializer = StripSuppression(cast.Expression);
        }

        string value;
        TableLayoutTarget layout;
        switch (initializer)
        {
            // `(Label)panel.Controls[i]` - the cast is what makes the local a Label at all.
            case ElementAccessExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Controls" } controls,
                ArgumentList.Arguments: [{ Expression: var index }],
            } when castType is not null
                && target.TryResolveTableLayout(controls.Expression, out layout)
                && TryRewriteExpression(index, target, out var indexText):
                value = $"({layout.ElementName}){layout.FieldName}.Children[{indexText}]";
                break;

            // WinForms answers null for an empty cell, and so does FirstOrDefault; the `!` only
            // keeps the generated project warning-free, it changes nothing at run time.
            case InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetControlFromPosition" } access,
                ArgumentList.Arguments: [{ NameColon: null, Expression: var column }, { NameColon: null, Expression: var row }],
            } when target.TryResolveTableLayout(access.Expression, out layout)
                && !target.Locals.TryGet(CellLambdaParameter, out _)
                && IsEvaluatedOnce(column) && IsEvaluatedOnce(row)
                && TryRewriteExpression(column, target, out var columnText)
                && TryRewriteExpression(row, target, out var rowText):
                value = $"{layout.FieldName}.Children.OfType<{layout.ElementName}>().FirstOrDefault("
                    + $"{CellLambdaParameter} => Grid.GetColumn({CellLambdaParameter}) == {columnText} "
                    + $"&& Grid.GetRow({CellLambdaParameter}) == {rowText})!";
                break;

            default:
                return false;
        }

        var childType = layout.ChildWinFormsTypeName;
        if ((castType is not null && RoslynTypeNameHelper.GetSimpleTypeName(castType) != childType)
            || !(declaredType.IsVar || RoslynTypeNameHelper.GetSimpleTypeName(declaredType) is var declared && (declared == childType || declared == "Control")))
        {
            return false;
        }

        target.DeclareTableChildAlias(name, layout);
        rewritten = new RewrittenStatement($"var {name} = {value};", RequiredUsings: ["System.Linq"]);
        return true;
    }

    /// <summary>
    /// A predicate argument is evaluated once per child, so it may only be something whose reading
    /// has no effect: a name or a literal.
    /// </summary>
    private static bool IsEvaluatedOnce(ExpressionSyntax expression) =>
        StripSuppression(expression) is IdentifierNameSyntax or LiteralExpressionSyntax;

    private sealed partial class ViewTarget
    {
        /// <summary>Every control a control-typed local may be, keyed by the local's name.</summary>
        private readonly Dictionary<string, IReadOnlyList<string>> _aliasCandidates = new(StringComparer.Ordinal);

        public bool TryResolveTableLayout(ExpressionSyntax receiver, out TableLayoutTarget layout)
        {
            layout = null!;

            if (!TryResolveControlField(receiver, out var fieldName)
                || _senderAliases.ContainsKey(fieldName)
                || !formModel.Controls.TryGetValue(fieldName, out var panel)
                || panel.ClrTypeName != "TableLayoutPanel"
                || controlMappings.Map(panel) is not { Status: MappingStatus.Direct, AvaloniaElementName: "Canvas" })
            {
                return false;
            }

            var children = panel.Children;
            if (children.Count == 0
                || children.Any(c => c.TableCell is null)
                || children.Select(c => c.TableCell).Distinct().Count() != children.Count
                || children.Any(c => c.ClrTypeName != children[0].ClrTypeName))
            {
                return false;
            }

            var mapped = children.Select(controlMappings.Map).ToList();
            if (mapped.Any(m => m.Status != MappingStatus.Direct || !m.SupportsName)
                || mapped.Select(m => m.AvaloniaElementName).Distinct(StringComparer.Ordinal).Count() != 1
                || mapped[0].AvaloniaElementName is not { } elementName)
            {
                return false;
            }

            layout = new TableLayoutTarget(fieldName, elementName, children[0].ClrTypeName, [.. children.Select(c => c.FieldName)]);
            return true;
        }

        public bool TryResolveControlCandidates(ExpressionSyntax expression, out string text, out IReadOnlyList<string> fieldNames)
        {
            text = "";
            fieldNames = [];

            if (!TryResolveControlField(expression, out var fieldName))
            {
                return false;
            }

            if (_aliasCandidates.TryGetValue(fieldName, out var candidates))
            {
                (text, fieldNames) = (fieldName, candidates);
                return true;
            }

            // A local standing for a control without a known set - a control the body created.
            if (_senderAliases.ContainsKey(fieldName) || !formModel.Controls.ContainsKey(fieldName))
            {
                return false;
            }

            (text, fieldNames) = (fieldName, [fieldName]);
            return true;
        }

        public void DeclareTableChildAlias(string localName, TableLayoutTarget layout)
        {
            _senderAliases[localName] = formModel.Controls[layout.ChildFieldNames[0]];
            _aliasCandidates[localName] = layout.ChildFieldNames;
            Locals.Declare(localName, LocalKind.Control, localName);
        }

        /// <summary>Records which controls a shared handler's <c>sender</c> local may be.</summary>
        private void RecordSenderCandidates(string localName, IEnumerable<ControlModel> sources) =>
            _aliasCandidates[localName] = [.. sources.Select(s => s.FieldName)];
    }
}
