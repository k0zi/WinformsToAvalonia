using System.Diagnostics;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Translating `new OtherForm().ShowDialog()` needs something no other rewrite does: the View a
/// Form will become, before that Form has been converted. This exercises the pipeline's separate
/// Form-discovery pass end to end, including the cross-namespace `using` a Form in a subfolder
/// forces.
/// </summary>
public class FormNavigationConversionTests
{
    [Fact]
    public async Task ConvertedFormNavigationApp_OpensGeneratedViewsAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "FormNavigationApp", "FormNavigationApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-navigation-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

            // Modal: async, owned by this window, and the dialog's View namespace imported.
            Assert.Contains("private async void settingsButton_Click", codeBehind);
            Assert.Contains("await new SettingsView().ShowDialog(this);", codeBehind);
            Assert.Contains(".Views.Dialogs;", codeBehind);

            // Modeless, after a translatable property write - both statements come across.
            Assert.Contains("statusLabel.Text = \"Opening help\";", codeBehind);
            Assert.Contains("new SettingsView().Show();", codeBehind);

            // The result drives a branch, and both halves of the contract are generated: the
            // caller awaits a bool...
            Assert.Contains("private async void confirmButton_Click", codeBehind);
            Assert.Contains("if (await new SettingsView().ShowDialog<bool>(this))", codeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(confirmButton_Click)", codeBehind);

            // ...and the dialog closes with one, synthesized from its designer-set DialogResult
            // buttons, which in WinForms needed no handler at all.
            Assert.True(result.Vfs.TryGetText("Views/Dialogs/SettingsView.axaml.cs", out var dialogCodeBehind));
            Assert.Contains("Close(true);", dialogCodeBehind);
            Assert.Contains("Close(false);", dialogCodeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(acceptButton_Click)", dialogCodeBehind);

            Assert.True(result.Vfs.TryGetText("Views/Dialogs/SettingsView.axaml", out var dialogAxaml));
            Assert.Contains("Click=\"acceptButton_Click\"", dialogAxaml);

            // The dialog's own View is emitted in the nested folder the navigation call names.
            Assert.Contains("Views/Dialogs/SettingsView.axaml", result.Vfs.RelativePaths);

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
