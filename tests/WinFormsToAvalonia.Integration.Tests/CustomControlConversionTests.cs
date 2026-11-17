using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A project's own subclass of a WinForms control (<c>RoundButton : Button</c>) used to have no
/// mapping and was simply left out of the window, its handlers and settings with it. It is now
/// converted as the control it derives from - by every table, so a Button subclass's Click is a
/// real Click and its caption's mnemonic an access key - and what only the subclass adds is said.
/// </summary>
public class CustomControlConversionTests
{
    [Fact]
    public async Task ConvertedCustomControlApp_EmitsSubclassesAsTheirBaseControlsAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "CustomControlApp", "CustomControlApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-custom-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));

            // A Button subclass: a real Button, its caption's mnemonic an access key, and its Click
            // promoted like any Button's.
            Assert.Matches("<Button x:Name=\"roundButton1\"[^>]* Content=\"_Round\"[^>]* Command=\"\\{Binding RoundButton1Command\\}\"", axaml);

            // Two levels below TextBox, through a class with no designer of its own.
            Assert.Matches("<TextBox x:Name=\"currencyBox1\"[^>]* TextAlignment=\"Right\"", axaml);
            Assert.Contains("TextChanged=\"currencyBox1_TextChanged\"", axaml);

            Assert.Contains(result.Report.Warnings, w => w.Contains("'roundButton1' is a RoundButton, this project's own subclass of Button"));
            Assert.Contains(result.Report.Warnings, w => w.Contains("RoundButton.CornerRadius (roundButton1)"));
            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("No mapping registered"));
            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("could not be traced"));

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
