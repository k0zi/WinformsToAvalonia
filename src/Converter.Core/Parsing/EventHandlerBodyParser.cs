using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// Extracts WinForms event-handler method source (verbatim, unreformatted) from the sibling
/// non-designer .cs file (resolved via SiblingFileResolver.ResolveCodeBehind). Deliberately
/// separate from WinFormsParser: a code-behind file is arbitrary, unconstrained user code
/// (unlike InitializeComponent's narrow, machine-generated shape), so this parser only looks
/// for named methods and is best-effort throughout - an unparseable file or a missing method
/// simply yields no entry for it, never a hard failure of the whole conversion. The extracted
/// text is never meant to be emitted as live/compiled code; callers embed it as a comment
/// block inside a compiling stub.
/// </summary>
public static class EventHandlerBodyParser
{
    /// <summary>
    /// Extracts the full source text (signature + body, exactly as written) of every method
    /// in <paramref name="codeBehindFilePath"/> whose name appears in
    /// <paramref name="methodNames"/>. Returns an empty dictionary - never throws - if the
    /// file is missing, unreadable, or fails to parse.
    /// </summary>
    public static async Task<Dictionary<string, string>> ExtractAsync(
        string codeBehindFilePath, IReadOnlySet<string> methodNames)
    {
        var result = new Dictionary<string, string>();
        if (methodNames.Count == 0)
        {
            return result;
        }

        try
        {
            var sourceCode = await File.ReadAllTextAsync(codeBehindFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = await syntaxTree.GetRootAsync();

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var name = method.Identifier.Text;
                if (!methodNames.Contains(name) || result.ContainsKey(name))
                {
                    continue;
                }

                result[name] = method.ToString().Trim();
            }
        }
        catch
        {
            // Best-effort: an unparseable/unreadable sibling file means no bodies get
            // extracted, not a failed conversion.
        }

        return result;
    }

    /// <summary>
    /// Re-parses a full method-source string previously produced by ExtractAsync (signature +
    /// body, exactly as written) and returns just its statement block ("{ ... }"), suitable for
    /// pasting directly into a differently-signed generated stub as live code. An expression-
    /// bodied method ("=> Foo();") is rewritten into an equivalent block. Best-effort: if
    /// re-parsing somehow fails (shouldn't normally happen, since this text was itself produced
    /// by a prior successful parse), falls back to wrapping the original text as an inert
    /// `//`-commented block instead of throwing or emitting nothing.
    /// </summary>
    public static string ExtractBodyText(string fullMethodSource)
    {
        try
        {
            var wrapper = $"class __Wrapper {{ {fullMethodSource} }}";
            var method = CSharpSyntaxTree.ParseText(wrapper).GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (method?.Body != null)
            {
                return method.Body.ToString();
            }

            if (method?.ExpressionBody != null)
            {
                return "{\n    " + method.ExpressionBody.Expression + ";\n}";
            }
        }
        catch
        {
            // Fall through to the comment-wrap fallback below.
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("    // Original body could not be re-parsed as live code - preserved for reference:");
        foreach (var line in fullMethodSource.Replace("\r\n", "\n").Split('\n'))
        {
            sb.AppendLine(string.IsNullOrWhiteSpace(line) ? "    //" : $"    // {line}");
        }
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Detects "async" in a full method-source string's signature line (everything before the
    /// first "{"). A freshly-constructed generated stub (a different signature than the
    /// original - different name, different parameter list, whatever) has no way to know the
    /// original was "async" otherwise; ExtractBodyText only returns the body, and a body
    /// containing "await" without an "async" modifier on the enclosing method doesn't compile.
    /// WinForms event handlers are commonly declared "async void" (fire-and-forget), so this
    /// matters in practice, not just in theory.
    /// </summary>
    public static bool IsAsyncMethodSignature(string fullMethodSource)
    {
        var signaturePart = fullMethodSource.Split('{', 2)[0];
        return Regex.IsMatch(signaturePart, @"\basync\b");
    }

    /// <summary>
    /// Adds the "async" modifier to a full method-source string if it isn't already present -
    /// used when a rewrite (e.g. MessageBoxTranspiler turning "MessageBox.Show(...)" into
    /// "await Dialogs.ShowAsync(...)") introduces an "await" into a method whose original
    /// signature wasn't async. Unlike IsAsyncMethodSignature's text-based check, this needs to
    /// actually edit the signature, so it re-parses the method (mirroring ExtractBodyText's own
    /// wrap-and-parse approach) and inserts the modifier via the syntax tree rather than a
    /// regex, since the accessibility/other modifiers already on the text can vary (this
    /// generator's own EnsureInternalAccessibility rewrite, an original "protected", etc.).
    /// Best-effort: any parse failure returns the input unchanged rather than throwing.
    /// </summary>
    public static string EnsureAsyncModifier(string fullMethodSource)
    {
        try
        {
            var wrapper = $"class __Wrapper {{ {fullMethodSource} }}";
            var method = CSharpSyntaxTree.ParseText(wrapper).GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (method == null || method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
            {
                return fullMethodSource;
            }

            var asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);
            var newMethod = method.WithModifiers(method.Modifiers.Add(asyncToken));
            return newMethod.ToFullString().Trim();
        }
        catch
        {
            return fullMethodSource;
        }
    }
}
