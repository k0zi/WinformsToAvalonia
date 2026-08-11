using Converter.Core.Configuration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// Finds .cs files (other than *.Designer.cs) that are themselves a complete, self-contained
/// custom control - a class directly deriving from Control/UserControl with its own
/// InitializeComponent() method - i.e. one that was never split into a Foo.Designer.cs/Foo.cs
/// pair the way a Form/composite UserControl following the Visual Studio designer convention
/// would be (e.g. WarehouseApp.Controls.AutocompleteSearchBox: a single .cs file whose
/// constructor calls a private InitializeComponent() that builds its own child controls via
/// Controls.Add). A file found here flows through the exact same
/// WinFormsParser.ParseDesignerFileAsync path a genuine .Designer.cs file does - that parser
/// only requires a class declaration + an InitializeComponent method, nothing
/// Designer.cs-specific. A pure owner-drawn control (an OnPaint override, no
/// InitializeComponent/child controls at all - there is no control tree to convert into AXAML)
/// never matches this and is left to SupportFileScanner's existing "needs manual port" handling.
/// </summary>
public static class SingleFileCustomControlDiscovery
{
    private static readonly HashSet<string> CustomControlBaseTypeNames = new(StringComparer.Ordinal)
    {
        "UserControl", "Control"
    };

    public static async Task<List<string>> DiscoverAsync(
        string sourcePath, IReadOnlySet<string> excludedFilePaths, IReadOnlyList<string> excludePatterns)
    {
        var discovered = new List<string>();

        string[] allCsFiles;
        try
        {
            allCsFiles = Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories);
        }
        catch
        {
            return discovered;
        }

        foreach (var file in allCsFiles.OrderBy(f => f, StringComparer.Ordinal))
        {
            if (excludedFilePaths.Contains(file) ||
                file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(file).Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ||
                ExcludePatternMatcher.IsExcluded(file, excludePatterns) ||
                IsInBuildOutputDirectory(sourcePath, file))
            {
                continue;
            }

            try
            {
                var sourceCode = await File.ReadAllTextAsync(file);
                var root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();

                var hasInitializeComponent = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .Any(m => m.Identifier.Text == "InitializeComponent");
                if (!hasInitializeComponent)
                {
                    continue;
                }

                var derivesFromCustomControlBase = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Any(c => c.BaseList?.Types.Any(b =>
                        SupportFileScanner.SimpleBaseTypeName(b.Type) is { } baseName &&
                        CustomControlBaseTypeNames.Contains(baseName)) == true);

                if (derivesFromCustomControlBase)
                {
                    discovered.Add(file);
                }
            }
            catch
            {
                // Best-effort: an unparseable/unreadable file simply isn't discovered as a
                // custom control, not a failed conversion.
            }
        }

        return discovered;
    }

    /// <summary>
    /// Finds every class deriving from Control/UserControl that overrides OnPaint but has no
    /// InitializeComponent - the inverse of DiscoverAsync's own check, i.e. the owner-drawn
    /// case that has no control tree to convert into AXAML at all (see
    /// SupportFileScanner.ScanAsync's identical detection for the file-level skip). Used by
    /// ConversionOrchestrator so an embedded *instance* of one of these controls gets the same
    /// specific "Custom-drawn control..." message in its "Unmapped Controls" manual step that
    /// the file-level skip already gives, instead of a generic "has no Avalonia mapping".
    /// </summary>
    public static async Task<HashSet<string>> DiscoverOwnerDrawnControlClassNamesAsync(
        string sourcePath, IReadOnlyList<string> excludePatterns)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        string[] allCsFiles;
        try
        {
            allCsFiles = Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories);
        }
        catch
        {
            return names;
        }

        foreach (var file in allCsFiles)
        {
            if (Path.GetFileName(file).Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ||
                ExcludePatternMatcher.IsExcluded(file, excludePatterns) ||
                IsInBuildOutputDirectory(sourcePath, file))
            {
                continue;
            }

            try
            {
                var sourceCode = await File.ReadAllTextAsync(file);
                var root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();

                foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var derivesFromCustomControlBase = classDeclaration.BaseList?.Types.Any(b =>
                        SupportFileScanner.SimpleBaseTypeName(b.Type) is { } baseName &&
                        CustomControlBaseTypeNames.Contains(baseName)) == true;
                    if (!derivesFromCustomControlBase)
                    {
                        continue;
                    }

                    var hasOnPaint = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
                        .Any(m => m.Identifier.Text == "OnPaint");
                    var hasInitializeComponent = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
                        .Any(m => m.Identifier.Text == "InitializeComponent");

                    if (hasOnPaint && !hasInitializeComponent)
                    {
                        names.Add(classDeclaration.Identifier.Text);
                    }
                }
            }
            catch
            {
                // Best-effort: an unparseable/unreadable file simply contributes no names.
            }
        }

        return names;
    }

    private static bool IsInBuildOutputDirectory(string sourcePath, string filePath)
    {
        var relative = Path.GetRelativePath(sourcePath, filePath).Replace('\\', '/');
        return relative.Split('/').Any(segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }
}
