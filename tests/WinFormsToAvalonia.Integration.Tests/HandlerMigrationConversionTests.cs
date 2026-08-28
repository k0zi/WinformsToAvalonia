using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
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

            // The pointer position resolves against the control that raised the event.
            Assert.Contains("statusLabel.Text = e.GetPosition(canvas).X + \",\" + e.GetPosition(canvas).Y;", codeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(canvas_MouseDown)", codeBehind);

            // A handler shared by two controls has no single control to measure against, so the
            // same expression is left for a human.
            Assert.Contains("ORIGINAL WINFORMS BODY of 'sharedMouseDown'", codeBehind);

            // Control flow: the condition and both branches translate, so the whole `if` does -
            // and the emitted block is indented as real code, not a one-line blob.
            Assert.Contains(
                """
                        if (string.IsNullOrWhiteSpace((nameTextBox.Text ?? string.Empty)))
                        {
                            statusLabel.Text = "Name is required";
                            nameTextBox.Focus();
                        }
                        else
                        {
                            statusLabel.Text = "Looks good";
                        }
                """,
                codeBehind.Replace("\r\n", "\n"));
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(validateButton_Click)", codeBehind);

            // A loop, a local, a nested `if` and an increment, translated together - and because
            // this handler only touches bindable properties it was promoted, so the whole thing
            // lands as a working [RelayCommand] against the ViewModel's own properties.
            Assert.True(result.Vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel));
            Assert.Contains(
                """
                        var vowels = 0;
                        foreach (var letter in NameTextBoxText)
                        {
                            if (letter == 'a' || letter == 'e')
                            {
                                vowels++;
                            }
                        }
                """,
                viewModel.Replace("\r\n", "\n"));

            // The WinForms validation idiom, onto the bundled fallback - a static call, and the
            // second place a *handler body* pulls a template in rather than the AXAML.
            Assert.Contains("ErrorProviderFallback.SetError(nameTextBox, \"A name is required.\");", codeBehind);
            Assert.Contains("Controls/ErrorProviderFallback.cs", result.Vfs.RelativePaths);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(flagButton_Click)", codeBehind);

            // A colour is a brush in Avalonia, and which of the two properties the element has
            // at all is decided by the same table the AXAML styling pass consults.
            Assert.Contains("statusLabel.Foreground = new SolidColorBrush(Color.Parse(\"#FFFF0000\"));", codeBehind);
            Assert.Contains("nameTextBox.Background = new SolidColorBrush(Color.Parse(\"#FFFFFFFF\"));", codeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(colorButton_Click)", codeBehind);

            // A dialog WinForms has and Avalonia does not, inlined onto a bundled window whose
            // result *is* the colour - and the package that window needs must reach the csproj.
            Assert.Contains("if (await ColorDialogFallback.ShowAsync(this) is { } colorDialog1Color)", codeBehind);
            Assert.Contains("nameTextBox.Background = new SolidColorBrush(colorDialog1Color);", codeBehind);
            Assert.Contains("Controls/ColorDialogFallback.cs", result.Vfs.RelativePaths);

            var csprojPath = Assert.Single(result.Vfs.RelativePaths, p => p.EndsWith(".csproj", StringComparison.Ordinal));
            Assert.True(result.Vfs.TryGetText(csprojPath, out var csproj));
            Assert.Contains("Avalonia.Controls.ColorPicker", csproj);

            // The two-button question collapses into one awaited call returning a bool.
            Assert.Contains("if (await MessageBoxFallback.ShowYesNoAsync(this, \"Discard changes?\", \"Demo\"))", codeBehind);

            // Null-conditional on a value, translated against whichever target the handler landed
            // on - this one promoted, so its receiver is the generated property.
            Assert.Contains("var trimmed = NameTextBoxText?.Trim();", viewModel);
            Assert.Contains("StatusLabelText = trimmed ?? \"empty\";", viewModel);

            // The guard-clause dialog shape: the picked value outlives the `if` because the
            // then-branch is an unconditional return, and the generated project has to compile
            // with an `is not` pattern relying on exactly that.
            Assert.Contains(
                "if (await ColorDialogFallback.ShowAsync(this) is not { } colorDialog1Color)",
                codeBehind);
            Assert.Contains("nameTextBox.Background = new SolidColorBrush(colorDialog1Color);", codeBehind);

            Assert.Equal(22, result.Report.MigratedStatementCount);
            Assert.Equal(25, result.Report.HandlerStatementCount);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);
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

}
