using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The WinForms event -> Avalonia event table, the event-level counterpart of
/// <see cref="ControlMappingRegistry"/>. Most events map the same way regardless of which
/// control raises them (MouseDown is always PointerPressed); the few that don't are resolved
/// through a per-control-type override - most importantly <c>Click</c>, which is a real
/// <c>Click</c> routed event (and a ViewModel command candidate) on a Button or menu item, but
/// only a pointer press on an arbitrary control.
/// </summary>
public sealed class EventMappingRegistry
{
    /// <summary>
    /// Control types whose Click is a genuine "the user invoked this" action AND whose Avalonia
    /// target (Button / MenuItem / HyperlinkButton) actually has both a Click event and a
    /// Command property. Every other control type falls through to the PointerPressed
    /// treatment below, which is a press, not a press+release invocation.
    /// </summary>
    private static readonly HashSet<string> ClickCommandControlTypes = new(StringComparer.Ordinal)
    {
        "Button", "ToolStripButton", "ToolStripMenuItem", "LinkLabel", "ToolStripDropDownButton", "ToolStripSplitButton",
    };

    /// <summary>
    /// Per-control-type event overrides, consulted before the generic <see cref="ControlEvents"/>
    /// table. These exist because the generic answer is "no Avalonia equivalent" only for
    /// *most* controls: a TrackBar's Scroll really is a Slider's ValueChanged, a ScrollBar's
    /// Scroll really is Avalonia's ScrollBar.Scroll, and a LinkLabel's LinkClicked really is
    /// the Click of the HyperlinkButton it now maps to. Keeping them here rather than
    /// loosening the generic entries preserves the honest "not translatable" answer for every
    /// other control type.
    /// </summary>
    private static readonly Dictionary<(string ControlType, string EventName), EventMapping> ControlTypeOverrides = new()
    {
        [("TrackBar", "Scroll")] = new("Scroll", "ValueChanged", "RangeBaseValueChangedEventArgs",
            Guidance: "WinForms' TrackBar.Scroll fires only on user drags; Avalonia's Slider.ValueChanged also fires on programmatic Value changes."),
        [("HScrollBar", "Scroll")] = new("Scroll", "Scroll", "ScrollEventArgs"),
        [("VScrollBar", "Scroll")] = new("Scroll", "Scroll", "ScrollEventArgs"),
        [("DataGridView", "CellClick")] = new("CellClick", "CellPointerPressed", "DataGridCellPointerPressedEventArgs",
            Guidance: "Avalonia's DataGrid reports the cell through DataGridCellPointerPressedEventArgs.Cell/Row, not a ColumnIndex/RowIndex pair."),
        [("LinkLabel", "LinkClicked")] = new("LinkClicked", "Click", "RoutedEventArgs", IsCommandCandidate: true,
            Guidance: "LinkLabel maps to a HyperlinkButton, whose Click replaces LinkClicked; the LinkLabelLinkClickedEventArgs.Link information has no equivalent."),
    };

    private static readonly EventMapping ClickAsCommand =
        new("Click", "Click", "RoutedEventArgs", IsCommandCandidate: true);

    private static readonly EventMapping ClickAsPointerPress =
        new("Click", "PointerPressed", "PointerPressedEventArgs",
            Guidance: "Avalonia has no Click event on this control type - mapped to PointerPressed, which fires on press rather than on press+release.");

