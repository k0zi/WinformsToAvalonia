using System.IO.Compression;
using System.Text.RegularExpressions;

namespace WinFormsToAvalonia.Core.Semantics;

/// <summary>
/// Where the contents of a NuGet package are found on this machine: the global packages folder a
/// <c>dotnet restore</c> fills, a legacy solution's <c>packages/</c> folder, and - unless the run is
/// offline - a cache of this tool's own that a package is downloaded into from nuget.org.
/// </summary>
/// <remarks>
/// The conversion only ever <em>reads</em> a package: its assemblies join the semantic compilation,
/// so code calling into it can be proven to compile in the generated project. Nothing is written
/// into NuGet's own folders - a half-written entry there would break the user's next restore.
/// </remarks>
public sealed partial class NuGetPackageStore
{
    private const string FlatContainer = "https://api.nuget.org/v3-flatcontainer";

    private readonly IReadOnlyList<string> _globalFolders;
    private readonly string? _downloadCache;
    private readonly Dictionary<string, string?> _located = new(StringComparer.OrdinalIgnoreCase);

    private NuGetPackageStore(IReadOnlyList<string> globalFolders, string? downloadCache)
    {
        _globalFolders = globalFolders;
        _downloadCache = downloadCache;
    }

    /// <summary>Never finds anything: the conversion behaves as if the project had no packages.</summary>
    public static NuGetPackageStore None { get; } = new([], null);

    /// <summary>The machine's NuGet folders, plus downloading when <paramref name="allowDownload"/>.</summary>
    public static NuGetPackageStore Default(bool allowDownload)
    {
        var global = Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        var cache = allowDownload
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinFormsToAvalonia", "packages")
            : null;

        return new NuGetPackageStore([global], cache);
    }

    /// <summary>Only the given folders, laid out like the global packages folder (<c>id/version/</c>, lower case).</summary>
    public static NuGetPackageStore FromFolders(params string[] folders) => new(folders, null);

    /// <summary>
    /// The folder holding the extracted package, or null when it is nowhere to be found. Only an exact
    /// version is ever looked up - a range or a floating version is a question for NuGet's resolver.
    /// </summary>
    internal string? Locate(string id, string version, string projectDirectory)
    {
        if (NormalizeVersion(version) is not { } normalized)
        {
            return null;
        }

        var key = $"{id}/{normalized}";
        if (_located.TryGetValue(key, out var known))
        {
            return known;
        }

        var found = FindInGlobalFolders(id, normalized)
            ?? FindInSolutionPackagesFolder(id, version, normalized, projectDirectory)
            ?? Download(id, normalized);
        _located[key] = found;
        return found;
    }

    /// <summary>
    /// NuGet's normalized form - <c>1.0</c> is <c>1.0.0</c>, <c>1.2.3.0</c> is <c>1.2.3</c>, a pinned
    /// <c>[1.2.3]</c> is <c>1.2.3</c> - or null for anything that is not one exact version.
    /// </summary>
    internal static string? NormalizeVersion(string version)
    {
        var text = version.Trim();
        if (text.StartsWith('[') && text.EndsWith(']') && !text.Contains(','))
        {
            text = text[1..^1].Trim();
        }

        var match = ExactVersion().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var numbers = match.Groups["numbers"].Value.Split('.').Select(n => long.Parse(n, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        while (numbers.Count < 3)
        {
            numbers.Add(0);
        }

        if (numbers.Count == 4 && numbers[3] == 0)
        {
            numbers.RemoveAt(3);
        }

        var release = match.Groups["release"].Value;
        return string.Join('.', numbers) + (release.Length > 0 ? "-" + release : "");
    }

    private string? FindInGlobalFolders(string id, string normalized)
    {
        foreach (var folder in _globalFolders.Append(_downloadCache).OfType<string>())
        {
            var candidate = Path.Combine(folder, id.ToLowerInvariant(), normalized.ToLowerInvariant());
            if (IsComplete(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>A packages.config project's <c>..\packages\Id.Version\</c>, searched upwards from the project.</summary>
    private static string? FindInSolutionPackagesFolder(string id, string version, string normalized, string projectDirectory)
    {
        var directory = new DirectoryInfo(projectDirectory);
        for (var level = 0; directory is not null && level < 4; level++, directory = directory.Parent)
        {
            var packages = Path.Combine(directory.FullName, "packages");
            if (!Directory.Exists(packages))
            {
                continue;
            }

            foreach (var name in new[] { $"{id}.{version}", $"{id}.{normalized}" })
            {
                var match = Directory.EnumerateDirectories(packages)
                    .FirstOrDefault(d => string.Equals(Path.GetFileName(d), name, StringComparison.OrdinalIgnoreCase));
                if (match is not null && IsComplete(match))
                {
                    return match;
                }
            }
        }

        return null;
    }

    private string? Download(string id, string normalized)
    {
        if (_downloadCache is null)
        {
            return null;
        }

        var lowerId = id.ToLowerInvariant();
        var lowerVersion = normalized.ToLowerInvariant();
        var target = Path.Combine(_downloadCache, lowerId, lowerVersion);
        var staging = target + ".w2a-" + Guid.NewGuid().ToString("N");

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var bytes = http.GetByteArrayAsync($"{FlatContainer}/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg")
                .GetAwaiter().GetResult();

            using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(staging);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (!Directory.Exists(target))
            {
                Directory.Move(staging, target);
            }

            return IsComplete(target) ? target : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException
            or InvalidDataException or UnauthorizedAccessException)
        {
            return null;
        }
        finally
        {
            try
            {
                if (Directory.Exists(staging))
                {
                    Directory.Delete(staging, recursive: true);
                }
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>An extracted package always carries its .nuspec at the root.</summary>
    private static bool IsComplete(string folder) =>
        Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*.nuspec").Any();

    [GeneratedRegex(@"^(?<numbers>\d+(\.\d+){0,3})(-(?<release>[0-9A-Za-z.-]+))?(\+[0-9A-Za-z.-]+)?$")]
    private static partial Regex ExactVersion();
}
