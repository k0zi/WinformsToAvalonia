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
public sealed record WriteToDiskResult(
    IReadOnlyList<string> Written,
    IReadOnlyList<string> Unchanged,
    IReadOnlyList<string> Preserved);

/// <summary>
/// In-memory staging area for generated project files. Every emitter writes here first;
/// this is what makes --dry-run trivial (render without touching disk) and what tests
/// assert against without filesystem races.
/// </summary>
public sealed class VirtualFileSystem
{
    /// <summary>Appended to a generated file whose real path already holds different content.</summary>
    public const string GeneratedFileSuffix = ".w2a-new";

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
    public WriteToDiskResult WriteToDisk(
        string rootDirectory, ExistingFileStrategy existingFiles = ExistingFileStrategy.PreserveExisting)
    {
        Directory.CreateDirectory(rootDirectory);

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

        return new WriteToDiskResult(Ordered(written), Ordered(unchanged), Ordered(preserved));
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
