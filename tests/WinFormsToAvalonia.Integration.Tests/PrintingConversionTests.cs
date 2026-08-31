using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// What the WinForms printing family becomes.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has no printing API - measured against its reference assemblies there is not one
/// <c>Print*</c> type, and that has not changed. What changed is that a <c>PrintPage</c> handler
/// is drawing code, and drawing code translates: once <c>e.Graphics</c> calls became
/// <c>DrawingContext</c> calls, a document could be rendered - and a rendered page is all a
/// preview, a page setup and an export ever needed.
/// </para>
/// <para>
/// So this is not printing, and the generated code says so. It is the half that is expressible.
/// </para>
/// </remarks>
public class PrintingConversionTests
{
    [Fact]
    public void ConvertedPrintingApp_DrawsPreviewsAndExportsButDoesNotPrint()
    {
        var sourceProject = Path.Combine(
            AppContext.BaseDirectory, "SampleApps", "PrintingApp", "PrintingApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-printing-" + Guid.NewGuid());

        var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir, DryRun: true));

        Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

        // The document is a field, its designer name is reproduced, and its PrintPage is wired.
        Assert.Contains("private readonly PrintDocumentFallback printDocument1 = new();", codeBehind);
        Assert.Contains("printDocument1.DocumentName = \"Sample report\";", codeBehind);
        Assert.Contains("printDocument1.PrintPage += printDocument1_PrintPage;", codeBehind);

        // The handler is drawing code, so it translates - including the layout rectangle, which
        // becomes the FormattedText's bounds.
        Assert.Contains("private void printDocument1_PrintPage(object? sender, PrintPageSurfaceEventArgs e)", codeBehind);
        Assert.Contains("e.Context.DrawRectangle(null, new Pen(", codeBehind);
        Assert.Contains("MaxTextWidth = e.MarginBounds.Width,", codeBehind);
        Assert.Contains("e.HasMorePages = false;", codeBehind);
        Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(printDocument1_PrintPage)", codeBehind);

        // A PrintDialog still has no printer to offer. Its one shape is translated whole: the
        // dialog moves into the destination picker the call it guarded now performs.
        Assert.Contains("await printDocument1.PrintAsync(this);", codeBehind);
        Assert.DoesNotContain("printDialog1", codeBehind.Split("ORIGINAL WINFORMS")[0]);

        // Both remaining dialogs resolve their document from the designer's Document property.
        Assert.Contains("await PrintPreviewDialogFallback.ShowAsync(this, printDocument1);", codeBehind);
        Assert.Contains("await PageSetupDialogFallback.ShowAsync(this, printDocument1);", codeBehind);

        Assert.Contains("Controls/PrintDocumentFallback.cs", result.Vfs.RelativePaths);
        Assert.Contains("Controls/PrintPreviewDialogFallback.cs", result.Vfs.RelativePaths);
        Assert.Contains("Controls/PageSetupDialogFallback.cs", result.Vfs.RelativePaths);

        // Nothing in the family is "no Avalonia API" any more - but the two entries that claim to
        // *print* still say plainly that they do not, because they still do not.
        Assert.Contains(
            result.Report.ConvertedElsewhereNotes,
            n => n.Contains("PrintDocumentFallback", StringComparison.Ordinal)
                && n.Contains("no printing API", StringComparison.Ordinal));
        Assert.Contains(
            result.Report.ConvertedElsewhereNotes,
            n => n.Contains("no printer to choose", StringComparison.Ordinal));
    }
}
