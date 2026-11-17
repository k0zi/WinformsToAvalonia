namespace WinFormsToAvalonia.Core.Tests.TestSupport;

/// <summary>Creates a throwaway directory for a synthetic project fixture and deletes it on dispose.</summary>
public sealed class TempProjectFixture : IDisposable
{
    private readonly string _rootDirectory;

    private TempProjectFixture(string rootDirectory) => _rootDirectory = rootDirectory;

    public static TempProjectFixture Create()
    {
        var dir = Path.Combine(Path.GetTempPath(), "w2a-fixture-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        return new TempProjectFixture(dir);
    }

    public string PathTo(string relativePath) => Path.GetFullPath(Path.Combine(_rootDirectory, relativePath));

    public void WriteFile(string relativePath, string content)
    {
        var fullPath = PathTo(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
