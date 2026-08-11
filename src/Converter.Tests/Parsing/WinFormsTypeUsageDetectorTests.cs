using Converter.Core.Parsing;
using Microsoft.CodeAnalysis.CSharp;

namespace Converter.Tests.Parsing;

public class WinFormsTypeUsageDetectorTests
{
    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void FindReferencedTypeNames_NoBlocklistedTypes_ReturnsEmpty()
    {
        var root = Parse("""
            namespace WarehouseApp.Common;

            public static class Db
            {
                public static int Add(int a, int b) => a + b;
            }
            """);

        var result = WinFormsTypeUsageDetector.FindReferencedTypeNames(root);

        Assert.Empty(result);
    }

    [Fact]
    public void FindReferencedTypeNames_FormLabelTextBoxButtonDialogResult_AllFound()
    {
        // Mirrors the real InputBoxHelper.cs shape: a static method imperatively building a
        // WinForms dialog, without itself deriving from anything (so it slips past a
        // base-type-only safety check).
        var root = Parse("""
            namespace WarehouseApp.Common;

            public static class InputBoxHelper
            {
                public static string? Show(IWin32Window owner, string title)
                {
                    using var form = new Form { Text = title };
                    var label = new Label { Text = "Value" };
                    var textBox = new TextBox();
                    var okButton = new Button { DialogResult = DialogResult.OK };
                    form.Controls.Add(label);
                    return form.ShowDialog(owner) == DialogResult.OK ? textBox.Text : null;
                }
            }
            """);

        var result = WinFormsTypeUsageDetector.FindReferencedTypeNames(root);

        Assert.Contains("Form", result);
        Assert.Contains("Label", result);
        Assert.Contains("TextBox", result);
        Assert.Contains("Button", result);
        Assert.Contains("DialogResult", result);
        Assert.Contains("IWin32Window", result);
    }

    [Fact]
    public void FindReferencedTypeNames_TreeNodeUsage_Found()
    {
        var root = Parse("""
            internal void LoadTree()
            {
                var node = new TreeNode("root");
                Nodes.Add(node);
            }
            """);

        var result = WinFormsTypeUsageDetector.FindReferencedTypeNames(root);

        Assert.Contains("TreeNode", result);
    }

    [Fact]
    public void FindReferencedTypeNames_QualifiedTypeName_StillFound()
    {
        var root = Parse("""
            internal void Show()
            {
                var result = System.Windows.Forms.DialogResult.OK;
            }
            """);

        var result = WinFormsTypeUsageDetector.FindReferencedTypeNames(root);

        Assert.Contains("DialogResult", result);
    }

    [Fact]
    public void FindReferencedTypeNames_DuplicateReferences_ReturnedOnce()
    {
        var root = Parse("""
            internal void Foo()
            {
                var a = new TreeNode("a");
                var b = new TreeNode("b");
            }
            """);

        var result = WinFormsTypeUsageDetector.FindReferencedTypeNames(root);

        Assert.Single(result, name => name == "TreeNode");
    }
}
