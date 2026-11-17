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
        // A dynamically computed icon has nothing static to resolve to.
        Assert.IsType<PropertyValue.Unresolved>(Evaluate("System.Drawing.Icon.FromHandle(someHandle)"));
    }

    /// <summary>
    /// The shape a non-localizable form uses for images and icons. The cast and parentheses are
    /// stripped by Unwrap, leaving the resource key for ConversionPipeline to resolve against
    /// the form's .resx.
    /// </summary>
    [Theory]
    [InlineData("((System.Drawing.Icon)(resources.GetObject(\"notifyIcon.Icon\")))", "notifyIcon.Icon")]
    [InlineData("((System.Drawing.Image)(resources.GetObject(\"pictureBox1.Image\")))", "pictureBox1.Image")]
    [InlineData("resources.GetString(\"label1.Text\")", "label1.Text")]
    public void Evaluate_ResourceLookup_BecomesAResourceReference(string expression, string expectedKey)
    {
        Assert.Equal(new PropertyValue.ResourceReference(expectedKey), Evaluate(expression));
    }

    /// <summary>
    /// "Project resource file" images and strings. These used to read as an enum member named
    /// after the resource - so a Label showed the word "Greeting".
    /// </summary>
    [Theory]
    [InlineData("global::App.Properties.Resources.logo", "logo")]
    [InlineData("((System.Drawing.Image)(global::App.Properties.Resources.logo))", "logo")]
    [InlineData("App.Properties.Resources.Greeting", "Greeting")]
    [InlineData("Properties.Resources.Greeting", "Greeting")]
    [InlineData("global::Properties.Resources.Greeting", "Greeting")]
    public void Evaluate_ProjectResource_BecomesAProjectResource(string expression, string expectedName)
    {
        Assert.Equal(new PropertyValue.ProjectResource(expectedName), Evaluate(expression));
    }

    [Theory]
    [InlineData("global::App.Properties.Settings.Default.UserName", "UserName")]
    [InlineData("Properties.Settings.Default.UserName", "UserName")]
    public void Evaluate_ProjectSetting_BecomesAProjectSetting(string expression, string expectedName)
    {
        Assert.Equal(new PropertyValue.ProjectSetting(expectedName), Evaluate(expression));
    }

    [Theory]
    [InlineData("Resources.logo")]
    [InlineData("App.Resources.logo")]
    [InlineData("System.Windows.Forms.DockStyle.Fill")]
    public void Evaluate_NotAProjectResource_IsNotOne(string expression)
    {
        Assert.IsNotType<PropertyValue.ProjectResource>(Evaluate(expression));
    }

    [Fact]
    public void Evaluate_ResourceLookupWithoutALiteralKey_StaysUnresolved()
    {
        Assert.IsType<PropertyValue.Unresolved>(Evaluate("resources.GetObject(keyVariable)"));
    }

    private static PropertyValue Evaluate(string expressionText)
    {
        var expression = SyntaxFactory.ParseExpression(expressionText);
        return ExpressionEvaluator.Evaluate(expression);
    }
    /// <summary>
    /// The designer writes every decimal as its four <c>decimal.GetBits</c> words. Unread, every
    /// NumericUpDown's Minimum/Maximum/Value/Increment was silently dropped.
    /// </summary>
    [Theory]
    [InlineData("new decimal(new int[] { 1000, 0, 0, 0 })", "1000")]
    [InlineData("new System.Decimal(new int[] { 25, 0, 0, 65536 })", "2.5")]
    [InlineData("new decimal(new int[] { 5, 0, 0, -2147483648 })", "-5")]
    public void Evaluate_DesignerDecimal(string expression, string expected)
    {
        Assert.Equal(
            new PropertyValue.Literal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)),
            Evaluate(expression));
    }

    [Fact]
    public void Evaluate_DecimalWithANonLiteralWord_IsNotGuessed()
    {
        Assert.IsType<PropertyValue.Unresolved>(Evaluate("new decimal(new int[] { x, 0, 0, 0 })"));
    }
    /// <summary>`2147483648` does not fit an int, so C# types it uint - and its negation was returned positive.</summary>
    [Fact]
    public void Evaluate_NegatedUintLiteral_IsNegative()
    {
        Assert.Equal(new PropertyValue.Literal(-2147483648L), Evaluate("-2147483648"));
    }
}
