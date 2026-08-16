using System.Diagnostics;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// UserControls used to be discovered but excluded from the conversion pipeline, so a Form
/// hosting one emitted a "no mapping registered" TODO comment where the control should be.
/// These tests cover both halves of the fix: the UserControl becomes its own Avalonia
/// UserControl View, and the hosting Form references it as a real element.
/// </summary>
public class UserControlConversionTests
{
    [Fact]
    public async Task ConvertedUserControlApp_EmitsAUserControlViewReferencedByTheHostFormAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "UserControlApp", "UserControlApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-usercontrol-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(
                new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir));
            var vfs = result.Vfs;

            Assert.Equal(1, result.Report.FormCount);
            Assert.Equal(1, result.Report.UserControlCount);

            // The UserControl's View mirrors its source subfolder, under Views/ like any Form.
            Assert.Contains("Views/Controls/CounterControlView.axaml", vfs.RelativePaths);
            Assert.Contains("Views/Controls/CounterControlView.axaml.cs", vfs.RelativePaths);
            Assert.Contains("ViewModels/Controls/CounterControlViewModel.cs", vfs.RelativePaths);

            vfs.TryGetText("Views/Controls/CounterControlView.axaml", out var userControlAxaml);
            Assert.StartsWith("<UserControl ", userControlAxaml);
            Assert.DoesNotContain("Title=", userControlAxaml);

            vfs.TryGetText("Views/Controls/CounterControlView.axaml.cs", out var userControlCodeBehind);
            Assert.Contains("public partial class CounterControlView : UserControl", userControlCodeBehind);

            // The host Form references the generated View instead of reporting it unmapped.
            vfs.TryGetText("Views/MainView.axaml", out var mainAxaml);
            Assert.Contains(":CounterControlView x:Name=\"counterControl1\"", mainAxaml);
            Assert.DoesNotContain("TODO(Winforms2Avalonia)", mainAxaml);

            // Only the Form can be the startup Window - a UserControl is not one.
            vfs.TryGetText("App.axaml.cs", out var appCodeBehind);
            Assert.Contains("desktop.MainWindow = new MainView();", appCodeBehind);

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
