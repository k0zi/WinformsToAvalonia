using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// An ImageList's images, from the .resx blob they are locked in to files in the generated
/// project - and into the one place Avalonia will show them.
/// </summary>
public class ImageListConversionTests
{
    [Fact]
    public async Task ConvertedImageListApp_ExtractsTheImagesAndPutsThemOnTheMenuItems()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "ImageListApp", "ImageListApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-imagelist-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var result = pipeline.Run(options);

            // One file per image in the list, including the one nothing references: they cannot be
            // recovered from the .resx by hand, so they are worth having on disk either way.
            Assert.Contains("Assets/iconList_0.png", result.Vfs.RelativePaths);
            Assert.Contains("Assets/iconList_1.png", result.Vfs.RelativePaths);
            Assert.Contains("Assets/iconList_2.png", result.Vfs.RelativePaths);
            Assert.DoesNotContain("Assets/iconList_3.png", result.Vfs.RelativePaths);

            result.Vfs.TryGetText("Views/MainView.axaml", out var axaml);

            // The ImageList lives on the MenuStrip and the ImageIndex on each item, exactly as
            // WinForms resolves it.
            Assert.Matches(
                """<MenuItem x:Name="openMenuItem" Header="Open">\s*<MenuItem\.Icon>\s*<Image Source="/Assets/iconList_0\.png" />""",
                axaml);
            Assert.Matches(
                """<MenuItem x:Name="saveMenuItem" Header="Save">\s*<MenuItem\.Icon>\s*<Image Source="/Assets/iconList_2\.png" />""",
                axaml);

            // A TreeView takes an image in WinForms and has nowhere to put one in Avalonia. The
            // image is still extracted; what the conversion refuses to do is invent a header
            // layout for it, and it says so by name.
            Assert.Contains(
                result.Report.Warnings,
                w => w.Contains("treeView1", StringComparison.Ordinal)
                    && w.Contains("iconList_1.png", StringComparison.Ordinal));

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);

            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }
}
