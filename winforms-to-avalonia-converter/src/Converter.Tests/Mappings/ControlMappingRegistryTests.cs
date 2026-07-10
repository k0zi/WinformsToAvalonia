using Converter.Mappings.BuiltIn;

namespace Converter.Tests.Mappings;

public class ControlMappingRegistryTests
{
    [Fact]
    public void GetAllMappings_HasAtLeastFortyEntries()
    {
        Assert.True(ControlMappingRegistry.GetAllMappings().Count >= 40,
            $"Expected >= 40 control mappings, found {ControlMappingRegistry.GetAllMappings().Count}");
    }

    [Theory]
    [InlineData("ToolStripSeparator")]
    [InlineData("CheckedListBox")]
    [InlineData("HScrollBar")]
    [InlineData("VScrollBar")]
    [InlineData("Splitter")]
    [InlineData("DomainUpDown")]
    public void GetMapping_ResolvesNewlyAddedControlTypes(string winFormsControlType)
    {
        Assert.NotNull(ControlMappingRegistry.GetMapping(winFormsControlType));
    }
}

public class PropertyMappingRegistryTests
{
    [Fact]
    public void GetMapping_TableLayoutPanelColumnSpan_MapsToGridColumnSpan()
    {
        var mapping = PropertyMappingRegistry.GetMapping("ColumnSpan", "TableLayoutPanel");

        Assert.NotNull(mapping);
        Assert.Equal("Grid.ColumnSpan", mapping!.AvaloniaProperty);
        Assert.True(mapping.DirectMapping);
    }

    [Fact]
    public void GetMapping_TableLayoutPanelRowSpan_MapsToGridRowSpan()
    {
        var mapping = PropertyMappingRegistry.GetMapping("RowSpan", "TableLayoutPanel");

        Assert.NotNull(mapping);
        Assert.Equal("Grid.RowSpan", mapping!.AvaloniaProperty);
        Assert.True(mapping.DirectMapping);
    }
}
