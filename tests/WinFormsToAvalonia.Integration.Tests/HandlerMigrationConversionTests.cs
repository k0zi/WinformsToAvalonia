using System.Diagnostics;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// End-to-end cover for the statement-level handler translation. The build assertion matters as
/// much as the content ones here: the emitted statements are real code now, and MessageBoxFallback
/// is a bundled template that is never compiled by the tool's own build - a converted project is
/// the only thing that proves either of them works.
/// </summary>
public class HandlerMigrationConversionTests
{
    [Fact]
    public async Task ConvertedHandlerMigrationApp_TranslatesWhatItCanAndStillBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "HandlerMigrationApp", "HandlerMigrationApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-handlers-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

            // Fully translated: two bindable writes and the Window's own Close(), so no marker.
            Assert.Contains("statusLabel.Text = \"Accepted\";", codeBehind);
            Assert.Contains("nameTextBox.Text = string.Empty;", codeBehind);
            Assert.Contains("Close();", codeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(okButton_Click)", codeBehind);

            // A message box is a dialog: the handler stays in code-behind and turns async.
            Assert.Contains("private async void aboutButton_Click", codeBehind);
            Assert.Contains("await MessageBoxFallback.ShowAsync(this, \"Handler migration demo\", \"About\");", codeBehind);
            Assert.Contains("Controls/MessageBoxFallback.cs", result.Vfs.RelativePaths);

            // Reads are null-guarded, so the generated project stays warning-free under nullable.
            Assert.Contains("counterLabel.Text = (int.Parse((counterLabel.Text ?? string.Empty)) + 1).ToString();", codeBehind);
            Assert.Contains("nameTextBox.Focus();", codeBehind);

            // The prefix rule: statement one lands as code, the unknown call stops the rest.
            Assert.Contains("statusLabel.Text = \"Saving\";", codeBehind);
            Assert.Contains("REMAINING WINFORMS BODY of 'saveButton_Click'", codeBehind);
            Assert.Contains("PersistToDisk();", codeBehind);

            // A body needing the pointer position cannot be translated at all.
            Assert.Contains("ORIGINAL WINFORMS BODY of 'canvas_MouseDown'", codeBehind);

            Assert.Equal(7, result.Report.MigratedStatementCount);
            Assert.Equal(10, result.Report.HandlerStatementCount);

            var buildResult = await RunDotnetAsync("build", outputDir);
            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");

            // Generated projects have always compiled warning-free; translated statements must
            // not change that (a bare Avalonia string property read would be a CS8604).
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
