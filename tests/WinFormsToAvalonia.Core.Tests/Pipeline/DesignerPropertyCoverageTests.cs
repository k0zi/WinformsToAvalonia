using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Pipeline;

public class DesignerPropertyCoverageTests
{
    private static ControlModel Control(string field, string type, params (string Name, PropertyValue Value)[] properties)
    {
        var control = new ControlModel { FieldName = field, ClrTypeName = type };
        foreach (var (name, value) in properties)
        {
            control.Properties[name] = value;
        }

        return control;
    }

    [Fact]
    public void PropertyBag_CountsLookupsThroughTheInterfacesToo()
    {
        var bag = new PropertyBag
        {
            ["Text"] = new PropertyValue.Literal("a"),
            ["Size"] = new PropertyValue.SizeValue(1, 2),
            ["Font"] = new PropertyValue.Literal("x"),
            ["Dock"] = new PropertyValue.EnumMembers(["Fill"]),
        };

        // The helpers that read these take IReadOnlyDictionary - a call through it must still count.
        IReadOnlyDictionary<string, PropertyValue> readOnly = bag;
        readOnly.TryGetValue("Text", out _);
        IDictionary<string, PropertyValue> dictionary = bag;
        _ = dictionary.ContainsKey("Size");
        _ = bag["Font"];

        // Enumerating looks at everything and carries nothing, so it does not count.
        _ = bag.ToList();
        _ = readOnly.Keys.ToList();

        Assert.Equal(["Dock"], bag.UnreadKeys);
    }

    [Fact]
    public void Report_NamesWhatNothingAskedAbout_GroupedByTypeAndProperty()
    {
        var form = new FormModel { ClassName = "MainForm" };
        form.FormProperties["HelpButton"] = new PropertyValue.Literal(true);
        form.FormProperties["Text"] = new PropertyValue.Literal("Main");
        _ = form.FormProperties.TryGetValue("Text", out _);

        foreach (var control in new[]
        {
            Control("textBox1", "TextBox", ("CharacterCasing", new PropertyValue.EnumMembers(["Upper"])), ("Name", new PropertyValue.Literal("textBox1"))),
            Control("textBox2", "TextBox", ("CharacterCasing", new PropertyValue.EnumMembers(["Lower"]))),
            Control("check1", "CheckBox", ("CheckState", new PropertyValue.EnumMembers(["Checked"]))),
            Control("check2", "CheckBox", ("CheckState", new PropertyValue.EnumMembers(["Indeterminate"]))),
            Control("custom1", "FancyGrid", ("Theme", new PropertyValue.Literal("dark"))),
        })
        {
            form.Controls[control.FieldName] = control;
        }

        var report = DesignerPropertyCoverage.Report(
            "MainForm", WinFormsArtifactKind.Form, form, c => c.ClrTypeName != "FancyGrid");

        // Name carries nothing; a Checked CheckState repeats Checked; an Indeterminate one does not;
        // a type with no mapping at all is already reported whole, so its properties are not listed.
        Assert.Equal(
            "'MainForm' sets designer properties nothing in this conversion translates, so they are not carried "
            + "over: CheckBox.CheckState (check2); Form.HelpButton; TextBox.CharacterCasing (textBox1, textBox2).",
            report);
    }

    [Fact]
    public void Report_EverythingAccountedFor_SaysNothing()
    {
        var form = new FormModel { ClassName = "MainForm" };
        form.FormProperties["AutoScaleMode"] = new PropertyValue.EnumMembers(["Font"]);

        Assert.Null(DesignerPropertyCoverage.Report("MainForm", WinFormsArtifactKind.Form, form, _ => true));
    }
}
