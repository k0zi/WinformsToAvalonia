using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A keyboard handler, end to end: the comparisons against <c>Keys</c> translate onto Avalonia's
/// split <c>Key</c>/<c>KeyModifiers</c>, the modifier flags and <c>Handled</c> come across, and the
/// subscription is made in the tunnelling phase - without which a TextBox swallowed Back, Delete
/// and the arrows before the handler ever ran.
/// </summary>
public class KeyboardConversionTests
{
    [Fact]
    public async Task ConvertedKeyboardApp_TranslatesKeyHandlersAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "KeyboardApp", "KeyboardApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-keyboard-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var view));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));

            Assert.Contains("if (e.Key == Key.Enter)", view);
            Assert.Contains("e.Handled = true;", view);
            Assert.Contains("if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Delete)", view);
            Assert.Contains("if ((e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control))", view);
            Assert.Contains("if (!(e.Key == Key.Escape) && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))", view);
            Assert.Contains("if (e.KeyModifiers == KeyModifiers.Alt)", view);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(nameBox_KeyDown)", view);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(MainForm_KeyDown)", view);

            // Tunnelled from the constructor - on the box, and on the Window for KeyPreview.
            Assert.Contains("nameBox.AddHandler(InputElement.KeyDownEvent, nameBox_KeyDown, RoutingStrategies.Tunnel);", view);
            Assert.Contains("        AddHandler(InputElement.KeyDownEvent, MainForm_KeyDown, RoutingStrategies.Tunnel);", view);
            Assert.DoesNotContain("KeyDown=\"", axaml);
            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("KeyPreview"));

            // KeyPress: tunnelled TextInput, and the digit filter translated over its string.
            Assert.Contains("amountBox.AddHandler(InputElement.TextInputEvent, amountBox_KeyPress, RoutingStrategies.Tunnel);", view);
            Assert.Contains("if (!(e.Text ?? string.Empty).All(char.IsControl) && !(e.Text ?? string.Empty).All(char.IsDigit))", view);
            Assert.Contains("if (e.Text == \"-\")", view);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(amountBox_KeyPress)", view);
            Assert.DoesNotContain("TextInput=\"", axaml);

            // Enter raises no TextInput at all, so that comparison is left for a human.
            Assert.Contains("MigrationTodo.NotMigrated(nameof(codeBox_KeyPress)", view);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);
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
}
