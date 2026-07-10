using Converter.Generator.CodeBehind;
using Converter.Generator.Mapping;
using Converter.Plugin.Abstractions;
using Microsoft.CodeAnalysis.CSharp;

namespace Converter.Tests.Generator;

public class CodeBehindGeneratorTests
{
    private static ControlNode BuildFormWithButtonHandler(string eventName, string handlerName)
    {
        var root = new ControlNode
        {
            ControlType = "Form",
            FullTypeName = "System.Windows.Forms.Form",
            Name = "SampleForm"
        };

        var button = new ControlNode
        {
            ControlType = "Button",
            FullTypeName = "System.Windows.Forms.Button",
            Name = "button1",
            Parent = root
        };
        button.EventHandlers[eventName] = handlerName;
        root.Children.Add(button);

        return root;
    }

    [Fact]
    public void Generate_PreserveEventHandlerEvent_EmitsCorrectlySignedStub()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root);

        Assert.Contains(
            "private void button1_MouseDown(object? sender, Avalonia.Input.PointerPressedEventArgs e)", content);
    }

    [Fact]
    public void Generate_HandlerBodyFound_EmbedsOriginalSourceAsIndividuallyCommentedLines()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");
        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_MouseDown"] = "private void button1_MouseDown(object sender, MouseEventArgs e)\n{\n    MessageBox.Show(\"Hi\");\n}"
        };

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root, handlerBodies);

        Assert.Contains("MessageBox.Show(\"Hi\");", content);
        Assert.Contains("// private void button1_MouseDown(object sender, MouseEventArgs e)", content);
        Assert.Matches(@"//\s*MessageBox\.Show\(""Hi""\);", content);
        Assert.DoesNotContain("TODO: original", content);
    }

    [Fact]
    public void Generate_HandlerBodyNotFound_EmitsPortManuallyTodo()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root);

        Assert.Contains("TODO: original \"button1_MouseDown\" handler body not found - port manually", content);
    }

    [Fact]
    public void Generate_ConvertToCommandEvent_DoesNotEmitCodeBehindStub()
    {
        var root = BuildFormWithButtonHandler("Click", "button1_Click");

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root);

        Assert.DoesNotContain("button1_Click", content);
    }

    [Fact]
    public void Generate_PluginClaimedEvent_SkipsStubForThatEvent()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");
        var overrides = new PluginMappingOverrides();
        overrides.EventMappings[(root.Children[0], "MouseDown")] = new EventMappingResult
        {
            AvaloniaEvent = "PointerPressed",
            PreserveEventHandler = true
        };

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root, overrides: overrides);

        Assert.DoesNotContain("button1_MouseDown", content);
    }

    [Fact]
    public void Generate_GotFocusHandler_DefaultsToV12FocusChangedEventArgs()
    {
        var root = BuildFormWithButtonHandler("GotFocus", "button1_GotFocus");

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root);

        Assert.Contains(
            "private void button1_GotFocus(object? sender, Avalonia.Input.FocusChangedEventArgs e)", content);
    }

    [Fact]
    public void Generate_GotFocusHandler_TargetingV11_UsesGotFocusEventArgs()
    {
        var root = BuildFormWithButtonHandler("GotFocus", "button1_GotFocus");

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "SampleForm", root, avaloniaMajorVersion: 11);

        Assert.Contains(
            "private void button1_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)", content);
    }

    [Fact]
    public void Generate_WithEmbeddedHandlerBodyComment_ProducesSyntacticallyValidCSharp()
    {
        var root = BuildFormWithButtonHandler("KeyDown", "textBox1_KeyDown");
        var handlerBodies = new Dictionary<string, string>
        {
            ["textBox1_KeyDown"] =
                "private void textBox1_KeyDown(object sender, KeyEventArgs e)\n{\n    // a nested comment\n    var x = \"a string with { braces } and \\\"quotes\\\"\";\n    /* block comment */\n}"
        };

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root, handlerBodies);

        var tree = CSharpSyntaxTree.ParseText(content);
        var diagnostics = tree.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(diagnostics);
    }
}
