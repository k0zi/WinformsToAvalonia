using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// <c>--mappings</c>, end to end: a vendor text box converted as a WinForms TextBox (so its handler
/// translates like any TextBox's), and a vendor gauge emitted as the Avalonia element the file names.
/// </summary>
public class ThirdPartyMappingsConversionTests
{
    [Fact]
    public async Task ConvertedThirdPartyApp_AppliesTheMappingsFileAndBuilds()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "SampleApps", "ThirdPartyApp");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-thirdparty-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(
                Path.Combine(folder, "ThirdPartyApp.csproj"), outputDir)
            {
                MappingsFile = Path.Combine(folder, "mappings.json"),
            });
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var view));

            // `as`: re-typed, its EditValue renamed to Text and EditValueChanged to TextChanged.
            Assert.Contains("<TextBox x:Name=\"nameEdit\"", axaml);
            Assert.Contains("Text=\"initial\"", axaml);
            Assert.Contains("TextChanged=\"nameEdit_EditValueChanged\"", axaml);
            Assert.Contains("statusLabel.Text = (nameEdit.Text ?? string.Empty);", view);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(nameEdit_EditValueChanged)", view);

            // `element`: the named element, with the attributes the file maps.
            Assert.Contains("<ProgressBar x:Name=\"gauge\"", axaml);
            Assert.Contains("Value=\"40\" Maximum=\"200\"", axaml);

            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("No mapping registered"));
            Assert.Contains(result.Report.Warnings, w => w.Contains("'nameEdit' (TextEdit) is converted as TextBox"));

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
