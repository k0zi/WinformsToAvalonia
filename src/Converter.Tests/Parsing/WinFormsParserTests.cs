using Converter.Core.Parsing;
using Converter.Tests.TestSupport;

namespace Converter.Tests.Parsing;

public class WinFormsParserTests
{
    private static readonly string SampleFormPath = FixturePath.Get("SampleForm.Designer.cs.txt");

    [Fact]
    public async Task ParseDesignerFileAsync_Succeeds_AndFindsRootFormNode()
    {
        var parser = new WinFormsParser();

        var result = await parser.ParseDesignerFileAsync(SampleFormPath);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.NotNull(result.RootControl);
        Assert.Equal("SampleForm", result.RootControl!.Name);
        Assert.Equal("Form", result.RootControl.ControlType);
    }

    [Fact]
    public async Task ParseDesignerFileAsync_FindsExpectedNamedControls()
    {
        var parser = new WinFormsParser();

        var result = await parser.ParseDesignerFileAsync(SampleFormPath);

        var button1 = result.AllControls.SingleOrDefault(c => c.Name == "button1");
        var textBox1 = result.AllControls.SingleOrDefault(c => c.Name == "textBox1");

        Assert.NotNull(button1);
        Assert.Equal("Button", button1!.ControlType);

        Assert.NotNull(textBox1);
        Assert.Equal("TextBox", textBox1!.ControlType);
    }

    [Fact]
    public async Task ParseDesignerFileAsync_PopulatesEventHandlers()
    {
        var parser = new WinFormsParser();

        var result = await parser.ParseDesignerFileAsync(SampleFormPath);

        var button1 = result.AllControls.Single(c => c.Name == "button1");

        Assert.True(button1.EventHandlers.TryGetValue("Click", out var handlerName));
        Assert.Equal("button1_Click", handlerName);
    }

    [Fact]
    public async Task ParseDesignerFileAsync_PopulatesDataBindings()
    {
        var parser = new WinFormsParser();

        var result = await parser.ParseDesignerFileAsync(SampleFormPath);

        var textBox1 = result.AllControls.Single(c => c.Name == "textBox1");

        var binding = Assert.Single(textBox1.DataBindings);
        Assert.Equal("Text", binding.PropertyName);
        Assert.Equal("CustomerName", binding.DataMember);
        Assert.True(binding.FormattingEnabled);
    }

    [Fact]
    public async Task ParseDesignerFileAsync_LinksTopLevelControlsAddChildrenToRoot()
    {
        // Regression test: "this.Controls.Add(...)" at the top of InitializeComponent must
        // attach children to the root form node. Previously "this" never resolved to the
        // root control's dictionary key, so RootControl.Children was always empty.
        var parser = new WinFormsParser();

        var result = await parser.ParseDesignerFileAsync(SampleFormPath);

        var childNames = result.RootControl!.Children.Select(c => c.Name).ToList();

        Assert.Contains("button1", childNames);
    }

    [Fact]
    public async Task ParseDesignerFileAsync_WalksIntoNestedIfBlocks()
    {
        // textBox1 is only added to Controls inside an `if (this.IsMdiContainer) { ... }`
        // block in the fixture - the parser must recurse into it, not just top-level statements.
        var parser = new WinFormsParser();

        var result = await parser.ParseDesignerFileAsync(SampleFormPath);

        var childNames = result.RootControl!.Children.Select(c => c.Name).ToList();

        Assert.Contains("textBox1", childNames);
    }

    [Fact]
    public async Task ParseDesignerFileAsync_DetectsCustomControlFieldsNotInKnownTypeList()
    {
        // customerCard1 is declared as SampleApp.Widgets.CustomerCard - not a recognized
        // WinForms/BCL type name - and should still be picked up as a custom control field.
        var parser = new WinFormsParser();

        var result = await parser.ParseDesignerFileAsync(SampleFormPath);

        var customerCard = result.AllControls.SingleOrDefault(c => c.Name == "customerCard1");

        Assert.NotNull(customerCard);
        Assert.True(customerCard!.IsCustomControl);
        Assert.Contains("customerCard1", result.RootControl!.Children.Select(c => c.Name));
    }
}
