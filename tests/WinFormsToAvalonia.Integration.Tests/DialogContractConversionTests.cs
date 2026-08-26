using System.Diagnostics;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// The hand-written half of the dialog-result contract, plus the two other places this
/// conversion has to talk about something it created itself: the Window a View *is*, and the
/// DispatcherTimer it emits for a WinForms Timer.
/// </summary>
public class DialogContractConversionTests
{
    [Fact]
    public async Task ConvertedDialogContractApp_ClosesWithItsResultAndStillBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "DialogContractApp", "DialogContractApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-dialog-contract-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));

            Assert.True(result.Vfs.TryGetText("Views/Dialogs/ConfirmView.axaml.cs", out var dialog));

            // `DialogResult = ...; Close();` is one act: two statements in WinForms, one
            // Close(true) here. A bare Close() left behind would reset the result to
            // default(bool) and undo the line above it.
            Assert.Contains("Close(true);", dialog);
            Assert.DoesNotContain("Close(true);\n        Close();", dialog.Replace("\r\n", "\n"));
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(okButton_Click)", dialog);

            // The assignment on its own already closes a modal form in WinForms - and the bare
            // spelling must stay in code-behind to be translatable at all, rather than being
            // promoted to a ViewModel that has no window to close.
            Assert.Contains("Close(false);", dialog);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(cancelButton_Click)", dialog);
            Assert.True(result.Vfs.TryGetText("ViewModels/Dialogs/ConfirmViewModel.cs", out var dialogViewModel));
            Assert.DoesNotContain("CancelButton", dialogViewModel);

            // Work after the assignment is not equivalent: WinForms keeps running the handler,
            // Avalonia's Close is the end of it.
            Assert.Contains("MigrationTodo.NotMigrated(nameof(applyButton_Click)", dialog);

            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var main));

            // Window properties, including the enum the two frameworks spell differently.
            Assert.Contains("Title = \"Maximized\";", main);
            Assert.Contains("WindowState = WindowState.Maximized;", main);
            Assert.Contains("statusLabel.Text = (Title ?? string.Empty);", main);

            // A window property on the local holding another converted Form's View, then that
            // dialog's bool result driving the branch.
            Assert.Contains("dialog.Title = \"Confirm the change\";", main);
            Assert.Contains("if (await dialog.ShowDialog<bool>(this))", main);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(configureButton_Click)", main);

            // The DispatcherTimer this conversion declares is something a body may now name.
            Assert.Contains("clockTimer.IsEnabled = !clockTimer.IsEnabled;", main);
            Assert.Contains("clockTimer.Interval = TimeSpan.FromMilliseconds(500);", main);
            Assert.Contains("clockTimer.Stop();", main);

            var buildResult = await RunDotnetAsync("build", outputDir);
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
