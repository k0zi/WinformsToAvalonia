using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// <c>Properties.Resources</c>, end to end: the designer's <c>global::App.Properties.Resources.logo</c>
/// becomes a copied asset, a string entry the string itself, the Form's icon a Window icon - and the
/// generated project carries a <c>Properties.Resources</c> class so handler bodies naming its strings
/// still translate.
/// </summary>
public class ProjectResourcesConversionTests
{
    [Fact]
    public async Task ConvertedProjectResourcesApp_CarriesResourcesAndBuilds()
    {
        var sourceProject = Path.Combine(
            AppContext.BaseDirectory, "SampleApps", "ProjectResourcesApp", "ProjectResourcesApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-projres-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            var projectName = WinFormsToAvalonia.Core.Emission.NamingConventions.DeriveProjectName(outputDir);
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));
            Assert.True(result.Vfs.TryGetText("Properties/Resources.cs", out var resources));
            Assert.True(result.Vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel));

            // Designer: a file reference, an embedded payload, a string and the Form's icon.
            Assert.Contains("Source=\"/Assets/Resources/logo.png\"", axaml);
            Assert.Contains("Source=\"/Assets/Resources/badge.png\"", axaml);
            Assert.Contains("Content=\"Hello from the resource file\"", axaml);
            Assert.Contains("Icon=\"/Assets/Resources/AppIcon.ico\"", axaml);
            Assert.True(result.Vfs.BinaryFiles.ContainsKey("Assets/Resources/logo.png"));
            Assert.True(result.Vfs.BinaryFiles.ContainsKey("Assets/Resources/badge.png"));
            Assert.True(result.Vfs.BinaryFiles.ContainsKey("Assets/Resources/AppIcon.ico"));

            // The generated class, and a handler body reading two of its strings.
            Assert.Contains($"namespace {projectName}.Properties;", resources);
            Assert.Contains("public static string Greeting => \"Hello from the resource file\";", resources);
            Assert.Contains("public static Bitmap logo =>", resources);
            Assert.Contains("public static WindowIcon AppIcon =>", resources);
            Assert.Contains($"StatusLabelText = {projectName}.Properties.Resources.Greeting;", viewModel);
            Assert.Contains($"NameBoxText = {projectName}.Properties.Resources.Farewell + \"!\";", viewModel);

            // What could not come across is named: the missing file, and the translation.
            Assert.Contains(result.Report.Warnings, w => w.Contains("Properties.Resources.missing") && w.Contains("does not exist"));
            Assert.Contains(result.Report.Warnings, w => w.Contains("translations (hu)"));
            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("'logo'") || w.Contains("Resources.logo,"));

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
