using System.Diagnostics;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class RealFormConversionBuildTests
{
    private static readonly string SampleAppsRoot = Path.Combine(AppContext.BaseDirectory, "SampleApps");

    [Theory]
    [InlineData("LegacyFrameworkApp", "LegacyFrameworkApp.csproj", "Views/Form1View.axaml")]
    [InlineData("ModernNetApp", "ModernNetApp.csproj", "Views/MainView.axaml")]
    [InlineData("TabControlApp", "TabControlApp.csproj", "Views/MainView.axaml")]
    [InlineData("DataGridViewApp", "DataGridViewApp.csproj", "Views/MainView.axaml")]
    [InlineData("ComplexApp", "ComplexApp.csproj", "Views/MainView.axaml")]
    [InlineData("NestedFormApp", "NestedFormApp.csproj", "Views/Forms/MainView.axaml")]
    public async Task ConvertedFixtureProject_BuildsSuccessfullyWithDotnetBuild(string appFolder, string csprojName, string expectedViewPath)
    {
        var sourceProject = Path.Combine(SampleAppsRoot, appFolder, csprojName);
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-realform-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            Assert.Contains(expectedViewPath, vfs.RelativePaths);
            Assert.True(File.Exists(Path.Combine(outputDir, "Controls", "Generated", "LayoutHint.cs")));

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
