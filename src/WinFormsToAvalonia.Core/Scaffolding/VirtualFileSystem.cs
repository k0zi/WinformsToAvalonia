using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinFormsToAvalonia.Core.Scaffolding;

/// <summary>What <see cref="VirtualFileSystem.WriteToDisk"/> does about a file that is already there.</summary>
public enum ExistingFileStrategy
{
    /// <summary>
    /// Never destroy an existing file: identical content is skipped, and different content is
    /// written beside the original as <c>&lt;name&gt;.w2a-new</c> for the user to merge.
    /// </summary>
    PreserveExisting,

    /// <summary>Write every generated file, replacing whatever is on disk.</summary>
    Overwrite,
}

/// <param name="Written">Files written to their real path (new, or the strategy was Overwrite).</param>
/// <param name="Unchanged">Files already on disk with byte-identical content - not rewritten.</param>
/// <param name="Preserved">
/// Files kept as the user has them because the generated content differed. The generated version
/// sits next to each one under the same path plus <see cref="VirtualFileSystem.GeneratedFileSuffix"/>.
/// </param>
/// <param name="Pruned">
/// Files an earlier run generated, this one no longer does, and nobody had edited - deleted
/// because pruning was asked for.
/// </param>
/// <param name="Orphaned">
/// Files an earlier run generated and this one no longer does, left on disk: either pruning was not
/// asked for, or the file was edited since it was generated.
/// </param>
public sealed record WriteToDiskResult(
    IReadOnlyList<string> Written,
    IReadOnlyList<string> Unchanged,
    IReadOnlyList<string> Preserved,
    IReadOnlyList<string>? Pruned = null,
    IReadOnlyList<string>? Orphaned = null)
{
    public IReadOnlyList<string> Pruned { get; } = Pruned ?? [];

    public IReadOnlyList<string> Orphaned { get; } = Orphaned ?? [];
}

/// <summary>
/// In-memory staging area for generated project files. Every emitter writes here first;
/// this is what makes --dry-run trivial (render without touching disk) and what tests
/// assert against without filesystem races.
/// </summary>
public sealed class VirtualFileSystem
{
    /// <summary>Appended to a generated file whose real path already holds different content.</summary>
    public const string GeneratedFileSuffix = ".w2a-new";

    /// <summary>
    /// What the last run generated, and the hash of each file as generated - written into the output
    /// root on every run, so the next one can tell its own stale files from a human's.
    /// </summary>
    public const string ManifestFileName = ".w2a-manifest.json";

    private readonly Dictionary<string, string> _textFiles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _binaryFiles = new(StringComparer.Ordinal);

    /// <summary>Relative paths always use '/' as the separator, regardless of host OS.</summary>
    public void AddText(string relativePath, string content)
    {
        var normalized = Normalize(relativePath);
        _textFiles[normalized] = content;
    }

    /// <summary>
    /// Stages a copied binary asset (a NotifyIcon's .ico, ...). Kept in its own dictionary so
    /// <see cref="Files"/>/<see cref="TryGetText"/> stay text-only for every caller that
    /// asserts on generated source.
    /// </summary>
    public void AddBinary(string relativePath, byte[] content)
    {
        var normalized = Normalize(relativePath);
        _binaryFiles[normalized] = content;
    }

    public bool TryGetText(string relativePath, out string content)
        => _textFiles.TryGetValue(Normalize(relativePath), out content!);

    public IReadOnlyDictionary<string, string> Files => _textFiles;

    public IReadOnlyDictionary<string, byte[]> BinaryFiles => _binaryFiles;

    public IEnumerable<string> RelativePaths =>
        _textFiles.Keys.Concat(_binaryFiles.Keys).OrderBy(p => p, StringComparer.Ordinal);

