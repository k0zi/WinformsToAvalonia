using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Pens and brushes a <c>Paint</c> handler builds itself: <c>using (var p = new Pen(Color.Black, 2))</c>,
/// <c>p.DashStyle = DashStyle.Dash;</c>, then <c>e.Graphics.DrawLine(p, …)</c>.
/// </summary>
/// <remarks>
/// <para>
/// A GDI+ <c>Pen</c> is a colour, a width and a dash pattern; an Avalonia <c>Pen</c> is a brush, a
/// thickness and a dash style - so the local is built as the Avalonia object straight away, and
/// every drawing call that names it takes it as it is. The local gets its own kind
/// (<see cref="LocalKind.Pen"/>, <see cref="LocalKind.Brush"/>), so it is accepted only where the
/// matching argument is, and only the members listed here may be written.
/// </para>
/// <para>
/// <c>using</c> is dropped rather than carried: the GDI+ objects hold a native handle, the Avalonia
/// ones are plain managed objects that are not <c>IDisposable</c> at all - keeping the statement
/// would be a CS1674 in the generated project. The scope it gave the local is kept as a block.
/// </para>
/// <para>
/// The dash patterns are GDI+'s own, in units of the pen's width - which is also how Avalonia
/// measures a <c>DashStyle</c>'s dashes - so a dashed line has the same rhythm on both sides, not
/// merely "some dashes". A custom pattern, a cap, a join and a transform all refuse: GDI+ caps each
/// end separately and has arrow anchors, and Avalonia has one cap for both ends and no arrows.
/// </para>
/// </remarks>
public sealed partial class HandlerBodyRewriter
{
    /// <summary>GDI+'s <c>DashStyle</c> patterns (dash, gap, …) in pen widths; <c>Solid</c> has none.</summary>
    private static readonly IReadOnlyDictionary<string, string?> DashPatterns =
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Solid"] = null,
            ["Dash"] = "3, 1",
            ["Dot"] = "1, 1",
            ["DashDot"] = "3, 1, 1, 1",
            ["DashDotDot"] = "3, 1, 1, 1, 1, 1",
        };

    private static readonly string[] DrawingUsings = ["Avalonia.Media"];

    /// <summary><c>var p = new Pen(Color.Black, 2);</c> / <c>Brush b = new SolidBrush(c);</c>, plain or <c>using</c>.</summary>
    private static bool TryRewriteDrawingToolDeclaration(
        TypeSyntax declaredType, string name, ExpressionSyntax initializer, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!TryRewriteDrawingToolCreation(initializer, target, out var kind, out var creation)
            || !(declaredType.IsVar || RoslynTypeNameHelper.GetSimpleTypeName(declaredType) is var typeName
                 && (kind == LocalKind.Pen ? typeName == "Pen" : typeName is "Brush" or "SolidBrush")))
        {
            return false;
        }

        target.Locals.Declare(name, kind);
        rewritten = new RewrittenStatement($"var {name} = {creation};", RequiredUsings: DrawingUsings);
        return true;
    }

    /// <summary><c>using (var p = new Pen(...)) { ... }</c> - the declaration and a block, without the <c>using</c>.</summary>
    private static bool TryRewriteDrawingToolUsing(UsingStatementSyntax usingStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (usingStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)
            || usingStatement.Declaration is not { Variables: [{ Initializer.Value: { } initializer } variable] } declaration)
        {
            return false;
        }

        target.Locals.Push();
        try
        {
            if (!TryRewriteDrawingToolDeclaration(declaration.Type, variable.Identifier.ValueText, initializer, target, out var declared)
                || !TryRewriteBranch(usingStatement.Statement, target, out var body))
            {
                return false;
            }

            rewritten = Merge($"{{\n{Indent(declared.Text)}\n{Indent(body.Text)}\n}}", [declared, body]);
            return true;
        }
        finally
        {
            target.Locals.Pop();
        }
    }

    /// <summary><c>p.Width = 2;</c>, <c>p.Color = Color.Red;</c>, <c>p.DashStyle = DashStyle.Dot;</c>, <c>b.Color = …;</c>.</summary>
    private static bool TryRewriteDrawingToolAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            || assignment.Left is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: var local },
                Name.Identifier.ValueText: var member,
            }
            || !target.Locals.TryGet(local, out var kind)
            || kind is not (LocalKind.Pen or LocalKind.Brush))
        {
            return false;
        }

        string? text = (kind, member) switch
        {
            (LocalKind.Pen, "Width") when TryRewriteExpression(assignment.Right, target, out var width) => $"{local}.Thickness = {width};",
            (LocalKind.Pen, "Color") when TryRewriteColorExpression(assignment.Right, target, out var brush) => $"{local}.Brush = {brush};",
            (LocalKind.Pen, "DashStyle") when TryRewriteDashStyle(assignment.Right, out var dash) => $"{local}.DashStyle = {dash};",
            // A SolidColorBrush's colour is its Color, already an Avalonia Color inside the brush text.
            (LocalKind.Brush, "Color") when TryRewriteColorExpression(assignment.Right, target, out var brush) && IsSolidBrushCreation(brush) =>
                $"{local}.Color = ({brush}).Color;",
            _ => null,
        };

        if (text is null)
        {
            return false;
        }

        rewritten = new RewrittenStatement(text, RequiredUsings: DrawingUsings);
        return true;
    }

    private static bool TryRewriteDrawingToolCreation(
        ExpressionSyntax expression, IRewriteTarget target, out LocalKind kind, out string creation)
    {
        kind = default;
        creation = "";

        if (expression is not ObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: var arguments } created)
        {
            return false;
        }

        switch (RoslynTypeNameHelper.GetSimpleTypeName(created.Type), arguments.Count)
        {
            case ("Pen", 1 or 2) when TryRewriteColorExpression(arguments[0].Expression, target, out var brush):
                kind = LocalKind.Pen;
                if (arguments.Count == 1)
                {
                    creation = $"new Pen({brush})";
                    return true;
                }

                if (!TryRewriteExpression(arguments[1].Expression, target, out var width))
                {
                    return false;
                }

                creation = $"new Pen({brush}, {width})";
                return true;

            case ("SolidBrush", 1) when TryRewriteColorExpression(arguments[0].Expression, target, out var brush) && IsSolidBrushCreation(brush):
                kind = LocalKind.Brush;
                creation = brush;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// A colour that came out as a brush of its own, not one read off a control: a brush local is
    /// a <c>SolidColorBrush</c> whose <c>Color</c> a later statement may write, and a control's
    /// brush is only an <c>IBrush</c> - and shared with the control.
    /// </summary>
    private static bool IsSolidBrushCreation(string brush) =>
        brush.StartsWith("new SolidColorBrush(", StringComparison.Ordinal);

    /// <summary><c>DashStyle.Dash</c> (however qualified) as an Avalonia dash style, or <c>null</c> for a solid line.</summary>
    private static bool TryRewriteDashStyle(ExpressionSyntax expression, out string text)
    {
        text = "";

        if (expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var member } access
            || access.Expression switch
               {
                   IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                   MemberAccessExpressionSyntax qualified => qualified.Name.Identifier.ValueText,
                   _ => null,
               } != "DashStyle"
            || !DashPatterns.TryGetValue(member, out var pattern))
        {
            return false;
        }

        text = pattern is null ? "null" : $"new DashStyle(new double[] {{ {pattern} }}, 0)";
        return true;
    }

    /// <summary>A pen or brush local of the kind the drawing call wants - the name itself.</summary>
    private static bool TryRewriteDrawingToolLocal(
        ExpressionSyntax expression, bool wantsPen, IRewriteTarget target, out string text)
    {
        text = "";

        if (expression is not IdentifierNameSyntax { Identifier.ValueText: var name }
            || !target.Locals.TryGet(name, out var kind)
            || kind != (wantsPen ? LocalKind.Pen : LocalKind.Brush))
        {
            return false;
        }

        text = name;
        return true;
    }
}
