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
}
