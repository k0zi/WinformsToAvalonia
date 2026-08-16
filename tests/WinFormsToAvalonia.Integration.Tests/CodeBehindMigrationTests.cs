using System.Diagnostics;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// End-to-end coverage of the code-behind migration split: one Form whose five handlers cover
/// every branch of the rule (promotable, Form-driving, EventArgs-using, no Avalonia equivalent,
/// Form lifecycle), converted and then actually `dotnet build`-ed. The build is the point - the
/// Avalonia XAML compiler rejects a Command binding to a missing ViewModel property or an event
/// attribute a control does not have, so only a green build proves the wiring is real.
/// </summary>
public class CodeBehindMigrationTests
{
    [Fact]
    public async Task ConvertedApp_SplitsHandlersBetweenViewModelAndCodeBehindAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "CodeBehindMigrationApp", "CodeBehindMigrationApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-codebehind-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            var vfs = result.Vfs;

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind);
            vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel);

            // greetButton_Click reads/writes only bindable value properties -> a RelayCommand,
            // bound from the AXAML, with an ObservableProperty per property it touches.
            Assert.Contains("Command=\"{Binding GreetButtonCommand}\"", axaml);
            Assert.Contains("[RelayCommand]", viewModel);
            Assert.Contains("private void GreetButton()", viewModel);
            Assert.Contains("public partial string NameTextBoxText { get; set; } = \"world\";", viewModel);
            Assert.Contains("public partial string GreetingLabelText { get; set; } = \"greeting\";", viewModel);
            Assert.Contains("Text=\"{Binding NameTextBoxText, Mode=TwoWay}\"", axaml);
            Assert.Contains("Text=\"{Binding GreetingLabelText, Mode=TwoWay}\"", axaml);

            // The designer literal moved to the ViewModel, so it is not also a plain attribute.
            Assert.DoesNotContain("Text=\"world\"", axaml);

            // clearButton_Click calls Close() -> stays event-driven.
            Assert.Contains("Click=\"clearButton_Click\"", axaml);
            Assert.Contains("private void clearButton_Click(object? sender, RoutedEventArgs e)", codeBehind);
            Assert.DoesNotContain("ClearButtonCommand", viewModel);

            // canvasPanel_MouseDown needs the pointer position -> stays event-driven.
            Assert.Contains("PointerPressed=\"canvasPanel_MouseDown\"", axaml);
            Assert.Contains("private void canvasPanel_MouseDown(object? sender, PointerPressedEventArgs e)", codeBehind);

            // Paint has no Avalonia equivalent: method emitted, nothing subscribed, warning raised.
            Assert.Contains("private void canvasPanel_Paint(object? sender, EventArgs e)", codeBehind);
            Assert.DoesNotContain("Paint=", axaml);
            Assert.Contains(result.Report.Warnings, w => w.Contains("Paint") && w.Contains("no Avalonia equivalent"));

            // Form.Load becomes the Window's Loaded event.
            Assert.Contains("Loaded=\"MainForm_Load\"", axaml);

            // Every original body survives, as a comment inside the method that replaced it.
            Assert.Contains("greetingLabel.Text = \"Hello, \" + nameTextBox.Text;", viewModel);
            Assert.Contains("panel.Text = e.X + \",\" + e.Y;", codeBehind);

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
