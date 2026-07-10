namespace Converter.Mappings.BuiltIn;

/// <summary>
/// Maps an Avalonia event name (EventMappingRegistry's AvaloniaEvent, not the original
/// WinForms name) to the EventArgs type its handler delegate expects, so CodeBehindGenerator
/// can emit a correctly-signed `private void Handler(object? sender, {EventArgsType} e)`
/// stub instead of guessing. Types are stored fully-qualified so the generator never has to
/// guess at `using` statements. Best-effort/commonly-correct for the Avalonia 11.x surface;
/// an event not listed here (or a future Avalonia version renaming a type) falls back to the
/// generic RoutedEventArgs shape, which still compiles as a valid EventHandler&lt;T&gt;
/// signature even if it isn't the exact type the real event uses.
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

    private static readonly EventSignature Default = new("Avalonia.Interactivity.RoutedEventArgs");

    public static EventSignature GetSignature(string avaloniaEvent) =>
        _signatures.TryGetValue(avaloniaEvent, out var signature) ? signature : Default;
}

/// <summary>Handler delegate shape for an Avalonia event: EventHandler&lt;EventArgsType&gt;.</summary>
public record EventSignature(string EventArgsType);
