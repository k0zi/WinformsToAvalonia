using Spectre.Console.Testing;
using WinFormsToAvalonia.Cli.Rendering;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Scaffolding;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests.Rendering;

public class SummaryRendererTests
{
    [Fact]
    public void RenderReport_TypicalReport_ShowsCountsAndFallbackKeys()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        var report = new ConversionReport(
            IsLegacyStyle: false,
            TargetFrameworks: ["net9.0-windows"],
            FormCount: 2,
            UserControlCount: 1,
            DirectControlCount: 5,
            FallbackControlCount: 1,
            UnsupportedControlCount: 1,
            UsedFallbackKeys: ["RichTextBoxFallback"],
            RequiredNuGetPackages: ["Avalonia.Controls.DataGrid"],
            Warnings: ["field 'x' (SomeControl) has no Avalonia mapping: no mapping registered."],
            Elapsed: TimeSpan.FromMilliseconds(123));

        SummaryRenderer.RenderReport(console, report, verbose: true);

        var output = console.Output;
        Assert.Contains("SDK-style", output);
        Assert.Contains("net9.0-windows", output);
        Assert.Contains("Forms converted", output);
        Assert.Contains("user controls", output);
        Assert.Contains("RichTextBoxFallback", output);
        Assert.Contains("Avalonia.Controls.DataGrid", output);
        Assert.Contains("1 warning(s)", output);
        Assert.Contains("SomeControl", output);
    }

    [Fact]
    public void RenderReport_NonVerbose_TruncatesLongWarningList()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        var warnings = Enumerable.Range(1, 8).Select(i => $"warning number {i}").ToList();
        var report = new ConversionReport(
            IsLegacyStyle: true,
            TargetFrameworks: ["v4.8"],
            FormCount: 1,
            UserControlCount: 0,
            DirectControlCount: 0,
            FallbackControlCount: 0,
            UnsupportedControlCount: 8,
            UsedFallbackKeys: [],
            RequiredNuGetPackages: [],
            Warnings: warnings,
            Elapsed: TimeSpan.FromMilliseconds(50));

        SummaryRenderer.RenderReport(console, report, verbose: false);

        Assert.Contains("warning number 1", console.Output);
        Assert.DoesNotContain("warning number 8", console.Output);
        Assert.Contains("more (use --verbose", console.Output);
    }

    [Fact]
    public void RenderFileTree_NestedPaths_ShowsFolderStructure()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        var vfs = new VirtualFileSystem();
        vfs.AddText("App.axaml", "");
        vfs.AddText("Views/MainView.axaml", "");
        vfs.AddText("Controls/Generated/LayoutHint.cs", "");

        SummaryRenderer.RenderFileTree(console, "Generated", vfs);

        var output = console.Output;
        Assert.Contains("App.axaml", output);
        Assert.Contains("Views", output);
        Assert.Contains("MainView.axaml", output);
        Assert.Contains("Generated", output);
        Assert.Contains("LayoutHint.cs", output);
    }
}
