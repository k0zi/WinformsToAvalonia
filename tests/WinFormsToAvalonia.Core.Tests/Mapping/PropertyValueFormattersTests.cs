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
}
