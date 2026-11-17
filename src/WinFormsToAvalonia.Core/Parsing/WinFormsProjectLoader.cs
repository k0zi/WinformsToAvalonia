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

        // A Shared Project (`<Import Project="..\Shared\Shared.projitems" Label="Shared" />`) is
        // how one set of Forms is compiled into several apps; its items are this project's too.
        var shared = ReadSharedProjectItems(root, ns, projectDirectory);

        if (isLegacyStyle)
        {
            compileFiles = ReadItemIncludePaths(root, ns, "Compile", "Include", projectDirectory)
                .Concat(shared.Compile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            resourceFiles = ReadItemIncludePaths(root, ns, "EmbeddedResource", "Include", projectDirectory)
                .Concat(shared.Resources)
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
                .Union(shared.Compile, StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // A linked .resx beside a linked Form (`<EmbeddedResource Include="..\Other\Form1.resx" />`)
            // and a shared project's are outside the glob.
            resourceFiles = GlobFiles(projectDirectory, "*.resx")
                .Union(ReadItemIncludePaths(root, ns, "EmbeddedResource", "Include", projectDirectory), StringComparer.OrdinalIgnoreCase)
                .Union(shared.Resources, StringComparer.OrdinalIgnoreCase)
                .Where(f => f.EndsWith(".resx", StringComparison.OrdinalIgnoreCase) && File.Exists(f))
                .ToList();
        }

        return new WinFormsProjectModel(
            fullPath,
            isLegacyStyle,
            rootNamespace,
            assemblyName,
            targetFrameworks,
            compileFiles,
            resourceFiles,
            projectReferences,
            ReadProperty(root, ns, "OutputType"),
            // Decides whether a source file copied into the generated project (which enables
            // nullable reference types) needs `#nullable disable` to keep meaning what it meant.
            string.Equals(ReadProperty(root, ns, "Nullable"), "enable", StringComparison.OrdinalIgnoreCase),
            ReadPackageReferences(root, ns, projectDirectory));
    }

    /// <summary>
    /// <c>&lt;PackageReference Include="X" Version="1.2.3" /&gt;</c> (the version also as a child element,
    /// or from central package management's <c>Directory.Packages.props</c>), and a legacy project's
    /// <c>packages.config</c>.
    /// </summary>
    private static List<NuGetPackageReference> ReadPackageReferences(XElement root, XNamespace ns, string projectDirectory)
    {
        var packages = new List<NuGetPackageReference>();
        Dictionary<string, string>? centralVersions = null;

        foreach (var item in root.Elements(ns + "ItemGroup").Elements(ns + "PackageReference"))
        {
            if (item.Attribute("Include")?.Value.Trim() is not { Length: > 0 } id)
            {
                continue;
            }

            var version = item.Attribute("Version")?.Value ?? item.Element(ns + "Version")?.Value;
            if (string.IsNullOrWhiteSpace(version))
            {
                centralVersions ??= ReadCentralPackageVersions(projectDirectory);
                centralVersions.TryGetValue(id, out version);
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                continue;
            }

            var privateAssets = item.Attribute("PrivateAssets")?.Value ?? item.Element(ns + "PrivateAssets")?.Value;
            packages.Add(new NuGetPackageReference(
                id, version.Trim(), string.Equals(privateAssets?.Trim(), "all", StringComparison.OrdinalIgnoreCase)));
        }

        var packagesConfig = Path.Combine(projectDirectory, "packages.config");
        if (File.Exists(packagesConfig))
        {
            try
            {
                foreach (var package in XDocument.Load(packagesConfig).Root?.Elements("package") ?? [])
                {
                    if (package.Attribute("id")?.Value is { Length: > 0 } id
                        && package.Attribute("version")?.Value is { Length: > 0 } version)
                    {
                        packages.Add(new NuGetPackageReference(
                            id, version, string.Equals(package.Attribute("developmentDependency")?.Value, "true", StringComparison.OrdinalIgnoreCase)));
                    }
                }
            }
            catch (System.Xml.XmlException)
            {
                // A malformed packages.config names no package.
            }
        }

        return packages
            .DistinctBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>The nearest <c>Directory.Packages.props</c> above the project, as <c>id → version</c>.</summary>
    private static Dictionary<string, string> ReadCentralPackageVersions(string projectDirectory)
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var directory = new DirectoryInfo(projectDirectory); directory is not null; directory = directory.Parent)
        {
            var props = Path.Combine(directory.FullName, "Directory.Packages.props");
            if (!File.Exists(props))
            {
                continue;
            }

            try
            {
                var propsRoot = XDocument.Load(props).Root;
                foreach (var item in propsRoot?.Descendants(propsRoot.Name.Namespace + "PackageVersion") ?? [])
                {
                    if (item.Attribute("Include")?.Value is { Length: > 0 } id
                        && item.Attribute("Version")?.Value is { Length: > 0 } version)
                    {
                        versions.TryAdd(id, version);
                    }
                }
            }
            catch (System.Xml.XmlException)
            {
            }

            break;
        }

        return versions;
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
            foreach (var part in include.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                foreach (var path in ExpandInclude(projectDirectory, part))
                {
                    yield return path;
                }
            }
        }
    }

    /// <summary>
    /// The Compile and EmbeddedResource items of every Shared Project this one imports. Their paths
    /// are written against <c>$(MSBuildThisFileDirectory)</c> - the .projitems' own folder.
    /// </summary>
    private static (List<string> Compile, List<string> Resources) ReadSharedProjectItems(
        XElement root, XNamespace ns, string projectDirectory)
    {
        var compile = new List<string>();
        var resources = new List<string>();
        foreach (var import in root.Elements(ns + "Import"))
        {
            if (import.Attribute("Project")?.Value is not { } projectAttribute
                || !projectAttribute.EndsWith(".projitems", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var itemsPath = ExpandInclude(projectDirectory, projectAttribute).FirstOrDefault();
            if (itemsPath is null || !File.Exists(itemsPath))
            {
                continue;
            }

            XElement? itemsRoot;
            try
            {
                itemsRoot = XDocument.Load(itemsPath).Root;
            }
            catch (System.Xml.XmlException)
            {
                continue;
            }

            if (itemsRoot is null)
            {
                continue;
            }

            var itemsDirectory = Path.GetDirectoryName(itemsPath)!;
            compile.AddRange(ReadItemIncludePaths(itemsRoot, itemsRoot.Name.Namespace, "Compile", "Include", itemsDirectory));
            resources.AddRange(ReadItemIncludePaths(itemsRoot, itemsRoot.Name.Namespace, "EmbeddedResource", "Include", itemsDirectory));
        }

        return (compile, resources);
    }

    /// <summary>
    /// One item include as the files it names: the two directory properties every linked or shared
    /// item uses, a wildcard (<c>..\FullFxApp\*.cs</c>, <c>Forms\**\*.cs</c>), and - for a plain
    /// path - the file as it is really spelled on disk.
    /// </summary>
    /// <remarks>
    /// The last one matters off Windows. A legacy csproj written by Visual Studio says
    /// <c>Form1.designer.cs</c> while the file is <c>Form1.Designer.cs</c>; Windows never noticed,
    /// and on Linux the whole conversion failed on "Could not find file".
    /// </remarks>
    private static IEnumerable<string> ExpandInclude(string projectDirectory, string include)
    {
        var expanded = include
            .Replace("$(MSBuildThisFileDirectory)", projectDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            .Replace("$(MSBuildProjectDirectory)", projectDirectory, StringComparison.OrdinalIgnoreCase);
        var full = NormalizeToAbsolutePath(projectDirectory, expanded);

        if (full.IndexOfAny(['*', '?']) < 0)
        {
            yield return ResolveCasing(full);
            yield break;
        }

        // Everything before the first wildcard segment is a directory; the rest is a pattern.
        var segments = full.Split(Path.DirectorySeparatorChar);
        var firstWildcard = Array.FindIndex(segments, segment => segment.IndexOfAny(['*', '?']) >= 0);
        var baseDirectory = ResolveCasing(string.Join(Path.DirectorySeparatorChar, segments[..firstWildcard]));
        if (!Directory.Exists(baseDirectory))
        {
            yield break;
        }

        // `dir\*.cs` and `dir\**\*.cs` - the two shapes item includes use. Anything more intricate
        // (a wildcard in a directory segment) names nothing here, rather than a guess.
        var rest = segments[firstWildcard..];
        var recursive = rest is ["**", _];
        if (rest.Length != 1 && !recursive)
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(
                     baseDirectory, rest[^1], recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
            if (!IsUnderExcludedDirectory(baseDirectory, file))
            {
                yield return Path.GetFullPath(file);
            }
        }
    }

    /// <summary>
    /// <paramref name="path"/> as it is spelled on disk, segment by segment, when it does not exist
    /// as written but exactly one case-insensitive match does. Otherwise unchanged.
    /// </summary>
    internal static string ResolveCasing(string path)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            return path;
        }

        var root = Path.GetPathRoot(path) ?? "";
        var current = root;
        foreach (var segment in path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            var exact = Path.Combine(current, segment);
            if (File.Exists(exact) || Directory.Exists(exact))
            {
                current = exact;
                continue;
            }

            if (!Directory.Exists(current))
            {
                return path;
            }

            var matches = Directory.EnumerateFileSystemEntries(current)
                .Where(entry => string.Equals(Path.GetFileName(entry), segment, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (matches.Count != 1)
            {
                return path;
            }

            current = matches[0];
        }

        return current;
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
