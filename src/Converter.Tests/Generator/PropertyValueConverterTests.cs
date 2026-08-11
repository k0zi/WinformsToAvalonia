using Converter.Generator.Axaml;
using Converter.Mappings.BuiltIn;

namespace Converter.Tests.Generator;

public class PropertyValueConverterTests
{
    [Fact]
    public void Convert_MinimumSize_ConvertsToMinWidthMinHeight()
    {
        var mapping = PropertyMappingRegistry.GetMapping("MinimumSize")!;

        var result = PropertyValueConverter.Convert(mapping, "new System.Drawing.Size(200, 100)");

        Assert.NotNull(result);
        Assert.Contains(("MinWidth", "200"), result);
        Assert.Contains(("MinHeight", "100"), result);
    }

    [Fact]
    public void Convert_MaximumSize_ConvertsToMaxWidthMaxHeight()
    {
        var mapping = PropertyMappingRegistry.GetMapping("MaximumSize")!;

        var result = PropertyValueConverter.Convert(mapping, "new System.Drawing.Size(800, 600)");

        Assert.NotNull(result);
        Assert.Contains(("MaxWidth", "800"), result);
        Assert.Contains(("MaxHeight", "600"), result);
    }

    [Fact]
    public void Convert_MinimumSize_MalformedValue_ReturnsNull()
    {
        var mapping = PropertyMappingRegistry.GetMapping("MinimumSize")!;

        var result = PropertyValueConverter.Convert(mapping, "not a size");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("TopLeft", "Left", "Top")]
    [InlineData("MiddleCenter", "Center", "Center")]
    [InlineData("BottomRight", "Right", "Bottom")]
    public void Convert_TextAlign_ConvertsToHorizontalAndVerticalContentAlignment(
        string contentAlignmentValue, string expectedHorizontal, string expectedVertical)
    {
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign")!;

        var result = PropertyValueConverter.Convert(mapping, $"System.Drawing.ContentAlignment.{contentAlignmentValue}");

        Assert.NotNull(result);
        Assert.Contains(("HorizontalContentAlignment", expectedHorizontal), result);
        Assert.Contains(("VerticalContentAlignment", expectedVertical), result);
    }

    [Fact]
    public void Convert_TextAlign_MalformedValue_ReturnsNull()
    {
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign")!;

        var result = PropertyValueConverter.Convert(mapping, "not an alignment");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("None", "0")]
    [InlineData("FixedSingle", "1")]
    [InlineData("Fixed3D", "1")]
    public void Convert_BorderStyle_ConvertsToBorderThickness(string borderStyleValue, string expectedThickness)
    {
        var mapping = PropertyMappingRegistry.GetMapping("BorderStyle")!;

        var result = PropertyValueConverter.Convert(mapping, $"System.Windows.Forms.BorderStyle.{borderStyleValue}");

        Assert.NotNull(result);
        Assert.Contains(("BorderThickness", expectedThickness), result);
    }

    [Fact]
    public void Convert_BorderStyle_MalformedValue_ReturnsNull()
    {
        var mapping = PropertyMappingRegistry.GetMapping("BorderStyle")!;

        var result = PropertyValueConverter.Convert(mapping, "not a border style");

        Assert.Null(result);
    }

    [Fact]
    public void Convert_FormBorderStyle_StillRoutesToCanResizeNotBorderThickness()
    {
        // Regression guard: BorderStyle (Control) and FormBorderStyle (Form) both contain the
        // substring "BorderStyle." in their raw value text but must route to different
        // converters via PropertyMappingRegistry's distinct AvaloniaProperty keys.
        var mapping = PropertyMappingRegistry.GetMapping("FormBorderStyle", "Form")!;

        var result = PropertyValueConverter.Convert(mapping, "System.Windows.Forms.FormBorderStyle.FixedSingle");

        Assert.NotNull(result);
        Assert.Contains(("CanResize", "False"), result);
        Assert.DoesNotContain(result, r => r.AttributeName == "BorderThickness");
    }

    [Fact]
    public void Convert_AutoSizeTrue_RecognizedButEmitsNoAttribute()
    {
        // Avalonia controls already size to content by default in the common case - matching
        // WinForms AutoSize=true needs no explicit attribute, same precedent as Dock="Fill".
        var mapping = PropertyMappingRegistry.GetMapping("AutoSize")!;

        var result = PropertyValueConverter.Convert(mapping, "true");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Convert_AutoSizeFalse_StillFallsThroughToNull()
    {
        // Disabling auto-size needs explicit dimensions this property alone doesn't carry -
        // the manual step should still fire for this case.
        var mapping = PropertyMappingRegistry.GetMapping("AutoSize")!;

        var result = PropertyValueConverter.Convert(mapping, "false");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("TopLeft", "Left", "Top")]
    [InlineData("MiddleCenter", "Center", "Center")]
    [InlineData("BottomRight", "Right", "Bottom")]
    public void Convert_LabelTextAlign_ConvertsToTextAlignmentAndVerticalAlignment(
        string contentAlignmentValue, string expectedHorizontal, string expectedVertical)
    {
        // Label maps to Avalonia TextBlock, which has no HorizontalContentAlignment/
        // VerticalContentAlignment at all (unlike CheckBox/RadioButton) - must resolve to a
        // different mapping than the plain "TextAlign" lookup above.
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "Label")!;

        var result = PropertyValueConverter.Convert(mapping, $"System.Drawing.ContentAlignment.{contentAlignmentValue}");

        Assert.NotNull(result);
        Assert.Contains(("TextAlignment", expectedHorizontal), result);
        Assert.Contains(("VerticalAlignment", expectedVertical), result);
        Assert.DoesNotContain(result, p => p.AttributeName is "HorizontalContentAlignment" or "VerticalContentAlignment");
    }

