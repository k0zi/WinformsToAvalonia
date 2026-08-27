using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class ToolStripItemAndDataGridColumnConversionTests
{
    [Fact]
    public async Task ConvertedToolStripApp_EmitsRealButtonChildInStackPanelAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "ToolStripApp", "ToolStripApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-toolstrip-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            Assert.Contains("Controls/ToolStripFallback.cs", vfs.RelativePaths);

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<controls:ToolStripFallback", axaml);
            Assert.Contains("<Button x:Name=\"newToolStripButton\" Content=\"New\" />", axaml);

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
    public async Task ConvertedStatusStripApp_EmitsRealStatusLabelChildInStackPanelAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "StatusStripApp", "StatusStripApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-statusstrip-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            Assert.Contains("Controls/StatusStripFallback.cs", vfs.RelativePaths);

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<controls:StatusStripFallback", axaml);
            Assert.Contains("<TextBlock x:Name=\"readyStatusLabel\" Text=\"Ready\" />", axaml);

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
    public async Task ConvertedDataGridViewColumnsApp_EmitsDataGridColumnsWrapperAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "DataGridViewColumnsApp", "DataGridViewColumnsApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-datagridcolumns-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<DataGrid.Columns>", axaml);
            Assert.Contains("<DataGridTextColumn Header=\"Name\" />", axaml);
            Assert.Contains("<DataGridCheckBoxColumn Header=\"Active\" />", axaml);

            // Avalonia has no ComboBox/Button/Image/Link column type - these four become
            // template columns. DataGridComboBoxColumn used to be emitted here and broke the build.
            Assert.DoesNotContain("DataGridComboBoxColumn", axaml);
            Assert.Contains("<DataGridTemplateColumn Header=\"Category\">", axaml);
            Assert.Contains("<DataGridTemplateColumn Header=\"Action\">", axaml);
            Assert.Contains("<Button Content=\"Run\" />", axaml);

            // A Details-mode ListView is a grid, so its ColumnHeaders have somewhere to live.
            Assert.Contains("<DataGrid x:Name=\"detailsListView\"", axaml);
            Assert.Contains("<DataGridTextColumn Header=\"File\" Width=\"200\" />", axaml);

            // DropDownItems nest through Button.Flyout > MenuFlyout instead of being dropped.
            Assert.Contains("<Button.Flyout>", axaml);
            Assert.Contains("<SplitButton x:Name=\"splitButton1\" Content=\"Run\">", axaml);
            Assert.Contains("<MenuFlyout>", axaml);
            Assert.Contains("<MenuItem x:Name=\"dropDownItemA\" Header=\"Drop-down item A\" />", axaml);

            Assert.DoesNotContain("has no Avalonia mapping", axaml);

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