    /// <summary>
    /// The one and only place bytes hit disk.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="ExistingFileStrategy.PreserveExisting"/> because this converter's
    /// whole premise is that a human migrates the output method by method: a re-run after that
    /// work has started must not silently delete it. A first conversion into an empty directory
    /// is unaffected either way - every file is new.
    /// </remarks>
    /// <param name="prune">
    /// Delete what an earlier run generated and this one no longer does (a Form removed or renamed
    /// in the source) - but only a file still exactly as it was generated. An edited one is a
    /// human's work and is only reported, in <see cref="WriteToDiskResult.Orphaned"/>.
    /// </param>
    public WriteToDiskResult WriteToDisk(
        string rootDirectory,
        ExistingFileStrategy existingFiles = ExistingFileStrategy.PreserveExisting,
        bool prune = false)
    {
        Directory.CreateDirectory(rootDirectory);
        var previous = ReadManifest(rootDirectory);

        var written = new List<string>();
        var unchanged = new List<string>();
        var preserved = new List<string>();

        foreach (var (relativePath, content) in _textFiles)
        {
            WriteOne(
                rootDirectory, relativePath, existingFiles, written, unchanged, preserved,
                matchesDisk: path => File.ReadAllText(path) == content,
                write: path => File.WriteAllText(path, content));
        }

        foreach (var (relativePath, content) in _binaryFiles)
        {
            WriteOne(
                rootDirectory, relativePath, existingFiles, written, unchanged, preserved,
                matchesDisk: path => File.ReadAllBytes(path).AsSpan().SequenceEqual(content),
                write: path => File.WriteAllBytes(path, content));
        }

        var current = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (relativePath, content) in _textFiles)
        {
            current[relativePath] = Hash(Encoding.UTF8.GetBytes(content));
        }

        foreach (var (relativePath, content) in _binaryFiles)
        {
            current[relativePath] = Hash(content);
        }

        var pruned = new List<string>();
        var orphaned = new List<string>();
        foreach (var (relativePath, generatedHash) in previous.Where(e => !current.ContainsKey(e.Key)))
        {
            var fullPath = Path.Combine(rootDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            if (prune && Hash(File.ReadAllBytes(fullPath)) == generatedHash)
            {
                File.Delete(fullPath);

                // A merge copy beside it is this tool's own output for a file that no longer exists.
                File.Delete(fullPath + GeneratedFileSuffix);
                RemoveEmptyDirectories(rootDirectory, Path.GetDirectoryName(fullPath));
                pruned.Add(relativePath);
            }
            else
            {
                orphaned.Add(relativePath);
            }
        }

        // An orphan left in place stays in the manifest with the hash it was generated with: the
        // usual sequence is a run that reports it, then a run with --prune - which has to still
        // know the file is this tool's, and whether it has been touched since.
        foreach (var relativePath in orphaned)
        {
            current[relativePath] = previous[relativePath];
        }

        WriteManifest(rootDirectory, current);

        return new WriteToDiskResult(
            Ordered(written), Ordered(unchanged), Ordered(preserved), Ordered(pruned), Ordered(orphaned));
    }

    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content));

    private static Dictionary<string, string> ReadManifest(string rootDirectory)
    {
        var path = Path.Combine(rootDirectory, ManifestFileName);
        try
        {
            if (File.Exists(path)
                && JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path)) is { } manifest
                && manifest.TryGetValue("files", out var files))
            {
                return files;
            }
        }
        catch (JsonException)
        {
            // A damaged manifest only means nothing is known to be stale - never a reason to fail.
        }

        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static void WriteManifest(string rootDirectory, SortedDictionary<string, string> files)
    {
        var manifest = new Dictionary<string, SortedDictionary<string, string>> { ["files"] = files };
        File.WriteAllText(
            Path.Combine(rootDirectory, ManifestFileName),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    /// <summary>Removes <paramref name="directory"/> and its parents while they are empty, stopping at the root.</summary>
    private static void RemoveEmptyDirectories(string rootDirectory, string? directory)
    {
        var root = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar);
        while (directory is not null
               && !string.Equals(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar), root, StringComparison.Ordinal)
               && Directory.Exists(directory)
               && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
            directory = Path.GetDirectoryName(directory);
        }
    }

    private static void WriteOne(
        string rootDirectory,
        string relativePath,
        ExistingFileStrategy existingFiles,
        List<string> written,
        List<string> unchanged,
        List<string> preserved,
        Func<string, bool> matchesDisk,
        Action<string> write)
    {
        var fullPath = EnsureDirectoryFor(rootDirectory, relativePath);

        if (existingFiles == ExistingFileStrategy.Overwrite || !File.Exists(fullPath))
        {
            write(fullPath);
            written.Add(relativePath);
            return;
        }

        if (matchesDisk(fullPath))
        {
            unchanged.Add(relativePath);
            return;
        }

        write(fullPath + GeneratedFileSuffix);
        preserved.Add(relativePath);
    }

    private static List<string> Ordered(List<string> paths)
    {
        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    private static string EnsureDirectoryFor(string rootDirectory, string relativePath)
    {
        var fullPath = Path.Combine(rootDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return fullPath;
    }

    private static string Normalize(string relativePath)
        => relativePath.Replace('\\', '/').TrimStart('/');
}
