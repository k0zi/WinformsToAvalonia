using Converter.Core.Parsing;
using Converter.Generator.ViewModels;
using Converter.Plugin.Abstractions;
using Converter.Tests.TestSupport;

namespace Converter.Tests.Generator;

/// <summary>
/// Integration tests proving real WinFormsParser output (post Phase-1 fix) flows through
/// to ViewModelGenerator producing non-empty [ObservableProperty]/[RelayCommand] members -
/// the specific defect this whole effort started from (previously always empty shells).
/// </summary>
public class ViewModelGeneratorTests
{
    [Fact]
    public async Task BuildEditableClass_FromParsedFixture_EmitsRelayCommandForClickHandler()
    {
        var parser = new WinFormsParser();
        var parseResult = await parser.ParseDesignerFileAsync(FixturePath.Get("SampleForm.Designer.cs.txt"));

        var generator = new ViewModelGenerator();
        var output = generator.BuildEditableClass(parseResult.RootControl!, "SampleApp", "SampleForm").Source;

        Assert.Contains("[CommunityToolkit.Mvvm.Input.RelayCommand]", output);
        Assert.Contains("button1Click", output);
    }

    [Fact]
    public async Task GeneratePartialClass_FromParsedFixture_EmitsObservablePropertyForDataBinding()
    {
        var parser = new WinFormsParser();
        var parseResult = await parser.ParseDesignerFileAsync(FixturePath.Get("SampleForm.Designer.cs.txt"));

        var generator = new ViewModelGenerator();
        var output = generator.GeneratePartialClass(parseResult.RootControl!, "SampleApp", "SampleForm");

        Assert.Contains("[ObservableProperty]", output);
        Assert.Contains("customerName", output);
    }

    [Fact]
    public void BuildEditableClass_EventSubscribedViaInlineLambda_SkipsCommandInsteadOfEmittingInvalidIdentifier()
    {
        // WinFormsParser.InlineLambdaHandlerMarker is not a valid C# identifier - it used to
        // flow straight through into `private void {MethodName}()`, producing a method
        // literally named "<inline lambda - manual review required>" that couldn't compile
        // (found via a real WarehouseApp sample conversion). ConversionOrchestrator surfaces
        // this as a manual step instead; the generator's job is just to not emit garbage.
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var button = new ControlNode { ControlType = "Button", FullTypeName = "System.Windows.Forms.Button", Name = "button1" };
        button.EventHandlers["Click"] = WinFormsParser.InlineLambdaHandlerMarker;
        root.Children.Add(button);

        var output = new ViewModelGenerator().BuildEditableClass(root, "SampleApp", "Form1").Source;

        Assert.DoesNotContain("inline lambda", output);
        Assert.DoesNotContain("RelayCommand", output);
    }

    [Fact]
    public void GeneratePartialClass_NoDataBindings_ReturnsEmptyString()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var button = new ControlNode { ControlType = "Button", FullTypeName = "System.Windows.Forms.Button", Name = "button1" };
        button.EventHandlers["Click"] = "button1_Click";
        root.Children.Add(button);

        var output = new ViewModelGenerator().GeneratePartialClass(root, "SampleApp", "Form1");

        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void GeneratePartialClass_HasProperties_DeclaresNoBaseType()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var textBox = new ControlNode { ControlType = "TextBox", FullTypeName = "System.Windows.Forms.TextBox", Name = "textBox1" };
        textBox.DataBindings.Add(new DataBinding { PropertyName = "Text", DataSource = "bindingSource1", DataMember = "CustomerName" });
        root.Children.Add(textBox);

        var output = new ViewModelGenerator().GeneratePartialClass(root, "SampleApp", "Form1");

