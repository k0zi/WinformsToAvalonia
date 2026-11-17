using System.Xml.Linq;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>One <c>&lt;Setting&gt;</c> of <c>Properties/Settings.settings</c>.</summary>
/// <param name="TypeName">The declared CLR type, as written - <c>System.String</c>, <c>System.Int32</c>, ...</param>
/// <param name="IsUserScoped">User scope can be changed and saved; application scope is read-only.</param>
/// <param name="DefaultValue">The <c>(Default)</c> profile's value, as text.</param>
public sealed record ProjectSettingEntry(string Name, string TypeName, bool IsUserScoped, string? DefaultValue);

/// <summary>The settings of a project, in declaration order.</summary>
public sealed class ProjectSettings(string? filePath, IReadOnlyList<ProjectSettingEntry> entries)
{
    public static ProjectSettings None { get; } = new(null, []);

    public string? FilePath { get; } = filePath;

    public IReadOnlyList<ProjectSettingEntry> Entries { get; } = entries;
}

/// <summary>
/// Reads <c>Properties/Settings.settings</c> - the designer file Visual Studio generates
/// <c>Properties.Settings.Default</c> from.
/// </summary>
/// <remarks>
/// The <c>.settings</c> file rather than <c>app.config</c>: it is the source both are generated from,
/// it carries the declared type and scope, and its default value is the one the generated class
/// falls back to. An <c>app.config</c> override of a default is a deployment detail and not read.
/// </remarks>
public static class ProjectSettingsReader
{
    public static ProjectSettings Read(WinFormsProjectModel project) =>
        Read(Path.Combine(project.ProjectDirectory, "Properties", "Settings.settings"));

    public static ProjectSettings Read(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return ProjectSettings.None;
        }

        XDocument document;
        try
        {
            document = XDocument.Load(filePath);
        }
        catch (System.Xml.XmlException)
        {
            return ProjectSettings.None;
        }

        // Older files carry the namespace, hand-written ones sometimes not; match on local names.
        var entries = document.Descendants()
            .Where(e => e.Name.LocalName == "Setting")
            .Select(setting => new ProjectSettingEntry(
                setting.Attribute("Name")?.Value ?? "",
                setting.Attribute("Type")?.Value ?? "System.String",
                !string.Equals(setting.Attribute("Scope")?.Value, "Application", StringComparison.OrdinalIgnoreCase),
                setting.Elements().FirstOrDefault(v => v.Name.LocalName == "Value")?.Value))
            .Where(e => e.Name.Length > 0)
            .ToList();
        return new ProjectSettings(filePath, entries);
    }
}
