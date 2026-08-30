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

            // A ToolStripControlHost is plumbing WinForms needs because a ToolStrip only takes
            // ToolStripItems - the fallback this maps to is an ordinary panel, so the hosted
            // control goes straight in and the host disappears. It used to be the other way round:
            // a TODO comment where the host was, and the control nowhere at all.
            Assert.Contains("<Slider x:Name=\"hostedTrackBar\"", axaml);
            Assert.DoesNotContain("toolStripControlHost1", axaml);

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
            // The designer's DataPropertyName becomes the column's binding - the two column types
            // Avalonia gives a Binding property to.
            Assert.Contains("<DataGridTextColumn Header=\"Name\" Binding=\"{ReflectionBinding Name}\" />", axaml);
            Assert.Contains("<DataGridCheckBoxColumn Header=\"Active\" Binding=\"{ReflectionBinding Active}\" />", axaml);

            // Avalonia has no ComboBox/Button/Image/Link column type - these four become
            // template columns. DataGridComboBoxColumn used to be emitted here and broke the build.
            Assert.DoesNotContain("DataGridComboBoxColumn", axaml);
            Assert.Contains("<DataGridTemplateColumn Header=\"Category\">", axaml);

            // A template column gets no generated binding - DataGridTemplateColumn has no Binding
            // property, and which member of the cell it belongs to is not decidable. The name the
            // designer did record is reported instead of thrown away.
            Assert.DoesNotContain("<DataGridTemplateColumn Header=\"Category\" Binding=", axaml);
            Assert.Contains("bind this cell to the row model's 'Category'", axaml);
            Assert.Contains("<DataGridTemplateColumn Header=\"Action\">", axaml);
            Assert.Contains("<Button Content=\"Run\" />", axaml);

            // A Details-mode ListView is a grid, so its ColumnHeaders have somewhere to live - and
            // each one carries a real Binding. Without it the column renders a header over an
            // empty strip forever: no binding, no cell, no matter what the rows hold. The index is
            // the column's own position, because a row is the ListViewItem's sub-item texts.
            Assert.Contains("<DataGrid x:Name=\"detailsListView\"", axaml);
            Assert.Contains(
                "<DataGridTextColumn Header=\"File\" Width=\"200\" Binding=\"{ReflectionBinding [0]}\" />", axaml);
            Assert.Contains("ItemsSource=\"{Binding DetailsListViewRows}\"", axaml);

            // Both halves of the ListView mapping now translate, and they translate differently.
            // A ListBox owns its items, so Items is mutated in place; a DataGrid's rows are data,
            // so they go to the ViewModel collection the grid binds to. Nothing is invented on the
            // way: the row is the string[] a ListViewItem already was.
            vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind);
            Assert.Contains("flatListView.Items.Add(new ListBoxItem { Content = \"readme.txt\" });", codeBehind);
            Assert.Contains("w2aViewModel.DetailsListViewRows.Add(new[] { \"notes.txt\" });", codeBehind);

            // The whole handler comes across now, so there is no marker left. It is still *not*
            // promoted to a [RelayCommand]: promotion requires every statement to touch nothing
            // but bindable properties, and neither of these shapes is one - which is exactly why
            // the ViewModel rewrite target refuses them both.
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(fillButton_Click)", codeBehind);

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
