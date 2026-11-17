using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

/// <summary>
/// The resx values must land as the *same* <see cref="PropertyValue"/> shapes designer C# produces -
/// that equivalence is what lets every downstream stage stay unaware of resources entirely.
/// </summary>
public class ResxPropertyProviderTests
{
    [Fact]
    public void Convert_StringEntryWithNoType_BecomesALiteral()
    {
        var value = ResxPropertyProvider.Convert(Entry("button1.Text", null, "OK"));

        Assert.Equal(new PropertyValue.Literal("OK"), value);
    }

    [Fact]
    public void Convert_Point_BecomesPointValue()
    {
        var value = ResxPropertyProvider.Convert(Entry("button1.Location", "System.Drawing.Point, System.Drawing", "12, 34"));

        Assert.Equal(new PropertyValue.PointValue(12, 34), value);
    }

    [Fact]
    public void Convert_Size_BecomesSizeValue()
    {
        var value = ResxPropertyProvider.Convert(Entry("$this.ClientSize", "System.Drawing.Size, System.Drawing", "284, 136"));

        Assert.Equal(new PropertyValue.SizeValue(284, 136), value);
    }

    [Theory]
    [InlineData("4, 2, 4, 2", 4, 2, 4, 2)]
    [InlineData("3", 3, 3, 3, 3)]
    public void Convert_Padding_BecomesPaddingValue(string raw, int left, int top, int right, int bottom)
    {
        var value = ResxPropertyProvider.Convert(
            Entry("button1.Padding", "System.Windows.Forms.Padding, System.Windows.Forms", raw));

        Assert.Equal(new PropertyValue.PaddingValue(left, top, right, bottom), value);
    }

    [Fact]
    public void Convert_FontWithoutStyle_BecomesFontValue()
    {
        var value = ResxPropertyProvider.Convert(Entry("$this.Font", "System.Drawing.Font, System.Drawing", "Segoe UI, 9pt"));

        Assert.Equal(new PropertyValue.FontValue("Segoe UI", 9f, []), value);
    }

    [Fact]
    public void Convert_FontWithMultipleStyleFlags_CollectsEveryFlag()
    {
        var value = ResxPropertyProvider.Convert(
            Entry("label1.Font", "System.Drawing.Font, System.Drawing", "Segoe UI, 9.75pt, style=Bold, Italic"));

        Assert.Equal(new PropertyValue.FontValue("Segoe UI", 9.75f, ["Bold", "Italic"]), value);
    }

    [Fact]
    public void Convert_ColorAsRgbTriple_BecomesOpaqueColorValue()
    {
        var value = ResxPropertyProvider.Convert(Entry("label1.ForeColor", "System.Drawing.Color, System.Drawing", "0, 90, 158"));

        Assert.Equal(new PropertyValue.ColorValue(null, 255, 0, 90, 158), value);
    }

    [Fact]
    public void Convert_ColorAsName_BecomesNamedColorValue()
    {
        var value = ResxPropertyProvider.Convert(Entry("label1.ForeColor", "System.Drawing.Color, System.Drawing", "Red"));

        Assert.Equal(new PropertyValue.ColorValue("Red", null, null, null, null), value);
    }

    [Fact]
    public void Convert_Boolean_BecomesBoolLiteral()
    {
        var value = ResxPropertyProvider.Convert(Entry("label1.AutoSize", "System.Boolean, mscorlib", "True"));

        Assert.Equal(new PropertyValue.Literal(true), value);
    }

    [Fact]
    public void Convert_EnumFlags_BecomeEnumMembers()
    {
        var value = ResxPropertyProvider.Convert(
            Entry("button1.Anchor", "System.Windows.Forms.AnchorStyles, System.Windows.Forms", "Bottom, Right"));

        Assert.Equal(new PropertyValue.EnumMembers(["Bottom", "Right"]), value);
    }

    /// <summary>A base64 payload is an asset to copy, not a value that can become a XAML attribute.</summary>
    [Fact]
    public void Convert_BinaryEntry_ReturnsNull()
    {
        var entry = new ResxEntry(
            "pictureBox1.Image",
            "System.Drawing.Bitmap, System.Drawing",
            "application/x-microsoft.net.object.bytearray.base64",
            "AAEC");

        Assert.Null(ResxPropertyProvider.Convert(entry));
    }

    [Fact]
    public void Convert_UnparseableValue_ReturnsNullRatherThanGuessing()
    {
        var value = ResxPropertyProvider.Convert(
            Entry("button1.Location", "System.Drawing.Point, System.Drawing", "not, a, point"));

        Assert.Null(value);
    }

    private static ResxEntry Entry(string name, string? typeName, string value) => new(name, typeName, null, value);
}
