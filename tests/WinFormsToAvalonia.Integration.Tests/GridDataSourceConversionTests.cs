using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A grid given its rows from code - <c>grid.DataSource = list;</c> - which stopped every handler
/// that filled a DataGridView, the corpus's most frequent blocker.
/// </summary>
public class GridDataSourceConversionTests
{
    private static readonly string SourceProject =
        Path.Combine(AppContext.BaseDirectory, "SampleApps", "GridDataSourceApp", "GridDataSourceApp.csproj");

    [Fact]
    public async Task ConvertedApp_HandsTheGridItsRows()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-griddatasource-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(SourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

            // No designer columns and the default: generation has to be asked for.
            Assert.Contains("autoGrid.AutoGenerateColumns = true;\n        autoGrid.ItemsSource = customers;", codeBehind.ReplaceLineEndings("\n"));
            // Generation explicitly off: the designer column binds, nothing else is said.
            Assert.Contains("boundGrid.ItemsSource = customers.ToArray();", codeBehind);
            Assert.DoesNotContain("boundGrid.AutoGenerateColumns", codeBehind);
            Assert.Contains("autoGrid.ItemsSource = null;", codeBehind);
            Assert.Contains("boundGrid.IsReadOnly = true;", codeBehind);

            Assert.Equal(result.Report.HandlerStatementCount, result.Report.MigratedStatementCount);

            var build = await DotnetRunner.RunAsync("build", outputDir);
            Assert.True(
                build.ExitCode == 0 && !build.StdOut.Contains(": warning ", StringComparison.Ordinal),
                $"dotnet build exit {build.ExitCode}.\n--- stdout ---\n{build.StdOut}\n--- stderr ---\n{build.StdErr}");
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
