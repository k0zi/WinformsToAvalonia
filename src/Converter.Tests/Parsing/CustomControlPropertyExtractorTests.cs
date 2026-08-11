using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class CustomControlPropertyExtractorTests
{
    private const string CustomerCardCodeBehind = """
        namespace SampleApp
        {
            public partial class CustomerCard : System.Windows.Forms.UserControl
            {
                public int CustomerId { get; set; }

                public string CardTitle
                {
                    get => titleLabel.Text;
                    set => titleLabel.Text = value;
                }

                public object Payload { get; set; }

                public string ReadOnlyNote { get; }

                private string InternalState { get; set; }

                public static string SharedThing { get; set; }
            }
        }
        """;

    private static async Task<CustomControlPropertyExtractionResult> ExtractAsync(string content, string className = "CustomerCard")
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-customcontrolprops-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, content);
        try
        {
            return await CustomControlPropertyExtractor.ExtractAsync(path, className);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_PublicAutoPropertyOfSupportedType_IsBindable()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        Assert.Contains(result.Bindable, p => p.Name == "CustomerId" && p.TypeName == "int");
    }

    [Fact]
    public async Task ExtractAsync_PropertyWithCustomGetterSetterLogic_IsSkipped()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        var skipped = Assert.Single(result.Skipped, p => p.Name == "CardTitle");
        Assert.Contains("custom getter/setter", skipped.Reason);
        Assert.DoesNotContain(result.Bindable, p => p.Name == "CardTitle");
    }

    [Fact]
    public async Task ExtractAsync_UnsupportedType_IsSkipped()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        var skipped = Assert.Single(result.Skipped, p => p.Name == "Payload");
        Assert.Contains("not supported", skipped.Reason);
        Assert.DoesNotContain(result.Bindable, p => p.Name == "Payload");
    }

    [Fact]
    public async Task ExtractAsync_GetterOnlyProperty_IsSkippedNotBindable()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        Assert.DoesNotContain(result.Bindable, p => p.Name == "ReadOnlyNote");
        Assert.Contains(result.Skipped, p => p.Name == "ReadOnlyNote");
    }

    [Fact]
    public async Task ExtractAsync_NonPublicProperty_IsIgnoredEntirely()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        Assert.DoesNotContain(result.Bindable, p => p.Name == "InternalState");
        Assert.DoesNotContain(result.Skipped, p => p.Name == "InternalState");
    }

    [Fact]
    public async Task ExtractAsync_StaticProperty_IsIgnoredEntirely()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        Assert.DoesNotContain(result.Bindable, p => p.Name == "SharedThing");
        Assert.DoesNotContain(result.Skipped, p => p.Name == "SharedThing");
    }

    [Fact]
    public async Task ExtractAsync_ClassNameNotFound_ReturnsEmpty()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind, className: "SomethingElse");

        Assert.Empty(result.Bindable);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task ExtractAsync_MissingFile_ReturnsEmpty()
    {
        var result = await CustomControlPropertyExtractor.ExtractAsync(
            Path.Combine(Path.GetTempPath(), "wf2av-does-not-exist.cs"), "CustomerCard");

        Assert.Empty(result.Bindable);
        Assert.Empty(result.Skipped);
    }
}
