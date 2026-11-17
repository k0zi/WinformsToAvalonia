using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class SplitContainerAndToolTipConversionTests
{
    [Fact]
    public async Task ConvertedSplitContainerApp_EmitsGridSplitterLayoutAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "SplitContainerApp", "SplitContainerApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-splitcontainer-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("ColumnDefinitions=\"*,Auto,*\"", axaml);
            Assert.Contains("<GridSplitter", axaml);
            Assert.Contains("<Button x:Name=\"leftButton\"", axaml);
            Assert.Contains("<TextBlock x:Name=\"rightLabel\"", axaml);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);

            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ConvertedToolTipApp_EmitsBothExtenderProviderAttributesAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "ToolTipApp", "ToolTipApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-tooltip-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var result = pipeline.Run(options);
            var vfs = result.Vfs;

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("ToolTip.Tip=\"Click to confirm\"", axaml);

            // The same extender-provider mechanism, for the provider whose text used to be
            // dropped without so much as a warning.
            Assert.Contains("AutomationProperties.HelpText=\"Confirms and closes the dialog.\"", axaml);

            // ...and the part of it that has no Avalonia target is named rather than dropped.
            Assert.Contains(
                result.Report.Warnings,
                w => w.Contains("helpProvider1", StringComparison.Ordinal)
                    && w.Contains("SetShowHelp", StringComparison.Ordinal));

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);

            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
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
