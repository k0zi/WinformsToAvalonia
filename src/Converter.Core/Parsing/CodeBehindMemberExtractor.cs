using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// A single field declaration extracted verbatim from a code-behind file, e.g.
/// "private int _counter = 0;" - <paramref name="Names"/> holds every variable name declared
/// by it (a single FieldDeclarationSyntax can declare more than one, e.g. "int a, b;").
/// </summary>
public record CodeBehindField(IReadOnlyList<string> Names, string DeclarationText);

/// <summary>
/// Non-handler members discovered in a code-behind file by CodeBehindMemberExtractor.
/// </summary>
public class CodeBehindMembers(
    IReadOnlyList<CodeBehindField>? fields = null,
    IReadOnlyDictionary<string, string>? helperMethods = null,
    IReadOnlyList<string>? skippedOverrideMethodNames = null,
    IReadOnlyList<string>? usingDirectives = null)
{
    public IReadOnlyList<CodeBehindField> Fields { get; } = fields ?? [];
    public IReadOnlyDictionary<string, string> HelperMethods { get; } = helperMethods ?? new Dictionary<string, string>();
    public IReadOnlyList<string> SkippedOverrideMethodNames { get; } = skippedOverrideMethodNames ?? [];

    /// <summary>
    /// Namespaces the sibling code-behind file itself imports (e.g. "WarehouseApp.Data.Models"),
    /// in source order, deduplicated. Migrated fields/methods/handler bodies commonly reference
    /// types from these - without them, generated code referencing the app's own domain types
    /// fails to compile with "type or namespace not found" even though the type itself is fine.
    /// </summary>
    public IReadOnlyList<string> UsingDirectives { get; } = usingDirectives ?? [];

    public static readonly CodeBehindMembers Empty = new();
}

/// <summary>
/// Extracts private fields, non-handler helper methods (verbatim, unreformatted), and the
/// file's own "using" directives from the sibling non-designer .cs file (resolved via
/// SiblingFileResolver.ResolveCodeBehind) - alongside EventHandlerBodyParser, which owns the
/// named event-handler methods themselves. Same best-effort philosophy: a code-behind file is
/// arbitrary user code, so this is syntax-only and never throws - an unparseable/missing file
/// simply yields CodeBehindMembers.Empty, never a hard failure of the whole conversion.
/// </summary>
public static class CodeBehindMemberExtractor
{
    /// <summary>
    /// Extracts every field declaration and every non-handler, non-override method from
    /// <paramref name="codeBehindFilePath"/>. Methods whose name appears in
    /// <paramref name="handlerMethodNames"/> are skipped (EventHandlerBodyParser already owns
    /// those); methods marked "override" (e.g. OnClosing/OnLoad lifecycle hooks) are skipped
    /// from HelperMethods and reported in SkippedOverrideMethodNames instead, since they're
    /// typically Form/Control-lifecycle-tied and have no clean ViewModel equivalent.
    /// </summary>
    public static async Task<CodeBehindMembers> ExtractAsync(
        string codeBehindFilePath, IReadOnlySet<string> handlerMethodNames)
    {
        var fields = new List<CodeBehindField>();
        var helperMethods = new Dictionary<string, string>();
        var skippedOverrides = new List<string>();
        var usingDirectives = new List<string>();

        try
        {
            var sourceCode = await File.ReadAllTextAsync(codeBehindFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = await syntaxTree.GetRootAsync();

            // Usings can sit at file scope (CompilationUnitSyntax.Usings) or inside a
            // block/file-scoped namespace (BaseNamespaceDeclarationSyntax.Usings) - collecting
            // both covers every WinForms designer's common shape.
            var usingNames = root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                .OfType<UsingDirectiveSyntax>()
                .Where(u => u.Alias == null && !u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && u.Name != null)
                .Select(u => u.Name!.ToString());
            foreach (var name in usingNames)
            {
                if (!usingDirectives.Contains(name))
                {
                    usingDirectives.Add(name);
                }
            }

            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                var names = field.Declaration.Variables.Select(v => v.Identifier.Text).ToList();
                fields.Add(new CodeBehindField(names, field.ToString().Trim()));
            }

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var name = method.Identifier.Text;
                if (handlerMethodNames.Contains(name))
                {
                    continue;
                }

                if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
                {
                    skippedOverrides.Add(name);
                    continue;
                }

                if (!helperMethods.ContainsKey(name))
                {
                    helperMethods[name] = method.ToString().Trim();
                }
            }
        }
        catch
        {
            // Best-effort: an unparseable/unreadable sibling file means nothing gets
            // extracted, not a failed conversion.
        }

        return new CodeBehindMembers(fields, helperMethods, skippedOverrides, usingDirectives);
    }
}
