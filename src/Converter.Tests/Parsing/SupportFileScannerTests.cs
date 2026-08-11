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
    public async Task ScanAsync_FileWithBothSafeAndUnsafeType_CopiesSafeTypeAndFlagsUnsafeOne()
    {
        // Mirrors the real WarehouseApp sample: a "BadgeStyle" enum and a "StatusBadgeControl :
        // Control" class declared in the same file - other migrated code (e.g. a ViewModel with
        // "using WarehouseApp.Controls;") depends on the harmless enum, so it must survive even
        // though the owner-drawn control next to it can't be copied as-is.
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

            var copied = Assert.Single(result.CopyableFiles);
            Assert.Contains("public enum BadgeStyle", copied.TransformedContent);
            Assert.DoesNotContain("StatusBadgeControl", copied.TransformedContent);

            var skippedEntry = Assert.Single(result.SkippedFiles);
            Assert.Contains("StatusBadgeControl", skippedEntry.Reason);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_MultipleTypesAllUnsafe_SkipsWholeFileNotPartial()
    {
        // Every type declaration is unsafe - nothing to split, the whole file stays skipped
        // exactly like the single-type case (no partial-copy manual step for an empty result).
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "TwoControls.cs");
            await File.WriteAllTextAsync(path, """
                namespace WarehouseApp.Controls;

                public class FirstControl : Control
                {
                }

                public class SecondControl : Control
                {
                }
                """);

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Empty(result.CopyableFiles);
            var skippedEntry = Assert.Single(result.SkippedFiles);
            Assert.DoesNotContain("Partially copied", skippedEntry.Reason);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_MultipleTypesAllSafe_CopiesWholeFileUnchanged()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "Dtos.cs");
            await File.WriteAllTextAsync(path, """
                namespace WarehouseApp.Common;

                public enum Priority { Low, High }

                public class OrderDto
                {
                }
                """);

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            var copied = Assert.Single(result.CopyableFiles);
            Assert.Null(copied.TransformedContent);
            Assert.Empty(result.SkippedFiles);
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

    [Fact]
    public async Task ScanAsync_PlainUtilityClass_CopyableFileHasNoTransformedContent()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "Db.cs");
            await File.WriteAllTextAsync(path, "namespace WarehouseApp.Common;\n\npublic static class Db\n{\n}\n");

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            var file = Assert.Single(result.CopyableFiles);
            Assert.Null(file.TransformedContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_StaticHelperUsingWinFormsUiTypesWithoutDerivingFromAnything_IsSkippedWithSpecificTypes()
    {
        // The real Common/InputBoxHelper.cs shape - a static class building a WinForms Form
        // imperatively, without itself deriving from anything, so the base-type-only check
        // alone would have let it slip through and get copied broken (the exact silent-failure
        // bug this test guards against).
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "InputBoxHelper.cs");
            await File.WriteAllTextAsync(path, """
                namespace WarehouseApp.Common;

                public static class InputBoxHelper
                {
                    public static string? Show(IWin32Window owner, string title)
                    {
                        using var form = new Form { Text = title };
                        var label = new Label { Text = "Value" };
                        var textBox = new TextBox();
                        var okButton = new Button { DialogResult = DialogResult.OK };
                        return form.ShowDialog(owner) == DialogResult.OK ? textBox.Text : null;
                    }
                }
                """);

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Empty(result.CopyableFiles);
            var skipped = Assert.Single(result.SkippedFiles);
            Assert.Contains("Form", skipped.Reason);
            Assert.Contains("no Avalonia equivalent", skipped.Reason);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_GdiDrawingFileWithinRecognizedVocabulary_IsCopyableWithTransformedContent()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "AppIcons.cs");
            await File.WriteAllTextAsync(path, """
                namespace WarehouseApp.Common;

                public static class AppIcons
                {
                    public static Bitmap CreateLogo(int size = 64)
                    {
                        var bmp = new Bitmap(size, size);
                        using var g = Graphics.FromImage(bmp);
                        using var bgBrush = new SolidBrush(Color.White);
                        g.FillEllipse(bgBrush, 0, 0, size, size);
                        return bmp;
                    }
                }
                """);

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            var file = Assert.Single(result.CopyableFiles);
            Assert.NotNull(file.TransformedContent);
            Assert.Contains("RenderTargetBitmap", file.TransformedContent);
            Assert.DoesNotContain("System.Drawing", file.TransformedContent);
            Assert.Empty(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_GdiDrawingFileOutsideRecognizedVocabulary_IsSkippedNotCopiedBroken()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "Weird.cs");
            await File.WriteAllTextAsync(path, """
                namespace WarehouseApp.Common;

                public static class Weird
                {
                    public static Bitmap DrawSomething(Bitmap source)
                    {
                        var bmp = new Bitmap(10, 10);
                        using var g = Graphics.FromImage(bmp);
                        g.DrawImage(source, 0, 0);
                        return bmp;
                    }
                }
                """);

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            Assert.Empty(result.CopyableFiles);
            var skipped = Assert.Single(result.SkippedFiles);
            Assert.Contains("GDI+", skipped.Reason);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_ColorOnlyFile_IsCopyableWithRewrittenColorReferences()
    {
        var sourceDir = CreateSourceDir();
        try
        {
            var path = Path.Combine(sourceDir, "Palette.cs");
            await File.WriteAllTextAsync(path, """
                using System.Drawing;

                namespace WarehouseApp.Common;

                public static class Palette
                {
                    public static Color Highlight => Color.CornflowerBlue;
                }
                """);

            var result = await SupportFileScanner.ScanAsync(sourceDir, new HashSet<string>(), []);

            var file = Assert.Single(result.CopyableFiles);
            Assert.NotNull(file.TransformedContent);
            Assert.Contains("Colors.CornflowerBlue", file.TransformedContent);
            Assert.Empty(result.SkippedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }
}
