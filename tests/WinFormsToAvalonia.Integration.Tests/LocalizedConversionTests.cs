using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A <c>Localizable=true</c> form sets every property - Text, Location, Size, Font - through
/// <c>resources.ApplyResources(...)</c> rather than in C#. Before the .resx was read, such a form
/// converted to a completely empty window that still compiled, which is exactly why a build-only
/// assertion is not enough here.
/// </summary>
public class LocalizedConversionTests
{
    [Fact]
    public async Task ConvertedLocalizedApp_TakesEveryPropertyFromTheResxAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "LocalizedApp", "LocalizedApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-localized-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));

            // $this entries configure the window itself.
            Assert.Contains("Title=\"Bejelentkezés\"", axaml);
            Assert.Contains("Width=\"284\"", axaml);
            Assert.Contains("Height=\"136\"", axaml);

            // Per-control text, geometry and styling, none of which appears in the designer C#.
            Assert.Contains("Text=\"Üdvözöljük!\"", axaml);
            Assert.Contains("Canvas.Left=\"12\"", axaml);
            Assert.Contains("Foreground=\"#FF005A9E\"", axaml);
            Assert.Contains("FontWeight=\"Bold\"", axaml);
            Assert.Contains("Content=\"Küldés\"", axaml);

            // Enum-typed entries reach the same LayoutHint treatment as designer-declared ones.
            Assert.Contains("w2a:LayoutHint.Anchor=\"Bottom,Right\"", axaml);

            // The image is recovered from its base64 payload and copied into Assets/.
            Assert.Contains("Source=\"/Assets/logoPictureBox_Image.png\"", axaml);
            var asset = Assert.Single(result.Vfs.BinaryFiles, f => f.Key == "Assets/logoPictureBox_Image.png");
            Assert.Equal([0x89, 0x50, 0x4E, 0x47], asset.Value[..4]);

            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("no .resx file was found", StringComparison.Ordinal));

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

    /// <summary>
    /// The same form without its .resx is the one case that genuinely cannot be converted - so it
    /// must say so loudly rather than quietly produce the empty window it used to.
    /// </summary>
    [Fact]
    public void LocalizedFormWithoutItsResx_ReportsThatEveryResourceBoundPropertyIsMissing()
    {
        var sourceDir = Path.Combine(AppContext.BaseDirectory, "SampleApps", "LocalizedApp");
        var strippedDir = Path.Combine(Path.GetTempPath(), "w2a-noresx-src-" + Guid.NewGuid());
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-noresx-out-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(strippedDir);
            foreach (var file in Directory.EnumerateFiles(sourceDir).Where(f => !f.EndsWith(".resx", StringComparison.Ordinal)))
            {
                File.Copy(file, Path.Combine(strippedDir, Path.GetFileName(file)));
            }

            var sourceProject = Path.Combine(strippedDir, "LocalizedApp.csproj");
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir, DryRun: true));

            var warning = Assert.Single(
                result.Report.Warnings,
                w => w.Contains("no .resx file was found", StringComparison.Ordinal));
            Assert.Contains("MainForm", warning, StringComparison.Ordinal);
        }
        finally
        {
            foreach (var dir in new[] { strippedDir, outputDir })
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
        }
    }

}
