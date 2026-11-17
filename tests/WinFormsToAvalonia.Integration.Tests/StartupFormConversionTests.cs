using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// The converted app has to start with the Form the original did. It used to start with the
/// first Form discovery listed - here <c>AboutForm</c>, which sorts ahead of <c>MainForm</c> - and
/// that built, launched and showed the wrong window with nothing anywhere saying so.
/// </summary>
public class StartupFormConversionTests
{
    private static readonly string SourceProject = Path.Combine(
        AppContext.BaseDirectory, "SampleApps", "StartupFormApp", "StartupFormApp.csproj");

    [Fact]
    public async Task ConvertedApp_StartsWithTheFormApplicationRunNames()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-startup-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(SourceProject, outputDir));

            Assert.True(result.Vfs.TryGetText("App.axaml.cs", out var app));
            Assert.Contains("desktop.MainWindow = new MainView();", app);
            Assert.DoesNotContain("new AboutView()", app);

            // Read from Program.cs with certainty, so there is nothing to report.
            Assert.DoesNotContain(result.Report.Warnings, w => w.StartsWith("Startup form:", StringComparison.Ordinal));

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);
            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
            Assert.DoesNotContain(": warning ", buildResult.StdOut);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void WithWeb_SplitsTheStartupForm_NotTheFirstOne()
    {
        // The --with-web split and the main window are one decision: splitting AboutForm while
        // App.axaml.cs opened MainView would hand the browser head a View nothing shows.
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-startup-web-" + Guid.NewGuid());
        var result = new ConversionPipeline().Run(new ConversionOptions(SourceProject, outputDir, DryRun: true, WithWeb: true));

        var mainAxaml = result.Vfs.RelativePaths.Single(p => p.EndsWith("/Views/MainView.axaml", StringComparison.Ordinal));
        var aboutAxaml = result.Vfs.RelativePaths.Single(p => p.EndsWith("/Views/AboutView.axaml", StringComparison.Ordinal));
        Assert.True(result.Vfs.TryGetText(mainAxaml, out var main));
        Assert.True(result.Vfs.TryGetText(aboutAxaml, out var about));

        Assert.StartsWith("<UserControl", main.TrimStart());
        Assert.StartsWith("<Window", about.TrimStart());
    }
}
