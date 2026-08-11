using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Converter.Core.Parsing;

/// <summary>
/// Result of GdiDrawingTranspiler.TryTranspile - Success/TransformedSource on a full,
/// syntactically-valid translation; Success == false and FailureReason set the moment anything
/// falls outside the recognized vocabulary (see class doc). All-or-nothing per file: never a
/// partially-transpiled result.
/// </summary>
public sealed record GdiTranspileResult(bool Success, string? TransformedSource, string? FailureReason)
{
    public static GdiTranspileResult Fail(string reason) => new(false, null, reason);
    public static GdiTranspileResult Ok(string source) => new(true, source, null);
}

/// <summary>
/// Translates a narrow, specific GDI+ drawing vocabulary (System.Drawing's
/// Bitmap/Graphics/Font/SolidBrush/Pen/Color, used to procedurally draw into an offscreen
/// bitmap - exactly the pattern WarehouseApp's Common/AppIcons.cs uses to generate glyph
/// icons at runtime) into the equivalent Avalonia
/// RenderTargetBitmap/DrawingContext/FormattedText/Typeface/Color code. The Avalonia API shapes
/// below were verified against the actual referenced Avalonia.Base.dll (12.0.0) via
/// decompilation, not assumed from memory - see the mapping table in this feature's plan.
///
/// This is intentionally narrow, not a general GDI+ transpiler: it recognizes exactly the
/// statement shapes listed in TranspileStatements/TranspileExpressionStatement below (bitmap
/// construction, Graphics.FromImage, SmoothingMode assignment (dropped), Clear, SolidBrush/Pen
/// construction, FillEllipse, DrawRectangle, Font construction + a single paired
/// MeasureString/DrawString call, named/ARGB Color values) and bails out for the whole file the
/// moment it encounters anything else - consistent with this codebase's "never emit
/// plausible-looking-but-wrong code" principle. A method signature (return type, parameters) is
/// never rewritten - "Bitmap"/"Color" resolve to the Avalonia types purely through the usings
/// this transpiler emits, since RenderTargetBitmap : Avalonia.Media.Imaging.Bitmap covers the
/// return-type case without any text change being needed.
/// </summary>
public static class GdiDrawingTranspiler
{
    private static readonly HashSet<string> GdiDrawingApiNames = new(StringComparer.Ordinal)
    {
        "Graphics", "Bitmap", "Font", "SolidBrush", "Pen", "SmoothingMode", "Brushes", "DashStyle"
    };

    /// <summary>
    /// True if <paramref name="root"/> references any GDI+ drawing API - the signal
    /// SupportFileScanner uses to decide whether a file is even a transpile candidate (as
    /// opposed to a file with no drawing code at all, which is just copied verbatim).
    /// </summary>
    public static bool HasGdiDrawingApiUsage(SyntaxNode root) =>
        root.DescendantNodes().OfType<SimpleNameSyntax>().Any(n => GdiDrawingApiNames.Contains(n.Identifier.Text));

    /// <summary>
    /// Lightweight sibling to TryTranspile for files that reference bare System.Drawing.Color
    /// (named colors, FromArgb) but none of the drawing APIs above - no statement restructuring
    /// needed, just token-level renames (Color.White -> Colors.White, the FromArgb/FromRgb
    /// rules from the same table TranslateColorExpression already implements for the full
    /// transpiler). Returns null when there's nothing to rewrite (caller should copy the file
    /// verbatim instead) rather than an unchanged copy of the input.
    /// </summary>
    public static string? TryRewriteColorOnly(string sourceCode)
    {
        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetCompilationUnitRoot();

            var replacements = new List<(TextSpan Span, string NewText)>();

            foreach (var node in root.DescendantNodes())
            {
                switch (node)
                {
                    case InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Color" }, Name.Identifier.Text: "FromArgb" }
                        } invocation:
                        var translated = TranslateColorExpression(invocation);
                        if (translated != null)
                        {
                            replacements.Add((invocation.Span, translated));
                        }
                        break;

                    case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Color" }, Name.Identifier.Text: var namedColor }
                        when namedColor != "FromArgb":
                        replacements.Add((node.Span, $"Colors.{namedColor}"));
                        break;
                }
            }

            if (replacements.Count == 0)
            {
                return null;
            }

            var text = sourceCode;
            foreach (var (span, newText) in replacements.OrderByDescending(r => r.Span.Start))
            {
                text = text[..span.Start] + newText + text[span.End..];
            }

            // "System.Drawing" would make "Color" ambiguous once Avalonia.Media is also in
            // scope (both declare a "Color" type) - drop it; add Avalonia.Media for Colors/Color.
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^using System\.Drawing;\r?\n", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            text = "using Avalonia.Media;\n" + text;

