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

    private static VirtualFileSystem Vfs(params (string Path, string Content)[] files)
    {
        var vfs = new VirtualFileSystem();
        foreach (var (path, content) in files)
        {
            vfs.AddText(path, content);
        }

        return vfs;
    }

    /// <summary>
    /// A Form removed from the source: its View was generated last time and is not now. Without
    /// --prune it is only reported; with it, it goes - folder included once empty.
    /// </summary>
    [Fact]
    public void WriteToDisk_StaleGeneratedFile_IsReportedThenPrunedWithItsEmptyFolder()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "w2a-vfs-prune-" + Guid.NewGuid());
        try
        {
            Vfs(("App.axaml", "<Application/>"), ("Views/Old/OldView.axaml", "<Window/>")).WriteToDisk(tempDir);
            Assert.True(File.Exists(Path.Combine(tempDir, VirtualFileSystem.ManifestFileName)));

            var reported = Vfs(("App.axaml", "<Application/>")).WriteToDisk(tempDir);
            Assert.Equal(["Views/Old/OldView.axaml"], reported.Orphaned);
            Assert.Empty(reported.Pruned);
            Assert.True(File.Exists(Path.Combine(tempDir, "Views", "Old", "OldView.axaml")));

            // The usual sequence: the run that reported it, then a run with --prune - which still
            // has to know the file is this tool's own.
            var pruned = Vfs(("App.axaml", "<Application/>")).WriteToDisk(tempDir, prune: true);

            Assert.Equal(["Views/Old/OldView.axaml"], pruned.Pruned);
            Assert.Empty(pruned.Orphaned);
            Assert.False(Directory.Exists(Path.Combine(tempDir, "Views")));
            Assert.True(File.Exists(Path.Combine(tempDir, "App.axaml")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>An edited stale file is a human's work: --prune never deletes it.</summary>
    [Fact]
    public void WriteToDisk_StaleFileEditedSinceGeneration_IsNeverPruned()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "w2a-vfs-prune-" + Guid.NewGuid());
        try
        {
            Vfs(("App.axaml", "<Application/>"), ("Views/OldView.axaml.cs", "// generated")).WriteToDisk(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "Views", "OldView.axaml.cs"), "// migrated by hand");

            var result = Vfs(("App.axaml", "<Application/>")).WriteToDisk(tempDir, prune: true);

            Assert.Empty(result.Pruned);
            Assert.Equal(["Views/OldView.axaml.cs"], result.Orphaned);
            Assert.Equal("// migrated by hand", File.ReadAllText(Path.Combine(tempDir, "Views", "OldView.axaml.cs")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
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

    [Fact]
    public void WriteToDisk_FreshDirectory_ReportsEverythingAsWritten()
    {
        var vfs = new VirtualFileSystem();
        vfs.AddText("App.axaml", "<Application/>");
        vfs.AddBinary("Assets/icon.ico", [1, 2, 3]);

        WithTempDir(dir =>
        {
            var result = vfs.WriteToDisk(dir);

            Assert.Equal(["App.axaml", "Assets/icon.ico"], result.Written);
            Assert.Empty(result.Unchanged);
            Assert.Empty(result.Preserved);
        });
    }

    /// <summary>
    /// The whole point of the default strategy: a re-run after a human has started migrating the
    /// generated code must not delete that work.
    /// </summary>
    [Fact]
    public void WriteToDisk_ExistingFileWithDifferentContent_KeepsItAndWritesTheGeneratedVersionAlongside()
    {
        var vfs = new VirtualFileSystem();
        vfs.AddText("Views/MainView.axaml.cs", "regenerated");

        WithTempDir(dir =>
        {
            var target = Path.Combine(dir, "Views", "MainView.axaml.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, "hand-migrated by a human");

            var result = vfs.WriteToDisk(dir);

            Assert.Equal("hand-migrated by a human", File.ReadAllText(target));
            Assert.Equal("regenerated", File.ReadAllText(target + VirtualFileSystem.GeneratedFileSuffix));
            Assert.Equal(["Views/MainView.axaml.cs"], result.Preserved);
            Assert.Empty(result.Written);
        });
    }

    [Fact]
    public void WriteToDisk_ExistingFileWithIdenticalContent_IsLeftAloneWithNoSidecar()
    {
        var vfs = new VirtualFileSystem();
        vfs.AddText("App.axaml", "<Application/>");

        WithTempDir(dir =>
        {
            var target = Path.Combine(dir, "App.axaml");
            Directory.CreateDirectory(dir);
            File.WriteAllText(target, "<Application/>");

            var result = vfs.WriteToDisk(dir);

            Assert.Equal(["App.axaml"], result.Unchanged);
            Assert.Empty(result.Preserved);
            Assert.False(File.Exists(target + VirtualFileSystem.GeneratedFileSuffix));
        });
    }

    [Fact]
    public void WriteToDisk_OverwriteStrategy_ReplacesTheExistingFile()
    {
        var vfs = new VirtualFileSystem();
        vfs.AddText("Views/MainView.axaml.cs", "regenerated");

        WithTempDir(dir =>
        {
            var target = Path.Combine(dir, "Views", "MainView.axaml.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, "hand-migrated by a human");

            var result = vfs.WriteToDisk(dir, ExistingFileStrategy.Overwrite);

            Assert.Equal("regenerated", File.ReadAllText(target));
            Assert.Equal(["Views/MainView.axaml.cs"], result.Written);
            Assert.Empty(result.Preserved);
            Assert.False(File.Exists(target + VirtualFileSystem.GeneratedFileSuffix));
        });
    }

    /// <summary>Binary assets go down the same path, compared by bytes rather than by text.</summary>
    [Fact]
    public void WriteToDisk_ExistingBinaryAssetWithDifferentBytes_IsPreserved()
    {
        var vfs = new VirtualFileSystem();
        vfs.AddBinary("Assets/icon.ico", [1, 2, 3]);

        WithTempDir(dir =>
        {
            var target = Path.Combine(dir, "Assets", "icon.ico");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, [9, 9, 9]);

            var result = vfs.WriteToDisk(dir);

            Assert.Equal([9, 9, 9], File.ReadAllBytes(target));
            Assert.Equal(["Assets/icon.ico"], result.Preserved);
        });
    }

    private static void WithTempDir(Action<string> body)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "w2a-vfs-test-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            body(tempDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
