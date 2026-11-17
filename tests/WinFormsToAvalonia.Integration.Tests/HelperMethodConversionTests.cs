using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Code-behind helper methods translated as real code. The negative case matters more than the
/// positive one here: a helper is emitted only when its <b>whole</b> body translates, because a
/// half-translated helper would look migrated at every call site while silently skipping work.
/// </summary>
public class HelperMethodConversionTests
{
    [Fact]
    public async Task ConvertedHelperMethodApp_PromotesWholeHelpersOnlyAndStillBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "HelperMethodApp", "HelperMethodApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-helpers-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));
            var text = codeBehind.Replace("\r\n", "\n");

            // A helper whose whole body translates becomes a real method - and its caller
            // translates too, which is the whole point.
            Assert.Contains(
                """
                    private void SetBusy(bool busy)
                    {
                        isBusy = busy;
                        startButton.IsEnabled = !busy;
                        statusLabel.Text = busy ? "Working" : "Ready";
                    }
                """,
                text);
            Assert.Contains("SetBusy(true);", text);

            // The private flag it maintains is carried over with it - without that the helper
            // could not have translated at all.
            Assert.Contains("private bool isBusy;", text);

            // A value-returning helper, called as an expression.
            Assert.Contains("private string Describe(int count)", text);
            Assert.Contains("statusLabel.Text = Describe(1);", text);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(startButton_Click)", text);

            // async propagates: the message box makes the helper `async Task` - never
            // `async void`, which callers could not await - and its caller async in turn.
            Assert.Contains("private async Task WarnAndReset()", text);
            Assert.Contains("private async void warnButton_Click", text);
            Assert.Contains("await WarnAndReset();", text);

            // The helper that cannot be finished stays a comment, and so its caller blocks.
            Assert.Contains("ORIGINAL WINFORMS MEMBERS", text);
            Assert.Contains("private void PersistToDisk()", text.Split("ORIGINAL WINFORMS MEMBERS")[1]);
            Assert.Contains("MigrationTodo.NotMigrated(nameof(saveButton_Click)", text);

            // A promoted helper must not also appear in the preserved block - that would read as
            // un-migrated with a compiling copy sitting above it.
            Assert.DoesNotContain("SetBusy", text.Split("ORIGINAL WINFORMS MEMBERS")[1].Split("ORIGINAL WINFORMS CODE-BEHIND")[0]);

            // Everything this pair touches is bindable, so the handler and its helper moved to
            // the ViewModel together - and the helper's own control access is what made the
            // property bindable at all, since nothing in the handler ever names tagLabel.
            Assert.True(result.Vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel));
            Assert.Contains("public partial string TagLabelText { get; set; }", viewModel);
            Assert.Contains("private void TagButton()", viewModel);
            Assert.Contains("Announce(\"done\");", viewModel);
            Assert.Contains(
                """
                    private void Announce(string what)
                    {
                        TagLabelText = what;
                    }
                """,
                viewModel.Replace("\r\n", "\n"));

            // SetBusy stays on the View: it writes a private field, which a ViewModel has no
            // binding for - so its caller stays in code-behind too.
            Assert.Contains("private void SetBusy(bool busy)", text);
            Assert.DoesNotContain("SetBusy", viewModel);

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
