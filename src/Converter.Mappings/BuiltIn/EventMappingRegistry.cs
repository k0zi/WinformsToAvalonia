using Converter.Plugin.Abstractions;

namespace Converter.Mappings.BuiltIn;

/// <summary>
/// Registry of WinForms events to Avalonia events and commands.
/// </summary>
public static class EventMappingRegistry
{
    private static readonly Dictionary<string, EventMapping> _mappings = new()
    {
        // Mouse Events
        ["Click"] = new("Click") { ConvertToCommand = true, CommandName = "ClickCommand" },
        ["DoubleClick"] = new("DoubleTapped") { ConvertToCommand = true, CommandName = "DoubleTapCommand" },
        ["MouseDown"] = new("PointerPressed") { PreserveEventHandler = true },
        ["MouseUp"] = new("PointerReleased") { PreserveEventHandler = true },
        ["MouseMove"] = new("PointerMoved") { PreserveEventHandler = true },
        ["MouseEnter"] = new("PointerEntered") { PreserveEventHandler = true },
        ["MouseLeave"] = new("PointerExited") { PreserveEventHandler = true },
        ["MouseWheel"] = new("PointerWheelChanged") { PreserveEventHandler = true },

        // Keyboard Events
        ["KeyDown"] = new("KeyDown") { PreserveEventHandler = true },
        ["KeyUp"] = new("KeyUp") { PreserveEventHandler = true },
        ["KeyPress"] = new("TextInput") { PreserveEventHandler = true, RequiresParameterConversion = true },

        // Focus Events
        ["GotFocus"] = new("GotFocus") { PreserveEventHandler = true },
        ["LostFocus"] = new("LostFocus") { PreserveEventHandler = true },
        ["Enter"] = new("GotFocus") { PreserveEventHandler = true },
        ["Leave"] = new("LostFocus") { PreserveEventHandler = true },

        // Value Changed Events
        ["TextChanged"] = new("PropertyChanged") { RequiresCustomLogic = true, Notes = "Bind to Text property changes" },
        ["ValueChanged"] = new("PropertyChanged") { RequiresCustomLogic = true },
        ["SelectedIndexChanged"] = new("SelectionChanged") { ConvertToCommand = true, CommandName = "SelectionChangedCommand" },
        ["CheckedChanged"] = new("PropertyChanged") { RequiresCustomLogic = true, Notes = "Bind to IsChecked property" },

        // Form Events
        ["Load"] = new("Loaded") { PreserveEventHandler = true },
        ["FormClosing"] = new("Closing") { PreserveEventHandler = true },
        ["FormClosed"] = new("Closed") { PreserveEventHandler = true },
        ["Resize"] = new("SizeChanged") { PreserveEventHandler = true },

        // Paint Events
        ["Paint"] = new("Render") { RequiresCustomLogic = true, Notes = "Use Avalonia's rendering system" },

        // Validation Events - LostFocus is already a fully supported PreserveEventHandler
        // target (signature, live-body embedding, ViewModel rewrite), so these are handled the
        // same way as MouseDown/KeyDown/etc. rather than left as a bare manual step.
        ["Validating"] = new("LostFocus")
        {
            PreserveEventHandler = true,
            Notes = "WinForms 'Validating' provides a cancellable CancelEventArgs (e.Cancel) with no Avalonia " +
                "LostFocus equivalent - the original body is embedded live, but any e.Cancel logic needs a " +
                "different approach (e.g. re-focusing the control) and must be reviewed."
        },
        ["Validated"] = new("LostFocus") { PreserveEventHandler = true },

        // Drag & Drop
        ["DragDrop"] = new("Drop") { PreserveEventHandler = true },
        ["DragEnter"] = new("DragEnter") { PreserveEventHandler = true },
        ["DragLeave"] = new("DragLeave") { PreserveEventHandler = true },
        ["DragOver"] = new("DragOver") { PreserveEventHandler = true },

        // Control-specific
        ["CellClick"] = new("CellPointerPressed") { ConvertToCommand = true, CommandName = "CellClickCommand" },
        ["NodeClick"] = new("SelectionChanged") { ConvertToCommand = true, CommandName = "NodeClickCommand" },
        ["Scroll"] = new("Scroll") { PreserveEventHandler = true },

        // Timer
        ["Tick"] = new("Tick") { PreserveEventHandler = true }
    };

    public static EventMapping? GetMapping(string winFormsEvent)
    {
        return _mappings.TryGetValue(winFormsEvent, out var mapping) ? mapping : null;
    }

    public static bool IsMapped(string winFormsEvent)
    {
        return _mappings.ContainsKey(winFormsEvent);
    }

    public static IReadOnlyDictionary<string, EventMapping> GetAllMappings()
    {
        return _mappings;
    }

    /// <summary>
    /// Determines if an event should be converted to a command.
    /// </summary>
    public static bool ShouldConvertToCommand(string eventName)
    {
        var mapping = GetMapping(eventName);
        return mapping?.ConvertToCommand ?? false;
    }

    /// <summary>
    /// WinForms property each RequiresCustomLogic "value changed" event corresponds to, when
    /// that value is also data-bound - e.g. a TextChanged handler is only automatable as a
    /// CommunityToolkit property-changed hook if the same control's Text is bound to an
    /// observable property in the first place.
    /// </summary>
    private static readonly Dictionary<string, string> CustomLogicEventToWinFormsProperty = new()
    {
        ["TextChanged"] = "Text",
        ["ValueChanged"] = "Value",
        ["CheckedChanged"] = "Checked",
    };

    /// <summary>
    /// Resolves the bound ViewModel property name (the DataBindings.DataMember) for a
    /// RequiresCustomLogic "value changed" event on <paramref name="control"/>, if one exists -
    /// ViewModelGenerator uses this to emit a live "partial void On{Property}Changed(...)" hook
    /// instead of leaving the event as a manual step; ConversionOrchestrator's manual-step
    /// collection uses the same lookup to know when NOT to flag it. Returns null for events
    /// with no bound-property automation path (e.g. "Paint") or when the control's relevant
    /// property isn't actually bound.
    /// </summary>
    public static string? FindBoundPropertyName(ControlNode control, string eventName)
    {
        return CustomLogicEventToWinFormsProperty.TryGetValue(eventName, out var winFormsProperty)
            ? control.DataBindings.FirstOrDefault(b => b.PropertyName == winFormsProperty)?.DataMember
            : null;
    }
}

/// <summary>
/// Represents an event mapping from WinForms to Avalonia.
/// </summary>
public record EventMapping(string AvaloniaEvent)
{
    /// <summary>
    /// Whether to convert this event to an ICommand in the ViewModel.
    /// </summary>
    public bool ConvertToCommand { get; init; }

    /// <summary>
    /// The suggested command name if converting to command.
    /// </summary>
    public string? CommandName { get; init; }

    /// <summary>
    /// Whether to preserve the event handler instead of converting.
    /// </summary>
    public bool PreserveEventHandler { get; init; }

    /// <summary>
    /// Whether custom conversion logic is required.
    /// </summary>
    public bool RequiresCustomLogic { get; init; }

    /// <summary>
    /// Whether event parameter conversion is required.
    /// </summary>
    public bool RequiresParameterConversion { get; init; }

    /// <summary>
    /// Additional notes about the mapping.
    /// </summary>
    public string? Notes { get; init; }
}
