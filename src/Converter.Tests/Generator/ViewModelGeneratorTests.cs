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
    public async Task GeneratePartialClass_FromParsedFixture_EmitsRelayCommandForClickHandler()
    {
        var parser = new WinFormsParser();
        var parseResult = await parser.ParseDesignerFileAsync(FixturePath.Get("SampleForm.Designer.cs.txt"));

        var generator = new ViewModelGenerator();
        var output = generator.GeneratePartialClass(parseResult.RootControl!, "SampleApp", "SampleForm");

        Assert.Contains("[RelayCommand]", output);
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
    public void GeneratePartialClass_EventSubscribedViaInlineLambda_SkipsCommandInsteadOfEmittingInvalidIdentifier()
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

        var output = new ViewModelGenerator().GeneratePartialClass(root, "SampleApp", "Form1");

        Assert.DoesNotContain("inline lambda", output);
        Assert.DoesNotContain("[RelayCommand]", output);
    }
}
