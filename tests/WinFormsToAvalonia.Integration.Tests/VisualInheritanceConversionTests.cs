using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A Form deriving from another of the project's designer Forms. WinForms runs the base's
/// InitializeComponent and then the derived one, on the same fields; Avalonia XAML has no
/// inheritance at all. The derived View used to come out with only its <em>own</em> controls -
/// every inherited button, its caption override and its handler gone, and nothing reported.
/// </summary>
public class VisualInheritanceConversionTests
{
    [Fact]
    public async Task DerivedForm_IsFlattenedOverItsBaseAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "VisualInheritanceApp", "VisualInheritanceApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-inheritance-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));

            Assert.True(result.Vfs.TryGetText("Views/SettingsView.axaml", out var settings));

            // Inherited through an intermediate class with no designer, and overridden here.
            Assert.Matches("<Button x:Name=\"okButton\"[^>]* Content=\"Save\"", settings);
            Assert.DoesNotContain("Content=\"OK\"", settings);
            Assert.Contains("x:Name=\"cancelButton\"", settings);
            Assert.Contains("x:Name=\"headerLabel\"", settings);
            Assert.Contains("x:Name=\"nameTextBox\"", settings);

            // The base Form's own chrome and dialog keys, which the derived designer never mentions.
            Assert.Contains("CanResize=\"False\"", settings);
            Assert.Matches("x:Name=\"okButton\"[^>]* IsDefault=\"True\"", settings);
            Assert.Matches("x:Name=\"cancelButton\"[^>]* IsCancel=\"True\"", settings);
            Assert.Contains("Title=\"Settings\"", settings);

            // The OK handler lives in the *base* class's code-behind, and still came across.
            Assert.True(result.Vfs.TryGetText("ViewModels/SettingsViewModel.cs", out var viewModel));
            Assert.Contains("HeaderLabelText = \"Saved\";", viewModel);

            Assert.Contains(result.Report.Warnings, w => w.Contains("'SettingsForm' derives from 'BaseDialogForm'"));

            // The base keeps a View of its own, unchanged by the derived overrides.
            Assert.True(result.Vfs.TryGetText("Views/Dialogs/BaseDialogView.axaml", out var baseView));
            Assert.Matches("<Button x:Name=\"okButton\"[^>]* Content=\"OK\"", baseView);
            Assert.DoesNotContain("nameTextBox", baseView);

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