            return text;
        }
        catch
        {
            return null;
        }
    }

    public static GdiTranspileResult TryTranspile(string sourceCode)
    {
        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetCompilationUnitRoot();

            var namespaceName = ExtractNamespaceName(root);
            if (namespaceName == null)
            {
                return GdiTranspileResult.Fail("Could not determine the file's namespace.");
            }

            var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToList();
            if (typeDeclarations.Count != 1)
            {
                return GdiTranspileResult.Fail("Expected exactly one type declaration in the file.");
            }

            var typeDecl = typeDeclarations[0];
            if (typeDecl.Members.Any(m => m is not MethodDeclarationSyntax))
            {
                return GdiTranspileResult.Fail("Expected the type to contain only methods (no fields/properties).");
            }

            var methods = typeDecl.Members.OfType<MethodDeclarationSyntax>().ToList();
            if (methods.Count == 0 || methods.Any(m => m.Body == null))
            {
                return GdiTranspileResult.Fail("Expected every method to have a block body.");
            }

            var transpiledMethods = new List<string>();
            foreach (var method in methods)
            {
                var transpiled = TryTranspileMethod(method);
                if (transpiled == null)
                {
                    return GdiTranspileResult.Fail($"Could not translate method \"{method.Identifier.Text}\".");
                }

                transpiledMethods.Add(transpiled);
            }

            var typeModifiers = string.Join(" ", typeDecl.Modifiers.Select(m => m.Text));

            var sb = new StringBuilder();
            sb.AppendLine("using System.Globalization;");
            sb.AppendLine("using Avalonia;");
            sb.AppendLine("using Avalonia.Media;");
            sb.AppendLine("using Avalonia.Media.Imaging;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
            sb.AppendLine($"{typeModifiers} class {typeDecl.Identifier.Text}");
            sb.AppendLine("{");
            foreach (var method in transpiledMethods)
            {
                sb.AppendLine(Indent(method, "    "));
                sb.AppendLine();
            }
            sb.AppendLine("}");

            var finalSource = sb.ToString();
            var errors = CSharpSyntaxTree.ParseText(finalSource).GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();
            if (errors.Count > 0)
            {
                return GdiTranspileResult.Fail(
                    "Transpiled output failed to parse: " + string.Join("; ", errors.Select(d => d.GetMessage())));
            }

            return GdiTranspileResult.Ok(finalSource);
        }
        catch (Exception ex)
        {
            return GdiTranspileResult.Fail("Unexpected error: " + ex.Message);
        }
    }

    private static string? ExtractNamespaceName(CompilationUnitSyntax root)
    {
        var fileScoped = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScoped != null)
        {
            return fileScoped.Name.ToString();
        }

        var blockScoped = root.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        return blockScoped?.Name.ToString();
    }

    private static string Indent(string text, string prefix) =>
        string.Join('\n', text.Split('\n').Select(line => line.Length == 0 ? line : prefix + line));

    /// <summary>
    /// The method's own signature (modifiers, return type, name, parameter list, default
    /// values) is copied verbatim - never rewritten. Only the body is translated.
    /// </summary>
    private static string? TryTranspileMethod(MethodDeclarationSyntax method)
    {
        var body = method.Body!;
        var fullText = method.ToString();
        var bodyOffsetInMethod = body.SpanStart - method.SpanStart;
        var signature = fullText[..bodyOffsetInMethod].TrimEnd();

        var state = new GdiTranspileState();
        var statements = TranspileStatements(body.Statements, state);
        if (statements == null || state.PendingMeasureString != null)
        {
            // A dangling MeasureString with no matching DrawString found would otherwise
            // silently vanish (its deferred declaration emits nothing on its own) - bail
            // instead of dropping a statement with no trace.
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine(signature);
        sb.AppendLine("{");
        foreach (var line in statements)
        {
            sb.AppendLine("    " + line);
        }
        sb.AppendLine("}");
        return sb.ToString().TrimEnd();
    }

    private sealed class GdiTranspileState
    {
        // varName -> (widthExpr, heightExpr)
        public Dictionary<string, (string Width, string Height)> BitmapVars { get; } = [];
        // drawingContextVarName -> bitmapVarName
        public Dictionary<string, string> GraphicsVars { get; } = [];
        // varName -> (familyExpr, sizeExpr, weightExpr, styleExpr)
        public Dictionary<string, (string Family, string Size, string Weight, string Style)> FontVars { get; } = [];

        // A MeasureString call seen but not yet emitted - deferred until the paired DrawString
        // call (expected to be the very next statement) supplies the brush FormattedText needs.
        public (string VarName, InvocationExpressionSyntax Invocation)? PendingMeasureString { get; set; }
    }

    private static List<string>? TranspileStatements(SyntaxList<StatementSyntax> statements, GdiTranspileState state)
    {
        var list = statements.ToList();
        var output = new List<string>();

        for (var i = 0; i < list.Count; i++)
        {
            switch (list[i])
            {
                case LocalDeclarationStatementSyntax localDecl:
                    var declLine = TryTranspileLocalDeclaration(localDecl, state);
                    if (declLine == null)
                    {
                        return null;
                    }
                    if (declLine.Length > 0)
                    {
                        output.Add(declLine);
                    }
                    break;

                case ExpressionStatementSyntax exprStmt:
                    {
                        var consumed = TryTranspileExpressionStatement(exprStmt, state, out var lines);
                        if (!consumed)
                        {
                            return null;
                        }
                        output.AddRange(lines!);
                        break;
                    }

                case IfStatementSyntax { Else: null, Statement: BlockSyntax ifBlock } ifStmt:
                    {
                        var innerLines = TranspileStatements(ifBlock.Statements, state);
                        if (innerLines == null)
                        {
                            return null;
                        }
                        output.Add($"if ({ifStmt.Condition})");
                        output.Add("{");
                        output.AddRange(innerLines.Select(l => "    " + l));
                        output.Add("}");
                        break;
                    }

                case ReturnStatementSyntax returnStmt when returnStmt.Expression != null:
                    output.Add($"return {returnStmt.Expression};");
                    break;

                default:
                    return null;
            }
        }

        return output;
    }

    /// <summary>
    /// Returns null (bail) for an unrecognized declaration shape, "" for a recognized
    /// declaration that intentionally emits nothing yet (a Font - deferred until the paired
    /// MeasureString/DrawString call actually needs it), or the translated declaration text.
    /// </summary>
    private static string? TryTranspileLocalDeclaration(LocalDeclarationStatementSyntax localDecl, GdiTranspileState state)
    {
        var declarator = localDecl.Declaration.Variables.SingleOrDefault();
        if (declarator?.Initializer == null)
        {
            return null;
        }

        var varName = declarator.Identifier.Text;
        var value = declarator.Initializer.Value;

        // var bmp = new Bitmap(w, h);
        if (value is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax { Identifier.Text: "Bitmap" } } bitmapCreation)
        {
            var args = bitmapCreation.ArgumentList?.Arguments;
            if (args is not { Count: 2 })
            {
                return null;
            }

            var width = args.Value[0].Expression.ToString();
            var height = args.Value[1].Expression.ToString();
            state.BitmapVars[varName] = (width, height);
            return $"var {varName} = new RenderTargetBitmap(new PixelSize({width}, {height}));";
        }

        // using var g = Graphics.FromImage(bmp);
        if (value is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Graphics" }, Name.Identifier.Text: "FromImage" },
                ArgumentList.Arguments: { Count: 1 } fromImageArgs
            } &&
            fromImageArgs[0].Expression is IdentifierNameSyntax { Identifier.Text: var bitmapArgName } &&
            state.BitmapVars.ContainsKey(bitmapArgName))
        {
            state.GraphicsVars[varName] = bitmapArgName;
            return $"using var {varName} = {bitmapArgName}.CreateDrawingContext();";
        }

        // using var font = new Font(family, size[, style[, unit]]);
        if (value is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax { Identifier.Text: "Font" } } fontCreation)
        {
            var args = fontCreation.ArgumentList?.Arguments;
            if (args is not { Count: 2 or 4 })
            {
                return null;
            }

            var family = args.Value[0].Expression.ToString();
            var size = args.Value[1].Expression.ToString();
            var weight = "FontWeight.Normal";
            var style = "FontStyle.Normal";
            if (args.Value.Count == 4)
            {
                var styleArgText = args.Value[2].Expression.ToString();
                if (styleArgText.Contains("Bold"))
                {
                    weight = "FontWeight.Bold";
                }
                if (styleArgText.Contains("Italic"))
                {
                    style = "FontStyle.Italic";
                }
            }

            state.FontVars[varName] = (family, size, weight, style);
            return ""; // deferred - nothing emitted until first MeasureString/DrawString use
        }

        // using var brush = new SolidBrush(color); - "using" is dropped regardless of the
        // original: Avalonia.Media.SolidColorBrush does not implement IDisposable (verified
        // against Avalonia.Base.dll 12.0.0 - "using var" here would be a compile error, not
        // just an unnecessary disposal).
        if (value is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax { Identifier.Text: "SolidBrush" }, ArgumentList.Arguments: { Count: 1 } brushArgs })
        {
            var color = TranslateColorExpression(brushArgs[0].Expression);
            if (color == null)
            {
                return null;
            }
            return $"var {varName} = new SolidColorBrush({color});";
        }

        // var pen = new Pen(color, width) [{ DashStyle = ... }]; - "using" dropped for the same
        // reason as SolidBrush above: Avalonia.Media.Pen does not implement IDisposable either.
        if (value is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax { Identifier.Text: "Pen" } } penCreation)
        {
            var args = penCreation.ArgumentList?.Arguments;
            if (args is not { Count: 2 })
            {
                return null;
            }

            var color = TranslateColorExpression(args.Value[0].Expression);
            if (color == null)
            {
                return null;
            }
            var width = args.Value[1].Expression.ToString();

            var hasDashInitializer = penCreation.Initializer?.Expressions
                .OfType<AssignmentExpressionSyntax>()
                .Any(a => a.Left is IdentifierNameSyntax { Identifier.Text: "DashStyle" }) == true;

            var dashArg = hasDashInitializer
                ? ", new DashStyle(new double[] { 2, 2 }, 0) /* best-effort dash pattern approximation - review visually */"
                : "";

            return $"var {varName} = new Pen(new SolidColorBrush({color}), {width}{dashArg});";
        }

        // var textSize = g.MeasureString(text, font); - handled together with its paired
        // DrawString call in TryTranspileExpressionStatement, since building the FormattedText
        // needs the brush from that later statement. Detected here just to bail cleanly if it
        // shows up somewhere TryTranspileExpressionStatement won't look (it only looks one
        // statement ahead), rather than silently mis-transpiling.
        if (value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "MeasureString" } })
        {
            return TranslateMeasureStringDeclaration(varName, (InvocationExpressionSyntax)value, state);
        }

        // Anything else that doesn't reference a GDI+ drawing API at all (e.g. "var text =
        // \"W\";") needs no translation - pass it through verbatim. Only bail (below) for a
        // declaration that DOES touch GDI+ but doesn't match a recognized shape.
        if (!ContainsGdiDrawingApiReference(value))
        {
            return localDecl.ToString().Trim();
        }

        return null;
    }

    private static bool ContainsGdiDrawingApiReference(ExpressionSyntax expr) =>
        expr.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().Any(n => GdiDrawingApiNames.Contains(n.Identifier.Text));

    private static string? TranslateMeasureStringDeclaration(string varName, InvocationExpressionSyntax invocation, GdiTranspileState state)
    {
        // Placeholder - actual emission happens once the paired DrawString call (with its
        // brush argument) is found; see TryTranspileExpressionStatement. Recorded so the
        // following statement can be matched against it.
        state.PendingMeasureString = (varName, invocation);
        return "";
    }

    private static bool TryTranspileExpressionStatement(
        ExpressionStatementSyntax exprStmt, GdiTranspileState state, out List<string>? lines)
    {
        lines = null;

        // g.SmoothingMode = ...; / g.TextRenderingHint = ...; - dropped, no Avalonia equivalent.
        if (exprStmt.Expression is AssignmentExpressionSyntax
            {
                Left: MemberAccessExpressionSyntax { Name.Identifier.Text: "SmoothingMode" or "TextRenderingHint" }
            })
        {
            lines = [];
            return true;
        }

        if (exprStmt.Expression is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: var receiver }, Name.Identifier.Text: var methodName }
            } invocation)
        {
            return false;
        }

        var args = invocation.ArgumentList.Arguments;

        // g.Clear(color);
        if (methodName == "Clear" && state.GraphicsVars.TryGetValue(receiver, out var bitmapForClear) && args.Count == 1)
        {
            var color = TranslateColorExpression(args[0].Expression);
            if (color == null) return false;
            var (w, h) = state.BitmapVars[bitmapForClear];
            lines = [$"{receiver}.FillRectangle(new SolidColorBrush({color}), new Rect(0, 0, {w}, {h}));"];
            return true;
        }

        // g.FillEllipse(brush, x, y, w, h);
        if (methodName == "FillEllipse" && state.GraphicsVars.ContainsKey(receiver) && args.Count == 5)
        {
            var brush = args[0].Expression.ToString();
            var x = args[1].Expression.ToString();
            var y = args[2].Expression.ToString();
            var w = args[3].Expression.ToString();
            var h = args[4].Expression.ToString();
            lines = [$"{receiver}.DrawEllipse({brush}, null, new Rect({x}, {y}, {w}, {h}));"];
            return true;
        }

        // g.DrawRectangle(pen, x, y, w, h);
        if (methodName == "DrawRectangle" && state.GraphicsVars.ContainsKey(receiver) && args.Count == 5)
        {
            var pen = args[0].Expression.ToString();
            var x = args[1].Expression.ToString();
            var y = args[2].Expression.ToString();
            var w = args[3].Expression.ToString();
            var h = args[4].Expression.ToString();
            lines = [$"{receiver}.DrawRectangle(null, {pen}, new Rect({x}, {y}, {w}, {h}));"];
            return true;
        }

        // g.DrawString(text, font, brush, x, y); - if a MeasureString for the same font was
        // just seen (recorded in state.PendingMeasureString), emit both the deferred
        // FormattedText declaration AND the DrawText call together here, since only now do we
        // know the brush.
        if (methodName == "DrawString" && state.GraphicsVars.ContainsKey(receiver) && args.Count == 5)
        {
            var text = args[0].Expression.ToString();
            var fontArg = args[1].Expression;
            var brush = args[2].Expression.ToString();
            var x = args[3].Expression.ToString();
            var y = args[4].Expression.ToString();

            if (fontArg is not IdentifierNameSyntax { Identifier.Text: var fontVarName } ||
                !state.FontVars.TryGetValue(fontVarName, out var font))
            {
                return false;
            }

            var typeface = $"new Typeface({font.Family}, {font.Style}, {font.Weight})";
            var formattedTextExpr = $"new FormattedText({text}, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, {typeface}, {font.Size}, {brush})";

            var result = new List<string>();
            string textSizeVarName;
            if (state.PendingMeasureString is { } pending && MeasureStringMatches(pending.Invocation, text, fontVarName))
            {
                textSizeVarName = pending.VarName;
                result.Add($"var {textSizeVarName} = {formattedTextExpr};");
                state.PendingMeasureString = null;
            }
            else
            {
                // No prior measurement - draw directly without needing a named variable.
                result.Add($"{receiver}.DrawText({formattedTextExpr}, new Point({x}, {y}));");
                lines = result;
                return true;
            }

            result.Add($"{receiver}.DrawText({textSizeVarName}, new Point({x}, {y}));");
            lines = result;
            return true;
        }

        return false;
    }

    private static bool MeasureStringMatches(InvocationExpressionSyntax measureInvocation, string drawText, string drawFontVarName)
    {
        var args = measureInvocation.ArgumentList.Arguments;
        if (args.Count != 2) return false;
        var measureText = args[0].Expression.ToString();
        var measureFontVarName = (args[1].Expression as IdentifierNameSyntax)?.Identifier.Text;
        return measureText == drawText && measureFontVarName == drawFontVarName;
    }

    /// <summary>
    /// Translates a System.Drawing.Color-shaped expression: a named color (Color.White ->
    /// Colors.White), Color.FromArgb with 3 args (WinForms' implicit-alpha-255 RGB overload ->
    /// Avalonia's differently-named FromRgb) or 4 args (-> Avalonia's FromArgb, same arg order,
    /// with explicit byte casts since Avalonia's overload takes byte not int), or a bare
    /// identifier/parameter already of type Color (passed through unchanged - "Color" resolves
    /// to the Avalonia type once the emitted usings replace System.Drawing). Returns null for
    /// anything else (bail).
    /// </summary>
    private static string? TranslateColorExpression(ExpressionSyntax expr)
    {
        switch (expr)
        {
            case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Color" }, Name.Identifier.Text: var namedColor }:
                return $"Colors.{namedColor}";

            case InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Color" }, Name.Identifier.Text: "FromArgb" },
                    ArgumentList.Arguments: { Count: 3 } rgbArgs
                }:
                return $"Color.FromRgb((byte)({rgbArgs[0].Expression}), (byte)({rgbArgs[1].Expression}), (byte)({rgbArgs[2].Expression}))";

            case InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Color" }, Name.Identifier.Text: "FromArgb" },
                    ArgumentList.Arguments: { Count: 4 } argbArgs
                }:
                return $"Color.FromArgb((byte)({argbArgs[0].Expression}), (byte)({argbArgs[1].Expression}), (byte)({argbArgs[2].Expression}), (byte)({argbArgs[3].Expression}))";

            case IdentifierNameSyntax identifier:
                // Already a Color-typed local/parameter (e.g. a "Color color" parameter, or a
                // pattern-matched "bc") - passes through unchanged.
                return identifier.Identifier.Text;

            default:
                return null;
        }
    }
}
