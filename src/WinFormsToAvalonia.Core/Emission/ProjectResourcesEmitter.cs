using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>A generated text file: where it goes in the project, and what it says.</summary>
public sealed record GeneratedTextFile(string RelativePath, string Text);

/// <summary>What the project's resource file turned into.</summary>
/// <param name="File">The generated <c>Properties/Resources.cs</c>, or null when there was nothing to carry.</param>
/// <param name="Strings">The string members a handler body may name.</param>
/// <param name="Assets">The image files to copy, by asset path.</param>
/// <param name="AssetPaths">Each carried image entry's asset path, by entry name.</param>
/// <param name="Warnings">Entries that were not carried, by name.</param>
public sealed record ProjectResourceClass(
    GeneratedTextFile? File,
    ProjectStringResources Strings,
    IReadOnlyDictionary<string, byte[]> Assets,
    IReadOnlyDictionary<string, string> AssetPaths,
    IReadOnlyList<string> Warnings)
{
    public static ProjectResourceClass None { get; } = new(
        null, ProjectStringResources.None,
        new Dictionary<string, byte[]>(StringComparer.Ordinal),
        new Dictionary<string, string>(StringComparer.Ordinal),
        []);
}

/// <summary>
/// The original project's <c>Properties/Resources.resx</c>, as a class of the same name and shape
/// in the generated project - so code that says <c>Properties.Resources.Greeting</c> still compiles.
/// </summary>
/// <remarks>
/// <para>
/// Visual Studio generates <c>Properties.Resources</c> from the .resx at design time; the Avalonia
/// project has no such generator, and a WinForms one would hand back <c>System.Drawing</c> types.
/// So the class is written out once, here: strings as constants, images as the Avalonia type that
/// shows them - a <c>Bitmap</c>, or a <c>WindowIcon</c> for an <c>Icon</c> - loaded from the file
/// copied to <c>Assets/Resources/</c>.
/// </para>
/// <para>
/// Strings are inlined rather than read from a carried .resx at run time. That loses the satellite
/// lookup a localized project relied on, which is reported; it keeps the generated project free of
/// a resource pipeline nobody asked for.
/// </para>
/// </remarks>
public static class ProjectResourcesEmitter
{
    public const string RelativePath = "Properties/Resources.cs";

    public static string AssetPath(string entryName, string extension) =>
        $"Assets/Resources/{ProjectResourcesReader.ToIdentifier(entryName)}{extension.ToLowerInvariant()}";

    public static ProjectResourceClass Emit(ProjectResources resources, string projectName)
    {
        if (resources.Entries.Count == 0)
        {
            return ProjectResourceClass.None;
        }

        var members = new List<string>();
        var strings = new HashSet<string>(StringComparer.Ordinal);
        var usedMembers = new HashSet<string>(StringComparer.Ordinal);
        var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var assetPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        var skipped = new List<string>();
        var usesImages = false;
        var usesIcons = false;

        foreach (var entry in resources.Entries.Values.OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            var member = ProjectResourcesReader.ToIdentifier(entry.Name);
            if (!usedMembers.Add(member))
            {
                // Two entry names that sanitize to one member - Visual Studio refuses the file too.
                skipped.Add(entry.Name);
                continue;
            }

            if (entry is { Kind: ProjectResourceKind.Text, TypeSimpleName: "String", Text: { } text })
            {
                strings.Add(member);
                members.Add(
                    $"    /// <summary>{EscapeXml(Summarize(text))}</summary>\n"
                    + $"    public static string {member} => {SymbolDisplay.FormatLiteral(text, quote: true)};");
                continue;
            }

            if (entry.IsImage && TryGetImage(entry, out var bytes, out var extension))
            {
                var assetPath = AssetPath(entry.Name, extension);
                assets[assetPath] = bytes;
                assetPaths[entry.Name] = assetPath;

                var isIcon = entry.TypeSimpleName == "Icon";
                usesIcons |= isIcon;
                usesImages |= !isIcon;
                var type = isIcon ? "WindowIcon" : "Bitmap";
                members.Add(
                    $"    public static {type} {member} => new(AssetLoader.Open(new Uri(\"avares://{projectName}/{assetPath}\")));");
                continue;
            }

            skipped.Add(entry.Name);
        }

        var warnings = new List<string>();
        if (skipped.Count > 0)
        {
            warnings.Add(
                $"Properties/Resources.resx: {string.Join(", ", skipped.Select(n => $"'{n}'"))} "
                + "could not be carried into the generated Properties.Resources class (not a string or a recoverable "
                + "image, or a file reference that does not exist) - code naming them will not compile until they are added by hand.");
        }

        if (members.Count == 0)
        {
            return new ProjectResourceClass(
                null, ProjectStringResources.None, assets, assetPaths, warnings);
        }

        var source = new StringBuilder();
        source.Append("using System;\n");
        if (usesIcons)
        {
            source.Append("using Avalonia.Controls;\n");
        }

        if (usesImages)
        {
            source.Append("using Avalonia.Media.Imaging;\n");
        }

        if (usesImages || usesIcons)
        {
            source.Append("using Avalonia.Platform;\n");
        }

        source.Append(CultureInfo.InvariantCulture, $"\nnamespace {projectName}.Properties;\n\n");
        source.Append("/// <summary>\n");
        source.Append("/// The entries of the original project's Properties/Resources.resx, carried over by wf2a.\n");
        source.Append("/// Strings are inlined; images load from Assets/Resources/.\n");
        source.Append("/// </summary>\n");
        source.Append("internal static class Resources\n{\n");
        source.Append(string.Join("\n\n", members));
        source.Append("\n}\n");

        return new ProjectResourceClass(
            new GeneratedTextFile(RelativePath, source.ToString()),
            new ProjectStringResources($"{projectName}.Properties.Resources", strings),
            assets,
            assetPaths,
            warnings);
    }

    private static bool TryGetImage(ProjectResourceEntry entry, out byte[] bytes, out string extension)
    {
        switch (entry)
        {
            case { Kind: ProjectResourceKind.File, FilePath: { } file }:
                bytes = System.IO.File.ReadAllBytes(file);
                extension = Path.GetExtension(file);
                return true;
            case { Kind: ProjectResourceKind.Embedded, Base64: { } base64 } when ResxImageExtractor.TryExtract(base64, out var image):
                bytes = image.Bytes;
                extension = image.FileExtension;
                return true;
            default:
                bytes = [];
                extension = "";
                return false;
        }
    }

    private static string Summarize(string text)
    {
        var line = text.Split('\n')[0].Trim();
        return line.Length > 80 ? line[..77] + "..." : line;
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
