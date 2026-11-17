using WinFormsToAvalonia.Core.Mapping;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

public class PlainDotNetTypeCatalogTests
{
    [Theory]
    [InlineData("void", true)]
    [InlineData("int", true)]
    [InlineData("string?", true)]
    [InlineData("int?", true)]
    [InlineData("DateTime", true)]
    [InlineData("System.TimeSpan", true)]
    [InlineData("TimeSpan?", true)]
    [InlineData("Guid[]", true)]
    [InlineData("string[]", true)]
    [InlineData("Color", false)]
    [InlineData("Font", false)]
    [InlineData("List<string>", false)]
    [InlineData("Other.DateTime", false)]
    public void IsPlain_AcceptsKeywordAndKnownSystemTypesOnly(string typeText, bool expected)
    {
        Assert.Equal(expected, PlainDotNetTypeCatalog.IsPlain(typeText));
    }
}
