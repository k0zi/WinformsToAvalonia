using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// WinForms' BackColor/ForeColor/Font/Padding survive the conversion, but only onto elements
/// that actually define them. A plain build test is not enough here: emitting *nothing* would
/// also build fine, and emitting a style attribute on the wrong target is an Avalonia XAML
/// compile error (AVLN2000) that only a real <c>dotnet build</c> of the output can catch - so
/// this test asserts both halves.
/// </summary>
public class VisualStyleConversionTests
{
    [Fact]
    public async Task ConvertedVisualStyleApp_EmitsStylingPerTargetElementAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "VisualStyleApp", "VisualStyleApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-visualstyle-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;
            Assert.True(vfs.TryGetText("Views/MainView.axaml", out var axaml));

            // The Form's own styling lands on the root Window - a WinForms Form's Font is
            // inherited by every child that never overrode it, and so is Avalonia's.
            Assert.Contains("Background=\"#FFF0F0F0\"", axaml);

            // TextBlock: the full surface, including the TextDecorations that carry
            // WinForms' Underline font style.
            Assert.Contains("Foreground=\"#FF1E90FF\"", axaml);
            Assert.Contains("FontWeight=\"Bold\"", axaml);
            Assert.Contains("FontStyle=\"Italic\"", axaml);
            Assert.Contains("TextDecorations=\"Underline\"", axaml);

            // TemplatedControl: background, foreground and padding all apply. 10pt -> 13.33px.
            Assert.Contains("Background=\"#FF0078D7\"", axaml);
            Assert.Contains("FontSize=\"13.33\"", axaml);
            Assert.Contains("Padding=\"6,2,6,2\"", axaml);

            // The tinted Panel maps to a Canvas, which has a Background but neither a
            // Foreground nor any font property - those must be dropped, not guessed at.
            var panelElement = SingleElementContaining(axaml, "x:Name=\"tintedPanel\"");
            Assert.Contains("Background=\"#FFFFFFE0\"", panelElement);
            Assert.DoesNotContain("Foreground=", panelElement);
            Assert.DoesNotContain("FontFamily=", panelElement);

            // Avalonia's Image derives straight from Control: no styling surface at all. Its
            // picture, though, comes through - recovered from the resources.GetObject(...)
            // payload in the form's .resx and copied into Assets/.
            var imageElement = SingleElementContaining(axaml, "x:Name=\"logoPictureBox\"");
            Assert.DoesNotContain("Background=", imageElement);
            Assert.Contains("Source=\"/Assets/logoPictureBox_Image.png\"", imageElement);
            Assert.Contains("Assets/logoPictureBox_Image.png", vfs.RelativePaths);

            // GroupBox stopped being a bundled fallback when Avalonia 12 shipped a real one, and
            // a real TemplatedControl carries the whole styling surface - so the designer's
            // BackColor and bold Font, previously dropped on the floor, now come through.
            var groupBoxElement = SingleElementContaining(axaml, "x:Name=\"styledGroupBox\"");
            Assert.Contains("Background=", groupBoxElement);
            Assert.Contains("FontWeight=", groupBoxElement);

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

    /// <summary>The single AXAML element carrying <paramref name="marker"/>, from its '&lt;' to its '&gt;'.</summary>
    private static string SingleElementContaining(string axaml, string marker)
    {
        var markerIndex = axaml.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"'{marker}' not found in the generated AXAML:\n{axaml}");

        var start = axaml.LastIndexOf('<', markerIndex);
        var end = axaml.IndexOf('>', markerIndex);
        return axaml[start..end];
    }

}
