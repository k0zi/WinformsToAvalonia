using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

public class PropertyValueFormattersTests
{
    [Fact]
    public void AsBrush_FromArgbThreeComponents_EmitsOpaqueHex()
    {
        var value = new PropertyValue.ColorValue(null, 255, 0x1E, 0x90, 0xFF);

        Assert.Equal("#FF1E90FF", PropertyValueFormatters.AsBrush(value));
    }

    [Fact]
    public void AsBrush_FromArgbWithAlpha_PreservesAlpha()
    {
        var value = new PropertyValue.ColorValue(null, 0x80, 0x00, 0x00, 0x00);

        Assert.Equal("#80000000", PropertyValueFormatters.AsBrush(value));
    }

    [Theory]
    [InlineData("Red", "#FFFF0000")]
    [InlineData("White", "#FFFFFFFF")]
    [InlineData("CornflowerBlue", "#FF6495ED")]
    [InlineData("Transparent", "#00FFFFFF")]
    public void AsBrush_KnownWebColorName_ResolvesToExplicitArgb(string name, string expected)
    {
        var value = new PropertyValue.ColorValue(name, null, null, null, null);

        Assert.Equal(expected, PropertyValueFormatters.AsBrush(value));
    }

    /// <summary>
    /// System colors must come from the converter's own table, never from the host desktop
    /// palette - otherwise the same input project would emit different AXAML per machine.
    /// </summary>
    [Theory]
    [InlineData("Control", "#FFF0F0F0")]
    [InlineData("ControlText", "#FF000000")]
    [InlineData("Highlight", "#FF3399FF")]
    [InlineData("Window", "#FFFFFFFF")]
    public void AsBrush_SystemColorName_ResolvesFromTheDeterministicTable(string name, string expected)
    {
        var value = new PropertyValue.ColorValue(name, null, null, null, null);

        Assert.Equal(expected, PropertyValueFormatters.AsBrush(value));
    }

    [Fact]
    public void AsBrush_UnknownColorName_ReturnsNullSoNothingIsEmitted()
    {
        var value = new PropertyValue.ColorValue("NotAColourAnyoneKnows", null, null, null, null);

        Assert.Null(PropertyValueFormatters.AsBrush(value));
    }

    [Fact]
    public void AsBrush_NonColorValue_ReturnsNull()
    {
        Assert.Null(PropertyValueFormatters.AsBrush(new PropertyValue.Literal("Red")));
    }

    /// <summary>WinForms serializes points, Avalonia wants device-independent pixels (96/72).</summary>
    [Theory]
    [InlineData(9f, "12")]
    [InlineData(8.25f, "11")]
    [InlineData(12f, "16")]
    [InlineData(10f, "13.33")]
    public void AsFontSize_ConvertsPointsToDeviceIndependentPixels(float points, string expected)
    {
        var value = new PropertyValue.FontValue("Segoe UI", points, []);

        Assert.Equal(expected, PropertyValueFormatters.AsFontSize(value));
    }

    [Fact]
    public void AsFontFamily_ReturnsTheFamilyName()
    {
        var value = new PropertyValue.FontValue("Segoe UI", 9f, []);

        Assert.Equal("Segoe UI", PropertyValueFormatters.AsFontFamily(value));
    }

    [Fact]
    public void AsFontWeightAndStyle_OnlyEmitWhenTheDesignerAskedForThem()
    {
        var plain = new PropertyValue.FontValue("Segoe UI", 9f, []);

        Assert.Null(PropertyValueFormatters.AsFontWeight(plain));
        Assert.Null(PropertyValueFormatters.AsFontStyle(plain));
    }

    [Fact]
    public void AsFontWeightAndStyle_BoldItalicCombination_EmitsBoth()
    {
        var value = new PropertyValue.FontValue("Segoe UI", 9f, ["Bold", "Italic"]);

        Assert.Equal("Bold", PropertyValueFormatters.AsFontWeight(value));
        Assert.Equal("Italic", PropertyValueFormatters.AsFontStyle(value));
    }

    [Theory]
    [InlineData("Underline", "Underline")]
    [InlineData("Strikeout", "Strikethrough")]
    public void AsTextDecorations_MapsWinFormsFontStyleToAvaloniaDecoration(string flag, string expected)
    {
        var value = new PropertyValue.FontValue("Segoe UI", 9f, [flag]);

        Assert.Equal(expected, PropertyValueFormatters.AsTextDecorations(value));
    }

    [Fact]
    public void AsTextDecorations_BoldOnly_ReturnsNull()
    {
        var value = new PropertyValue.FontValue("Segoe UI", 9f, ["Bold"]);

        Assert.Null(PropertyValueFormatters.AsTextDecorations(value));
    }

    [Fact]
    public void AsThickness_EmitsLeftTopRightBottom()
    {
        var value = new PropertyValue.PaddingValue(1, 2, 3, 4);

        Assert.Equal("1,2,3,4", PropertyValueFormatters.AsThickness(value));
    }

    /// <summary>
    /// A binding path the XAML compiler cannot parse is an error in the *generated* project,
    /// which this project's own build never sees - so anything but a plain identifier refuses.
    /// </summary>
    [Theory]
    [InlineData("Name", "{ReflectionBinding Name}")]
    [InlineData("_private", "{ReflectionBinding _private}")]
    [InlineData("Column2", "{ReflectionBinding Column2}")]
    public void AsBinding_AcceptsAPlainIdentifier(string dataPropertyName, string expected) =>
        Assert.Equal(expected, PropertyValueFormatters.AsBinding(new PropertyValue.Literal(dataPropertyName)));

    [Theory]
    [InlineData("")]
    [InlineData(" Name")]
    [InlineData("2Name")]
    // Valid Avalonia, but WinForms does not resolve a dotted path either - its binding is against
    // one property on the row object - so accepting it would invent behaviour.
    [InlineData("Order.Total")]
    public void AsBinding_RefusesAnythingElse(string dataPropertyName) =>
        Assert.Null(PropertyValueFormatters.AsBinding(new PropertyValue.Literal(dataPropertyName)));

    [Fact]
    public void AsBinding_RefusesANonStringValue() =>
        Assert.Null(PropertyValueFormatters.AsBinding(new PropertyValue.Literal(42)));
}
