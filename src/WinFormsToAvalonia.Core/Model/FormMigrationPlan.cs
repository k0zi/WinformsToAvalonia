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
/// <param name="Rewrite">
/// What <c>HandlerBodyRewriter</c> made of the body: the statements it could translate into real
/// Avalonia code, and the un-migrated remainder that still goes into the comment block. Null when
/// nothing was attempted.
/// </param>
public sealed record CodeBehindHandlerPlan(
    string MethodName,
    string EventArgsTypeName,
    bool IsAsync,
    string OriginalMethodName,
    string OriginalBody,
    IReadOnlyList<EventSubscriptionPlan> Subscriptions,
    RewrittenBody? Rewrite = null)
{
    /// <summary>The part of the original body still waiting for a human.</summary>
    public string RemainingBody => Rewrite?.RemainingBody ?? OriginalBody;

    /// <summary>
    /// True when Avalonia can raise this handler <em>during</em> <c>InitializeComponent</c> - see
    /// <see cref="EventMapping.RaisedDuringInitialization"/>. Such a handler has to return early
    /// until the View is built, because until then it has no fields to touch.
    /// </summary>
    public bool NeedsInitializationGuard =>
        Subscriptions.Any(s => !s.Suppressed && s.Mapping.RaisedDuringInitialization);

    /// <summary>
    /// True when the generated method still carries a <c>MigrationTodo</c> marker.
    /// </summary>
    /// <remarks>
    /// Computed here rather than at each site so the emitter and the generated
    /// <c>MIGRATION.md</c> cannot disagree about which methods are finished - a checklist that
    /// drifts from the code it describes is worse than no checklist.
    /// </remarks>
    public bool IsUnfinished => RemainingBody.Length > 0 || Rewrite?.MigratedStatementCount is null or 0;
}

/// <summary>
/// A handler promoted to a CommunityToolkit [RelayCommand] on the ViewModel, together with the
/// control whose Command property binds to it.
/// </summary>
/// <param name="Rewrite">
/// The command body translated against the ViewModel's own [ObservableProperty] names - see
/// <c>HandlerBodyRewriter</c>. A promoted handler's vocabulary was already proved bindable, so
/// this is usually a complete rewrite rather than a prefix.
/// </param>
/// <param name="CanExecuteExpression">
/// The command's guard, translated against this ViewModel's own properties - derived from a
/// handler whose whole job was keeping the control's <c>Enabled</c> state in sync. Null when no
/// such handler was found, which is the common case.
/// </param>
public sealed record ViewModelCommandPlan(
    string CommandMethodName,
    string ControlFieldName,
    string OriginalMethodName,
    string OriginalBody,
    bool IsAsync,
    RewrittenBody? Rewrite = null,
    string? CanExecuteExpression = null)
{
    /// <summary>The generated guard method CommunityToolkit binds through CanExecute, e.g. "CanOk".</summary>
    public string CanExecuteMethodName => $"Can{CommandMethodName}";

    /// <summary>The generated ICommand property name CommunityToolkit derives from the method, e.g. "Ok" -> "OkCommand".</summary>
    public string CommandPropertyName => $"{CommandMethodName}Command";

    /// <summary>The part of the original body still waiting for a human.</summary>
    public string RemainingBody => Rewrite?.RemainingBody ?? OriginalBody;

    /// <summary>True when the generated command still carries a <c>MigrationTodo</c> marker.</summary>
    public bool IsUnfinished => RemainingBody.Length > 0 || Rewrite?.MigratedStatementCount is null or 0;
}

/// <summary>
/// A control property that a promoted command's body reads or writes, and which therefore must
/// exist as an [ObservableProperty] on the ViewModel *and* as a {Binding} on the element. The
/// two are always planned together - a ViewModel property with no binding behind it is dead code.
/// </summary>
/// <param name="NotifiesCommands">
/// ICommand property names whose CanExecute depends on this property, so the generated
/// [ObservableProperty] can raise their CanExecuteChanged. Without it a derived guard would only
/// ever be evaluated once.
/// </param>
public sealed record BoundPropertyPlan(
    string ControlFieldName,
    string AvaloniaPropertyName,
    string ViewModelPropertyName,
    string ClrTypeName,
    string DefaultValueSuffix,
    IReadOnlyList<string>? NotifiesCommands = null)
{
    public IReadOnlyList<string> NotifiesCommands { get; } = NotifiesCommands ?? [];
}

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
/// A non-visual WinForms component that is really a plain .NET type
/// (<see cref="WinFormsToAvalonia.Core.Mapping.ComponentFieldCatalog"/>), emitted as a real field
/// on the generated View so handler bodies can name it.
/// </summary>
/// <param name="Initializers">
/// Designer-set properties whose value could be reproduced as a C# literal, already formatted
/// (<c>"Path = \"/tmp\""</c>). Everything else about the component is reported rather than guessed.
/// </param>
/// <param name="Subscriptions">Designer-wired events, as (event name, handler method name).</param>
public sealed record ComponentFieldPlan(
    string FieldName,
    string ClrTypeName,
    string Namespace,
    string? NuGetPackage,
    bool WindowsOnly,
    IReadOnlyList<string> Initializers,
    IReadOnlyList<(string EventName, string HandlerMethodName)> Subscriptions);