        Assert.Contains("[ObservableProperty]", output);
        Assert.DoesNotContain(": ObservableObject", output);
        Assert.DoesNotContain(": CommunityToolkit", output);
    }

    [Fact]
    public void BuildEditableClass_MigratesFieldsWithInternalAccessibility()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var codeBehindMembers = new CodeBehindMembers(
            fields: [new CodeBehindField(["_counter"], "private int _counter = 0;")]);

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", codeBehindMembers: codeBehindMembers).Source;

        Assert.Contains("internal int _counter = 0;", output);
        Assert.DoesNotContain("private int _counter", output);
        Assert.Contains(": CommunityToolkit.Mvvm.ComponentModel.ObservableObject", output);
    }

    [Fact]
    public void BuildEditableClass_MigratesHelperMethodBodyLive()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var codeBehindMembers = new CodeBehindMembers(
            helperMethods: new Dictionary<string, string>
            {
                ["DoSomething"] = "private void DoSomething()\n{\n    _counter = 0;\n}"
            });

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", codeBehindMembers: codeBehindMembers).Source;

        Assert.Contains("internal void DoSomething()", output);
        Assert.Contains("_counter = 0;", output);
    }

    [Fact]
    public void BuildEditableClass_MigratesProtectedHelperMethod_UpgradesToInternal()
    {
        // A migrated business-logic override (see CodeBehindMemberExtractor.
        // KnownWinFormsOverrideMethodNames) is commonly "protected" (with "override" already
        // stripped by the extractor by the time it reaches here) - "protected" isn't reachable
        // from CodeBehindGenerator's "ViewModel.X" accessor rewrite (a different class), so it
        // needs the same private->internal upgrade treatment.
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var codeBehindMembers = new CodeBehindMembers(
            helperMethods: new Dictionary<string, string>
            {
                ["SaveToEntity"] = "protected void SaveToEntity()\n{\n    Entity.Name = nameTextBox.Text;\n}"
            });

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", codeBehindMembers: codeBehindMembers).Source;

        Assert.Contains("internal void SaveToEntity()", output);
        Assert.DoesNotContain("protected void SaveToEntity", output);
    }

    [Fact]
    public async Task BuildEditableClass_HandlerBodyFound_UsesLiveBodyNotComment()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var button = new ControlNode { ControlType = "Button", FullTypeName = "System.Windows.Forms.Button", Name = "button1" };
        button.EventHandlers["Click"] = "button1_Click";
        root.Children.Add(button);

        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_Click"] = "private void button1_Click(object sender, System.EventArgs e)\n{\n    DoWork();\n}"
        };

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", handlerBodies: handlerBodies).Source;

        Assert.Contains("DoWork();", output);
        Assert.DoesNotContain("// DoWork();", output);
        Assert.DoesNotContain("TODO", output);
        await Task.CompletedTask;
    }

    [Fact]
    public void BuildEditableClass_NoHandlerBodyFound_EmitsTodo()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var button = new ControlNode { ControlType = "Button", FullTypeName = "System.Windows.Forms.Button", Name = "button1" };
        button.EventHandlers["Click"] = "button1_Click";
        root.Children.Add(button);

        var output = new ViewModelGenerator().BuildEditableClass(root, "SampleApp", "Form1").Source;

        Assert.Contains("TODO: Implement Click logic", output);
    }

    [Fact]
    public void BuildEditableClass_OriginalHandlerWasAsyncVoid_PreservesAsyncModifier()
    {
        // WinForms event handlers are commonly "private async void Foo(...)" - a body
        // containing "await" without the enclosing method staying "async" doesn't compile.
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var button = new ControlNode { ControlType = "Button", FullTypeName = "System.Windows.Forms.Button", Name = "button1" };
        button.EventHandlers["Click"] = "button1_Click";
        root.Children.Add(button);

        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_Click"] = "private async void button1_Click(object sender, System.EventArgs e)\n{\n    await DoWorkAsync();\n}"
        };

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", handlerBodies: handlerBodies).Source;

        Assert.Contains("private async void button1Click()", output);
        Assert.Contains("await DoWorkAsync();", output);
    }

    [Fact]
    public void BuildEditableClass_OriginalHandlerWasSynchronous_DoesNotAddAsyncModifier()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var button = new ControlNode { ControlType = "Button", FullTypeName = "System.Windows.Forms.Button", Name = "button1" };
        button.EventHandlers["Click"] = "button1_Click";
        root.Children.Add(button);

        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_Click"] = "private void button1_Click(object sender, System.EventArgs e)\n{\n    DoWork();\n}"
        };

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", handlerBodies: handlerBodies).Source;

        Assert.Contains("private void button1Click()", output);
        Assert.DoesNotContain("async void button1Click", output);
    }

    [Fact]
    public void BuildEditableClass_AlwaysEmitsBaselineImplicitUsings()
    {
        // The generated .csproj does not enable <ImplicitUsings>, so migrated code relying on
        // List<T>/Task/etc. without an explicit "using" (as ImplicitUsings-enabled WinForms
        // projects commonly do) needs these carried in explicitly.
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };

        var output = new ViewModelGenerator().BuildEditableClass(root, "SampleApp", "Form1").Source;

        Assert.Contains("using System;", output);
        Assert.Contains("using System.Collections.Generic;", output);
        Assert.Contains("using System.Linq;", output);
        Assert.Contains("using System.Threading.Tasks;", output);
    }

    [Fact]
    public void BuildEditableClass_CopiesDomainUsingsFromCodeBehind()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var codeBehindMembers = new CodeBehindMembers(usingDirectives: ["WarehouseApp.Data.Models"]);

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", codeBehindMembers: codeBehindMembers).Source;

        Assert.Contains("using WarehouseApp.Data.Models;", output);
    }

    [Fact]
    public void BuildEditableClass_FiltersOutWinFormsAndDrawingUsings()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var codeBehindMembers = new CodeBehindMembers(
            usingDirectives: ["System.Windows.Forms", "System.Drawing", "System.Drawing.Printing", "WarehouseApp.Common"]);

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", codeBehindMembers: codeBehindMembers).Source;

        Assert.DoesNotContain("using System.Windows.Forms;", output);
        Assert.DoesNotContain("using System.Drawing;", output);
        Assert.DoesNotContain("using System.Drawing.Printing;", output);
        Assert.Contains("using WarehouseApp.Common;", output);
    }

    [Fact]
    public void BuildEditableClass_DoesNotDuplicateUsingAlreadyInBaseline()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var codeBehindMembers = new CodeBehindMembers(usingDirectives: ["System.Linq"]);

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", codeBehindMembers: codeBehindMembers).Source;

        Assert.Single(output.Split('\n'), line => line.Trim() == "using System.Linq;");
    }

    [Fact]
    public void BuildEditableClass_TextChangedWithBoundProperty_EmitsLivePropertyChangedHook()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var textBox = new ControlNode { ControlType = "TextBox", FullTypeName = "System.Windows.Forms.TextBox", Name = "textBox1" };
        textBox.EventHandlers["TextChanged"] = "textBox1_TextChanged";
        textBox.DataBindings.Add(new DataBinding { PropertyName = "Text", DataSource = "bindingSource1", DataMember = "CustomerName" });
        root.Children.Add(textBox);

        var handlerBodies = new Dictionary<string, string>
        {
            ["textBox1_TextChanged"] = "private void textBox1_TextChanged(object sender, System.EventArgs e)\n{\n    Validate();\n}"
        };

        var output = new ViewModelGenerator().BuildEditableClass(root, "SampleApp", "Form1", handlerBodies: handlerBodies).Source;

        Assert.Contains("partial void OnCustomerNameChanged(string value)", output);
        Assert.Contains("Validate();", output);
    }

    [Fact]
    public void BuildBoundControlPropertyLookup_DataBoundControl_MapsToObservablePropertyName()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var textBox = new ControlNode { ControlType = "TextBox", FullTypeName = "System.Windows.Forms.TextBox", Name = "textBox1" };
        textBox.DataBindings.Add(new DataBinding { PropertyName = "Text", DataSource = "bindingSource1", DataMember = "CustomerName" });
        root.Children.Add(textBox);

        var lookup = new ViewModelGenerator().BuildBoundControlPropertyLookup(root);

        Assert.Equal("CustomerName", lookup[("textBox1", "Text")]);
    }

    [Fact]
    public void BuildEditableClass_RelayCommandBodyReferencesBoundControlProperty_RewritesToObservableProperty()
    {
        // A migrated Click handler commonly reads/writes another control directly
        // ("textBox1.Text") - the ViewModel has no "textBox1" field (that's a View concern), so
        // left as-is this would not compile. When that control's property is already bound
        // (DataBindings.Add), the reference should be rewritten to the ViewModel's own
        // [ObservableProperty] instead of reaching into the View.
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var button = new ControlNode { ControlType = "Button", FullTypeName = "System.Windows.Forms.Button", Name = "button1" };
        button.EventHandlers["Click"] = "button1_Click";
        var textBox = new ControlNode { ControlType = "TextBox", FullTypeName = "System.Windows.Forms.TextBox", Name = "textBox1" };
        textBox.DataBindings.Add(new DataBinding { PropertyName = "Text", DataSource = "bindingSource1", DataMember = "CustomerName" });
        root.Children.Add(button);
        root.Children.Add(textBox);

        var handlerBodies = new Dictionary<string, string>
        {
            ["button1_Click"] = "private void button1_Click(object sender, System.EventArgs e)\n{\n    Save(textBox1.Text);\n}"
        };

        var output = new ViewModelGenerator().BuildEditableClass(
            root, "SampleApp", "Form1", handlerBodies: handlerBodies).Source;

        Assert.Contains("Save(CustomerName);", output);
        Assert.DoesNotContain("textBox1", output);
    }

    [Fact]
    public void BuildEditableClass_TextChangedWithoutBoundProperty_EmitsNoHook()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "Form1" };
        var textBox = new ControlNode { ControlType = "TextBox", FullTypeName = "System.Windows.Forms.TextBox", Name = "textBox1" };
        textBox.EventHandlers["TextChanged"] = "textBox1_TextChanged";
        root.Children.Add(textBox);

        var handlerBodies = new Dictionary<string, string>
        {
            ["textBox1_TextChanged"] = "private void textBox1_TextChanged(object sender, System.EventArgs e)\n{\n    Validate();\n}"
        };

        var output = new ViewModelGenerator().BuildEditableClass(root, "SampleApp", "Form1", handlerBodies: handlerBodies).Source;

        Assert.DoesNotContain("partial void", output);
    }
}
