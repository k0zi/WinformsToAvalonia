using System.Diagnostics;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class SplitContainerAndToolTipConversionTests
{
    [Fact]
    public async Task ConvertedSplitContainerApp_EmitsGridSplitterLayoutAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "SplitContainerApp", "SplitContainerApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-splitcontainer-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("ColumnDefinitions=\"*,Auto,*\"", axaml);
            Assert.Contains("<GridSplitter", axaml);
            Assert.Contains("<Button x:Name=\"leftButton\"", axaml);
            Assert.Contains("<TextBlock x:Name=\"rightLabel\"", axaml);

            var buildResult = await RunDotnetAsync("build", outputDir);

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

    [Fact]
    public async Task ConvertedToolTipApp_EmitsToolTipTipAttributeAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "ToolTipApp", "ToolTipApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-tooltip-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("ToolTip.Tip=\"Click to confirm\"", axaml);

            var buildResult = await RunDotnetAsync("build", outputDir);

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

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunDotnetAsync(string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await stdOutTask, await stdErrTask);
    }
}
