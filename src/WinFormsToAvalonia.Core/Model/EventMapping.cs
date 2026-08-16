namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// How one WinForms event maps onto Avalonia. <see cref="AvaloniaEventName"/> is null when
/// Avalonia has no equivalent at all - the handler method is still emitted (so its original
/// body isn't lost) but nothing subscribes to it, and <see cref="Guidance"/> explains why.
/// </summary>
/// <param name="AvaloniaEventArgsTypeName">The generated handler's second parameter type.</param>
/// <param name="AttachedOwnerTypeName">
/// Set for attached events that are written as `Owner.Event` in XAML (Avalonia's drag/drop
/// events live on the attached <c>DragDrop</c> class, not on Control).
/// </param>
/// <param name="SubscribeInCode">
/// True for events that can't be an element attribute in AXAML because their source isn't the
/// element itself (a DispatcherTimer's Tick) - the generated code-behind subscribes them instead.
/// </param>
/// <param name="IsCommandCandidate">
/// True only for the "user invoked this control" events (a Button/menu item Click). Every other
/// event carries state or payload that a parameterless ICommand cannot express, so it is never
/// eligible for ViewModel promotion regardless of what its body does.
/// </param>
public sealed record EventMapping(
    string WinFormsEventName,
    string? AvaloniaEventName,
    string AvaloniaEventArgsTypeName = "EventArgs",
    string? AttachedOwnerTypeName = null,
    bool SubscribeInCode = false,
    bool IsCommandCandidate = false,
    string? Guidance = null)
{
    /// <summary>The AXAML attribute name for this event, e.g. "Click" or "DragDrop.Drop".</summary>
    public string? XamlAttributeName => AvaloniaEventName is null || SubscribeInCode
        ? null
        : AttachedOwnerTypeName is null ? AvaloniaEventName : $"{AttachedOwnerTypeName}.{AvaloniaEventName}";
}
