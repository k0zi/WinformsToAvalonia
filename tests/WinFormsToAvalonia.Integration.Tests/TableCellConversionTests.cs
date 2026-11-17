using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A TableLayoutPanel asked about its cells - the shape that stopped dotnet/samples' MatchingGame:
/// <c>GetColumn</c>/<c>GetRow</c> on a shared handler's sender, the children walked by index, and
/// <c>GetControlFromPosition</c>, with the cell numbers handed on to the game's own logic.
/// </summary>
public class TableCellConversionTests
{
    private static readonly string SourceProject =
        Path.Combine(AppContext.BaseDirectory, "SampleApps", "TableCellApp", "TableCellApp.csproj");

    [Fact]
    public async Task ConvertedApp_AsksTheCellsWhatTheDesignerPutThere()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-tablecell-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(SourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

            Assert.Contains("x:Name=\"label2\" Canvas.Left=\"103\" Canvas.Top=\"3\" Width=\"94\" Height=\"94\" Grid.Column=\"1\" Grid.Row=\"0\"", axaml);

            Assert.Contains("var label = (TextBlock)tableLayoutPanel1.Children[i];", codeBehind);
            Assert.Contains("var column = Grid.GetColumn(label);", codeBehind);
            // The int the translated call returns goes on into the game's own logic.
            Assert.Contains("label.Text = _board.GetCard(column, row).ToString();", codeBehind);
            Assert.Contains("label.Foreground = AmbientBackColorFallback.Of(label);", codeBehind);
            Assert.Contains(
                "var label = tableLayoutPanel1.Children.OfType<TextBlock>().FirstOrDefault(w2aCell => Grid.GetColumn(w2aCell) == w && Grid.GetRow(w2aCell) == h)!;",
                codeBehind);
            Assert.Contains(
                "label.Foreground = (_board.IsOpen(w, h) ? (IBrush?)new SolidColorBrush(Color.Parse(\"#FF000000\")) : AmbientBackColorFallback.Of(label));",
                codeBehind);
            Assert.True(result.Vfs.TryGetText("Controls/AmbientBackColorFallback.cs", out _));

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
