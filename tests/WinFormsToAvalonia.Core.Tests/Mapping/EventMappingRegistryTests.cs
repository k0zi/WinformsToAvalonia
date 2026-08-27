using WinFormsToAvalonia.Core.Mapping;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

public class EventMappingRegistryTests
{
    private readonly EventMappingRegistry _registry = new();

    [Theory]
    [InlineData("Button")]
    [InlineData("ToolStripButton")]
    [InlineData("ToolStripMenuItem")]
    public void ResolveControlEvent_ClickOnInvokableControl_IsCommandCandidate(string typeName)
    {
        var mapping = _registry.ResolveControlEvent(typeName, "Click");

        Assert.Equal("Click", mapping.AvaloniaEventName);
        Assert.Equal("RoutedEventArgs", mapping.AvaloniaEventArgsTypeName);
        Assert.True(mapping.IsCommandCandidate);
        Assert.Equal("Click", mapping.XamlAttributeName);
    }

    [Theory]
    [InlineData("Label")]
    [InlineData("PictureBox")]
    [InlineData("Panel")]
    public void ResolveControlEvent_ClickOnPlainControl_FallsBackToPointerPressedAndIsNotACommand(string typeName)
    {
        var mapping = _registry.ResolveControlEvent(typeName, "Click");

        Assert.Equal("PointerPressed", mapping.AvaloniaEventName);
        Assert.False(mapping.IsCommandCandidate);
        Assert.NotNull(mapping.Guidance);
    }

    [Fact]
    public void ResolveControlEvent_DragDrop_IsAnAttachedDragDropEvent()
    {
        var mapping = _registry.ResolveControlEvent("TreeView", "DragDrop");

        Assert.Equal("Drop", mapping.AvaloniaEventName);
        Assert.Equal("DragDrop", mapping.AttachedOwnerTypeName);
        Assert.Equal("DragDrop.Drop", mapping.XamlAttributeName);
        Assert.Equal("DragEventArgs", mapping.AvaloniaEventArgsTypeName);
    }

    [Fact]
    public void ResolveControlEvent_TimerTick_IsSubscribedFromCodeNotXaml()
    {
        var mapping = _registry.ResolveControlEvent("Timer", "Tick");

        Assert.Equal("Tick", mapping.AvaloniaEventName);
        Assert.True(mapping.SubscribeInCode);
        Assert.Null(mapping.XamlAttributeName);
    }

    [Theory]
    [InlineData("TrackBar", "Scroll", "ValueChanged", "RangeBaseValueChangedEventArgs")]
    [InlineData("HScrollBar", "Scroll", "Scroll", "ScrollEventArgs")]
    [InlineData("VScrollBar", "Scroll", "Scroll", "ScrollEventArgs")]
    [InlineData("DataGridView", "CellClick", "CellPointerPressed", "DataGridCellPointerPressedEventArgs")]
    [InlineData("LinkLabel", "LinkClicked", "Click", "RoutedEventArgs")]
    public void ResolveControlEvent_EventsOnlySomeControlTypesCanExpress_UseThePerTypeOverride(
        string controlType, string winFormsEvent, string expectedAvaloniaEvent, string expectedEventArgs)
    {
        var mapping = _registry.ResolveControlEvent(controlType, winFormsEvent);

        Assert.Equal(expectedAvaloniaEvent, mapping.AvaloniaEventName);
        Assert.Equal(expectedEventArgs, mapping.AvaloniaEventArgsTypeName);
        Assert.Equal(expectedAvaloniaEvent, mapping.XamlAttributeName);
    }

    /// <summary>
    /// The per-type overrides must not loosen the generic answer: Scroll really has no
    /// equivalent on a control that isn't a TrackBar or a ScrollBar.
    /// </summary>
    [Fact]
    public void ResolveControlEvent_ScrollOnAnUnrelatedControl_StillHasNoEquivalent()
    {
        var mapping = _registry.ResolveControlEvent("Panel", "Scroll");

        Assert.Null(mapping.AvaloniaEventName);
        Assert.NotNull(mapping.Guidance);
    }

    [Fact]
    public void ResolveControlEvent_LinkLabelClick_IsACommandCandidateNowThatItMapsToAHyperlinkButton()
    {
        var mapping = _registry.ResolveControlEvent("LinkLabel", "Click");

        Assert.Equal("Click", mapping.AvaloniaEventName);
        Assert.True(mapping.IsCommandCandidate);
    }

    [Fact]
    public void ResolveControlEvent_Paint_HasNoEquivalentButKeepsGuidance()
    {
        var mapping = _registry.ResolveControlEvent("Panel", "Paint");

        Assert.Null(mapping.AvaloniaEventName);
        Assert.Null(mapping.XamlAttributeName);
        Assert.Contains("Render", mapping.Guidance);
    }

    [Fact]
    public void ResolveControlEvent_UnknownEvent_IsUnmappedWithGuidance()
    {
        var mapping = _registry.ResolveControlEvent("Button", "SomeVendorEvent");

        Assert.Null(mapping.AvaloniaEventName);
        Assert.Contains("SomeVendorEvent", mapping.Guidance);
    }

    /// <summary>
    /// Load pairs with Opened and Shown with Loaded, not the other way round: WinForms runs Load
    /// *before* the form is displayed and Shown once it first is, while Avalonia raises Opened as
    /// the window opens and Loaded only after layout and render complete. Pairing them by name
    /// would put Load after the window was already on screen, and run a form's two handlers in the
    /// opposite order to the original.
    /// </summary>
    [Theory]
    [InlineData("Load", "Opened")]
    [InlineData("FormClosing", "Closing")]
    [InlineData("FormClosed", "Closed")]
    [InlineData("Shown", "Loaded")]
    public void ResolveFormEvent_LifecycleEvents_MapToWindowEquivalents(string winFormsEvent, string avaloniaEvent)
    {
        var mapping = _registry.ResolveFormEvent(winFormsEvent);

        Assert.Equal(avaloniaEvent, mapping.AvaloniaEventName);
        Assert.False(mapping.IsCommandCandidate);
    }

    [Fact]
    public void ResolveFormEvent_PlainControlEvent_FallsBackToTheControlTable()
    {
        var mapping = _registry.ResolveFormEvent("KeyDown");

        Assert.Equal("KeyDown", mapping.AvaloniaEventName);
        Assert.Equal("KeyEventArgs", mapping.AvaloniaEventArgsTypeName);
    }
}
