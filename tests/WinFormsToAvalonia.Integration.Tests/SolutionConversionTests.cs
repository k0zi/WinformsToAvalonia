using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Converting a whole solution rather than one project at a time - the limit
/// <c>samples/convert.sh</c> exists to work around.
/// </summary>
public class SolutionConversionTests
{
    [Fact]
    public async Task ConvertedSolution_ConvertsEveryWinFormsProjectAndBuilds()
    {
        var solutionPath = Path.Combine(AppContext.BaseDirectory, "SampleApps", "MultiProjectSolution", "MultiProjectSolution.slnx");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-solution-" + Guid.NewGuid());
        try
        {
            var result = new SolutionConversionPipeline().Run(new ConversionOptions(solutionPath, outputDir));

            Assert.Equal(
                ["Shell.csproj", "Widgets.csproj"],
                result.Converted.Select(c => Path.GetFileName(c.SourceProjectPath)).OrderBy(n => n, StringComparer.Ordinal));

            // Each project lands in its own folder, and one generated solution ties them together.
            Assert.Equal("MultiProjectSolution.slnx", result.SolutionFileName);
            var solutionFile = Path.Combine(outputDir, result.SolutionFileName);
            Assert.True(File.Exists(solutionFile));

            var solutionText = await File.ReadAllTextAsync(solutionFile);
            Assert.Contains("Shell/Shell.csproj", solutionText);
            Assert.Contains("Widgets/Widgets.csproj", solutionText);

            // The hard half: Shell's Form hosts a UserControl that Widgets defines. It has to
            // resolve to an element - through the assembly-qualified xmlns form, since `using:`
            // can only name the assembly being compiled - and Shell has to reference Widgets.
            var shell = result.Converted.Single(c => c.SourceProjectPath.EndsWith("Shell.csproj", StringComparison.Ordinal));
            Assert.DoesNotContain(shell.Result.Report.Warnings, w => w.Contains("sharedPanel1"));

            var mainView = await File.ReadAllTextAsync(Path.Combine(shell.OutputDirectory, "Views", "MainView.axaml"));
            Assert.Contains("xmlns:uc0=\"clr-namespace:Widgets.Views.Controls;assembly=Widgets\"", mainView);
            Assert.Contains("<uc0:SharedPanelView", mainView);

            var shellCsproj = await File.ReadAllTextAsync(Path.Combine(shell.OutputDirectory, "Shell.csproj"));
            Assert.Contains("<ProjectReference Include=\"..\\Widgets\\Widgets.csproj\" />", shellCsproj);

            // ...and not the other way round: Widgets names nothing of Shell's.
            var widgets = result.Converted.Single(c => c.SourceProjectPath.EndsWith("Widgets.csproj", StringComparison.Ordinal));
            Assert.DoesNotContain("ProjectReference", await File.ReadAllTextAsync(Path.Combine(widgets.OutputDirectory, "Widgets.csproj")));

            // The point of the whole thing: `dotnet build` on the *generated solution*.
            var buildResult = await DotnetRunner.RunAsync($"build {result.SolutionFileName}", outputDir);
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

    /// <summary>Nothing reaches disk, the generated solution file included.</summary>
    [Fact]
    public void ConvertedSolution_WritesNothingOnADryRun()
    {
        var solutionPath = Path.Combine(AppContext.BaseDirectory, "SampleApps", "MultiProjectSolution", "MultiProjectSolution.slnx");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-solution-dry-" + Guid.NewGuid());

        var result = new SolutionConversionPipeline().Run(new ConversionOptions(solutionPath, outputDir) { DryRun = true });

        Assert.Equal(2, result.Converted.Count);
        Assert.False(Directory.Exists(outputDir));
    }

}
