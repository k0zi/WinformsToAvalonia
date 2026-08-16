namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One `+=` subscription re-expressed in Avalonia terms: which element raises it, under which
/// Avalonia event name, and which generated method handles it.
/// </summary>
/// <param name="ControlFieldName">The designer field raising the event, or null for a Form/Window-level event.</param>
/// <param name="Suppressed">
/// Set when another subscription on the same element already claimed this Avalonia event. Two
/// distinct WinForms events can collapse onto one Avalonia event (a PictureBox's Click and
/// MouseDown both become PointerPressed), and emitting both would be a duplicate XML attribute.
/// The handler method is still generated; only the subscription is dropped, with a warning.
/// </param>
public sealed record EventSubscriptionPlan(
    string? ControlFieldName,
    string ControlClrTypeName,
    string WinFormsEventName,
    EventMapping Mapping,
    string HandlerMethodName,
    bool Suppressed = false);

/// <summary>
/// A handler that stays event-driven: emitted as a real method on the generated View, with the
/// mapped Avalonia signature, and subscribed from AXAML (or from code, for a DispatcherTimer).
/// </summary>
public sealed record CodeBehindHandlerPlan(
    string MethodName,
    string EventArgsTypeName,
    bool IsAsync,
    string OriginalMethodName,
    string OriginalBody,
    IReadOnlyList<EventSubscriptionPlan> Subscriptions);

/// <summary>
/// A handler promoted to a CommunityToolkit [RelayCommand] on the ViewModel, together with the
/// control whose Command property binds to it.
/// </summary>
public sealed record ViewModelCommandPlan(
    string CommandMethodName,
    string ControlFieldName,
    string OriginalMethodName,
    string OriginalBody,
    bool IsAsync)
{
    /// <summary>The generated ICommand property name CommunityToolkit derives from the method, e.g. "Ok" -> "OkCommand".</summary>
    public string CommandPropertyName => $"{CommandMethodName}Command";
}

/// <summary>
/// A control property that a promoted command's body reads or writes, and which therefore must
/// exist as an [ObservableProperty] on the ViewModel *and* as a {Binding} on the element. The
/// two are always planned together - a ViewModel property with no binding behind it is dead code.
/// </summary>
public sealed record BoundPropertyPlan(
    string ControlFieldName,
    string AvaloniaPropertyName,
    string ViewModelPropertyName,
    string ClrTypeName,
    string DefaultValueSuffix);

/// <summary>
/// A WinForms <c>Timer</c> component with a Tick handler. Avalonia's replacement,
/// <c>DispatcherTimer</c>, is not a control, so it is created and subscribed from the View's
/// constructor rather than declared in AXAML.
/// </summary>
public sealed record TimerFieldPlan(
    string FieldName,
    string TickHandlerMethodName,
    int IntervalMilliseconds,
    bool StartImmediately);

/// <summary>
/// A WinForms file/folder dialog component. Avalonia's replacement is
/// <c>TopLevel.StorageProvider</c>, an async API reached from the View (which <em>is</em> the
/// TopLevel) - so unlike in earlier revisions this lands in code-behind, with no
/// Application.Current.MainWindow lookup needed.
/// </summary>
public sealed record FileDialogPlan(
    string FieldName,
    string MethodName,
    string PickerMethodName,
    string OptionsTypeName);

/// <summary>
/// The single migration decision set for one Form, built once by FormMigrationPlanner and shared
/// by all three emitters (AXAML, View code-behind, ViewModel) so they can never disagree about
/// where a handler went or which properties are bound.
/// </summary>
public sealed record FormMigrationPlan(
    IReadOnlyList<CodeBehindHandlerPlan> CodeBehindHandlers,
    IReadOnlyList<ViewModelCommandPlan> ViewModelCommands,
    IReadOnlyList<BoundPropertyPlan> BoundProperties,
    IReadOnlyList<TimerFieldPlan> Timers,
    IReadOnlyList<FileDialogPlan> FileDialogs,
    IReadOnlyList<HelperMemberModel> PreservedMembers,
    IReadOnlyList<string> ConstructorExtraStatements,
    IReadOnlyList<string> Warnings)
{
    public static FormMigrationPlan Empty { get; } = new([], [], [], [], [], [], [], []);

    /// <summary>AXAML event attributes to emit on the given control, e.g. ("Click", "button1_Click").</summary>
    public IEnumerable<(string AttributeName, string HandlerMethodName)> XamlEventAttributesFor(string? controlFieldName) =>
        CodeBehindHandlers
            .SelectMany(h => h.Subscriptions)
            .Where(s => string.Equals(s.ControlFieldName, controlFieldName, StringComparison.Ordinal))
            .Where(s => !s.Suppressed && s.Mapping.XamlAttributeName is not null)
            .Select(s => (s.Mapping.XamlAttributeName!, s.HandlerMethodName));

    /// <summary>The ICommand property a control's Command attribute should bind to, if it was promoted.</summary>
    public string? CommandPropertyFor(string controlFieldName) =>
        ViewModelCommands
            .FirstOrDefault(c => string.Equals(c.ControlFieldName, controlFieldName, StringComparison.Ordinal))
            ?.CommandPropertyName;

    public IEnumerable<BoundPropertyPlan> BoundPropertiesFor(string controlFieldName) =>
        BoundProperties.Where(p => string.Equals(p.ControlFieldName, controlFieldName, StringComparison.Ordinal));
}
