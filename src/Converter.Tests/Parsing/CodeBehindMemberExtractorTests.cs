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

    private const string FileScopedNamespaceWithUsings = """
        using WarehouseApp.Common;
        using WarehouseApp.Data.Models;
        using System.Windows.Forms;

        namespace SampleApp;

        partial class SampleForm
        {
            private void button1_Click(object sender, System.EventArgs e)
            {
            }
        }
        """;

    private const string BlockScopedNamespaceWithUsings = """
        using WarehouseApp.Data.Models;

        namespace SampleApp
        {
            using WarehouseApp.Common;

            partial class SampleForm
            {
                private void button1_Click(object sender, System.EventArgs e)
                {
                }
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_FileScopedNamespace_CapturesTopLevelUsings()
    {
        var path = await WriteTempFileAsync(FileScopedNamespaceWithUsings);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            Assert.Contains("WarehouseApp.Common", result.UsingDirectives);
            Assert.Contains("WarehouseApp.Data.Models", result.UsingDirectives);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_BlockScopedNamespace_CapturesUsingsInsideAndOutsideNamespace()
    {
        var path = await WriteTempFileAsync(BlockScopedNamespaceWithUsings);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string> { "button1_Click" });

            Assert.Contains("WarehouseApp.Data.Models", result.UsingDirectives);
            Assert.Contains("WarehouseApp.Common", result.UsingDirectives);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_UsingDirectives_AreDeduplicated()
    {
        const string content = """
            using WarehouseApp.Common;
            using WarehouseApp.Common;

            namespace SampleApp;

            partial class SampleForm
            {
            }
            """;
        var path = await WriteTempFileAsync(content);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string>());

            Assert.Single(result.UsingDirectives, ns => ns == "WarehouseApp.Common");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private const string ProjectLocalBaseClassOverrideContent = """
        namespace SampleApp
        {
            partial class ProductDetailForm
            {
                protected override void LoadFromEntity()
                {
                    nameTextBox.Text = Entity.Name;
                }

                protected override async System.Threading.Tasks.Task PersistAsync()
                {
                    await Db.SaveAsync(Entity);
                }
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_OverrideOfProjectLocalBaseClassMember_MigratedAsHelperMethodWithOverrideStripped()
    {
        var path = await WriteTempFileAsync(ProjectLocalBaseClassOverrideContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string>());

            Assert.True(result.HelperMethods.TryGetValue("LoadFromEntity", out var source));
            Assert.DoesNotContain("override", source);
            Assert.Contains("nameTextBox.Text = Entity.Name;", source);
            Assert.DoesNotContain("LoadFromEntity", result.SkippedOverrideMethodNames);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_AsyncOverrideOfProjectLocalBaseClassMember_PreservesAsyncAndStripsOverride()
    {
        var path = await WriteTempFileAsync(ProjectLocalBaseClassOverrideContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string>());

            Assert.True(result.HelperMethods.TryGetValue("PersistAsync", out var source));
            Assert.DoesNotContain("override", source);
            Assert.Contains("async", source);
            Assert.Contains("await Db.SaveAsync(Entity);", source);
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

    private const string SingleFileCustomControlContent = """
        namespace SampleApp
        {
            public class AutocompleteSearchBox : System.Windows.Forms.UserControl
            {
                private System.Windows.Forms.TextBox _textBox;
                private int _counter;

                private void InitializeComponent()
                {
                    _textBox = new System.Windows.Forms.TextBox();
                    Controls.Add(_textBox);
                }

                private void ClosePopupSoon()
                {
                    _counter++;
                }
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_ControlFieldName_IsExcludedFromMigratedFields()
    {
        // A single-file custom control's own child-control fields (already captured by
        // WinFormsParser as ControlNode instances) must not be re-migrated as ViewModel
        // fields - that's a View concern.
        var path = await WriteTempFileAsync(SingleFileCustomControlContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(
                path, new HashSet<string>(), controlFieldNames: new HashSet<string> { "_textBox" });

            Assert.DoesNotContain(result.Fields, f => f.Names.Contains("_textBox"));
            Assert.Contains(result.Fields, f => f.Names.Contains("_counter"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_NoControlFieldNames_MigratesAllFieldsAsBefore()
    {
        var path = await WriteTempFileAsync(SingleFileCustomControlContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string>());

            Assert.Contains(result.Fields, f => f.Names.Contains("_textBox"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_InitializeComponent_IsNeverMigratedAsHelperMethod()
    {
        var path = await WriteTempFileAsync(SingleFileCustomControlContent);
        try
        {
            var result = await CodeBehindMemberExtractor.ExtractAsync(path, new HashSet<string>());

            Assert.DoesNotContain(result.HelperMethods.Keys, n => n == "InitializeComponent");
            Assert.Contains(result.HelperMethods.Keys, n => n == "ClosePopupSoon");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
