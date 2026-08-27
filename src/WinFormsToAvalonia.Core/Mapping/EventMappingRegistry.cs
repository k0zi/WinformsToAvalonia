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
    /// Events declared by the *source project's own* Components, when this run carries their
    /// source over. Per-run rather than in a static table, because nothing outside that project
    /// knows they exist - which is why this is the one registry built with data.
    /// </summary>
    private readonly IReadOnlyDictionary<(string Type, string Event), string> _projectComponentEvents;

    public EventMappingRegistry(IReadOnlyDictionary<(string Type, string Event), string>? projectComponentEvents = null) =>
        _projectComponentEvents = projectComponentEvents ?? new Dictionary<(string, string), string>();

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
    /// <remarks>
    /// Declared before <see cref="ControlTypeOverrides"/>, which names them: static initializers
    /// run in source order, and the other way round every entry below would be null.
    /// </remarks>
    /// <remarks>
    /// All four are <see cref="EventMapping.RaisedDuringInitialization"/>: each fires when its
    /// property is set, and XAML sets properties <em>after</em> wiring the handler - so every one
    /// of them runs inside InitializeComponent, before the View has any fields.
    /// </remarks>
    private static EventMapping TextChanged { get; } =
        new("TextChanged", "TextChanged", "TextChangedEventArgs", RaisedDuringInitialization: true);

    private static EventMapping IsCheckedChanged { get; } =
        new("CheckedChanged", "IsCheckedChanged", "RoutedEventArgs", RaisedDuringInitialization: true);

    private static EventMapping SelectionChanged { get; } =
        new("SelectedIndexChanged", "SelectionChanged", "SelectionChangedEventArgs", RaisedDuringInitialization: true);

    private static EventMapping RangeValueChanged { get; } =
        new("ValueChanged", "ValueChanged", "RangeBaseValueChangedEventArgs", RaisedDuringInitialization: true);

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

        // The value/selection events. Every WinForms Control declares them; the Avalonia elements
        // that raise them are specific types, so each entry below names a WinForms type whose
        // *mapped element* really has the event. Anything not listed falls through to the generic
        // table's honest refusal - see NotOnEveryControl.
        [("TextBox", "TextChanged")] = TextChanged,

        [("CheckBox", "CheckedChanged")] = IsCheckedChanged,
        [("CheckBox", "CheckStateChanged")] = IsCheckedChanged,
        [("RadioButton", "CheckedChanged")] = IsCheckedChanged,
        [("RadioButton", "CheckStateChanged")] = IsCheckedChanged,

        [("ComboBox", "SelectedIndexChanged")] = SelectionChanged,
        [("ComboBox", "SelectedValueChanged")] = SelectionChanged,
        [("ComboBox", "SelectionChangeCommitted")] = SelectionChanged,
        [("ToolStripComboBox", "SelectedIndexChanged")] = SelectionChanged,
        [("ListBox", "SelectedIndexChanged")] = SelectionChanged,
        [("ListBox", "SelectedValueChanged")] = SelectionChanged,
        [("CheckedListBox", "SelectedIndexChanged")] = SelectionChanged,
        [("ListView", "SelectedIndexChanged")] = SelectionChanged,
        [("TabControl", "SelectedIndexChanged")] = SelectionChanged,
        [("DataGridView", "SelectionChanged")] = SelectionChanged,
        [("TreeView", "AfterSelect")] = new("AfterSelect", "SelectionChanged", "SelectionChangedEventArgs",
            RaisedDuringInitialization: true,
            Guidance: "Avalonia's TreeView reports selection through SelectionChanged; the selected node is TreeView.SelectedItem, not TreeViewEventArgs.Node."),

        // Two different ValueChanged events, with two different args types: a NumericUpDown has
        // its own, everything range-shaped inherits RangeBase's.
        [("NumericUpDown", "ValueChanged")] = new(
            "ValueChanged", "ValueChanged", "NumericUpDownValueChangedEventArgs",
            RaisedDuringInitialization: true),
        [("TrackBar", "ValueChanged")] = RangeValueChanged,
        [("ProgressBar", "ValueChanged")] = RangeValueChanged,
        [("HScrollBar", "ValueChanged")] = RangeValueChanged,
        [("VScrollBar", "ValueChanged")] = RangeValueChanged,
    };


    /// <summary>
    /// Why a value/selection event has no generic answer. Written once, because the reason is the
    /// same every time and it is the thing a reader of the report needs to know.
    /// </summary>
    private static string NotOnEveryControl(string avaloniaEventName, string whatHasIt) =>
        $"Avalonia raises {avaloniaEventName} on {whatHasIt}, not on every control - so it is only "
        + "translated for the control types whose Avalonia element really has it. On this one the "
        + "two-way {Binding} the conversion emits is usually what you want instead.";

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
        // Both carry FocusChangedEventArgs in Avalonia 12 - GotFocusEventArgs was its name in 11,
        // and LostFocus was never a bare RoutedEventArgs. A handler signed with either would not
        // compile against the event the AXAML attribute binds to.
        ["Enter"] = new("Enter", "GotFocus", "FocusChangedEventArgs"),
        ["Leave"] = new("Leave", "LostFocus", "FocusChangedEventArgs"),
        ["GotFocus"] = new("GotFocus", "GotFocus", "FocusChangedEventArgs"),
        ["LostFocus"] = new("LostFocus", "LostFocus", "FocusChangedEventArgs"),

        // Drag and drop - attached events on Avalonia's DragDrop class, not on Control.
        ["DragEnter"] = new("DragEnter", "DragEnter", "DragEventArgs", AttachedOwnerTypeName: "DragDrop"),
        ["DragOver"] = new("DragOver", "DragOver", "DragEventArgs", AttachedOwnerTypeName: "DragDrop"),
        ["DragLeave"] = new("DragLeave", "DragLeave", "DragEventArgs", AttachedOwnerTypeName: "DragDrop"),
        ["DragDrop"] = new("DragDrop", "Drop", "DragEventArgs", AttachedOwnerTypeName: "DragDrop"),

        // Value/selection changes. Every WinForms Control has them; almost no Avalonia element
        // does - TextChanged is a TextBox's, IsCheckedChanged a ToggleButton's, SelectionChanged a
        // SelectingItemsControl's. So they live in ControlTypeOverrides above, per control type,
        // and the generic answer here is an honest "not on this one" rather than an attribute the
        // element would reject at XAML compile time.
        ["TextChanged"] = new("TextChanged", null, Guidance: NotOnEveryControl("TextChanged", "a TextBox")),
        ["CheckedChanged"] = new("CheckedChanged", null, Guidance: NotOnEveryControl("IsCheckedChanged", "a CheckBox or RadioButton")),
        ["CheckStateChanged"] = new("CheckStateChanged", null, Guidance: NotOnEveryControl("IsCheckedChanged", "a CheckBox or RadioButton")),
        ["SelectedIndexChanged"] = new("SelectedIndexChanged", null, Guidance: NotOnEveryControl("SelectionChanged", "a list, combo box, tab control, tree or grid")),
        ["SelectedValueChanged"] = new("SelectedValueChanged", null, Guidance: NotOnEveryControl("SelectionChanged", "a list, combo box, tab control, tree or grid")),
        ["SelectionChanged"] = new("SelectionChanged", null, Guidance: NotOnEveryControl("SelectionChanged", "a list, combo box, tab control, tree or grid")),
        ["ValueChanged"] = new("ValueChanged", null, Guidance: NotOnEveryControl("ValueChanged", "a NumericUpDown, slider or progress bar")),
        ["AfterSelect"] = new("AfterSelect", null, Guidance: NotOnEveryControl("SelectionChanged", "a TreeView")),
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

        // The same, for a Component this project declares and this run carries over: the type is
        // unchanged, so the event keeps its name and its own args type.
        if (_projectComponentEvents.TryGetValue((winFormsControlTypeName, eventName), out var argsTypeName))
        {
            return new EventMapping(eventName, eventName, argsTypeName, SubscribeInCode: true);
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

    /// <summary>
    /// Every WinForms event name this registry can answer for, so WinFormsToAvalonia.Mapping.Tests
    /// can resolve each one and check the Avalonia event and args type it names really exist.
    /// </summary>
    /// <remarks>
    /// The per-control overrides come with the type they belong to; the generic entries with null,
    /// meaning "ask whatever control type you like". Component events are excluded: those are
    /// plain .NET types that survived unchanged, so Avalonia has no opinion about them.
    /// </remarks>
    public static IEnumerable<(string? ControlTypeName, string EventName)> ProbableControlEvents =>
        ControlTypeOverrides.Keys.Select(k => ((string?)k.ControlType, k.EventName))
            .Concat(ControlEvents.Keys.Select(e => ((string?)null, e)))
            .Concat(ClickCommandControlTypes.Select(t => ((string?)t, "Click")));

    /// <summary>Every Form-level event name, for the same check against a Window.</summary>
    public static IEnumerable<string> FormEventNames => FormEvents.Keys;

    private static EventMapping Unmapped(string eventName) =>
        new(eventName, null, Guidance: $"No Avalonia equivalent is registered for the WinForms '{eventName}' event.");
}
