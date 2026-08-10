using Converter.Core.Configuration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// A non-Form .cs file the source WinForms project owns directly (as opposed to a referenced
/// sibling project - see ProjectReferenceResolver) that SupportFileScanner determined is safe
/// to copy verbatim into the generated Avalonia project - its own namespace is left untouched,
/// so any "using" a ViewModel/code-behind migrated from the original project's code-behind
/// (via CodeBehindMemberExtractor) still resolves against it without any rewriting.
/// </summary>
public sealed record CopyableSourceFile(string AbsolutePath, string RelativePath);

/// <summary>
/// A .cs file SupportFileScanner chose not to copy, and why.
/// </summary>
public sealed record SkippedSourceFile(string RelativePath, string Reason);

public sealed class SupportFileScanResult
{
    public IReadOnlyList<CopyableSourceFile> CopyableFiles { get; init; } = [];
    public IReadOnlyList<SkippedSourceFile> SkippedFiles { get; init; } = [];

    public static readonly SupportFileScanResult Empty = new();
}

/// <summary>
/// Discovers the source WinForms project's own non-Form C# files - typically a "Common"/
/// "Controls"/"Helpers" folder of utility classes and custom controls sitting alongside
/// Forms/ - that the rest of the pipeline (which only ever touches *.Designer.cs and their
/// sibling code-behind) never looks at, and therefore never migrates. Splits them into two
/// buckets by a syntax-only heuristic (no semantic model, consistent with the rest of this
/// codebase): a file whose top-level type declares no direct WinForms UI base type (Form/
/// Control/UserControl/Component/ContainerControl) is almost certainly a plain utility class
/// with no WinForms dependency, so it's copied byte-for-byte; a file that does declare one is
/// a custom Form/Control that needs a real Avalonia port (different rendering model entirely),
/// so it's left alone and surfaced as a manual step instead of copied over broken.
///
/// Known limitation: the base-type check only looks at the *direct* base type name written in
/// the source, not a full inheritance chain - a class deriving from another project-local
/// custom control (rather than directly from Control/UserControl) would be misclassified as
/// copyable. Not resolved here; would need a semantic model this codebase doesn't build.
/// </summary>
public static class SupportFileScanner
{
    private static readonly HashSet<string> WinFormsUiBaseTypeNames = new(StringComparer.Ordinal)
    {
        "Form", "Control", "UserControl", "Component", "ContainerControl"
    };

    /// <summary>
    /// Scans every *.cs file under <paramref name="sourcePath"/> except ones in
    /// <paramref name="handledFilePaths"/> (already-migrated *.Designer.cs files and their
    /// sibling code-behind), files matching <paramref name="excludePatterns"/>, and
    /// Program.cs (regenerated separately with an Avalonia entry point). Always skips bin/obj
    /// regardless of exclude patterns - a built project routinely has generated .cs files
    /// there (AssemblyInfo.cs, GlobalUsings.g.cs) that were never source the user wrote.
    /// </summary>
    public static async Task<SupportFileScanResult> ScanAsync(
        string sourcePath, IReadOnlySet<string> handledFilePaths, IReadOnlyList<string> excludePatterns)
    {
        var copyable = new List<CopyableSourceFile>();
        var skipped = new List<SkippedSourceFile>();

        string[] allCsFiles;
        try
        {
            allCsFiles = Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories);
        }
        catch
        {
            return SupportFileScanResult.Empty;
        }

        foreach (var file in allCsFiles.OrderBy(f => f, StringComparer.Ordinal))
        {
            if (handledFilePaths.Contains(file) ||
                IsInBuildOutputDirectory(sourcePath, file) ||
                ExcludePatternMatcher.IsExcluded(file, excludePatterns) ||
                Path.GetFileName(file).Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourcePath, file);

            try
            {
                var sourceCode = await File.ReadAllTextAsync(file);
                var root = await CSharpSyntaxTree.ParseText(sourceCode).GetRootAsync();

                var unsafeBaseType = root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .SelectMany(t => t.BaseList?.Types.Select(b => SimpleBaseTypeName(b.Type)) ?? [])
                    .FirstOrDefault(name => name != null && WinFormsUiBaseTypeNames.Contains(name));

                if (unsafeBaseType != null)
                {
                    skipped.Add(new SkippedSourceFile(relativePath,
                        $"Declares a type deriving from WinForms '{unsafeBaseType}' - needs a manual Avalonia " +
                        "port (a different rendering/control model entirely), not a plain file copy."));
                    continue;
                }

                copyable.Add(new CopyableSourceFile(file, relativePath));
            }
            catch
            {
                skipped.Add(new SkippedSourceFile(relativePath, "Could not be parsed - copy/port manually."));
            }
        }

        return new SupportFileScanResult { CopyableFiles = copyable, SkippedFiles = skipped };
    }

    private static bool IsInBuildOutputDirectory(string sourcePath, string filePath)
    {
        var relative = Path.GetRelativePath(sourcePath, filePath).Replace('\\', '/');
        return relative.Split('/').Any(segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string? SimpleBaseTypeName(TypeSyntax type)
    {
        return type switch
        {
            SimpleNameSyntax simple => simple.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            _ => null
        };
    }
}
