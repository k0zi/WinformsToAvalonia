using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

public class FormWindowAttributeCatalogTests
{
    private static WindowChromeTranslation Translate(
        Dictionary<string, PropertyValue> properties, int? clientWidth = 300, int? clientHeight = 250) =>
        FormWindowAttributeCatalog.Translate("Form1", properties, clientWidth, clientHeight);

    private static PropertyValue Enum(string member) => new PropertyValue.EnumMembers([member]);

    private static PropertyValue Bool(bool value) => new PropertyValue.Literal(value);

    [Fact]
    public void Translate_NothingSet_EmitsNothing()
    {
        var result = Translate([]);

        Assert.Empty(result.Attributes);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Translate_IconCopiedToAssets_BecomesTheWindowIcon()
    {
        var result = Translate(new() { ["Icon"] = new PropertyValue.Literal("/Assets/Form1_Icon.ico") });

        Assert.Equal([("Icon", "/Assets/Form1_Icon.ico")], result.Attributes);
    }

    [Fact]
    public void Translate_IconThatIsNotAnAsset_EmitsNothing()
    {
        // A bare path from `new Icon("app.ico")` has not been copied anywhere.
        var result = Translate(new() { ["Icon"] = new PropertyValue.Literal("app.ico") });

        Assert.Empty(result.Attributes);
    }

    [Theory]
    [InlineData("CenterScreen", "CenterScreen")]
    [InlineData("CenterParent", "CenterOwner")]
    public void Translate_StartPosition_BecomesWindowStartupLocation(string startPosition, string expected)
    {
        var result = Translate(new() { ["StartPosition"] = Enum(startPosition) });

        Assert.Equal([("WindowStartupLocation", expected)], result.Attributes);
    }

    [Fact]
    public void Translate_ManualPositionWithLocation_IsReportedNotGuessed()
    {
        var result = Translate(new()
        {
            ["StartPosition"] = Enum("Manual"),
            ["Location"] = new PropertyValue.PointValue(100, 100),
        });

        Assert.Empty(result.Attributes);
        Assert.Contains(result.Warnings, w => w.Contains("StartPosition = Manual"));
    }

    [Fact]
    public void Translate_DesignerSetWindowProperties_ReuseTheHandlerCatalogsSpelling()
    {
        var result = Translate(new()
        {
            ["TopMost"] = Bool(true),
            ["ShowInTaskbar"] = Bool(false),
            ["Opacity"] = new PropertyValue.Literal(0.85),
            ["WindowState"] = Enum("Maximized"),
        });

        Assert.Equal(
            [("Topmost", "True"), ("ShowInTaskbar", "False"), ("Opacity", "0.85"), ("WindowState", "Maximized")],
            result.Attributes);
    }

    [Theory]
    [InlineData("FixedDialog")]
    [InlineData("FixedSingle")]
    [InlineData("Fixed3D")]
    public void Translate_FixedBorder_CannotResize(string borderStyle)
    {
        var result = Translate(new()
        {
            ["FormBorderStyle"] = Enum(borderStyle),
            ["MaximizeBox"] = Bool(false),
            ["MinimizeBox"] = Bool(false),
        });

        Assert.Equal([("CanResize", "False"), ("CanMinimize", "False"), ("CanMaximize", "False")], result.Attributes);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Translate_BorderlessForm_HasNoDecorations()
    {
        var result = Translate(new() { ["FormBorderStyle"] = Enum("None"), ["ControlBox"] = Bool(false) });

        // ControlBox means nothing without a title bar, so it adds neither attributes nor a warning.
        Assert.Equal([("WindowDecorations", "None")], result.Attributes);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Translate_NoControlBox_DisablesWhatItCanAndSaysWhatItCannot()
    {
        var result = Translate(new() { ["ControlBox"] = Bool(false), ["MaximizeBox"] = Bool(false) });

        // CanMaximize is asked for twice and written once - a duplicate attribute would not parse.
        Assert.Equal([("CanMinimize", "False"), ("CanMaximize", "False")], result.Attributes);
        Assert.Contains(result.Warnings, w => w.Contains("close button"));
    }

    [Fact]
    public void Translate_SizeLimits_AreClampedToTheDesignedClientSize()
    {
        var result = Translate(new()
        {
            // The usual designer habit: the outer size of the form as it was drawn.
            ["MinimumSize"] = new PropertyValue.SizeValue(316, 289),
            // 0 is "no limit" on that axis.
            ["MaximumSize"] = new PropertyValue.SizeValue(800, 0),
        });

        Assert.Equal([("MinWidth", "300"), ("MinHeight", "250"), ("MaxWidth", "800")], result.Attributes);
        Assert.Single(result.Warnings);
    }

    /// <summary>KeyPreview is carried by the planner (tunnelled key handlers), not as window chrome.</summary>
    [Fact]
    public void Translate_KeyPreview_IsNotWindowChrome()
    {
        var result = Translate(new() { ["KeyPreview"] = Bool(true) });

        Assert.Empty(result.Attributes);
        Assert.Empty(result.Warnings);
    }
}
