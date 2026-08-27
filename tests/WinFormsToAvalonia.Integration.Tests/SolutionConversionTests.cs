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

            // A UserControl from the *other* project has no mapping yet, and says so rather than
            // being dropped silently.
            var shell = result.Converted.Single(c => c.SourceProjectPath.EndsWith("Shell.csproj", StringComparison.Ordinal));
            Assert.Contains(shell.Result.Report.Warnings, w => w.Contains("sharedPanel1") && w.Contains("no Avalonia mapping"));

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
