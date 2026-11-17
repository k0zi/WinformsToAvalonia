using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Lambdas and anonymous methods subscribed in InitializeComponent used to be reported and
/// dropped - their bodies were not even kept as a comment. Lifted into named methods, they are
/// ordinary handlers: translated, promoted, guarded and checklisted like any other.
/// </summary>
public class LambdaConversionTests
{
    [Fact]
    public async Task ConvertedLambdaApp_LiftsEveryInlineHandlerAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "LambdaApp", "LambdaApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-lambdas-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));

            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("inline lambda"));
            Assert.True(result.Vfs.TryGetText("MIGRATION.md", out var checklist));
            Assert.Contains("## Nothing left", checklist);

            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var view));
            var generated = view.Split("ORIGINAL WINFORMS CODE-BEHIND")[0];

            // The taken name is avoided; the existing method keeps its own.
            Assert.Contains("Command=\"{Binding GoButtonInlineCommand}\"", axaml);
            Assert.Contains("TextChanged=\"nameBox_TextChanged\"", axaml);
            Assert.Contains("statusLabel.Text = (nameBox.Text ?? string.Empty);", generated);
            Assert.Contains("Opened=\"MainForm_Load\"", axaml);
            Assert.Contains("private async void askButton_Click", generated);
            Assert.Contains("await MessageBoxFallback.ShowAsync(this, \"Sure?\", \"Lambda\");", generated);

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
