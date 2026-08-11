using Converter.Mappings.BuiltIn;
using Converter.Plugin.Abstractions;

namespace Converter.Tests.Mappings;

public class EventMappingRegistryTests
{
    [Fact]
    public void FindBoundPropertyName_TextChangedWithMatchingBinding_ReturnsDataMember()
    {
        var control = new ControlNode { ControlType = "TextBox", FullTypeName = "System.Windows.Forms.TextBox", Name = "textBox1" };
        control.DataBindings.Add(new DataBinding { PropertyName = "Text", DataSource = "bindingSource1", DataMember = "CustomerName" });

        var result = EventMappingRegistry.FindBoundPropertyName(control, "TextChanged");

        Assert.Equal("CustomerName", result);
    }

    [Fact]
    public void FindBoundPropertyName_NoMatchingBinding_ReturnsNull()
    {
        var control = new ControlNode { ControlType = "TextBox", FullTypeName = "System.Windows.Forms.TextBox", Name = "textBox1" };

        var result = EventMappingRegistry.FindBoundPropertyName(control, "TextChanged");

        Assert.Null(result);
    }

    [Fact]
    public void FindBoundPropertyName_EventWithNoAutomationPath_ReturnsNull()
    {
        var control = new ControlNode { ControlType = "Panel", FullTypeName = "System.Windows.Forms.Panel", Name = "panel1" };
        control.DataBindings.Add(new DataBinding { PropertyName = "Text", DataSource = "bindingSource1", DataMember = "Whatever" });

        var result = EventMappingRegistry.FindBoundPropertyName(control, "Paint");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Validating")]
    [InlineData("Validated")]
    public void GetMapping_ValidatingAndValidated_ReportPreserveEventHandlerNotRequiresCustomLogic(string eventName)
    {
        // Regression: these used to be RequiresCustomLogic (silently no manual step at all,
        // and no automatic migration) - LostFocus is already a fully supported
        // PreserveEventHandler target, so there's no reason not to handle them the same way.
        var mapping = EventMappingRegistry.GetMapping(eventName);

        Assert.NotNull(mapping);
        Assert.True(mapping!.PreserveEventHandler);
        Assert.False(mapping.RequiresCustomLogic);
        Assert.Equal("LostFocus", mapping.AvaloniaEvent);
    }

    [Fact]
    public void GetMapping_Validating_NotesFlagTheCancelSemanticGap()
    {
        var mapping = EventMappingRegistry.GetMapping("Validating");

        Assert.NotNull(mapping!.Notes);
        Assert.Contains("e.Cancel", mapping.Notes);
    }
}
