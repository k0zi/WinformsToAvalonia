namespace Converter.Mappings.BuiltIn;

/// <summary>
/// Maps an Avalonia event name (EventMappingRegistry's AvaloniaEvent, not the original
/// WinForms name) to the EventArgs type its handler delegate expects, so CodeBehindGenerator
/// can emit a correctly-signed `private void Handler(object? sender, {EventArgsType} e)`
/// stub instead of guessing. Types are stored fully-qualified so the generator never has to
/// guess at `using` statements. Best-effort/commonly-correct for the Avalonia 11.x/12.x
/// surface; an event not listed here (or a future Avalonia version renaming a type) falls
/// back to the generic RoutedEventArgs shape, which still compiles as a valid
/// EventHandler&lt;T&gt; signature even if it isn't the exact type the real event uses.
/// </summary>
public static class EventSignatureRegistry
{
    private static readonly Dictionary<string, EventSignature> _signatures = new()
    {
        ["PointerPressed"] = new("Avalonia.Input.PointerPressedEventArgs"),
        ["PointerReleased"] = new("Avalonia.Input.PointerReleasedEventArgs"),
        ["PointerMoved"] = new("Avalonia.Input.PointerEventArgs"),
        ["PointerEntered"] = new("Avalonia.Input.PointerEventArgs"),
        ["PointerExited"] = new("Avalonia.Input.PointerEventArgs"),
        ["PointerWheelChanged"] = new("Avalonia.Input.PointerWheelEventArgs"),
        ["KeyDown"] = new("Avalonia.Input.KeyEventArgs"),
        ["KeyUp"] = new("Avalonia.Input.KeyEventArgs"),
        ["TextInput"] = new("Avalonia.Input.TextInputEventArgs"),
        ["GotFocus"] = new("Avalonia.Input.GotFocusEventArgs"),
        ["LostFocus"] = new("Avalonia.Interactivity.RoutedEventArgs"),
        ["Loaded"] = new("Avalonia.Interactivity.RoutedEventArgs"),
        ["Closing"] = new("Avalonia.Controls.WindowClosingEventArgs"),
        ["Closed"] = new("System.EventArgs"),
        ["SizeChanged"] = new("Avalonia.Controls.SizeChangedEventArgs"),
        ["Drop"] = new("Avalonia.Input.DragEventArgs"),
        ["DragEnter"] = new("Avalonia.Input.DragEventArgs"),
        ["DragLeave"] = new("Avalonia.Input.DragEventArgs"),
        ["DragOver"] = new("Avalonia.Input.DragEventArgs"),
        ["Scroll"] = new("Avalonia.Interactivity.RoutedEventArgs"),
        ["Tick"] = new("System.EventArgs")
    };

    /// <summary>
    /// Avalonia 12 overrides of the v11 table above - confirmed via the official breaking-
    /// changes doc: GotFocus (previously GotFocusEventArgs) and LostFocus (previously plain
    /// RoutedEventArgs) both now use the unified FocusChangedEventArgs. Every other event in
    /// the base table is unaffected between v11 and v12.
    /// </summary>
    private static readonly Dictionary<string, EventSignature> _v12Overrides = new()
    {
        ["GotFocus"] = new("Avalonia.Input.FocusChangedEventArgs"),
        ["LostFocus"] = new("Avalonia.Input.FocusChangedEventArgs")
    };

    private static readonly EventSignature Default = new("Avalonia.Interactivity.RoutedEventArgs");

    /// <summary>
    /// Parses the major version number out of a configured Avalonia version string (e.g.
    /// "12.0.0" -> 12, "11.2.0" -> 11, "12.0.0-preview1" -> 12). Falls back to 11 (the older,
    /// more conservative baseline) on an unparseable string, rather than assuming the newer
    /// v12 surface for a version we couldn't actually identify.
    /// </summary>
    public static int ParseMajorVersion(string avaloniaVersion)
    {
        var majorSegment = avaloniaVersion.Split('.', 2)[0];
        return int.TryParse(majorSegment, out var major) ? major : 11;
    }

    /// <summary>
    /// <paramref name="avaloniaMajorVersion"/> defaults to 12, matching this converter's
    /// current default generated-project target (ProjectGenerationConfig.AvaloniaVersion).
    /// </summary>
    public static EventSignature GetSignature(string avaloniaEvent, int avaloniaMajorVersion = 12)
    {
        if (avaloniaMajorVersion >= 12 && _v12Overrides.TryGetValue(avaloniaEvent, out var v12Signature))
        {
            return v12Signature;
        }

        return _signatures.TryGetValue(avaloniaEvent, out var signature) ? signature : Default;
    }
}

/// <summary>Handler delegate shape for an Avalonia event: EventHandler&lt;EventArgsType&gt;.</summary>
public record EventSignature(string EventArgsType);
