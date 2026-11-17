using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A Form that draws in an <c>OnPaint</c> override, a GroupBox that both contains a label and
/// draws, and an <c>OnLoad</c> override beside a designer Load handler - the three shapes that
/// used to stay comments because Avalonia has no Paint event, a container cannot both draw and
/// contain, and the View's base class has no <c>OnLoad</c> to override.
/// </summary>
public class PaintLayerConversionTests
{
    private static readonly string SourceProject =
        Path.Combine(AppContext.BaseDirectory, "SampleApps", "PaintLayerApp", "PaintLayerApp.csproj");

    [Fact]
    public async Task ConvertedApp_DrawsOnPaintLayersAndRunsBothLoadHandlers()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-paintlayer-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(SourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

            // The Form's layer comes first in the root Canvas, so every control draws over it.
            var formLayer = axaml.IndexOf("<controls:PaintSurfaceFallback x:Name=\"mainFormPaintLayer\" Canvas.Left=\"0\" Canvas.Top=\"0\" Width=\"240\" Height=\"200\" />", StringComparison.Ordinal);
            Assert.True(formLayer >= 0, axaml);
            Assert.True(formLayer < axaml.IndexOf("x:Name=\"drawingGroup\"", StringComparison.Ordinal));

            // The GroupBox keeps its children; its drawing goes on a layer under them.
            var groupLayer = axaml.IndexOf("<controls:PaintSurfaceFallback x:Name=\"drawingGroupPaintLayer\"", StringComparison.Ordinal);
            Assert.True(groupLayer >= 0, axaml);
            Assert.True(groupLayer < axaml.IndexOf("x:Name=\"captionLabel\"", StringComparison.Ordinal));
            Assert.Contains("<GroupBox x:Name=\"drawingGroup\"", axaml);

            // Both handlers are translated drawing code on their layers.
            Assert.Contains("mainFormPaintLayer.Paint += MainForm_Paint;", codeBehind);
            Assert.Contains("drawingGroupPaintLayer.Paint += drawingGroup_Paint;", codeBehind);
            Assert.Contains("e.Context.DrawRectangle(null, new Pen(", codeBehind);
            Assert.Contains("e.Context.DrawLine(new Pen(", codeBehind);
            // A pen local in a `using`: an Avalonia Pen in a block, the dash pattern in pen widths.
            Assert.Contains("var dashed = new Pen(new SolidColorBrush(Color.Parse(\"#FF000080\")), 2);", codeBehind);
            Assert.Contains("dashed.DashStyle = new DashStyle(new double[] { 3, 1 }, 0);", codeBehind);
            Assert.Contains("e.Context.DrawRectangle(fill, null, new Rect(60, 70, 20, 20));", codeBehind);

            // Repainting follows the drawing.
            Assert.Contains("drawingGroupPaintLayer.InvalidateVisual();", codeBehind);
            Assert.Contains("mainFormPaintLayer.InvalidateVisual();", codeBehind);

            // OnLoad called base first: the designer's handler, then the override's body.
            Assert.Contains("Opened=\"MainForm_Load\"", axaml);
            Assert.Contains("Opened += MainFormOverride_Load;", codeBehind);
            Assert.Contains("statusLabel.Text = \"loaded\";", codeBehind);
            Assert.DoesNotContain("base.OnLoad", codeBehind.Split("ORIGINAL WINFORMS CODE-BEHIND")[0]);
            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("call 'MainFormOverride_Load' from it by hand"));

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

    /// <summary>
    /// With <c>--with-web</c> the main View is a UserControl, which has no <c>Opened</c>: both Load
    /// handlers have to be forwarded by the wrapper Window, and the View must not subscribe one itself.
    /// </summary>
    [Fact]
    public void ConvertedAppWithWeb_WrapperForwardsBothLoadHandlers()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-paintlayer-web-" + Guid.NewGuid());
        var result = new ConversionPipeline().Run(new ConversionOptions(SourceProject, outputDir, DryRun: true, WithWeb: true));

        var wrapper = result.Vfs.RelativePaths.Single(p => p.EndsWith("Views/MainWindow.axaml.cs", StringComparison.Ordinal));
        Assert.True(result.Vfs.TryGetText(wrapper, out var wrapperCode));
        Assert.Contains(".MainForm_Load(sender, e);", wrapperCode);
        Assert.Contains(".MainFormOverride_Load(sender, e);", wrapperCode);

        var view = result.Vfs.RelativePaths.Single(p => p.EndsWith("Views/MainView.axaml.cs", StringComparison.Ordinal));
        Assert.True(result.Vfs.TryGetText(view, out var viewCode));
        Assert.DoesNotContain("Opened +=", viewCode);
        Assert.Contains("internal void MainFormOverride_Load(", viewCode);
    }
}
