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
/// <param name="RaisedDuringInitialization">
/// True when Avalonia raises this event <em>while the AXAML is still being populated</em> - a
/// TabControl selects its first tab as it initialises, a CheckBox raises IsCheckedChanged when
/// XAML sets IsChecked. The handler attribute is wired before those properties are set, so the
/// handler runs inside <c>InitializeComponent</c>, before a single <c>x:Name</c> field exists.
/// WinForms had no such window - every control field was assigned at the top of its own
/// InitializeComponent - so a converted handler firing this early is an artifact of the
/// conversion, and <c>ViewCodeBehindEmitter</c> guards against it.
/// </param>
public sealed record EventMapping(
    string WinFormsEventName,
    string? AvaloniaEventName,
    string AvaloniaEventArgsTypeName = "EventArgs",
    string? AttachedOwnerTypeName = null,
    bool SubscribeInCode = false,
    bool IsCommandCandidate = false,
    bool RaisedDuringInitialization = false,
    string? Guidance = null)
{
    /// <summary>The AXAML attribute name for this event, e.g. "Click" or "DragDrop.Drop".</summary>
    public string? XamlAttributeName => AvaloniaEventName is null || SubscribeInCode
        ? null
        : AttachedOwnerTypeName is null ? AvaloniaEventName : $"{AttachedOwnerTypeName}.{AvaloniaEventName}";
}
