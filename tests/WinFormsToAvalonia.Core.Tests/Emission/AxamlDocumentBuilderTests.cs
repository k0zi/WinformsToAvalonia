using System.Xml.Linq;
using WinFormsToAvalonia.Core.Emission;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Emission;

public class AxamlDocumentBuilderTests
{
    /// <summary>
    /// A parser normalizes a literal line break in an attribute value to a space, so a multi-line
    /// Label arrived as one line; written as character references, it survives.
    /// </summary>
    [Fact]
    public void Attribute_LineBreaksAndTabs_SurviveParsing()
    {
        var builder = new AxamlDocumentBuilder();
        builder.OpenElement("TextBlock");
        builder.Attribute("Text", "Line one\r\nLine two\tend & \"quoted\" <x>");
        builder.CloseElement();

        var parsed = XDocument.Parse(builder.ToString());

        Assert.Equal("Line one\r\nLine two\tend & \"quoted\" <x>", parsed.Root!.Attribute("Text")!.Value);
    }

    /// <summary>
    /// An XML comment may not contain `--`. A pairwise replace turned `---` into `- --`, which
    /// still does - and the whole document failed to parse.
    /// </summary>
    [Theory]
    [InlineData("Status ---- unresolved")]
    [InlineData("a---b")]
    [InlineData("ends with -")]
    [InlineData("--")]
    public void Comment_AnyRunOfDashes_StaysAParsableComment(string text)
    {
        var builder = new AxamlDocumentBuilder();
        builder.OpenElement("Canvas");
        builder.Comment(text);
        builder.CloseElement();

        var comment = Assert.Single(XDocument.Parse(builder.ToString()).Root!.Nodes().OfType<XComment>());
        Assert.DoesNotContain("--", comment.Value);
    }
}
