using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class SingleFileCustomControlDiscoveryTests
{
    private const string CompositeControlContent = """
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
                    _textBox = new System.Windows.Forms.TextBox();
                    Controls.Add(_textBox);
                }
            }
        }
        """;

    private const string OwnerDrawnControlContent = """
        namespace SampleApp
        {
            public class GaugeControl : System.Windows.Forms.Control
            {
                protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
                {
                    base.OnPaint(e);
                }
            }
        }
        """;

    private const string PlainUtilityClassContent = """
        namespace SampleApp
        {
            public static class MathHelpers
            {
                public static int Add(int a, int b) => a + b;
            }
        }
        """;

    [Fact]
    public async Task DiscoverAsync_CompositeCustomControl_IsFound()
    {
        var dir = Directory.CreateTempSubdirectory("wf2av-discovery-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "AutocompleteSearchBox.cs"), CompositeControlContent);

            var discovered = await SingleFileCustomControlDiscovery.DiscoverAsync(dir, new HashSet<string>(), []);

            Assert.Contains(discovered, f => f.EndsWith("AutocompleteSearchBox.cs"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverAsync_OwnerDrawnControl_IsNotFound()
    {
        // No InitializeComponent/child controls at all - there is no control tree to convert,
        // so this must stay out of discovery and fall through to SupportFileScanner's existing
        // "needs manual port" handling instead.
        var dir = Directory.CreateTempSubdirectory("wf2av-discovery-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "GaugeControl.cs"), OwnerDrawnControlContent);

            var discovered = await SingleFileCustomControlDiscovery.DiscoverAsync(dir, new HashSet<string>(), []);

            Assert.Empty(discovered);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverAsync_PlainUtilityClass_IsNotFound()
    {
        var dir = Directory.CreateTempSubdirectory("wf2av-discovery-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "MathHelpers.cs"), PlainUtilityClassContent);

            var discovered = await SingleFileCustomControlDiscovery.DiscoverAsync(dir, new HashSet<string>(), []);

            Assert.Empty(discovered);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverAsync_DesignerFile_IsNeverConsidered()
    {
        // Already handled by the ordinary *.Designer.cs discovery path - even if it somehow
        // had an InitializeComponent + UserControl base, it must not be double-discovered here.
        var dir = Directory.CreateTempSubdirectory("wf2av-discovery-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "AutocompleteSearchBox.Designer.cs"), CompositeControlContent);

            var discovered = await SingleFileCustomControlDiscovery.DiscoverAsync(dir, new HashSet<string>(), []);

            Assert.Empty(discovered);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverAsync_AlreadyExcludedFile_IsSkipped()
    {
        var dir = Directory.CreateTempSubdirectory("wf2av-discovery-").FullName;
        try
        {
            var path = Path.Combine(dir, "AutocompleteSearchBox.cs");
            await File.WriteAllTextAsync(path, CompositeControlContent);

            var discovered = await SingleFileCustomControlDiscovery.DiscoverAsync(
                dir, new HashSet<string> { path }, []);

            Assert.Empty(discovered);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
