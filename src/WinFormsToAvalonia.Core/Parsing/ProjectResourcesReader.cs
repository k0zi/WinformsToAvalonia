using System.Xml.Linq;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>What one entry of <c>Properties/Resources.resx</c> holds.</summary>
public enum ProjectResourceKind
{
    /// <summary>A string, stored inline - <see cref="ProjectResourceEntry.Text"/>.</summary>
    Text,

    /// <summary>A file beside the project (<c>ResXFileRef</c>) - <see cref="ProjectResourceEntry.FilePath"/>.</summary>
    File,

    /// <summary>A base64 payload stored in the .resx itself - <see cref="ProjectResourceEntry.Base64"/>.</summary>
    Embedded,
}

/// <param name="TypeSimpleName">The CLR type the generated property returns - "Bitmap", "Icon", "String", ...</param>
/// <param name="FilePath">For <see cref="ProjectResourceKind.File"/>: the absolute path the reference resolves to.</param>
public sealed record ProjectResourceEntry(
    string Name,
    ProjectResourceKind Kind,
    string? TypeSimpleName,
    string? Text = null,
    string? FilePath = null,
    string? Base64 = null)
{
    public bool IsImage => TypeSimpleName is "Bitmap" or "Image" or "Icon";
}

/// <summary>The entries of the project's strongly-typed resource file, by name.</summary>
public sealed class ProjectResources(string? filePath, IReadOnlyDictionary<string, ProjectResourceEntry> entries)
{
    public static ProjectResources None { get; } = new(null, new Dictionary<string, ProjectResourceEntry>());

    /// <summary>The .resx these came from, or null when the project has none.</summary>
    public string? FilePath { get; } = filePath;

    public IReadOnlyDictionary<string, ProjectResourceEntry> Entries { get; } = entries;

    /// <summary>
    /// The entry behind a member of the generated class. Visual Studio's generator turns an entry
    /// name that is no C# identifier (<c>my-logo</c>) into one (<c>my_logo</c>), and code only ever
    /// names the latter.
    /// </summary>
    public bool TryGet(string memberName, out ProjectResourceEntry entry)
    {
        if (Entries.TryGetValue(memberName, out entry!))
        {
            return true;
        }

        entry = Entries.Values.FirstOrDefault(e => ProjectResourcesReader.ToIdentifier(e.Name) == memberName)!;
        return entry is not null;
    }
}

/// <summary>
/// Reads <c>Properties/Resources.resx</c> - the project-wide resource file behind
/// <c>Properties.Resources.X</c>, as opposed to the per-Form .resx <see cref="ResxReader"/> reads.
/// </summary>
/// <remarks>
/// <para>
/// The two files look alike and are keyed differently: a Form's entries are
/// <c>"field.Property"</c>, this one's are bare names (<c>"logo"</c>), which is exactly what
/// <see cref="ResxReader"/> skips. And an image here is usually not embedded at all but a
/// <c>ResXFileRef</c> - <c>..\Resources\logo.png;System.Drawing.Bitmap, System.Drawing</c> - to a
/// real file in the project's <c>Resources/</c> folder, which can simply be copied.
/// </para>
/// <para>
/// A file reference that does not exist on disk is kept, with no path - the pipeline reports it by
/// name rather than pointing an asset at nothing.
/// </para>
/// </remarks>
public static class ProjectResourcesReader
{
    public static ProjectResources Read(WinFormsProjectModel project)
    {
        var path = project.ResourceFiles.FirstOrDefault(IsProjectResourceFile);
        return path is null ? ProjectResources.None : Read(path);
    }

    public static bool IsProjectResourceFile(string path) =>
        string.Equals(Path.GetFileName(path), "Resources.resx", StringComparison.OrdinalIgnoreCase)
        && string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "Properties", StringComparison.OrdinalIgnoreCase);

    public static ProjectResources Read(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return ProjectResources.None;
        }

        XDocument document;
        try
        {
            document = XDocument.Load(filePath);
        }
        catch (System.Xml.XmlException)
        {
            return ProjectResources.None;
        }

        var directory = Path.GetDirectoryName(filePath)!;
        var entries = new Dictionary<string, ProjectResourceEntry>(StringComparer.Ordinal);
        foreach (var data in document.Root?.Elements("data") ?? [])
        {
            if (data.Attribute("name")?.Value is not { Length: > 0 } name || name.StartsWith(">>", StringComparison.Ordinal))
            {
                continue;
            }

            var type = data.Attribute("type")?.Value;
            var value = data.Element("value")?.Value ?? "";
            entries[name] = ReadEntry(name, type, data.Attribute("mimetype")?.Value, value, directory);
        }

        return new ProjectResources(filePath, entries);
    }

    private static ProjectResourceEntry ReadEntry(string name, string? type, string? mimeType, string value, string directory)
    {
        if (type is not null && SimpleName(type) == "ResXFileRef")
        {
            // `relative\path.png;Full.Type.Name, Assembly[;encoding]` - the path is relative to
            // the .resx, and written with Windows separators.
            var parts = value.Split(';');
            var relative = parts[0].Trim().Replace('\\', Path.DirectorySeparatorChar);
            var referencedType = parts.Length > 1 ? SimpleName(parts[1]) : null;
            var full = Path.GetFullPath(Path.Combine(directory, relative));

            // A text file reference is still a string as far as the code is concerned.
            if (referencedType == "String")
            {
                return File.Exists(full)
                    ? new ProjectResourceEntry(name, ProjectResourceKind.Text, "String", Text: File.ReadAllText(full))
                    : new ProjectResourceEntry(name, ProjectResourceKind.File, "String");
            }

            return new ProjectResourceEntry(
                name, ProjectResourceKind.File, referencedType, FilePath: File.Exists(full) ? full : null);
        }

        if (mimeType is not null && mimeType.Contains("base64", StringComparison.OrdinalIgnoreCase))
        {
            return new ProjectResourceEntry(name, ProjectResourceKind.Embedded, type is null ? null : SimpleName(type), Base64: value);
        }

        return new ProjectResourceEntry(name, ProjectResourceKind.Text, type is null ? "String" : SimpleName(type), Text: value);
    }

    /// <summary>The member name Visual Studio's resource generator gives an entry.</summary>
    public static string ToIdentifier(string name)
    {
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
        var identifier = new string(chars);
        if (identifier.Length == 0 || char.IsDigit(identifier[0]))
        {
            identifier = "_" + identifier;
        }

        return Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(identifier) != Microsoft.CodeAnalysis.CSharp.SyntaxKind.None
            ? "_" + identifier
            : identifier;
    }

    /// <summary>"Bitmap" for "System.Drawing.Bitmap, System.Drawing, Version=...".</summary>
    private static string SimpleName(string assemblyQualifiedName)
    {
        var typeName = assemblyQualifiedName.Split(',')[0].Trim();
        var lastDot = typeName.LastIndexOf('.');
        return lastDot < 0 ? typeName : typeName[(lastDot + 1)..];
    }
}