/// <summary>
/// A private backing field of the original Form, carried over as real code - which is what lets
/// the helper that maintains it (the classic <c>SetBusy</c> / <c>isBusy</c> pair) translate at all.
/// </summary>
public sealed record PromotedFieldPlan(string Name, string ModifiersText, string TypeText, string? InitializerText);

/// <summary>
/// A code-behind helper method whose <em>entire</em> body translated, so the generated View can
/// carry it as real, compiling code rather than as a comment - which is also what lets the
/// handlers that call it translate.
/// </summary>
public sealed record PromotedHelperPlan(
    string Name,
    string ReturnTypeText,
    string ParameterListText,
    RewrittenBody Rewrite,
    bool IsAsync);

/// <summary>
/// A property of the original Form or UserControl whose accessor bodies translated <em>whole</em>,
/// so the generated View can carry it as real, compiling code.
/// </summary>
/// <remarks>
/// This is the converted View's public surface, and it is the reason a handler elsewhere can say
/// <c>dialog.EnteredText</c> or <c>demoUserControl1.Caption = "..."</c> at all: without it the
/// property survives only as a comment, and every use of it stops translating.
/// </remarks>
public sealed record PromotedPropertyPlan(
    string Name,
    string ModifiersText,
    string TypeText,
    RewrittenBody? Getter,
    RewrittenBody? Setter);

/// <summary>
/// The single migration decision set for one Form, built once by FormMigrationPlanner and shared
/// by all three emitters (AXAML, View code-behind, ViewModel) so they can never disagree about
/// where a handler went or which properties are bound.
/// </summary>
/// <summary>
/// A control bound to a WinForms <c>BindingSource</c>, and the ViewModel collection that replaces
/// it.
/// </summary>
/// <remarks>
/// <para>
/// The element type is deliberately <c>object</c>. The row type in a WinForms form is usually a
/// private nested class the conversion cannot carry over, and it does not need to: the generated
/// <c>DataGridTextColumn</c>s bind with <c>{ReflectionBinding}</c>, which resolves against the
/// row's runtime type. So the columns start working the moment real rows are added, whatever
/// their type turns out to be.
/// </para>
/// <para>
/// What this is <b>not</b> is a translation of the data. The collection is generated empty and
/// stays empty: <c>bindingSource1.DataSource = new BindingList&lt;T&gt; { ... }</c> in a handler is
/// still refused. This is the wiring, and the conversion says so rather than implying the rows
/// came across.
/// </para>
/// </remarks>
public sealed record DataSourceBindingPlan(
    string ControlFieldName,
    string SourceFieldName,
    string ViewModelPropertyName);

