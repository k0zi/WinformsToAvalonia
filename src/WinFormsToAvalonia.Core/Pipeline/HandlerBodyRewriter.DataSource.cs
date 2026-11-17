using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// A grid handed its rows from code: <c>dataGridView1.DataSource = orders;</c> becomes
/// <c>dataGridView1.ItemsSource = orders;</c>.
/// </summary>
/// <remarks>
/// <para>
/// The value is proven, not guessed: it must pass through as UI-neutral code, and the semantic
/// model must say it is a plain <c>IList</c> - what WinForms shows one row per element of, reading
/// each row's public properties, which is exactly what Avalonia's grid does with an
/// <c>ItemsSource</c>. A <c>DataTable</c>/<c>DataSet</c> is refused (WinForms reads it through
/// property descriptors Avalonia has no counterpart for), and so is a <c>BindingSource</c>, which
/// binds to an error type and never passes through.
/// </para>
/// <para>
/// WinForms generates the columns unless told not to; Avalonia only when told to. So the
/// translation says so - <c>AutoGenerateColumns = true</c> - where the original grid had no columns
/// of its own and the designer left the default. A grid with designer columns <em>and</em> the
/// default is refused: WinForms would add only the properties those columns do not already show,
/// Avalonia every one of them again.
/// </para>
/// </remarks>
public sealed partial class HandlerBodyRewriter
{
    private static bool TryRewriteGridDataSource(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            || assignment.Left is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "DataSource" } access
            || !target.TryResolveControlField(access.Expression, out var field)
            || !target.TryResolveGridItemsSource(field, out var enableAutoGenerate))
        {
            return false;
        }

        string value;
        if (assignment.Right.IsKind(SyntaxKind.NullLiteralExpression))
        {
            value = "null";
            enableAutoGenerate = false;
        }
        else if (target.Semantics is not { } semantics
                 || !semantics.IsPlainListDataSource(assignment.Right)
                 || !TryPassThrough(assignment.Right, target, out value, out var usesAwait)
                 || usesAwait)
        {
            return false;
        }

        rewritten = new RewrittenStatement(enableAutoGenerate
            ? $"{field}.AutoGenerateColumns = true;\n{field}.ItemsSource = {value};"
            : $"{field}.ItemsSource = {value};");
        return true;
    }

    private sealed partial class ViewTarget
    {
        public bool TryResolveGridItemsSource(string fieldName, out bool enableAutoGenerate)
        {
            enableAutoGenerate = false;

            if (!TryGetControl(fieldName, out var grid)
                || grid.ClrTypeName != "DataGridView"
                || controlMappings.Map(grid) is not { Status: MappingStatus.Direct, AvaloniaElementName: "DataGrid" }
                || grid.Properties.ContainsKey("DataSource"))
            {
                return false;
            }

            var explicitlyOff = grid.Properties.TryGetValue("AutoGenerateColumns", out var autoGenerate)
                && autoGenerate is PropertyValue.Literal(false);
            if (explicitlyOff)
            {
                return true;
            }

            enableAutoGenerate = true;
            return grid.Children.Count == 0;
        }
    }
}
