namespace WinFormsToAvalonia.Core.Scaffolding;

/// <summary>
/// In-memory staging area for generated project files. Every emitter writes here first;
/// this is what makes --dry-run trivial (render without touching disk) and what tests
/// assert against without filesystem races.
/// </summary>
public sealed class VirtualFileSystem
{
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

    public void WriteToDisk(string rootDirectory)
    {
        Directory.CreateDirectory(rootDirectory);

        foreach (var (relativePath, content) in _textFiles)
        {
            File.WriteAllText(EnsureDirectoryFor(rootDirectory, relativePath), content);
        }

        foreach (var (relativePath, content) in _binaryFiles)
        {
            File.WriteAllBytes(EnsureDirectoryFor(rootDirectory, relativePath), content);
        }
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
