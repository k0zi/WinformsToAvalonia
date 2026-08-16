using WinFormsToAvalonia.Core.Scaffolding;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Scaffolding;

public class VirtualFileSystemTests
{
    [Fact]
    public void AddText_NormalizesBackslashesAndLeadingSlash()
    {
        var vfs = new VirtualFileSystem();

        vfs.AddText(@"Views\Sub\Foo.axaml", "content");
        vfs.AddText("/Rooted/Bar.cs", "content2");

        Assert.Contains("Views/Sub/Foo.axaml", vfs.RelativePaths);
        Assert.Contains("Rooted/Bar.cs", vfs.RelativePaths);
    }

    [Fact]
    public void WriteToDisk_CreatesNestedDirectoriesAndFiles()
    {
        var vfs = new VirtualFileSystem();
        vfs.AddText("App.axaml", "<Application/>");
        vfs.AddText("Views/MainWindowView.axaml", "<Window/>");

        var tempDir = Path.Combine(Path.GetTempPath(), "w2a-vfs-test-" + Guid.NewGuid());
        try
        {
            vfs.WriteToDisk(tempDir);

            Assert.True(File.Exists(Path.Combine(tempDir, "App.axaml")));
            Assert.True(File.Exists(Path.Combine(tempDir, "Views", "MainWindowView.axaml")));
            Assert.Equal("<Application/>", File.ReadAllText(Path.Combine(tempDir, "App.axaml")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
