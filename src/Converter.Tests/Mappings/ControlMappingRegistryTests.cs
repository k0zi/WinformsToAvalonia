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

    [Fact]
    public void GetMapping_TreeViewSelectedNode_MapsToSelectedItem()
    {
        // WinForms TreeView.SelectedNode is single-selection - Avalonia's exact match is
        // SelectedItem, not SelectedItems (the separate multi-select collection). Previously
        // unmapped entirely, which let AxamlGenerator's usage-inferred-binding fallback leak
        // the raw WinForms name "SelectedNode" straight into generated AXAML (found via a real
        // WarehouseApp sample conversion - not a real Avalonia TreeView property).
        var mapping = PropertyMappingRegistry.GetMapping("SelectedNode", "TreeView");

        Assert.NotNull(mapping);
        Assert.Equal("SelectedItem", mapping!.AvaloniaProperty);
        Assert.True(mapping.DirectMapping);
    }

    [Theory]
    [InlineData("Label")]
    [InlineData("ToolStripLabel")]
    public void GetMapping_TextAlignOnTextBlockControls_TargetsTextAlignmentNotContentAlignment(string controlType)
    {
        // Label/ToolStripLabel map to Avalonia TextBlock, which - unlike a ContentControl -
        // has no HorizontalContentAlignment/VerticalContentAlignment at all. Previously fell
        // through to the common TextAlign mapping and emitted attributes that don't compile
        // (found via a real WarehouseApp sample conversion).
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", controlType);

        Assert.NotNull(mapping);
        Assert.Equal("TextAlignment,VerticalAlignment", mapping!.AvaloniaProperty);
    }

    [Fact]
    public void GetMapping_TextAlignOnCheckBox_StillUsesCommonContentAlignmentMapping()
    {
        // Regression guard: CheckBox/RadioButton are genuine ContentControl-derived Avalonia
        // types, so HorizontalContentAlignment/VerticalContentAlignment is correct for them -
        // must not accidentally inherit Label's TextBlock-specific override.
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "CheckBox");

        Assert.NotNull(mapping);
        Assert.Equal("HorizontalContentAlignment,VerticalContentAlignment", mapping!.AvaloniaProperty);
    }

    [Fact]
    public void GetMapping_TextBoxTextAlign_TargetsTextAlignment()
    {
        // TextBox.TextAlign uses a different WinForms enum from Label's TextAlign
        // (System.Windows.Forms.HorizontalAlignment, not System.Drawing.ContentAlignment) and
        // TextBox (not ContentControl-derived either) has its own TextAlignment property.
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "TextBox");

        Assert.NotNull(mapping);
        Assert.Equal("TextAlignment", mapping!.AvaloniaProperty);
    }

    [Fact]
    public void GetMapping_DataGridViewRows_TargetsItemsSourceNotItems()
    {
        // Avalonia's DataGrid has no "Items" property, only the bindable "ItemsSource" (found
        // via inspection while fixing the TreeView/SelectedNode and Label/TextAlign bugs above -
        // not reproducible in WarehouseApp itself, since its DataGridView lives entirely inside
        // a hand-rolled base class the parser never sees, but wrong on its face regardless).
        var mapping = PropertyMappingRegistry.GetMapping("Rows", "DataGridView");

        Assert.NotNull(mapping);
        Assert.Equal("ItemsSource", mapping!.AvaloniaProperty);
    }

    [Fact]
    public void GetMapping_FormText_StillMapsToTitle()
    {
        var mapping = PropertyMappingRegistry.GetMapping("Text", "Form");

        Assert.NotNull(mapping);
        Assert.Equal("Title", mapping!.AvaloniaProperty);
    }

    [Theory]
    [InlineData("PictureBox")]
    [InlineData("Panel")]
    [InlineData("FlowLayoutPanel")]
    [InlineData("SplitContainer")]
    [InlineData("TableLayoutPanel")]
    [InlineData("ToolStrip")]
    [InlineData("StatusStrip")]
    public void GetMapping_BorderStyleOnBorderIncapableControls_HasNoMapping(string controlType)
    {
        // Image/Panel/Grid/WrapPanel/StackPanel (these control types' Avalonia targets) have no
        // BorderThickness of their own - AxamlGenerator wraps them in a real <Border> instead
        // (see AxamlGeneratorTests), computing the value directly from the common mapping;
        // this null override just keeps the normal per-property loop from also, redundantly
        // and invalidly, emitting it on the inner element (found via a real WarehouseApp
        // conversion: productPictureBox, colorPreviewPanel).
        Assert.Null(PropertyMappingRegistry.GetMapping("BorderStyle", controlType));
    }

    [Fact]
    public void GetMapping_BorderStyleOnTextBox_StillFallsThroughToCommonMapping()
    {
        // Regression guard: TextBox (and other TemplatedControl-derived targets) really do
        // have BorderBrush/BorderThickness - must not be affected by the null overrides above.
        var mapping = PropertyMappingRegistry.GetMapping("BorderStyle", "TextBox");

        Assert.NotNull(mapping);
        Assert.Equal("BorderBrush,BorderThickness", mapping!.AvaloniaProperty);
    }

    [Fact]
    public void GetMapping_DateTimePickerValue_MapsToSelectedDate()
    {
        // DateTimePicker maps to Avalonia's DatePicker, whose selection property is
        // SelectedDate, not Value (found via user report - not reproducible in WarehouseApp
        // itself, since no form there sets DateTimePicker.Value to a literal).
        var mapping = PropertyMappingRegistry.GetMapping("Value", "DateTimePicker");

        Assert.NotNull(mapping);
        Assert.Equal("SelectedDate", mapping!.AvaloniaProperty);
    }

    [Fact]
    public void GetMapping_NumericUpDownValue_StillMapsToValue()
    {
        // Regression guard: NumericUpDown/TrackBar/ProgressBar's Avalonia equivalents really
        // do have a Value property - must not be affected by DateTimePicker's override.
        var mapping = PropertyMappingRegistry.GetMapping("Value", "NumericUpDown");

        Assert.NotNull(mapping);
        Assert.Equal("Value", mapping!.AvaloniaProperty);
    }
}
