using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>What the project's settings turned into.</summary>
/// <param name="File">The generated <c>Properties/Settings.cs</c>, or null when there was nothing to carry.</param>
/// <param name="Access">What a handler body may read, write and call on it.</param>
/// <param name="Warnings">Settings that were not carried, by name.</param>
public sealed record ProjectSettingsClass(GeneratedTextFile? File, ProjectSettingsAccess Access, IReadOnlyList<string> Warnings)
{
    public static ProjectSettingsClass None { get; } = new(null, ProjectSettingsAccess.None, []);
}

/// <summary>
/// <c>Properties/Settings.settings</c> as a class of the same name and shape in the generated
/// project - <c>Properties.Settings.Default.UserName</c>, <c>.Save()</c>, <c>.Reload()</c>,
/// <c>.Reset()</c> - persisted as JSON under the user's application-data folder.
/// </summary>
/// <remarks>
/// <para>
/// WinForms' <c>ApplicationSettingsBase</c> stores user settings in a per-version
/// <c>user.config</c> through <c>System.Configuration</c>, which an Avalonia app has no reason to
/// reference and which a browser head cannot run. The behaviour code relies on is small, though:
/// typed properties with a default, application-scoped ones read-only, and an explicit
/// <c>Save</c>. That is what this writes, and nothing more - so existing values in a user's
/// <c>user.config</c> are not migrated, which is reported.
/// </para>
/// <para>
/// Only the types System.Text.Json round-trips without a converter and C# can write as a literal.
/// A <c>Color</c>, a <c>Point</c> or a <c>StringCollection</c> is left out and named.
/// </para>
/// </remarks>
public static class ProjectSettingsEmitter
{
    public const string RelativePath = "Properties/Settings.cs";

    /// <summary>The setting types carried over: declared CLR name → C# type.</summary>
    public static IReadOnlyDictionary<string, string> CarriedTypes { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.String"] = "string",
            ["System.Boolean"] = "bool",
            ["System.Int32"] = "int",
            ["System.Int64"] = "long",
            ["System.Double"] = "double",
            ["System.Single"] = "float",
            ["System.Decimal"] = "decimal",
            ["System.DateTime"] = "DateTime",
            ["System.TimeSpan"] = "TimeSpan",
        };

    /// <summary>The members the generated class declares itself, which no setting may be named.</summary>
    private static readonly IReadOnlySet<string> OwnMembers = new HashSet<string>(StringComparer.Ordinal)
    {
        "Default", "Save", "Reload", "Reset", "FilePath", "Load", "CopyFrom", "Settings",
    };