public sealed record FormMigrationPlan(
    IReadOnlyList<CodeBehindHandlerPlan> CodeBehindHandlers,
    IReadOnlyList<ViewModelCommandPlan> ViewModelCommands,
    IReadOnlyList<BoundPropertyPlan> BoundProperties,
    IReadOnlyList<TimerFieldPlan> Timers,
    IReadOnlyList<ComponentFieldPlan> Components,
    IReadOnlyList<FileDialogPlan> FileDialogs,
    IReadOnlyList<PromotedFieldPlan> PromotedFields,
    IReadOnlyList<PromotedHelperPlan> PromotedHelpers,
    IReadOnlyList<PromotedHelperPlan> ViewModelHelpers,
    IReadOnlyList<PromotedPropertyPlan> PromotedProperties,
    IReadOnlyList<HelperMemberModel> PreservedMembers,
    IReadOnlyList<string> ConstructorExtraStatements,
    IReadOnlyList<string> Warnings,
    /// <summary>
    /// Controls whose WinForms <c>DataSource</c> pointed at a <c>BindingSource</c>, and the
    /// ViewModel collection each one now binds its <c>ItemsSource</c> to.
    /// </summary>
    IReadOnlyList<DataSourceBindingPlan>? DataSourceBindings = null)
{
    public IReadOnlyList<DataSourceBindingPlan> DataSourceBindings { get; } = DataSourceBindings ?? [];

    public static FormMigrationPlan Empty { get; } = new([], [], [], [], [], [], [], [], [], [], [], [], []);

    /// <summary>Every body rewrite in this plan - both the code-behind and the ViewModel side.</summary>
    private IEnumerable<RewrittenBody> Rewrites =>
        CodeBehindHandlers.Select(h => h.Rewrite)
            .Concat(ViewModelCommands.Select(c => c.Rewrite))
            .OfType<RewrittenBody>();

    /// <summary>
    /// Every rewrite whose *output* lands in this project, helper bodies included. Separate from
    /// <see cref="Rewrites"/> because a helper's statements are real emitted code - so they can
    /// pull in a `using` or a bundled template - but they are not <em>handler</em> statements, and
    /// counting them would quietly change what the migration rate means.
    /// </summary>
    private IEnumerable<RewrittenBody> AllEmittedRewrites =>
        Rewrites
            .Concat(PromotedHelpers.Select(h => h.Rewrite))
            .Concat(ViewModelHelpers.Select(h => h.Rewrite))
            .Concat(PromotedProperties.SelectMany(p => new[] { p.Getter, p.Setter }).OfType<RewrittenBody>());

    /// <summary>
    /// Whether the View declares the field the close-confirmation rewrite reads, so a
    /// programmatic second <c>Close()</c> falls straight through instead of asking again.
    /// </summary>
    public bool RequiresCloseGuard => AllEmittedRewrites.Any(r => r.RequiresCloseGuard);

    /// <summary>Whether any handler here can be raised before the View has finished initializing.</summary>
    public bool RequiresInitializationGuard => CodeBehindHandlers.Any(h => h.NeedsInitializationGuard);

    /// <summary>Extra `using`s the translated statements need, beyond a generated View's usual set.</summary>
    public IReadOnlyList<string> RequiredUsings =>
        [.. AllEmittedRewrites.SelectMany(r => r.RequiredUsings).Distinct(StringComparer.Ordinal).OrderBy(u => u, StringComparer.Ordinal)];

    /// <summary>
    /// Bundled templates a translated statement depends on (today: MessageBoxFallback). Unlike
    /// every other fallback key these come from a *handler body* rather than the AXAML, so
    /// ConversionPipeline has to union them in separately.
    /// </summary>
    public IReadOnlyList<string> RequiredFallbackKeys =>
        [.. AllEmittedRewrites.SelectMany(r => r.RequiredFallbackKeys).Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal)];

    /// <summary>
    /// NuGet packages the emitted component fields need. Like a mapper's
    /// <c>RequiredNuGetPackage</c>, this only takes effect if the same package is also listed in
    /// <c>AvaloniaProjectScaffolder.ExtraPackageVersions</c>.
    /// </summary>
    public IReadOnlyList<string> RequiredNuGetPackages =>
        [.. Components.Select(c => c.NuGetPackage).OfType<string>().Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal)];

    /// <summary>Statements translated into real Avalonia code, out of every statement seen.</summary>
    public (int Migrated, int Total) StatementMigration =>
        (Rewrites.Sum(r => r.MigratedStatementCount), Rewrites.Sum(r => r.TotalStatementCount));

    /// <summary>AXAML event attributes to emit on the given control, e.g. ("Click", "button1_Click").</summary>
    public IEnumerable<(string AttributeName, string HandlerMethodName)> XamlEventAttributesFor(string? controlFieldName) =>
        CodeBehindHandlers
            .SelectMany(h => h.Subscriptions)
            .Where(s => string.Equals(s.ControlFieldName, controlFieldName, StringComparison.Ordinal))
            .Where(s => !s.Suppressed && s.Mapping.XamlAttributeName is not null)
            .Select(s => (s.Mapping.XamlAttributeName!, s.HandlerMethodName));

    /// <summary>
    /// Tray-icon events the generated View's constructor has to subscribe, because a NotifyIcon
    /// has no element in this View at all - its TrayIcon lives in App.axaml.
    /// </summary>
    /// <remarks>
    /// Derived rather than stored, like <see cref="XamlEventAttributesFor"/>: it is a different
    /// question about the same subscriptions, and a second copy could disagree with the first.
    /// Suppressed ones are excluded, which is what keeps the constructor from naming a TrayIcon
    /// that App.axaml emitted commented out.
    /// </remarks>
    public IEnumerable<(string FieldName, string AvaloniaEventName, string HandlerMethodName)> TrayIconSubscriptions =>
        CodeBehindHandlers
            .SelectMany(h => h.Subscriptions)
            .Where(s => !s.Suppressed
                && s.ControlClrTypeName == "NotifyIcon"
                && s.ControlFieldName is not null
                && s.Mapping is { SubscribeInCode: true, AvaloniaEventName: not null })
            .Select(s => (s.ControlFieldName!, s.Mapping.AvaloniaEventName!, s.HandlerMethodName));

    /// <summary>The ICommand property a control's Command attribute should bind to, if it was promoted.</summary>
    public string? CommandPropertyFor(string controlFieldName) =>
        ViewModelCommands
            .FirstOrDefault(c => string.Equals(c.ControlFieldName, controlFieldName, StringComparison.Ordinal))
            ?.CommandPropertyName;

    public IEnumerable<BoundPropertyPlan> BoundPropertiesFor(string controlFieldName) =>
        BoundProperties.Where(p => string.Equals(p.ControlFieldName, controlFieldName, StringComparison.Ordinal));
}
