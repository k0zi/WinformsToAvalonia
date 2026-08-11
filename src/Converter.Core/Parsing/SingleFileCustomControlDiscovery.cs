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

    private static bool IsInBuildOutputDirectory(string sourcePath, string filePath)
    {
        var relative = Path.GetRelativePath(sourcePath, filePath).Replace('\\', '/');
        return relative.Split('/').Any(segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }
}
