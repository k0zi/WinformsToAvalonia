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
