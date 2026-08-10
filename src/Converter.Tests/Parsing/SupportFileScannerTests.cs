using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class SupportFileScannerTests
{
    private static string CreateSourceDir() => Directory.CreateTempSubdirectory("wf2av-supportfiles-").FullName;

    [Fact]
    public async Task ScanAsync_PlainUtilityClass_IsCopyable()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var commonDir = Directory.CreateDirectory(Path.Combine(sourceDir, "Common")).FullName;
            var path = Path.Combine(commonDir, "Db.cs");
            await File.WriteAllTextAsync(path, "namespace WarehouseApp.Common;\n\npublic static class Db\n{\n}\n");

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Single(result.CopyableFiles, f => f.RelativePath == Path.Combine("Common", "Db.cs"));
            Assert.Empty(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Theory]
    [InlineData("Form")]
    [InlineData("Control")]
    [InlineData("UserControl")]
    [InlineData("Component")]
    [InlineData("ContainerControl")]
    public async Task ScanAsync_ClassDerivingFromWinFormsUiBaseType_IsSkipped(string baseType)
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var controlsDir = Directory.CreateDirectory(Path.Combine(sourceDir, "Controls")).FullName;
            var path = Path.Combine(controlsDir, "Widget.cs");
            await File.WriteAllTextAsync(path, $"namespace WarehouseApp.Controls;\n\npublic class Widget : {baseType}\n{{\n}}\n");

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Empty(result.CopyableFiles);
            var skipped = Assert.Single(result.SkippedFiles);
            Assert.Contains(baseType, skipped.Reason);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_QualifiedWinFormsBaseType_IsDetected()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "Widget.cs");
            await File.WriteAllTextAsync(
                path, "namespace WarehouseApp.Controls;\n\npublic class Widget : System.Windows.Forms.Control\n{\n}\n");

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Empty(result.CopyableFiles);
            Assert.Single(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_FileWithBothSafeAndUnsafeType_SkipsWholeFile()
    {
        // Mirrors the real WarehouseApp sample: a "BadgeStyle" enum and a "StatusBadgeControl :
        // Control" class declared in the same file - file-level granularity means the whole
        // file is skipped, not just the unsafe declaration.
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "StatusBadgeControl.cs");
            await File.WriteAllTextAsync(path, """
                namespace WarehouseApp.Controls;

                public enum BadgeStyle { Success, Warning }

                public class StatusBadgeControl : Control
                {
                }
                """);

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Empty(result.CopyableFiles);
            Assert.Single(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_HandledFilePath_IsExcludedFromBothLists()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "Form1.cs");
            await File.WriteAllTextAsync(path, "namespace SampleApp;\n\npartial class Form1\n{\n}\n");

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string> { path }, []);

            Assert.Empty(result.CopyableFiles);
            Assert.Empty(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_ProgramCs_IsExcludedFromBothLists()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "Program.cs");
            await File.WriteAllTextAsync(path, "class Program { static void Main() { } }");

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Empty(result.CopyableFiles);
            Assert.Empty(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_FileUnderObjDirectory_IsExcluded()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var objDir = Directory.CreateDirectory(Path.Combine(sourceDir, "obj", "Debug")).FullName;
            var path = Path.Combine(objDir, "MyApp.AssemblyInfo.cs");
            await File.WriteAllTextAsync(path, "// generated");

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Empty(result.CopyableFiles);
            Assert.Empty(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_FileMatchingExcludePattern_IsExcluded()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var legacyDir = Directory.CreateDirectory(Path.Combine(sourceDir, "Legacy")).FullName;
            var path = Path.Combine(legacyDir, "OldHelper.cs");
            await File.WriteAllTextAsync(path, "namespace Legacy;\n\npublic static class OldHelper { }\n");

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), ["Legacy"]);

            Assert.Empty(result.CopyableFiles);
            Assert.Empty(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_UnparseableFile_IsSkippedWithReason()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "Broken.cs");
            // Roslyn's error-tolerant parser rarely throws on garbage text (it just yields no
            // matching type declarations), so exercising the actual catch block reliably needs
            // an unreadable file rather than merely-invalid syntax.
            await File.WriteAllTextAsync(path, "public class Fine { }");
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

                Assert.Empty(result.CopyableFiles);
                Assert.Single(result.SkippedFiles);
            }
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }
}
