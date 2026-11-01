using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A Form's window chrome and keyboard behaviour. Every one of these used to be dropped without a
/// word: a fixed, centred dialog that Enter accepts and Escape cancels became a resizable window,
/// opened wherever the platform liked, that ignored both keys and tabbed in the order the
/// designer happened to add its controls.
/// </summary>
public class WindowChromeConversionTests
{
    [Fact]
    public async Task ConvertedWindowChromeApp_CarriesChromeAndKeyboardBehaviourAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "WindowChromeApp", "WindowChromeApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-chrome-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));

            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var main));
            Assert.Contains("WindowStartupLocation=\"CenterScreen\"", main);
            Assert.Contains("MinWidth=\"300\" MinHeight=\"200\"", main);
            Assert.DoesNotContain("CanResize", main);

            Assert.True(result.Vfs.TryGetText("Views/OptionsView.axaml", out var options));
            Assert.Contains("WindowStartupLocation=\"CenterOwner\"", options);
            Assert.Contains("ShowInTaskbar=\"False\"", options);
            Assert.Contains("CanResize=\"False\" CanMinimize=\"False\" CanMaximize=\"False\"", options);

            Assert.Matches("<TextBox x:Name=\"nameTextBox\"[^>]* TabIndex=\"0\"", options);
            Assert.Matches("<Button x:Name=\"okButton\"[^>]* TabIndex=\"1\"[^>]* IsDefault=\"True\"", options);
            Assert.Matches("<Button x:Name=\"cancelButton\"[^>]* IsTabStop=\"False\"[^>]* IsCancel=\"True\"", options);
            Assert.DoesNotMatch("okButton\"[^>]*IsCancel", options);

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
