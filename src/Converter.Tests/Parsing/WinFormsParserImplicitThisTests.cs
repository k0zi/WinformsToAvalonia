using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

/// <summary>
/// Real Visual Studio designer output always writes "this."-qualified statements
/// ("this.Controls.Add(this.textBox1); this.textBox1.Dock = ...;"), one statement per property.
/// A hand-written custom control (no Designer.cs split - see SingleFileCustomControlDiscovery)
/// commonly instead uses implicit "this." ("Controls.Add(_textBox);") and combines creation +
/// property initialization in one statement via an object initializer
/// ("_textBox = new TextBox { Dock = ..., PlaceholderText = ... };"). These tests cover
/// WinFormsParser's support for that shape specifically.
/// </summary>
public class WinFormsParserImplicitThisTests
{
    private const string ImplicitThisContent = """
        namespace SampleApp
        {
            public class AutocompleteSearchBox : System.Windows.Forms.UserControl
            {
                private System.Windows.Forms.TextBox _textBox;

                public AutocompleteSearchBox()
                {
                    InitializeComponent();
                }

                private void InitializeComponent()
                {
                    _textBox = new System.Windows.Forms.TextBox
                    {
                        Dock = System.Windows.Forms.DockStyle.Fill,
                        PlaceholderText = "Search products..."
                    };
                    _textBox.TextChanged += TextBox_TextChanged;

                    Controls.Add(_textBox);
                }

                private void TextBox_TextChanged(object sender, System.EventArgs e) { }
            }
        }
        """;

    private static async Task<string> WriteTempFileAsync(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-implicitthis-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [Fact]
    public async Task ParseDesignerFileAsync_ImplicitThisControlsAdd_LinksChildToRoot()
    {
        var path = await WriteTempFileAsync(ImplicitThisContent);
        try
        {
            var result = await new WinFormsParser().ParseDesignerFileAsync(path);

            Assert.Contains("_textBox", result.RootControl!.Children.Select(c => c.Name));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseDesignerFileAsync_ObjectInitializerCreation_CapturesProperties()
    {
        var path = await WriteTempFileAsync(ImplicitThisContent);
        try
        {
            var result = await new WinFormsParser().ParseDesignerFileAsync(path);

            var textBox = result.AllControls.Single(c => c.Name == "_textBox");
            Assert.Equal("System.Windows.Forms.DockStyle.Fill", textBox.Properties["Dock"].Value);
            Assert.Equal("Search products...", textBox.Properties["PlaceholderText"].Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseDesignerFileAsync_ImplicitThisControlsAdd_StillPopulatesEventHandlers()
    {
        var path = await WriteTempFileAsync(ImplicitThisContent);
        try
        {
            var result = await new WinFormsParser().ParseDesignerFileAsync(path);

            var textBox = result.AllControls.Single(c => c.Name == "_textBox");
            Assert.Equal("TextBox_TextChanged", textBox.EventHandlers["TextChanged"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseDesignerFileAsync_ExplicitThisControlsAdd_StillWorksUnchanged()
    {
        // Regression guard: the widened bare-identifier "Controls.Add(...)" path must not
        // interfere with the ordinary "this.Controls.Add(...)" shape real designer output uses.
        const string explicitThisContent = """
            namespace SampleApp
            {
                partial class SampleForm
                {
                    private System.Windows.Forms.Button button1;

                    private void InitializeComponent()
                    {
                        this.button1 = new System.Windows.Forms.Button();
                        this.SuspendLayout();
                        this.button1.Name = "button1";
                        this.Controls.Add(this.button1);
                        this.Name = "SampleForm";
                        this.ResumeLayout(false);
                    }
                }
            }
            """;

        var path = await WriteTempFileAsync(explicitThisContent);
        try
        {
            var result = await new WinFormsParser().ParseDesignerFileAsync(path);

            Assert.Contains("button1", result.RootControl!.Children.Select(c => c.Name));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