    [Fact]
    public void Convert_ToolStripLabelTextAlign_AlsoUsesTextBlockConversion()
    {
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "ToolStripLabel")!;

        var result = PropertyValueConverter.Convert(mapping, "System.Drawing.ContentAlignment.MiddleLeft");

        Assert.NotNull(result);
        Assert.Contains(("TextAlignment", "Left"), result);
        Assert.Contains(("VerticalAlignment", "Center"), result);
    }

    [Fact]
    public void Convert_LabelTextAlign_MalformedValue_ReturnsNull()
    {
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "Label")!;

        var result = PropertyValueConverter.Convert(mapping, "not an alignment");

        Assert.Null(result);
    }

    [Fact]
    public void Convert_CheckBoxTextAlign_StillUsesContentAlignmentMapping()
    {
        // Regression guard: CheckBox/RadioButton are genuine ContentControl-derived Avalonia
        // types, so they must keep resolving to the common HorizontalContentAlignment/
        // VerticalContentAlignment mapping, not accidentally pick up Label's TextBlock-specific
        // override.
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "CheckBox")!;

        var result = PropertyValueConverter.Convert(mapping, "System.Drawing.ContentAlignment.MiddleCenter");

        Assert.NotNull(result);
        Assert.Contains(("HorizontalContentAlignment", "Center"), result);
        Assert.Contains(("VerticalContentAlignment", "Center"), result);
    }

    [Theory]
    [InlineData("Left", "Left")]
    [InlineData("Right", "Right")]
    [InlineData("Center", "Center")]
    public void Convert_TextBoxTextAlign_ConvertsToTextAlignment(string horizontalAlignmentValue, string expected)
    {
        // TextBox.TextAlign uses a different WinForms enum from Label's (System.Windows.Forms.
        // HorizontalAlignment, not System.Drawing.ContentAlignment) - no vertical component.
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "TextBox")!;

        var result = PropertyValueConverter.Convert(mapping, $"System.Windows.Forms.HorizontalAlignment.{horizontalAlignmentValue}");

        Assert.NotNull(result);
        Assert.Contains(("TextAlignment", expected), result);
        Assert.Single(result);
    }

    [Fact]
    public void Convert_TextBoxTextAlign_MalformedValue_ReturnsNull()
    {
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "TextBox")!;

        var result = PropertyValueConverter.Convert(mapping, "not an alignment");

        Assert.Null(result);
    }

    [Fact]
    public void Convert_TextBoxTextAlign_ContentAlignmentShapedValue_DoesNotMatch()
    {
        // TextBox's converter must not accidentally match Label's ContentAlignment shape (or
        // vice versa) despite sharing the WinForms property name "TextAlign".
        var mapping = PropertyMappingRegistry.GetMapping("TextAlign", "TextBox")!;

        var result = PropertyValueConverter.Convert(mapping, "System.Drawing.ContentAlignment.MiddleCenter");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("new System.DateTime(2024, 1, 1)", "2024-01-01")]
    [InlineData("new System.DateTime(2024,12,31)", "2024-12-31")]
    public void Convert_DateTimePickerValue_ConvertsDateOnlyConstructorToIsoDate(string rawValue, string expectedDate)
    {
        var mapping = PropertyMappingRegistry.GetMapping("Value", "DateTimePicker")!;

        var result = PropertyValueConverter.Convert(mapping, rawValue);

        Assert.NotNull(result);
        Assert.Contains(("SelectedDate", expectedDate), result);
    }

    [Theory]
    [InlineData("System.DateTime.Now")]
    [InlineData("System.DateTime.Today")]
    [InlineData("new System.DateTime(2024, 1, 1, 8, 30, 0)")]
    [InlineData("not a date")]
    public void Convert_DateTimePickerValue_UnrecognizedShape_ReturnsNull(string rawValue)
    {
        // Best-effort only: an unrecognized shape is dropped rather than guessed at, same as
        // every other converter in this file.
        var mapping = PropertyMappingRegistry.GetMapping("Value", "DateTimePicker")!;

        var result = PropertyValueConverter.Convert(mapping, rawValue);

        Assert.Null(result);
    }
}
