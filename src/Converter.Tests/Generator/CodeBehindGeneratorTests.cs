using Converter.Core.Parsing;
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
    public void Generate_UserControlRoot_EmitsUserControlBaseClass()
    {
        var root = new ControlNode { ControlType = "UserControl", FullTypeName = "System.Windows.Forms.UserControl", Name = "CustomerCard" };

        var content = new CodeBehindGenerator().Generate("SampleApp", "CustomerCard", root);

        Assert.Contains("public partial class CustomerCard : UserControl", content);

        // A converted custom control lives in Controls/, not Views/ - its namespace must match
        // where ConversionOrchestrator actually writes the file (and AxamlGenerator's x:Class).
        Assert.Contains("namespace SampleApp.Controls;", content);
    }

    [Fact]
    public void Generate_FormRoot_StillEmitsWindowBaseClass()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "SampleForm" };

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root);

        Assert.Contains("public partial class SampleForm : Window", content);
        Assert.Contains("namespace SampleApp.Views;", content);
    }

    [Fact]
    public void Generate_WithBindableProperties_EmitsStyledPropertyAndClrWrapper()
    {
        var root = new ControlNode { ControlType = "UserControl", FullTypeName = "System.Windows.Forms.UserControl", Name = "CustomerCard" };
        var bindableProperties = new List<CustomControlProperty>
        {
            new("CustomerId", "int", CustomControlPropertyKind.PlainBindable)
        };

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "CustomerCard", root, bindableProperties: bindableProperties);

        Assert.Contains(
            "public static readonly Avalonia.StyledProperty<int> CustomerIdProperty =", content);
        Assert.Contains(
            "Avalonia.AvaloniaProperty.Register<CustomerCard, int>(nameof(CustomerId));", content);
        Assert.Contains("public int CustomerId", content);
        Assert.Contains("get => GetValue(CustomerIdProperty);", content);
        Assert.Contains("set => SetValue(CustomerIdProperty, value);", content);
        Assert.DoesNotContain("coerce:", content);

        var tree = CSharpSyntaxTree.ParseText(content);
        var errors = tree.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Generate_WithCoercedProperty_EmitsCoerceArgumentAndMethod()
    {
        var root = new ControlNode { ControlType = "UserControl", FullTypeName = "System.Windows.Forms.UserControl", Name = "NumericStepperControl" };
        var bindableProperties = new List<CustomControlProperty>
        {
            new(
                "Increment", "decimal", CustomControlPropertyKind.Coerced,
                BackingFieldName: "_increment",
                CoerceMethodName: "CoerceIncrement",
                CoerceMethodBody: "private static decimal CoerceIncrement(Avalonia.AvaloniaObject sender, decimal value) => " +
                    "System.Math.Max(value, 0m);")
        };

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "NumericStepperControl", root, bindableProperties: bindableProperties);

        Assert.Contains("coerce: CoerceIncrement", content);
        Assert.Contains(
            "private static decimal CoerceIncrement(Avalonia.AvaloniaObject sender, decimal value) => System.Math.Max(value, 0m);",
            content);

        var tree = CSharpSyntaxTree.ParseText(content);
        var errors = tree.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Generate_WithCoercionFallbackProperty_EmitsPlainStyledPropertyWithoutCoerceArgument()
    {
        var root = new ControlNode { ControlType = "UserControl", FullTypeName = "System.Windows.Forms.UserControl", Name = "CustomerCard" };
        var bindableProperties = new List<CustomControlProperty>
        {
            new("Sku", "string", CustomControlPropertyKind.CoercionFallback, BackingFieldName: "_sku")
        };

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "CustomerCard", root, bindableProperties: bindableProperties);

        Assert.Contains(
            "Avalonia.AvaloniaProperty.Register<CustomerCard, string>(nameof(Sku));", content);
        Assert.DoesNotContain("coerce:", content);
    }

    [Fact]
    public void Generate_WithDelegatingProperty_EmitsPlainForwardingProperty()
    {
        var root = new ControlNode { ControlType = "UserControl", FullTypeName = "System.Windows.Forms.UserControl", Name = "AutocompleteSearchBox" };
        var delegatingProperties = new List<DelegatingCustomControlProperty>
        {
            new("PlaceholderText", "string", "_textBox", "PlaceholderText", FallbackExpression: null, WasOverride: false)
        };

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "AutocompleteSearchBox", root, delegatingProperties: delegatingProperties);

        Assert.Contains("public string PlaceholderText", content);
        Assert.Contains("get => _textBox.PlaceholderText;", content);
        Assert.Contains("set => _textBox.PlaceholderText = value;", content);
        Assert.DoesNotContain("StyledProperty", content);
        Assert.DoesNotContain("PlaceholderTextProperty", content);
    }

    [Fact]
    public void Generate_WithOverrideDelegatingPropertyAndFallback_StripsOverrideKeywordAndEmitsFallback()
    {
        var root = new ControlNode { ControlType = "UserControl", FullTypeName = "System.Windows.Forms.UserControl", Name = "AutocompleteSearchBox" };
        var delegatingProperties = new List<DelegatingCustomControlProperty>
        {
            new("Text", "string", "_textBox", "Text", FallbackExpression: "string.Empty", WasOverride: true)
        };

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "AutocompleteSearchBox", root, delegatingProperties: delegatingProperties);

        Assert.DoesNotContain("override string Text", content);
        Assert.Contains("public string Text", content);
        Assert.Contains("set => _textBox.Text = value ?? string.Empty;", content);

        var tree = CSharpSyntaxTree.ParseText(content);
        var errors = tree.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Generate_NoBindableProperties_EmitsNoStyledProperty()
    {
        var root = new ControlNode { ControlType = "UserControl", FullTypeName = "System.Windows.Forms.UserControl", Name = "CustomerCard" };

        var content = new CodeBehindGenerator().Generate("SampleApp", "CustomerCard", root);

        Assert.DoesNotContain("StyledProperty", content);
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
    public void Generate_PreserveEventHandlerViaInlineLambda_SkipsStubInsteadOfEmittingInvalidIdentifier()
    {
        // Same defect class as ViewModelGenerator's inline-lambda handling: the sentinel
        // marker is not a valid C# identifier and must never be emitted as a method name.
        var root = BuildFormWithButtonHandler("MouseDown", WinFormsParser.InlineLambdaHandlerMarker);

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root);

        Assert.DoesNotContain("inline lambda", content);
    }

    [Fact]
    public void Generate_HandlerBodyFound_EmbedsOriginalSourceAsLiveCode()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");
        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_MouseDown"] = "private void button1_MouseDown(object sender, MouseEventArgs e)\n{\n    MessageBox.Show(\"Hi\");\n}"
        };

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root, handlerBodies);

        Assert.Contains("MessageBox.Show(\"Hi\");", content);
        Assert.DoesNotContain("// MessageBox.Show", content);
        Assert.DoesNotContain("private void button1_MouseDown(object sender, MouseEventArgs e)", content);
        Assert.DoesNotContain("TODO: original", content);
    }

    [Fact]
    public void Generate_ConstructorWiresDataContext()
    {
        var root = BuildFormWithButtonHandler("Click", "button1_Click");

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root);

        Assert.Contains("DataContext = new SampleApp.ViewModels.SampleFormViewModel();", content);
    }

    [Fact]
    public void Generate_EmitsViewModelAccessorProperty()
    {
        var root = BuildFormWithButtonHandler("Click", "button1_Click");

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root);

        Assert.Contains(
            "private SampleApp.ViewModels.SampleFormViewModel ViewModel => (SampleApp.ViewModels.SampleFormViewModel)DataContext!;",
            content);
    }

    [Fact]
    public void Generate_PreserveEventHandlerBody_RewritesMigratedFieldToViewModelAccessor()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");
        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_MouseDown"] = "private void button1_MouseDown(object sender, MouseEventArgs e)\n{\n    _counter++;\n}"
        };
        var codeBehindMembers = new CodeBehindMembers(
            fields: [new CodeBehindField(["_counter"], "private int _counter;")]);

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "SampleForm", root, handlerBodies, codeBehindMembers: codeBehindMembers);

        Assert.Contains("ViewModel._counter++;", content);
    }

    [Fact]
    public void Generate_PreserveEventHandlerBody_RewritesMigratedHelperMethodCallToViewModelAccessor()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");
        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_MouseDown"] = "private void button1_MouseDown(object sender, MouseEventArgs e)\n{\n    DoSomething();\n}"
        };
        var codeBehindMembers = new CodeBehindMembers(
            helperMethods: new Dictionary<string, string> { ["DoSomething"] = "private void DoSomething() { }" });

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "SampleForm", root, handlerBodies, codeBehindMembers: codeBehindMembers);

        Assert.Contains("ViewModel.DoSomething();", content);
    }

    [Fact]
    public void Generate_PreserveEventHandlerBody_DoesNotRewriteUnrelatedIdentifiers()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");
        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_MouseDown"] = "private void button1_MouseDown(object sender, MouseEventArgs e)\n{\n    var localValue = 1;\n}"
        };
        var codeBehindMembers = new CodeBehindMembers(
            fields: [new CodeBehindField(["_counter"], "private int _counter;")]);

        var content = new CodeBehindGenerator().Generate(
            "SampleApp", "SampleForm", root, handlerBodies, codeBehindMembers: codeBehindMembers);

        Assert.Contains("var localValue = 1;", content);
        Assert.DoesNotContain("ViewModel.localValue", content);
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
    public void Generate_PreservedHandlerOriginalWasAsyncVoid_PreservesAsyncModifier()
    {
        var root = BuildFormWithButtonHandler("MouseDown", "button1_MouseDown");
        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_MouseDown"] =
                "private async void button1_MouseDown(object sender, MouseEventArgs e)\n{\n    await Task.Delay(1);\n}"
        };

        var content = new CodeBehindGenerator().Generate("SampleApp", "SampleForm", root, handlerBodies);

        Assert.Contains(
            "private async void button1_MouseDown(object? sender, Avalonia.Input.PointerPressedEventArgs e)", content);
        Assert.Contains("await Task.Delay(1);", content);
    }

    [Fact]
    public void Generate_WithEmbeddedLiveHandlerBody_ProducesSyntacticallyValidCSharp()
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
