using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class SiblingFileResolverTests
{
    private const string UserControlCodeBehind = """
        namespace SampleApp
        {
            public partial class CustomerCard : System.Windows.Forms.UserControl
            {
                public CustomerCard()
                {
                    InitializeComponent();
                }
            }
        }
        """;

    private const string FormCodeBehind = """
        namespace SampleApp
        {
            public partial class MainForm : System.Windows.Forms.Form
            {
                public MainForm()
                {
                    InitializeComponent();
                }
            }
        }
        """;

    [Fact]
    public async Task ResolveRootBaseTypeAsync_SiblingDeclaresFullyQualifiedUserControl_NormalizesToShortName()
    {
        var dir = Directory.CreateTempSubdirectory("wf2av-siblingresolver-").FullName;
        try
        {
            var designerPath = Path.Combine(dir, "CustomerCard.Designer.cs");
            await File.WriteAllTextAsync(designerPath, "namespace SampleApp { partial class CustomerCard { } }");
            await File.WriteAllTextAsync(Path.Combine(dir, "CustomerCard.cs"), UserControlCodeBehind);

            var baseType = await SiblingFileResolver.ResolveRootBaseTypeAsync(designerPath);

            Assert.Equal("UserControl", baseType);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ResolveRootBaseTypeAsync_SiblingDeclaresForm_ReturnsForm()
    {
        var dir = Directory.CreateTempSubdirectory("wf2av-siblingresolver-").FullName;
        try
        {
            var designerPath = Path.Combine(dir, "MainForm.Designer.cs");
            await File.WriteAllTextAsync(designerPath, "namespace SampleApp { partial class MainForm { } }");
            await File.WriteAllTextAsync(Path.Combine(dir, "MainForm.cs"), FormCodeBehind);

            var baseType = await SiblingFileResolver.ResolveRootBaseTypeAsync(designerPath);

            Assert.Equal("Form", baseType);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ResolveRootBaseTypeAsync_NoSiblingFile_ReturnsNull()
    {
        var dir = Directory.CreateTempSubdirectory("wf2av-siblingresolver-").FullName;
        try
        {
            var designerPath = Path.Combine(dir, "Orphan.Designer.cs");
            await File.WriteAllTextAsync(designerPath, "namespace SampleApp { partial class Orphan { } }");

            var baseType = await SiblingFileResolver.ResolveRootBaseTypeAsync(designerPath);

            Assert.Null(baseType);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ResolveRootBaseTypeAsync_SiblingHasNoBaseListEither_ReturnsNull()
    {
        // Mirrors real Visual Studio output for a Designer.cs whose sibling doesn't declare a
        // base type on the same partial fragment WinFormsParser looked at (e.g. it's declared
        // on yet another partial, or genuinely absent) - best-effort, not a hard failure.
        var dir = Directory.CreateTempSubdirectory("wf2av-siblingresolver-").FullName;
        try
        {
            var designerPath = Path.Combine(dir, "NoBase.Designer.cs");
            await File.WriteAllTextAsync(designerPath, "namespace SampleApp { partial class NoBase { } }");
            await File.WriteAllTextAsync(
                Path.Combine(dir, "NoBase.cs"), "namespace SampleApp { partial class NoBase { } }");

            var baseType = await SiblingFileResolver.ResolveRootBaseTypeAsync(designerPath);

            Assert.Null(baseType);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ResolveRootBaseTypeAsync_NotADesignerFile_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-notdesigner-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, FormCodeBehind);

        try
        {
            var baseType = await SiblingFileResolver.ResolveRootBaseTypeAsync(path);

            Assert.Null(baseType);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
