using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// <c>Properties.Settings</c>, end to end: the generated project carries a same-shaped class, and
/// handler bodies reading, assigning and saving settings translate onto it - except an assignment to
/// an application-scoped setting, which has no setter on either side.
/// </summary>
public class SettingsConversionTests
{
    [Fact]
    public async Task ConvertedSettingsApp_CarriesSettingsAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "SettingsApp", "SettingsApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-settings-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            var instance = $"{NamingConventions.DeriveProjectName(outputDir)}.Properties.Settings.Default";
            Assert.True(result.Vfs.TryGetText("Properties/Settings.cs", out var settings));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var view));
            Assert.True(result.Vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel));

            Assert.Contains("public string UserName { get; set; } = \"guest\";", settings);
            Assert.Contains("public double Volume { get; set; } = 0.75d;", settings);
            Assert.Contains("[JsonIgnore]\n    public string ServerUrl { get; } = \"https://example.invalid/api\";", settings);
            Assert.DoesNotContain("AccentColor", settings);

            Assert.Contains($"urlLabel.Text = {instance}.ServerUrl;", view);
            Assert.Contains($"{instance}.UserName = NameBoxText;", viewModel);
            Assert.Contains($"{instance}.LaunchCount += 1;", viewModel);
            Assert.Contains($"{instance}.Save();", viewModel);
            Assert.Contains($"{instance}.Reset();", viewModel);

            // Application scope: read-only, so the assignment is where the prefix stops.
            Assert.Contains("MigrationTodo.NotMigrated(nameof(ResetButton)", viewModel);

            Assert.Contains(result.Report.Warnings, w => w.Contains("'AccentColor' (System.Drawing.Color)"));
            Assert.Contains(result.Report.Warnings, w => w.Contains("'nameBox.Text' is bound to the application setting 'UserName'"));
            Assert.Contains(result.Report.Warnings, w => w.Contains("user.config are not migrated"));

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
