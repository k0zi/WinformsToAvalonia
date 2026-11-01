using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

public sealed class UserMappingsTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "w2a-mappings-" + Guid.NewGuid() + ".json");

    public void Dispose() => File.Delete(_path);

    private IReadOnlyDictionary<string, UserMapping> Load(string json)
    {
        File.WriteAllText(_path, json);
        return UserMappings.Load(_path, new ControlMappingRegistry());
    }

    [Fact]
    public void Load_AsAndElementEntries()
    {
        var mappings = Load("""
            {
              // comments and trailing commas are allowed
              "controls": {
                "TextEdit": { "as": "TextBox", "properties": { "EditValue": "Text" }, "events": { "EditValueChanged": "TextChanged" } },
                "Gauge": { "element": "ProgressBar", "properties": { "Value": "Value" } },
              }
            }
            """);

        Assert.Equal("TextBox", mappings["TextEdit"].As);
        Assert.Equal("Text", mappings["TextEdit"].Properties["EditValue"]);
        Assert.Equal("TextChanged", mappings["TextEdit"].Events["EditValueChanged"]);
        Assert.Equal("ProgressBar", mappings["Gauge"].Element);
    }

    [Theory]
    [InlineData("""{ "controls": { "X": { } } }""", "exactly one of")]
    [InlineData("""{ "controls": { "X": { "as": "TextBox", "element": "TextBox" } } }""", "exactly one of")]
    [InlineData("""{ "controls": { "X": { "as": "NoSuchControl" } } }""", "is not a WinForms control")]
    [InlineData("""{ "controls": { "Button": { "as": "TextBox" } } }""", "already maps")]
    [InlineData("""{ "controls": { "X": { "element": "dx:Gauge" } } }""", "is not an element name")]
    [InlineData("""{ "controls": { "X": { "element": "Gauge", "events": { "A": "B" } } } }""", "needs \"as\"")]
    [InlineData("""{ "controls": { "X": { "as": "TextBox", "colour": "red" } } }""", "unknown key 'colour'")]
    [InlineData("""{ "mappings": { } }""", "\"controls\" object")]
    [InlineData("""{ "controls": { "MetroForm": { "as": "Form", "properties": { "A": "B" } } } }""", "takes no properties")]
    [InlineData("""{ not json""", "")]
    public void Load_AnyError_RejectsTheWholeFile(string json, string expected)
    {
        File.WriteAllText(_path, json);

        var error = Assert.Throws<UserMappingsException>(() => UserMappings.Load(_path, new ControlMappingRegistry()));

        Assert.Contains(error.Errors, e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void BaseTypes_AreTheFormAndUserControlTargets()
    {
        var mappings = Load("""{ "controls": { "MetroForm": { "as": "Form" }, "XtraUserControl": { "as": "UserControl" }, "TextEdit": { "as": "TextBox" } } }""");

        Assert.Equal(
            new Dictionary<string, string> { ["MetroForm"] = "Form", ["XtraUserControl"] = "UserControl" },
            UserMappings.BaseTypes(mappings));
    }

    [Fact]
    public void Retype_RenamesPropertiesAndEvents_AndKeepsTheWrittenType()
    {
        var control = new ControlModel { FieldName = "nameEdit", ClrTypeName = "TextEdit" };
        control.Properties["EditValue"] = new PropertyValue.Literal("x");
        control.Events.Add(new EventHandlerBinding("EditValueChanged", "nameEdit_EditValueChanged", null));

        UserMappings.Retype(control, new UserMapping(
            "TextEdit", "TextBox", null,
            new Dictionary<string, string> { ["EditValue"] = "Text" },
            new Dictionary<string, string> { ["EditValueChanged"] = "TextChanged" }));

        Assert.Equal("TextBox", control.ClrTypeName);
        Assert.Equal("TextEdit", control.DeclaredClrTypeName);
        Assert.Equal(new PropertyValue.Literal("x"), control.Properties["Text"]);
        Assert.False(control.Properties.ContainsKey("EditValue"));
        Assert.Equal("TextChanged", Assert.Single(control.Events).EventName);
    }

    [Theory]
    [InlineData(true, "True")]
    [InlineData("text", "text")]
    [InlineData(40, "40")]
    public void AsAttributeValue_FormatsByTheValuesOwnShape(object literal, string expected) =>
        Assert.Equal(expected, UserMappings.AsAttributeValue(new PropertyValue.Literal(literal)));
}