    public static ProjectSettingsClass Emit(ProjectSettings settings, string projectName)
    {
        if (settings.Entries.Count == 0)
        {
            return ProjectSettingsClass.None;
        }

        var carried = new List<(ProjectSettingEntry Entry, string Type, string Default)>();
        var skipped = new List<string>();
        foreach (var entry in settings.Entries)
        {
            if (!SyntaxFacts.IsValidIdentifier(entry.Name)
                || SyntaxFacts.GetKeywordKind(entry.Name) != SyntaxKind.None
                || OwnMembers.Contains(entry.Name)
                || !CarriedTypes.TryGetValue(entry.TypeName, out var type)
                || DefaultLiteral(entry.TypeName, entry.DefaultValue) is not { } literal)
            {
                skipped.Add($"'{entry.Name}' ({entry.TypeName})");
                continue;
            }

            carried.Add((entry, type, literal));
        }

        var warnings = new List<string>();
        if (skipped.Count > 0)
        {
            warnings.Add(
                $"Properties/Settings.settings: {string.Join(", ", skipped)} could not be carried into the generated "
                + "Properties.Settings class (a type JSON cannot store without a converter, or a name the class "
                + "uses itself) - code naming them will not compile until they are added by hand.");
        }

        if (carried.Count == 0)
        {
            return new ProjectSettingsClass(null, ProjectSettingsAccess.None, warnings);
        }

        warnings.Add(
            "Properties.Settings is carried over as Properties/Settings.cs, saved as JSON under the user's "
            + $"application-data folder ({projectName}/settings.json). Values users already saved in a WinForms "
            + "user.config are not migrated.");

        var source = new StringBuilder();
        source.Append("using System;\nusing System.IO;\nusing System.Text.Json;\nusing System.Text.Json.Serialization;\n\n");
        source.Append(CultureInfo.InvariantCulture, $"namespace {projectName}.Properties;\n\n");
        source.Append("/// <summary>\n");
        source.Append("/// The original project's Properties/Settings.settings, carried over by wf2a: user-scoped settings\n");
        source.Append("/// are read/write and persisted by Save(); application-scoped ones are read-only.\n");
        source.Append("/// </summary>\n");
        source.Append("internal sealed class Settings\n{\n");
        source.Append(CultureInfo.InvariantCulture,
            $"    private static readonly string FilePath = Path.Combine(\n        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), \"{projectName}\", \"settings.json\");\n\n");
        source.Append("    public static Settings Default { get; } = Load();\n");

        foreach (var (entry, type, literal) in carried)
        {
            source.Append('\n');
            source.Append(entry.IsUserScoped
                ? $"    public {type} {entry.Name} {{ get; set; }} = {literal};\n"
                : $"    [JsonIgnore]\n    public {type} {entry.Name} {{ get; }} = {literal};\n");
        }

        var userScoped = carried.Where(c => c.Entry.IsUserScoped).ToList();

        source.Append("\n    /// <summary>Writes the user-scoped settings to disk.</summary>\n");
        source.Append("    public void Save()\n    {\n");
        source.Append("        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);\n");
        source.Append("        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));\n    }\n");

        source.Append("\n    /// <summary>Discards unsaved changes, re-reading what was last saved.</summary>\n");
        source.Append("    public void Reload() => CopyFrom(Load());\n");

        source.Append("\n    /// <summary>Restores every user-scoped setting to its default, and saves that.</summary>\n");
        source.Append("    public void Reset()\n    {\n        CopyFrom(new Settings());\n        Save();\n    }\n");

        source.Append("\n    private void CopyFrom(Settings other)\n    {\n");
        foreach (var (entry, _, _) in userScoped)
        {
            source.Append(CultureInfo.InvariantCulture, $"        {entry.Name} = other.{entry.Name};\n");
        }

        if (userScoped.Count == 0)
        {
            source.Append("        _ = other;\n");
        }

        source.Append("    }\n");

        source.Append("\n    private static Settings Load()\n    {\n");
        source.Append("        try\n        {\n");
        source.Append("            if (File.Exists(FilePath))\n            {\n");
        source.Append("                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();\n");
        source.Append("            }\n        }\n");
        source.Append("        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)\n");
        source.Append("        {\n            // An unreadable file means the defaults, as WinForms falls back on a corrupt user.config.\n        }\n\n");
        source.Append("        return new Settings();\n    }\n}\n");

        var access = new ProjectSettingsAccess(
            $"{projectName}.Properties.Settings.Default",
            carried.Select(c => c.Entry.Name).ToHashSet(StringComparer.Ordinal),
            userScoped.Select(c => c.Entry.Name).ToHashSet(StringComparer.Ordinal));

        return new ProjectSettingsClass(new GeneratedTextFile(RelativePath, source.ToString()), access, warnings);
    }

    /// <summary>The default value as a C# literal of its type, or null when it does not parse as one.</summary>
    public static string? DefaultLiteral(string typeName, string? value)
    {
        var invariant = CultureInfo.InvariantCulture;
        if (typeName == "System.String")
        {
            return SymbolDisplay.FormatLiteral(value ?? "", quote: true);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return "default";
        }

        value = value.Trim();
        return typeName switch
        {
            "System.Boolean" => bool.TryParse(value, out var b) ? (b ? "true" : "false") : null,
            "System.Int32" => int.TryParse(value, NumberStyles.Integer, invariant, out var i) ? i.ToString(invariant) : null,
            "System.Int64" => long.TryParse(value, NumberStyles.Integer, invariant, out var l) ? $"{l.ToString(invariant)}L" : null,
            "System.Double" => double.TryParse(value, NumberStyles.Float, invariant, out var d) && double.IsFinite(d)
                ? $"{d.ToString("R", invariant)}d" : null,
            "System.Single" => float.TryParse(value, NumberStyles.Float, invariant, out var f) && float.IsFinite(f)
                ? $"{f.ToString("R", invariant)}f" : null,
            "System.Decimal" => decimal.TryParse(value, NumberStyles.Number, invariant, out var m) ? $"{m.ToString(invariant)}m" : null,
            "System.DateTime" => DateTime.TryParse(value, invariant, DateTimeStyles.None, out var dt)
                ? $"new DateTime({dt.Ticks.ToString(invariant)}L)" : null,
            "System.TimeSpan" => TimeSpan.TryParse(value, invariant, out var ts)
                ? $"new TimeSpan({ts.Ticks.ToString(invariant)}L)" : null,
            _ => null,
        };
    }
}
