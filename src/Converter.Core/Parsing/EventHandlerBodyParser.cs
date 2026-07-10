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
}
