using WinFormsToAvalonia.Core.Emission;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Emission;

public class NamingConventionsTests
{
    [Theory]
    [InlineData("/home/user/out/MyAvaloniaApp", "MyAvaloniaApp")]
    [InlineData("./out/My-App.v2", "My_App_v2")]
    [InlineData("out/", "out")]
    [InlineData("out/9App", "_9App")]
    public void DeriveProjectName_SanitizesToValidIdentifier(string outputDirectory, string expected)
    {
        var name = NamingConventions.DeriveProjectName(outputDirectory);

        Assert.Equal(expected, name);
    }
}