    private static readonly Dictionary<string, EventMapping> ControlEvents = new(StringComparer.Ordinal)
    {
        // Pointer.
        ["MouseDown"] = new("MouseDown", "PointerPressed", "PointerPressedEventArgs"),
        ["MouseUp"] = new("MouseUp", "PointerReleased", "PointerReleasedEventArgs"),
        ["MouseMove"] = new("MouseMove", "PointerMoved", "PointerEventArgs"),
        ["MouseEnter"] = new("MouseEnter", "PointerEntered", "PointerEventArgs"),
        ["MouseLeave"] = new("MouseLeave", "PointerExited", "PointerEventArgs"),
        ["MouseWheel"] = new("MouseWheel", "PointerWheelChanged", "PointerWheelEventArgs"),
        ["DoubleClick"] = new("DoubleClick", "DoubleTapped", "TappedEventArgs"),
        ["MouseDoubleClick"] = new("MouseDoubleClick", "DoubleTapped", "TappedEventArgs"),

        // Keyboard.
        ["KeyDown"] = new("KeyDown", "KeyDown", "KeyEventArgs"),
        ["KeyUp"] = new("KeyUp", "KeyUp", "KeyEventArgs"),
        ["KeyPress"] = new("KeyPress", "TextInput", "TextInputEventArgs",
            Guidance: "WinForms KeyPress exposed a char; Avalonia's TextInput exposes the whole inserted string via TextInputEventArgs.Text."),

        // Focus.
        ["Enter"] = new("Enter", "GotFocus", "GotFocusEventArgs"),
        ["Leave"] = new("Leave", "LostFocus", "RoutedEventArgs"),
        ["GotFocus"] = new("GotFocus", "GotFocus", "GotFocusEventArgs"),
        ["LostFocus"] = new("LostFocus", "LostFocus", "RoutedEventArgs"),

        // Drag and drop - attached events on Avalonia's DragDrop class, not on Control.
        ["DragEnter"] = new("DragEnter", "DragEnter", "DragEventArgs", AttachedOwnerTypeName: "DragDrop"),
        ["DragOver"] = new("DragOver", "DragOver", "DragEventArgs", AttachedOwnerTypeName: "DragDrop"),
        ["DragLeave"] = new("DragLeave", "DragLeave", "RoutedEventArgs", AttachedOwnerTypeName: "DragDrop"),
        ["DragDrop"] = new("DragDrop", "Drop", "DragEventArgs", AttachedOwnerTypeName: "DragDrop"),

        // Value/selection changes. These are the events a two-way {Binding} usually replaces
        // entirely, but they are still wired as real events so no behaviour is silently lost.
        ["TextChanged"] = new("TextChanged", "TextChanged", "TextChangedEventArgs"),
        ["CheckedChanged"] = new("CheckedChanged", "IsCheckedChanged", "RoutedEventArgs"),
        ["CheckStateChanged"] = new("CheckStateChanged", "IsCheckedChanged", "RoutedEventArgs"),
        ["SelectedIndexChanged"] = new("SelectedIndexChanged", "SelectionChanged", "SelectionChangedEventArgs"),
        ["SelectedValueChanged"] = new("SelectedValueChanged", "SelectionChanged", "SelectionChangedEventArgs"),
        ["SelectionChanged"] = new("SelectionChanged", "SelectionChanged", "SelectionChangedEventArgs"),
        ["ValueChanged"] = new("ValueChanged", "ValueChanged", "NumericUpDownValueChangedEventArgs"),
        ["AfterSelect"] = new("AfterSelect", "SelectionChanged", "SelectionChangedEventArgs",
            Guidance: "Avalonia's TreeView reports selection through SelectionChanged; the selected node is TreeView.SelectedItem, not TreeViewEventArgs.Node."),
        ["ItemCheck"] = new("ItemCheck", null, Guidance: "Avalonia's ListBox has no per-item check event; model the checked state on the item's own view model instead."),

        // Layout.
        ["Resize"] = new("Resize", "SizeChanged", "SizeChangedEventArgs"),
        ["SizeChanged"] = new("SizeChanged", "SizeChanged", "SizeChangedEventArgs"),
        ["VisibleChanged"] = new("VisibleChanged", null, Guidance: "Avalonia has no VisibleChanged event; observe the IsVisible property instead."),

        // No Avalonia equivalent.
        ["Paint"] = new("Paint", null, Guidance: "Avalonia has no Paint event - override Control.Render(DrawingContext) on a custom control, or use a Path/Shape."),
        ["Validating"] = new("Validating", null, Guidance: "Avalonia has no Validating event - use INotifyDataErrorInfo / DataAnnotations validation on the bound view model property."),
        ["Validated"] = new("Validated", null, Guidance: "Avalonia has no Validated event - see the Validating guidance."),
        ["Scroll"] = new("Scroll", null, Guidance: "Avalonia raises ScrollChanged on the ScrollViewer inside the control, not on the control itself."),
    };

    private static readonly Dictionary<string, EventMapping> FormEvents = new(StringComparer.Ordinal)
    {
        ["Load"] = new("Load", "Loaded", "RoutedEventArgs"),
        ["Shown"] = new("Shown", "Opened"),
        ["FormClosing"] = new("FormClosing", "Closing", "WindowClosingEventArgs"),
        ["FormClosed"] = new("FormClosed", "Closed"),
        ["Activated"] = new("Activated", "Activated"),
        ["Deactivate"] = new("Deactivate", "Deactivated"),
        ["Resize"] = new("Resize", "SizeChanged", "SizeChangedEventArgs"),
    };

    /// <summary>Non-visual components whose events are subscribed from code, never as an AXAML attribute.</summary>
    private static readonly Dictionary<string, EventMapping> ComponentEvents = new(StringComparer.Ordinal)
    {
        ["Tick"] = new("Tick", "Tick", "EventArgs", SubscribeInCode: true),
    };

    /// <summary>Resolves an event raised by a control of the given WinForms type.</summary>
    public EventMapping ResolveControlEvent(string winFormsControlTypeName, string eventName)
    {
        if (eventName == "Click")
        {
            return ClickCommandControlTypes.Contains(winFormsControlTypeName) ? ClickAsCommand : ClickAsPointerPress;
        }

        if (ControlTypeOverrides.TryGetValue((winFormsControlTypeName, eventName), out var overrideMapping))
        {
            return overrideMapping;
        }

        if (winFormsControlTypeName == "Timer" && ComponentEvents.TryGetValue(eventName, out var componentMapping))
        {
            return componentMapping;
        }

        // A non-visual component this run emits as a real field of the same, unchanged .NET type:
        // the event keeps its name and its own args type, and the constructor subscribes it. The
        // catalog is consulted rather than duplicated here because the same table decides that
        // the field exists at all - the two answers must not drift apart.
        if (ComponentFieldCatalog.TryGetEvent(winFormsControlTypeName, eventName, out var componentEvent))
        {
            return new EventMapping(eventName, eventName, componentEvent.ArgsTypeName, SubscribeInCode: true);
        }

        return ControlEvents.TryGetValue(eventName, out var mapping) ? mapping : Unmapped(eventName);
    }

    /// <summary>Resolves a Form-level event (`this.Load += ...`), which targets the generated Window.</summary>
    public EventMapping ResolveFormEvent(string eventName) =>
        FormEvents.TryGetValue(eventName, out var mapping)
            ? mapping
            : ControlEvents.TryGetValue(eventName, out var controlMapping)
                ? controlMapping
                : Unmapped(eventName);

    private static EventMapping Unmapped(string eventName) =>
        new(eventName, null, Guidance: $"No Avalonia equivalent is registered for the WinForms '{eventName}' event.");
}
