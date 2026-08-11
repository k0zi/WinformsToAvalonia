using Converter.Core.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// A non-Form .cs file the source WinForms project owns directly (as opposed to a referenced
/// sibling project - see ProjectReferenceResolver) that SupportFileScanner determined is safe
/// to copy into the generated Avalonia project - its own namespace is left untouched, so any
/// "using" a ViewModel/code-behind migrated from the original project's code-behind (via
/// CodeBehindMemberExtractor) still resolves against it without any rewriting.
/// <paramref name="TransformedContent"/> is null for a byte-for-byte verbatim copy (the common
/// case - no WinForms/GDI+ references at all); non-null when GdiDrawingTranspiler or the
/// Color-only rewrite path produced translated Avalonia code that should be written instead of
/// the original file's bytes.
/// </summary>
public sealed record CopyableSourceFile(string AbsolutePath, string RelativePath, string? TransformedContent = null);

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
/// sibling code-behind) never looks at, and therefore never migrates. Classifies each file,
/// syntax-only (no semantic model, consistent with the rest of this codebase):
///
/// 1. A type deriving from a WinForms UI base type (Form/Control/UserControl/Component/
///    ContainerControl) - needs a real Avalonia port (different rendering/control model
///    entirely). Not copied; surfaced as a manual step.
/// 2. No such base type, but the file references a WinForms/System.Drawing type with no
///    Avalonia equivalent at all (WinFormsTypeUsageDetector - Form/Label/TextBox/TreeNode/
///    DataGridView/... used *without* the file itself deriving from anything, e.g. a static
///    helper that builds a dialog imperatively). Also not copied; surfaced as a manual step
///    naming the specific type(s) found - this used to be the silent gap where such a file
///    slipped past check #1 and got copied verbatim, producing build errors with zero warning.
/// 3. No such base type, but the file uses GDI+ drawing APIs (Graphics/Bitmap/Font/SolidBrush/
///    Pen) - handed to GdiDrawingTranspiler. Success: copied with the translated Avalonia
///    RenderTargetBitmap/DrawingContext code. Failure (a call outside the transpiler's
///    recognized vocabulary): not copied, surfaced as a manual step instead of copied broken.
/// 4. No such base type, but the file references bare System.Drawing.Color (no drawing APIs) -
///    the cheaper GdiDrawingTranspiler.TryRewriteColorOnly rename-only path. Copied with the
///    translated content.
/// 5. None of the above - a plain utility class with no WinForms dependency at all. Copied
///    byte-for-byte, unchanged.
///
/// Known limitation: the base-type check in bucket 1 only looks at the *direct* base type name
/// written in the source, not a full inheritance chain - a class deriving from another
/// project-local custom control (rather than directly from Control/UserControl) would be
/// misclassified as copyable. Not resolved here; would need a semantic model this codebase
/// doesn't build.
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

                var splitResult = TrySplitMixedSafetyFile(root, sourceCode);
                if (splitResult != null)
                {
                    copyable.Add(new CopyableSourceFile(file, relativePath, splitResult.Value.TransformedContent));
                    skipped.Add(new SkippedSourceFile(relativePath, splitResult.Value.SkipReason));
                    continue;
                }

                var unsafeBaseType = root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .SelectMany(t => t.BaseList?.Types.Select(b => SimpleBaseTypeName(b.Type)) ?? [])
                    .FirstOrDefault(name => name != null && WinFormsUiBaseTypeNames.Contains(name));

                if (unsafeBaseType != null)
                {
                    // A composite custom control (its own InitializeComponent + child
                    // Controls.Add, just never split into a Foo.Designer.cs/Foo.cs pair) is
                    // handled separately by SingleFileCustomControlDiscovery before this scanner
                    // ever runs (see handledFilePaths) - reaching here with no
                    // InitializeComponent method means there's no control tree to convert at
                    // all, most commonly because it's owner-drawn (an OnPaint override doing
                    // its own GDI+ rendering) - worth a more specific message than the generic
                    // "needs a manual port" for that common case.
                    var isLikelyOwnerDrawn = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                            .Any(m => m.Identifier.Text == "OnPaint") &&
                        !root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                            .Any(m => m.Identifier.Text == "InitializeComponent");

                    skipped.Add(new SkippedSourceFile(relativePath, isLikelyOwnerDrawn
                        ? $"Custom-drawn control (derives from WinForms '{unsafeBaseType}', overrides OnPaint, " +
                          "no InitializeComponent/child controls) - there is no control tree to convert into " +
                          "AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a " +
                          "Control subclass overriding Render(DrawingContext))."
                        : $"Declares a type deriving from WinForms '{unsafeBaseType}' - needs a manual Avalonia " +
                          "port (a different rendering/control model entirely), not a plain file copy."));
                    continue;
                }

                var noEquivalentTypes = WinFormsTypeUsageDetector.FindReferencedTypeNames(root);
                if (noEquivalentTypes.Count > 0)
                {
                    skipped.Add(new SkippedSourceFile(relativePath,
                        $"Uses WinForms type(s) with no Avalonia equivalent ({string.Join(", ", noEquivalentTypes)}) - " +
                        "needs a manual Avalonia port, not a plain file copy."));
                    continue;
                }

                if (GdiDrawingTranspiler.HasGdiDrawingApiUsage(root))
                {
                    var transpiled = GdiDrawingTranspiler.TryTranspile(sourceCode);
                    if (!transpiled.Success)
                    {
                        skipped.Add(new SkippedSourceFile(relativePath,
                            $"Uses GDI+ drawing APIs that could not be automatically translated to Avalonia " +
                            $"({transpiled.FailureReason}) - needs a manual port, not a plain file copy."));
                        continue;
                    }

                    copyable.Add(new CopyableSourceFile(file, relativePath, transpiled.TransformedSource));
                    continue;
                }

                var colorOnlyRewrite = GdiDrawingTranspiler.TryRewriteColorOnly(sourceCode);
                copyable.Add(new CopyableSourceFile(file, relativePath, colorOnlyRewrite));
            }
            catch
            {
                skipped.Add(new SkippedSourceFile(relativePath, "Could not be parsed - copy/port manually."));
            }
        }

        return new SupportFileScanResult { CopyableFiles = copyable, SkippedFiles = skipped };
    }

    /// <summary>
    /// A file with two or more top-level (namespace-scoped, not nested) type declarations where
    /// some are unsafe (bucket 1/2 below) and some aren't is a real, observed shape - e.g.
    /// WarehouseApp.Controls.StatusBadgeControl.cs declares both a harmless "enum BadgeStyle"
    /// and an owner-drawn "class StatusBadgeControl : Control" in the same file. The whole-file
    /// classification below would drop the safe enum along with the unsafe control, even though
    /// other migrated code depends on it and it has zero WinForms dependency of its own. Instead,
    /// copy a version of the file with only the unsafe declaration(s) removed (via
    /// SyntaxNode.RemoveNodes, so usings/namespace/other members are preserved verbatim) and
    /// report the removal as a manual step - the whole-file skip/copy path below still handles
    /// every other case (0 or 1 type declarations, or a file where every/no declaration is
    /// unsafe) exactly as before. Returns null when there's nothing to split (falls through to
    /// the existing whole-file logic).
    /// </summary>
    private static (string TransformedContent, string SkipReason)? TrySplitMixedSafetyFile(
        SyntaxNode root, string sourceCode)
    {
        var typeDeclarations = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
            .Where(t => t.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax)
            .ToList();

        if (typeDeclarations.Count < 2)
        {
            return null;
        }

        var unsafeDeclarations = new List<(BaseTypeDeclarationSyntax Declaration, string Reason)>();
        foreach (var declaration in typeDeclarations)
        {
            var declaredBaseType = (declaration as TypeDeclarationSyntax)?.BaseList?.Types
                .Select(b => SimpleBaseTypeName(b.Type))
                .FirstOrDefault(name => name != null && WinFormsUiBaseTypeNames.Contains(name));
            if (declaredBaseType != null)
            {
                unsafeDeclarations.Add((declaration, $"derives from WinForms '{declaredBaseType}'"));
                continue;
            }

            var noEquivalentTypes = WinFormsTypeUsageDetector.FindReferencedTypeNames(declaration);
            if (noEquivalentTypes.Count > 0)
            {
                unsafeDeclarations.Add((declaration,
                    $"uses WinForms type(s) with no Avalonia equivalent ({string.Join(", ", noEquivalentTypes)})"));
            }
        }

        if (unsafeDeclarations.Count == 0 || unsafeDeclarations.Count == typeDeclarations.Count)
        {
            // All safe or all unsafe - the existing whole-file logic already handles both
            // correctly (copies everything / skips everything with the same reasoning).
            return null;
        }

        var reducedRoot = root.RemoveNodes(unsafeDeclarations.Select(u => u.Declaration), SyntaxRemoveOptions.KeepNoTrivia);
        var reducedSource = reducedRoot?.ToFullString() ?? sourceCode;

        // The surviving safe declaration(s) may themselves reference bare System.Drawing.Color
        // (e.g. a "ChartSeries" DTO next to an unsafe owner-drawn "ChartControl" in the same
        // file) - the whole-file bucket-4 path below already handles this via the same
        // rename-only rewrite; apply it here too instead of leaving an unqualified "Color" that
        // doesn't resolve in the generated project.
        var transformedContent = GdiDrawingTranspiler.TryRewriteColorOnly(reducedSource) ?? reducedSource;

        var droppedNames = unsafeDeclarations.Select(u => u.Declaration.Identifier.Text).ToList();
        var skipReason = $"Partially copied - {string.Join(", ", droppedNames)} " +
            $"{(droppedNames.Count == 1 ? "was" : "were")} removed " +
            $"({string.Join("; ", unsafeDeclarations.Select(u => u.Reason))}) and need a manual Avalonia port; " +
            "every other type declared in this file was copied unchanged.";

        return (transformedContent, skipReason);
    }

    private static bool IsInBuildOutputDirectory(string sourcePath, string filePath)
    {
        var relative = Path.GetRelativePath(sourcePath, filePath).Replace('\\', '/');
        return relative.Split('/').Any(segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extracts a base-type reference's simple name (e.g. "UserControl" from both "UserControl"
    /// and "System.Windows.Forms.UserControl"). Public so SingleFileCustomControlDiscovery can
    /// reuse the exact same base-type check this scanner's own bucket-1 classification uses.
    /// </summary>
    public static string? SimpleBaseTypeName(TypeSyntax type)
    {
        return type switch
        {
            SimpleNameSyntax simple => simple.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            _ => null
        };
    }
}
