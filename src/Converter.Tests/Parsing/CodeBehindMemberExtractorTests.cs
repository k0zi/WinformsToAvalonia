using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class CodeBehindMemberExtractorTests
{
    private const string CodeBehindContent = """
        namespace SampleApp
        {
            partial class SampleForm
            {
                private int _counter = 0;
                private string _label, _tooltip;

                private void button1_Click(object sender, System.EventArgs e)
                {
                    _counter++;
                }

                private void DoSomething()
                {
                    _counter = 0;
                }

                protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
                {
                    base.OnClosing(e);
                }
            }
        }
        """;

    private static async Task<string> WriteTempFileAsync(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-membxtract-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [Fact]
    public async Task ExtractAsync_SingleDeclaratorField_CapturesNameAndText()
    {
        var path = await WriteTempFileAsync(CodeBehindContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            var counterField = Assert.Single(result.Fields, f => f.Names.Contains("_counter"));
            Assert.Contains("private int _counter = 0;", counterField.DeclarationText);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_MultiDeclaratorField_CapturesAllNames()
    {
        var path = await WriteTempFileAsync(CodeBehindContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            var multiField = Assert.Single(result.Fields, f => f.Names.Contains("_label"));
            Assert.Contains("_label", multiField.Names);
            Assert.Contains("_tooltip", multiField.Names);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_NonHandlerMethod_CapturedAsHelperMethod()
    {
        var path = await WriteTempFileAsync(CodeBehindContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            Assert.True(result.HelperMethods.TryGetValue("DoSomething", out var source));
            Assert.Contains("_counter = 0;", source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_HandlerNamedMethod_ExcludedFromHelperMethods()
    {
        var path = await WriteTempFileAsync(CodeBehindContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            Assert.False(result.HelperMethods.ContainsKey("button1_Click"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_OverrideMethod_ExcludedFromHelperMethodsAndReportedAsSkipped()
    {
        var path = await WriteTempFileAsync(CodeBehindContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            Assert.False(result.HelperMethods.ContainsKey("OnClosing"));
            Assert.Contains("OnClosing", result.SkippedOverrideMethodNames);
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

        var result = await CodeBehindMemberExtractor.ExtractAsync(missingPath, new HashSet<string>());

        Assert.Empty(result.Fields);
        Assert.Empty(result.HelperMethods);
        Assert.Empty(result.SkippedOverrideMethodNames);
    }

    [Fact]
    public async Task ExtractAsync_UnparseableFile_ReturnsEmptyWithoutThrowing()
    {
        var path = await WriteTempFileAsync("this is not valid C# {{{ at all !!");
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string>());

            Assert.Empty(result.Fields);
            Assert.Empty(result.HelperMethods);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
