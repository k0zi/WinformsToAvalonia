using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// WinForms designer files always come as a `Foo.Designer.cs` / `Foo.cs` / `Foo.resx` trio
/// sharing the same base name and directory. This resolves the other two from the
/// `.Designer.cs` path the orchestrator already discovers.
/// </summary>
public static class SiblingFileResolver
{
    private const string DesignerSuffix = ".Designer.cs";

    /// <summary>
    /// Resolves the sibling `.resx` file, or null if the path isn't a `.Designer.cs` file or
    /// no matching `.resx` exists on disk.
    /// </summary>
    public static string? ResolveResx(string designerFilePath) => ResolveSibling(designerFilePath, ".resx");

    /// <summary>
    /// Resolves the sibling non-designer `.cs` file (e.g. "Foo.cs" for "Foo.Designer.cs"),
    /// or null if the path isn't a `.Designer.cs` file or no matching file exists on disk.
    /// </summary>
    public static string? ResolveCodeBehind(string designerFilePath) => ResolveSibling(designerFilePath, ".cs");

    /// <summary>
    /// Resolves the real root base type (e.g. "Form", "UserControl") for a `.Designer.cs` file
    /// by reading it off the sibling non-designer `.cs` file's matching partial class
    /// declaration - real Visual Studio designer output never redeclares the base type on the
    /// `.Designer.cs` partial itself (it's declared once, on the other partial), so
    /// WinFormsParser's own BaseList lookup on the designer file almost never finds anything.
    /// Best-effort like EventHandlerBodyParser: a missing sibling, unparseable file, or no
    /// matching class declaration all simply yield null rather than a hard failure. A
    /// fully-qualified base type (e.g. "System.Windows.Forms.UserControl") is normalized to its
    /// short name ("UserControl") to match ControlMappingRegistry's keys.
    /// </summary>
    public static async Task<string?> ResolveRootBaseTypeAsync(string designerFilePath)
    {
        var fileName = Path.GetFileName(designerFilePath);
        if (!fileName.EndsWith(DesignerSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var className = fileName[..^DesignerSuffix.Length];
        var codeBehindPath = ResolveSibling(designerFilePath, ".cs");
        if (codeBehindPath == null)
        {
            return null;
        }

        try
        {
            var sourceCode = await File.ReadAllTextAsync(codeBehindPath);
            var root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();

            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == className && c.BaseList != null);

            var baseType = classDeclaration?.BaseList?.Types.FirstOrDefault()?.Type.ToString();
            return baseType?.Split('.').Last();
        }
        catch
        {
            // Best-effort: an unparseable/unreadable sibling file means no override, not a
            // failed conversion.
            return null;
        }
    }

    private static string? ResolveSibling(string designerFilePath, string extension)
    {
        var fileName = Path.GetFileName(designerFilePath);
        if (!fileName.EndsWith(DesignerSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var baseName = fileName[..^DesignerSuffix.Length];
        var directory = Path.GetDirectoryName(designerFilePath);
        var siblingPath = directory != null
            ? Path.Combine(directory, baseName + extension)
            : baseName + extension;

        return File.Exists(siblingPath) ? siblingPath : null;
    }
}
