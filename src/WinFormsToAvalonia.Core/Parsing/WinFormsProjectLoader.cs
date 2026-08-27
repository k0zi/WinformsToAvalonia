using System.Xml.Linq;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Loads a WinForms .csproj without invoking the MSBuild evaluation engine: the target
/// machine running this tool is not guaranteed to have a matching MSBuild toolset (or any
/// Windows Desktop workload) available, since the whole point is converting projects
/// cross-platform. Instead this reimplements the small, well-documented subset of MSBuild
/// semantics that actually matters for source discovery: legacy-style explicit
/// &lt;Compile Include&gt; item lists vs. SDK-style implicit globbing (with
/// &lt;Compile Remove&gt; support).
/// </summary>
public sealed class WinFormsProjectLoader
{
    private static readonly string[] DefaultExcludedDirectoryNames = ["bin", "obj", ".vs", ".git"];

    public WinFormsProjectModel Load(string projectFilePath)
    {
        var fullPath = Path.GetFullPath(projectFilePath);
        var doc = XDocument.Load(fullPath);
        var root = doc.Root ?? throw new InvalidOperationException($"'{fullPath}' has no root <Project> element.");
        var ns = root.Name.Namespace;

        var isLegacyStyle = root.Attribute("Sdk") is null;
        var projectDirectory = Path.GetDirectoryName(fullPath)!;
        var projectNameFallback = Path.GetFileNameWithoutExtension(fullPath);

        var rootNamespace = ReadProperty(root, ns, "RootNamespace") ?? projectNameFallback;
        var assemblyName = ReadProperty(root, ns, "AssemblyName") ?? projectNameFallback;
        var targetFrameworks = ReadTargetFrameworks(root, ns);

        // Which sibling projects this one may name types from. Only ever consulted when a whole
        // solution is being converted: it is what tells that run which *other* projects'
        // UserControls a Form here could legally host.
        var projectReferences = ReadItemIncludePaths(root, ns, "ProjectReference", "Include", projectDirectory)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> compileFiles;
        List<string> resourceFiles;

        if (isLegacyStyle)
        {
            compileFiles = ReadItemIncludePaths(root, ns, "Compile", "Include", projectDirectory).ToList();
            resourceFiles = ReadItemIncludePaths(root, ns, "EmbeddedResource", "Include", projectDirectory)
                .Where(f => f.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            var removed = ReadItemIncludePaths(root, ns, "Compile", "Remove", projectDirectory)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            compileFiles = GlobFiles(projectDirectory, "*.cs")
                .Where(f => !removed.Contains(f))
                .Union(ReadItemIncludePaths(root, ns, "Compile", "Include", projectDirectory), StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            resourceFiles = GlobFiles(projectDirectory, "*.resx").ToList();
        }

        return new WinFormsProjectModel(
            fullPath,
            isLegacyStyle,
            rootNamespace,
            assemblyName,
            targetFrameworks,
            compileFiles,
            resourceFiles,
            projectReferences);
    }

    private static IReadOnlyList<string> ReadTargetFrameworks(XElement root, XNamespace ns)
    {
        var multi = ReadProperty(root, ns, "TargetFrameworks");
        if (multi is not null)
        {
            return multi.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var single = ReadProperty(root, ns, "TargetFramework");
        if (single is not null)
        {
            return [single];
        }

        var legacyVersion = ReadProperty(root, ns, "TargetFrameworkVersion");
        return legacyVersion is not null ? [legacyVersion] : [];
    }

    private static string? ReadProperty(XElement root, XNamespace ns, string propertyName)
    {
        return root
            .Elements(ns + "PropertyGroup")
            .Elements(ns + propertyName)
            .Select(e => e.Value.Trim())
            .LastOrDefault(v => v.Length > 0);
    }

    private static IEnumerable<string> ReadItemIncludePaths(XElement root, XNamespace ns, string itemName, string attributeName, string projectDirectory)
    {
        foreach (var item in root.Elements(ns + "ItemGroup").Elements(ns + itemName))
        {
            var include = item.Attribute(attributeName)?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            // Old-style csproj items can list multiple files separated by ';'.
            foreach (var part in include.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                yield return NormalizeToAbsolutePath(projectDirectory, part);
            }
        }
    }

    private static IEnumerable<string> GlobFiles(string projectDirectory, string searchPattern)
    {
        return Directory.EnumerateFiles(projectDirectory, searchPattern, SearchOption.AllDirectories)
            .Where(f => !IsUnderExcludedDirectory(projectDirectory, f))
            .Select(Path.GetFullPath);
    }

    private static bool IsUnderExcludedDirectory(string projectDirectory, string filePath)
    {
        var relative = Path.GetRelativePath(projectDirectory, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Take(segments.Length - 1)
            .Any(segment => DefaultExcludedDirectoryNames.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }

    private static string NormalizeToAbsolutePath(string projectDirectory, string relativeOrWindowsPath)
    {
        var normalized = relativeOrWindowsPath.Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(projectDirectory, normalized));
    }
}
