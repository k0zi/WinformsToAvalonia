using WinFormsToAvalonia.Core.Emission;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Emission;

public class NamingConventionsTests
{
    /// <summary>
    /// A folder name becomes a namespace segment, and a folder can be called anything: a Form in
    /// `User Controls/2024` opened its generated file with a namespace that did not parse.
    /// </summary>
    [Theory]
    [InlineData("", "Demo.Views")]
    [InlineData("Admin/Users", "Demo.Views.Admin.Users")]
    [InlineData("User Controls/2024", "Demo.Views.User_Controls._2024")]
    [InlineData("Dialogs-Old/class", "Demo.Views.Dialogs_Old._class")]
    public void NamespaceOf_SanitizesEveryFolderSegment(string relativeFolder, string expected)
    {
        Assert.Equal(expected, NamingConventions.NamespaceOf("Demo.Views", relativeFolder));
    }

    /// <summary>
    /// The output directory's leaf name becomes the generated project's root namespace, so it has
    /// to be a legal identifier - every generated file opens with it.
    /// </summary>
    /// <remarks>
    /// The keyword rows are the interesting ones. `--output ./out` used to produce
    /// <c>namespace out;</c>, which does not parse, so the *entire* generated project failed to
    /// compile - found by accident while using `out` as a scratch directory. Only reserved words
    /// need the escape: `var` and `record` are contextual keywords and perfectly good namespaces.
    /// </remarks>
    [Theory]
    [InlineData("/home/user/out/MyAvaloniaApp", "MyAvaloniaApp")]
    [InlineData("./out/My-App.v2", "My_App_v2")]
    [InlineData("out/9App", "_9App")]
    [InlineData("out/", "_out")]
    [InlineData("/tmp/class", "_class")]
    [InlineData("/tmp/new", "_new")]
    [InlineData("/tmp/var", "var")]
    [InlineData("/tmp/record", "record")]
    public void DeriveProjectName_SanitizesToValidIdentifier(string outputDirectory, string expected)
    {
        var name = NamingConventions.DeriveProjectName(outputDirectory);

        Assert.Equal(expected, name);
    }
}
