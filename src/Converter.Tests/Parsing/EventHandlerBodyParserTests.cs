using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class EventHandlerBodyParserTests
{
    private const string CodeBehindContent = """
        namespace SampleApp
        {
            partial class SampleForm
            {
                private void button1_Click(object sender, System.EventArgs e)
                {
                    System.Windows.Forms.MessageBox.Show("Hello");
                }

                private void button2_Click(object sender, System.EventArgs e) => DoSomething();

                private void DoSomething() { }
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_FindsRequestedMethod_CapturesFullSourceVerbatim()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-handlerbody-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, CodeBehindContent);

        try
        {
            var result = await EventHandlerBodyParser.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            Assert.True(result.TryGetValue("button1_Click", out var source));
            Assert.Contains("button1_Click", source);
            Assert.Contains("MessageBox.Show(\"Hello\")", source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_ExpressionBodiedMethod_CapturesFullSource()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-handlerbody-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, CodeBehindContent);

        try
        {
            var result = await EventHandlerBodyParser.ExtractAsync(path, new HashSet<string> { "button2_Click" });

            Assert.True(result.TryGetValue("button2_Click", out var source));
            Assert.Contains("DoSomething()", source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_MethodNotPresent_HasNoEntryForIt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-handlerbody-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, CodeBehindContent);

        try
        {
            var result = await EventHandlerBodyParser.ExtractAsync(path, new HashSet<string> { "doesNotExist_Click" });

            Assert.False(result.ContainsKey("doesNotExist_Click"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_MissingFile_ReturnsEmptyWithoutThrowing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "wf2av-does-not-exist-" + Path.GetRandomFileName() + ".cs");

        var result = await EventHandlerBodyParser.ExtractAsync(missingPath, new HashSet<string> { "button1_Click" });

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractAsync_UnparseableFile_ReturnsEmptyWithoutThrowing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-handlerbody-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, "this is not valid C# {{{ at all !!");

        try
        {
            var result = await EventHandlerBodyParser.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            Assert.Empty(result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_EmptyMethodNameSet_ReturnsEmptyWithoutReadingFile()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "wf2av-does-not-exist-" + Path.GetRandomFileName() + ".cs");

        var result = await EventHandlerBodyParser.ExtractAsync(missingPath, new HashSet<string>());

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractBodyText_BlockBodiedMethod_ReturnsJustTheBlock()
    {
        const string fullSource = """
            private void button1_Click(object sender, System.EventArgs e)
            {
                System.Windows.Forms.MessageBox.Show("Hello");
            }
            """;

        var body = EventHandlerBodyParser.ExtractBodyText(fullSource);

        Assert.DoesNotContain("button1_Click", body);
        Assert.Contains("MessageBox.Show(\"Hello\");", body);
        Assert.StartsWith("{", body.TrimStart());
        Assert.EndsWith("}", body.TrimEnd());
    }

    [Fact]
    public void ExtractBodyText_ExpressionBodiedMethod_WrapsIntoBlock()
    {
        const string fullSource = "private void button2_Click(object sender, System.EventArgs e) => DoSomething();";

        var body = EventHandlerBodyParser.ExtractBodyText(fullSource);

        Assert.Contains("DoSomething();", body);
        Assert.StartsWith("{", body.TrimStart());
        Assert.EndsWith("}", body.TrimEnd());
    }

    [Fact]
    public void ExtractBodyText_UnparseableInput_FallsBackToCommentWrap()
    {
        const string garbage = "this is not a method at all {{{ !!";

        var body = EventHandlerBodyParser.ExtractBodyText(garbage);

        Assert.Contains("// this is not a method at all", body);
        Assert.StartsWith("{", body.TrimStart());
        Assert.EndsWith("}", body.TrimEnd());
    }

    [Fact]
    public void IsAsyncMethodSignature_AsyncVoidMethod_ReturnsTrue()
    {
        const string source = "private async void button1_Click(object sender, System.EventArgs e)\n{\n    await Foo();\n}";

        Assert.True(EventHandlerBodyParser.IsAsyncMethodSignature(source));
    }

    [Fact]
    public void IsAsyncMethodSignature_SynchronousMethod_ReturnsFalse()
    {
        const string source = "private void button1_Click(object sender, System.EventArgs e)\n{\n    Foo();\n}";

        Assert.False(EventHandlerBodyParser.IsAsyncMethodSignature(source));
    }
}
