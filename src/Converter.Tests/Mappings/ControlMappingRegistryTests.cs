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

    [Theory]
    [InlineData("ToolStrip")]
    [InlineData("StatusStrip")]
    public void GetMapping_ToolStripAndStatusStrip_MapToAvaloniaControlsThatActuallyExist(string winFormsControlType)
    {
        // Avalonia.Controls.ToolBar and Avalonia.Controls.Primitives.StatusBar don't exist -
        // Avalonia has no ToolBar/StatusBar control at all (unlike WPF). Mapping to those type
        // names previously made AXAML referencing a ToolStrip/StatusStrip fail to compile for
        // every such form (found via a real WarehouseApp sample conversion).
        var mapping = ControlMappingRegistry.GetMapping(winFormsControlType);

        Assert.NotNull(mapping);
        Assert.DoesNotContain("ToolBar", mapping!.FullTypeName);
        Assert.DoesNotContain("StatusBar", mapping.FullTypeName);
        Assert.StartsWith("Avalonia.Controls.", mapping.FullTypeName);
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

    [Theory]
    [InlineData("Button")]
    [InlineData("CheckBox")]
    [InlineData("RadioButton")]
    public void GetMapping_TextOnContentControls_MapsToContent_NotText(string controlType)
    {
        // Avalonia's Button/CheckBox/RadioButton are ContentControl/ToggleButton-derived and
        // expose their caption via Content, not Text - the generic "Text"->"Text" common
        // mapping previously applied here too, producing an attribute Avalonia's compiler
        // rejects outright (found via a real WarehouseApp sample conversion).
        var mapping = PropertyMappingRegistry.GetMapping("Text", controlType);

        Assert.NotNull(mapping);
        Assert.Equal("Content", mapping!.AvaloniaProperty);
    }

    [Theory]
    [InlineData("Panel")]
    [InlineData("FlowLayoutPanel")]
    [InlineData("SplitContainer")]
    [InlineData("ToolStrip")]
    [InlineData("StatusStrip")]
    public void GetMapping_PaddingOnPanelDerivedControls_HasNoMapping(string controlType)
    {
        // Every Avalonia target for these WinForms control types (Panel/WrapPanel/Grid/
        // StackPanel) is Panel-derived and none of them expose a Padding property - the
        // control-specific null override must suppress the generic common "Padding" mapping
        // rather than falling through to it.
        Assert.Null(PropertyMappingRegistry.GetMapping("Padding", controlType));
    }

    [Fact]
    public void GetMapping_PaddingOnTextBox_StillFallsThroughToCommonMapping()
    {
        // Sanity check for the null-override fallthrough logic itself: a control type with no
        // control-specific "Padding" entry at all must still resolve via common mappings.
        var mapping = PropertyMappingRegistry.GetMapping("Padding", "TextBox");

        Assert.NotNull(mapping);
        Assert.Equal("Padding", mapping!.AvaloniaProperty);
    }
}
