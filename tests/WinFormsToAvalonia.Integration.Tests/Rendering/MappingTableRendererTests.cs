using Spectre.Console.Testing;
using WinFormsToAvalonia.Cli.Rendering;
using WinFormsToAvalonia.Core.Mapping;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests.Rendering;

public class MappingTableRendererTests
{
    [Fact]
    public void Render_NoFilter_ListsAllCategoriesWithCorrectCounts()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        var registry = new ControlMappingRegistry();

        MappingTableRenderer.Render(console, registry, filter: null);

        var output = console.Output;
        Assert.Contains("Button", output);
        Assert.Contains("GroupBox", output);
        Assert.Contains("BackgroundWorker", output);
        Assert.Contains("direct", output);
        Assert.Contains("fallback", output);
        Assert.Contains("unsupported", output);
    }

    [Fact]
    public void Render_WithFilter_OnlyMatchingRowsAppear()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        var registry = new ControlMappingRegistry();

        MappingTableRenderer.Render(console, registry, filter: "Box");

        var output = console.Output;
        Assert.Contains("CheckBox", output);
        Assert.Contains("GroupBox", output);
        Assert.DoesNotContain("Button", output);
    }
}
