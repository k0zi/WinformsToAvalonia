using Microsoft.CodeAnalysis.CSharp;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class ExpressionEvaluatorTests
{
    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("42", 42)]
    [InlineData("-5", -5)]
    [InlineData("true", true)]
    public void Evaluate_Literals(string expression, object expected)
    {
        var value = Evaluate(expression);
        Assert.Equal(new PropertyValue.Literal(expected), value);
    }

    [Fact]
    public void Evaluate_Point()
    {
        Assert.Equal(new PropertyValue.PointValue(12, -3), Evaluate("new System.Drawing.Point(12, -3)"));
    }

    [Fact]
    public void Evaluate_Size()
    {
        Assert.Equal(new PropertyValue.SizeValue(100, 40), Evaluate("new Size(100, 40)"));
    }

    [Fact]
    public void Evaluate_PointEmpty_ReturnsZeroPointValue()
    {
        Assert.Equal(new PropertyValue.PointValue(0, 0), Evaluate("System.Drawing.Point.Empty"));
    }

    [Fact]
    public void Evaluate_SizeEmpty_ReturnsZeroSizeValue()
    {
        Assert.Equal(new PropertyValue.SizeValue(0, 0), Evaluate("System.Drawing.Size.Empty"));
    }

    [Fact]
    public void Evaluate_Padding_Uniform()
    {
        Assert.Equal(new PropertyValue.PaddingValue(4, 4, 4, 4), Evaluate("new System.Windows.Forms.Padding(4)"));
    }

    [Fact]
    public void Evaluate_Padding_FourArgs()
    {
        Assert.Equal(new PropertyValue.PaddingValue(1, 2, 3, 4), Evaluate("new Padding(1, 2, 3, 4)"));
    }

    [Fact]
    public void Evaluate_SingleEnumMember()
    {
        Assert.Equal(new PropertyValue.EnumMembers(["Fill"]), Evaluate("System.Windows.Forms.DockStyle.Fill"));
    }

    [Fact]
    public void Evaluate_OrCombinedEnumFlags_CastWrapped()
    {
        var result = Evaluate(
            "((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)))");

        Assert.Equal(new PropertyValue.EnumMembers(["Bottom", "Left"]), result);
    }

    [Fact]
    public void Evaluate_NamedColor()
    {
        Assert.Equal(new PropertyValue.ColorValue("Red", null, null, null, null), Evaluate("System.Drawing.Color.Red"));
    }

    [Fact]
    public void Evaluate_SystemColor()
    {
        Assert.Equal(new PropertyValue.ColorValue("Control", null, null, null, null), Evaluate("System.Drawing.SystemColors.Control"));
    }

    [Fact]
    public void Evaluate_ColorFromArgb_ThreeArgs()
    {
        Assert.Equal(
            new PropertyValue.ColorValue(null, 255, 10, 20, 30),
            Evaluate("System.Drawing.Color.FromArgb(10, 20, 30)"));
    }

    [Fact]
    public void Evaluate_ColorFromArgb_FourArgs()
    {
        Assert.Equal(
            new PropertyValue.ColorValue(null, 128, 10, 20, 30),
            Evaluate("System.Drawing.Color.FromArgb(128, 10, 20, 30)"));
    }

    [Fact]
    public void Evaluate_Font_WithStyle()
    {
        var result = Evaluate("new System.Drawing.Font(\"Segoe UI\", 9.75F, System.Drawing.FontStyle.Bold)");

        Assert.Equal(new PropertyValue.FontValue("Segoe UI", 9.75f, ["Bold"]), result);
    }

    [Fact]
    public void Evaluate_UnrecognizedExpression_BecomesUnresolved()
    {
        var result = Evaluate("SomeHelper.Compute(1, 2)");

        Assert.IsType<PropertyValue.Unresolved>(result);
    }

    [Fact]
    public void Evaluate_ThisFieldReference_ReturnsControlReference()
    {
        Assert.Equal(new PropertyValue.ControlReference("contextMenuStrip1"), Evaluate("this.contextMenuStrip1"));
    }

    [Fact]
    public void Evaluate_IconWithLiteralPath_ReturnsLiteral()
    {
        Assert.Equal(new PropertyValue.Literal("app.ico"), Evaluate("new System.Drawing.Icon(\"app.ico\")"));
    }

    [Fact]
    public void Evaluate_IconFromHandle_StaysUnresolved()
    {
        // The common real-world shapes (dynamic computation, resx lookup) aren't literal
        // paths - only `new Icon("literal.ico")` is recognized.
        Assert.IsType<PropertyValue.Unresolved>(Evaluate("System.Drawing.Icon.FromHandle(someHandle)"));
        Assert.IsType<PropertyValue.Unresolved>(Evaluate("((System.Drawing.Icon)(resources.GetObject(\"notifyIcon.Icon\")))"));
    }

    private static PropertyValue Evaluate(string expressionText)
    {
        var expression = SyntaxFactory.ParseExpression(expressionText);
        return ExpressionEvaluator.Evaluate(expression);
    }
}
