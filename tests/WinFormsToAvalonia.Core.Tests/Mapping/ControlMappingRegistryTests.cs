using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

public class ControlMappingRegistryTests
{
    [Fact]
    public void Map_DirectMappedControl_TranslatesKnownPropertiesToAttributes()
    {
        var registry = new ControlMappingRegistry();
        var button = new ControlModel { FieldName = "button1", ClrTypeName = "Button" };
        button.Properties["Text"] = new PropertyValue.Literal("Click me");
        button.Properties["Enabled"] = new PropertyValue.Literal(false); // no mapping spec for Enabled yet - must be ignored, not error

        var mapped = registry.Map(button);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("Button", mapped.AvaloniaElementName);
        Assert.Equal("Click me", mapped.Attributes["Content"]);
        Assert.DoesNotContain("Enabled", mapped.Attributes.Keys);
        Assert.Empty(mapped.Warnings);
    }

    [Theory]
    [InlineData("Panel")]
    [InlineData("TableLayoutPanel")]
    [InlineData("FlowLayoutPanel")]
    public void Map_ContainerControls_MapDirectlyToCanvas(string winFormsTypeName)
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "panel1", ClrTypeName = winFormsTypeName };

        var mapped = registry.Map(control);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("Canvas", mapped.AvaloniaElementName);
    }

    [Fact]
    public void Map_TabPage_MapsToTabItemWithCanvasChildWrapper()
    {
        var registry = new ControlMappingRegistry();
        var tabPage = new ControlModel { FieldName = "tabPage1", ClrTypeName = "TabPage" };
        tabPage.Properties["Text"] = new PropertyValue.Literal("Page 1");

        var mapped = registry.Map(tabPage);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("TabItem", mapped.AvaloniaElementName);
        Assert.Equal("Page 1", mapped.Attributes["Header"]);
        Assert.Equal(["Canvas"], mapped.ChildWrapperElementNames);
    }

    [Theory]
    [InlineData("ToolStripDropDownButton", "Button", "Button.Flyout")]
    [InlineData("ToolStripSplitButton", "SplitButton", "SplitButton.Flyout")]
    public void Map_DropDownToolStripButtons_NestTheirItemsInATwoLevelMenuFlyoutWrapper(
        string winFormsTypeName, string expectedElementName, string expectedOuterWrapper)
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "_probe", ClrTypeName = winFormsTypeName };
        control.Properties["Text"] = new PropertyValue.Literal("Layout");

        var mapped = registry.Map(control);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal(expectedElementName, mapped.AvaloniaElementName);
        Assert.Equal("Layout", mapped.Attributes["Content"]);
        Assert.Equal([expectedOuterWrapper, "MenuFlyout"], mapped.ChildWrapperElementNames);
    }

    [Fact]
    public void Map_DataGridView_MapsToDataGridAndRequiresNuGetPackage()
    {
        var registry = new ControlMappingRegistry();
        var grid = new ControlModel { FieldName = "grid1", ClrTypeName = "DataGridView" };

        var mapped = registry.Map(grid);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("DataGrid", mapped.AvaloniaElementName);
        Assert.Equal("Avalonia.Controls.DataGrid", mapped.RequiredNuGetPackage);
    }

    [Fact]
    public void Map_CheckBox_MapsCheckedToIsChecked()
    {
        var registry = new ControlMappingRegistry();
        var checkBox = new ControlModel { FieldName = "checkBox1", ClrTypeName = "CheckBox" };
        checkBox.Properties["Checked"] = new PropertyValue.Literal(true);

        var mapped = registry.Map(checkBox);

        Assert.Equal("True", mapped.Attributes["IsChecked"]);
    }

    [Theory]
    [InlineData("DateTimePicker", "CalendarDatePicker")]
    [InlineData("TrackBar", "Slider")]
    [InlineData("SplitContainer", "Grid")]
    public void Map_NewDirectMappedControls_MapDirectlyToExpectedAvaloniaElement(string winFormsTypeName, string expectedAvaloniaElementName)
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "_probe", ClrTypeName = winFormsTypeName };

        var mapped = registry.Map(control);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal(expectedAvaloniaElementName, mapped.AvaloniaElementName);
    }

    [Theory]
    [InlineData("HScrollBar", "Horizontal")]
    [InlineData("VScrollBar", "Vertical")]
    public void Map_ScrollBarControls_MapToScrollBarWithFixedOrientation(string winFormsTypeName, string expectedOrientation)
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "_probe", ClrTypeName = winFormsTypeName };

        var mapped = registry.Map(control);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("ScrollBar", mapped.AvaloniaElementName);
        Assert.Equal(expectedOrientation, mapped.Attributes["Orientation"]);
    }

    [Theory]
    [InlineData("MenuStrip", "Menu")]
    [InlineData("ToolStripMenuItem", "MenuItem")]
    [InlineData("ToolStripSeparator", "Separator")]
    [InlineData("ToolStripButton", "Button")]
    [InlineData("ToolStripLabel", "TextBlock")]
    [InlineData("ToolStripStatusLabel", "TextBlock")]
    [InlineData("ToolStripComboBox", "ComboBox")]
    [InlineData("ToolStripTextBox", "TextBox")]
    [InlineData("ToolStripProgressBar", "ProgressBar")]
    [InlineData("DataGridViewTextBoxColumn", "DataGridTextColumn")]
    [InlineData("DataGridViewCheckBoxColumn", "DataGridCheckBoxColumn")]
    [InlineData("ColumnHeader", "DataGridTextColumn")]
    [InlineData("Splitter", "GridSplitter")]
    [InlineData("LinkLabel", "HyperlinkButton")]
    public void Map_ToolStripItemAndDataGridColumnFamilies_MapDirectlyToExpectedAvaloniaElement(string winFormsTypeName, string expectedAvaloniaElementName)
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "_probe", ClrTypeName = winFormsTypeName };

        var mapped = registry.Map(control);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal(expectedAvaloniaElementName, mapped.AvaloniaElementName);
    }

    /// <summary>
    /// Avalonia's DataGrid has no ComboBox/Button/Image/Link column type - mapping to a
    /// DataGridComboBoxColumn used to be an AVLN2000 build break in the generated project.
    /// </summary>
    [Theory]
    [InlineData("DataGridViewComboBoxColumn", "ComboBox")]
    [InlineData("DataGridViewButtonColumn", "Button")]
    [InlineData("DataGridViewImageColumn", "Image")]
    [InlineData("DataGridViewLinkColumn", "HyperlinkButton")]
    public void Map_ColumnsWithoutAnAvaloniaColumnType_BecomeTemplateColumnsCarryingACellTemplate(
        string winFormsTypeName, string expectedCellElementName)
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "_probe", ClrTypeName = winFormsTypeName };
        control.Properties["HeaderText"] = new PropertyValue.Literal("Action");

        var mapped = registry.Map(control);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("DataGridTemplateColumn", mapped.AvaloniaElementName);
        Assert.Equal("Action", mapped.Attributes["Header"]);
        Assert.False(mapped.SupportsName);
        Assert.Equal("Avalonia.Controls.DataGrid", mapped.RequiredNuGetPackage);

        var cellTemplate = Assert.Single(mapped.NestedElements);
        Assert.Equal("DataGridTemplateColumn.CellTemplate", cellTemplate.ElementName);
        var dataTemplate = Assert.Single(cellTemplate.Children);
        Assert.Equal("DataTemplate", dataTemplate.ElementName);
        Assert.Equal(expectedCellElementName, Assert.Single(dataTemplate.Children).ElementName);
    }

    [Fact]
    public void Map_ListViewInDetailsMode_MapsToADataGridSoItsColumnHeadersHaveAHome()
    {
        var registry = new ControlMappingRegistry();
        var listView = new ControlModel { FieldName = "listView1", ClrTypeName = "ListView" };
        listView.Properties["View"] = new PropertyValue.EnumMembers(["Details"]);

        var mapped = registry.Map(listView);

        Assert.Equal("DataGrid", mapped.AvaloniaElementName);
        Assert.Equal(["DataGrid.Columns"], mapped.ChildWrapperElementNames);
        Assert.Equal("Avalonia.Controls.DataGrid", mapped.RequiredNuGetPackage);
    }

    [Fact]
    public void Map_ListViewWithColumnHeaderChildren_MapsToADataGridEvenWithoutAnExplicitDetailsView()
    {
        var registry = new ControlMappingRegistry();
        var listView = new ControlModel { FieldName = "listView1", ClrTypeName = "ListView" };
        listView.Children.Add(new ControlModel { FieldName = "nameColumn", ClrTypeName = "ColumnHeader" });

        Assert.Equal("DataGrid", registry.Map(listView).AvaloniaElementName);
    }

    [Fact]
    public void Map_ListViewWithoutColumns_StaysAPlainListBox()
    {
        var registry = new ControlMappingRegistry();
        var listView = new ControlModel { FieldName = "listView1", ClrTypeName = "ListView" };
        listView.Properties["View"] = new PropertyValue.EnumMembers(["LargeIcon"]);

        var mapped = registry.Map(listView);

        Assert.Equal("ListBox", mapped.AvaloniaElementName);
        Assert.Null(mapped.RequiredNuGetPackage);
    }

    [Theory]
    [InlineData("DomainUpDown", "DomainUpDownFallback")]
    [InlineData("ToolStripContainer", "ToolStripContainerFallback")]
    [InlineData("ToolStripPanel", "ToolStripPanelFallback")]
    [InlineData("ToolStripContentPanel", "ToolStripContentPanelFallback")]
    [InlineData("PropertyGrid", "PropertyGridFallback")]
    [InlineData("BindingNavigator", "BindingNavigatorFallback")]
    [InlineData("WebBrowser", "WebBrowserFallback")]
    [InlineData("PrintPreviewControl", "PrintPreviewControlFallback")]
    public void Map_QuickWinFallbackControls_ReturnFallbackStatusAndTemplateKey(string winFormsTypeName, string expectedTemplateKey)
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "_probe", ClrTypeName = winFormsTypeName };

        var mapped = registry.Map(control);

        Assert.Equal(MappingStatus.Fallback, mapped.Status);
        Assert.Equal(expectedTemplateKey, mapped.FallbackTemplateKey);
    }

    [Fact]
    public void Map_DomainUpDown_TranslatesWrapProperty()
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "domainUpDown1", ClrTypeName = "DomainUpDown" };
        control.Properties["Wrap"] = new PropertyValue.Literal(false);

        var mapped = registry.Map(control);

        Assert.Equal("False", mapped.Attributes["Wrap"]);
    }

    [Theory]
    [InlineData("Timer")]
    [InlineData("ToolStripControlHost")]
    [InlineData("HelpProvider")]
    public void Map_NewUnsupportedEntries_ReturnUnsupportedStatusAndGuidance(string winFormsTypeName)
    {
        var registry = new ControlMappingRegistry();
        var control = new ControlModel { FieldName = "_probe", ClrTypeName = winFormsTypeName };

        var mapped = registry.Map(control);

        Assert.Equal(MappingStatus.Unsupported, mapped.Status);
        Assert.Null(mapped.AvaloniaElementName);
        Assert.NotEmpty(mapped.Warnings);
    }

    [Fact]
    public void Map_FallbackControl_ReturnsFallbackStatusAndTemplateKey()
    {
        var registry = new ControlMappingRegistry();
        var richTextBox = new ControlModel { FieldName = "richTextBox1", ClrTypeName = "RichTextBox" };

        var mapped = registry.Map(richTextBox);

        Assert.Equal(MappingStatus.Fallback, mapped.Status);
        Assert.Equal("RichTextBoxFallback", mapped.FallbackTemplateKey);
        Assert.NotEmpty(mapped.Warnings);
    }

    /// <summary>
    /// Avalonia 12 ships both of these, so they stopped being fallbacks. GroupBox wraps its
    /// children in a Canvas like every other converted container, and MaskedTextBox finally
    /// carries the mask itself instead of storing it for a human to re-apply.
    /// </summary>
    [Fact]
    public void Map_GroupBox_IsADirectGroupBoxWrappingItsChildrenInACanvas()
    {
        var registry = new ControlMappingRegistry();
        var groupBox = new ControlModel { FieldName = "groupBox1", ClrTypeName = "GroupBox" };
        groupBox.Properties["Text"] = new PropertyValue.Literal("Options");

        var mapped = registry.Map(groupBox);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("GroupBox", mapped.AvaloniaElementName);
        Assert.Equal("Options", mapped.Attributes["Header"]);
        Assert.Equal(["Canvas"], mapped.ChildWrapperElementNames);
        Assert.Null(mapped.FallbackTemplateKey);
    }

    /// <summary>
    /// The half of DateTimePicker that used to be thrown away: a Format=Time picker is a clock,
    /// and a CalendarDatePicker cannot hold a time of day at all.
    /// </summary>
    [Theory]
    [InlineData("Time", "TimePicker")]
    [InlineData("Short", "CalendarDatePicker")]
    [InlineData("Long", "CalendarDatePicker")]
    public void Map_DateTimePicker_PicksTheControlTheFormatAsksFor(string format, string expectedElement)
    {
        var control = new ControlModel { FieldName = "picker1", ClrTypeName = "DateTimePicker" };
        control.Properties["Format"] = new PropertyValue.EnumMembers([format]);

        var mapped = new ControlMappingRegistry().Map(control);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal(expectedElement, mapped.AvaloniaElementName);
        Assert.Empty(mapped.Warnings);
    }

    /// <summary>No Format at all is WinForms' default, which is a date.</summary>
    [Fact]
    public void Map_DateTimePickerWithNoFormat_IsADatePicker()
    {
        var mapped = new ControlMappingRegistry()
            .Map(new ControlModel { FieldName = "picker1", ClrTypeName = "DateTimePicker" });

        Assert.Equal("CalendarDatePicker", mapped.AvaloniaElementName);
    }

    [Fact]
    public void Map_DateTimePickerWithCustomFormat_KeepsTheDatePickerAndReportsTheFormatString()
    {
        var control = new ControlModel { FieldName = "picker1", ClrTypeName = "DateTimePicker" };
        control.Properties["Format"] = new PropertyValue.EnumMembers(["Custom"]);

        var mapped = new ControlMappingRegistry().Map(control);

        Assert.Equal("CalendarDatePicker", mapped.AvaloniaElementName);
        Assert.Contains(mapped.Warnings, w => w.Contains("CustomFormat", StringComparison.Ordinal));
    }

    /// <summary>
    /// A ListBox, and no <c>SelectionMode="Multiple"</c>. That attribute used to stand in for
    /// "several items are ticked at once" - a defensible approximation only while the tick had
    /// nowhere else to live. It has one now (an ItemTemplate with a CheckBox), so a converted
    /// CheckedListBox selects the way the original did, and the two states are separate again.
    /// </summary>
    [Fact]
    public void Map_CheckedListBox_IsAPlainListBoxAndSaysWhereTheTickWent()
    {
        var mapped = new ControlMappingRegistry()
            .Map(new ControlModel { FieldName = "optionsList", ClrTypeName = "CheckedListBox" });

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("ListBox", mapped.AvaloniaElementName);
        Assert.DoesNotContain("SelectionMode", mapped.Attributes.Keys);
        Assert.Contains(mapped.Warnings, w => w.Contains("optionsList", StringComparison.Ordinal));
        Assert.Contains(mapped.Warnings, w => w.Contains("ItemTemplate", StringComparison.Ordinal));
    }

    [Fact]
    public void Map_MaskedTextBox_IsADirectMaskedTextBoxCarryingTheMask()
    {
        var registry = new ControlMappingRegistry();
        var masked = new ControlModel { FieldName = "phoneBox", ClrTypeName = "MaskedTextBox" };
        masked.Properties["Mask"] = new PropertyValue.Literal("(000) 000-0000");
        masked.Properties["PromptChar"] = new PropertyValue.Literal('_');
        masked.Properties["AsciiOnly"] = new PropertyValue.Literal(true);

        var mapped = registry.Map(masked);

        Assert.Equal(MappingStatus.Direct, mapped.Status);
        Assert.Equal("MaskedTextBox", mapped.AvaloniaElementName);
        Assert.Equal("(000) 000-0000", mapped.Attributes["Mask"]);
        Assert.Equal("_", mapped.Attributes["PromptChar"]);
        Assert.Equal("True", mapped.Attributes["AsciiOnly"]);
    }

    [Fact]
    public void Map_UnsupportedControl_ReturnsUnsupportedStatusAndGuidance()
    {
        var registry = new ControlMappingRegistry();
        var worker = new ControlModel { FieldName = "backgroundWorker1", ClrTypeName = "BackgroundWorker" };

        var mapped = registry.Map(worker);

        Assert.Equal(MappingStatus.Unsupported, mapped.Status);
        Assert.Null(mapped.AvaloniaElementName);
        Assert.NotEmpty(mapped.Warnings);
    }

    [Fact]
    public void Map_UnknownControlType_ReturnsUnsupportedWithDiagnosticWarning()
    {
        var registry = new ControlMappingRegistry();
        var custom = new ControlModel { FieldName = "myCustomControl1", ClrTypeName = "SomeThirdPartyGrid" };

        var mapped = registry.Map(custom);

        Assert.Equal(MappingStatus.Unsupported, mapped.Status);
        Assert.Contains(mapped.Warnings, w => w.Contains("SomeThirdPartyGrid"));
    }

    [Fact]
    public void Mappers_EveryRegisteredKeyMatchesItsOwnWinFormsTypeName()
    {
        var registry = new ControlMappingRegistry();

        foreach (var (key, mapper) in registry.Mappers)
        {
            Assert.Equal(key, mapper.WinFormsTypeName);
        }
    }
}
