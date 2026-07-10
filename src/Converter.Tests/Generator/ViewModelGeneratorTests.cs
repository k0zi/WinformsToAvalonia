using Converter.Core.Parsing;
using Converter.Generator.ViewModels;
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
}
