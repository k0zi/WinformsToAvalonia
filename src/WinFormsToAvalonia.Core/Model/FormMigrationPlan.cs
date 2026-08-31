namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One `+=` subscription re-expressed in Avalonia terms: which element raises it, under which
/// Avalonia event name, and which generated method handles it.
/// </summary>
/// <param name="ControlFieldName">The designer field raising the event, or null for a Form/Window-level event.</param>
/// <param name="Suppressed">
/// Set when there is nothing to subscribe to at all - a NotifyIcon whose icon never resolved has
/// no TrayIcon in App.axaml, so naming it would not compile. The handler method is still
/// generated; only the subscription is dropped, with a warning.
/// </param>
/// <param name="ChainedInConstructor">
/// Set when another subscription on the same element already claimed this Avalonia event. Two
/// distinct WinForms events can collapse onto one (a PictureBox's Click and MouseDown both become
/// PointerPressed), and emitting both as attributes would be a duplicate XML attribute - which
/// does not merge, it fails to parse. An <em>event</em> takes any number of handlers though, so
/// the loser is subscribed from the constructor with a <c>+=</c> instead of being dropped. Both
/// bodies run, which is what the WinForms original did.
/// </param>
public sealed record EventSubscriptionPlan(
    string? ControlFieldName,
    string ControlClrTypeName,
    string WinFormsEventName,
    EventMapping Mapping,
    string HandlerMethodName,
    bool Suppressed = false,
    bool ChainedInConstructor = false);

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
/// A control bound to a WinForms <c>BindingSource</c>, and the ViewModel collection that replaces
/// it.
/// </summary>
/// <remarks>
/// <para>
/// <paramref name="ElementTypeName"/> is null - and the collection therefore an
/// <c>ObservableCollection&lt;object&gt;</c> - whenever the row type could not be determined or
/// did not come across. That is not a hole: the generated <c>DataGridTextColumn</c>s bind with
/// <c>{ReflectionBinding}</c>, which resolves against the row's runtime type, so the columns
/// work either way. A named element type is what lets the *population* be translated, since the
/// rewriter then knows which property names are real.
/// </para>
/// </remarks>
/// <param name="ElementTypeName">
/// The row type, when a handler's <c>{source}.DataSource = new SomeList&lt;T&gt;</c> named one this
/// run lifted into <c>Models/</c>. Null otherwise.
/// </param>
/// <param name="ElementPropertyNames">
/// That type's settable auto-properties. This is the <b>entire</b> vocabulary an object
/// initializer for it may use when a handler's population statement is translated - read off the
/// parsed declaration, which is what makes the translation provable rather than a guess.
/// </param>
public sealed record DataSourceBindingPlan(
    string ControlFieldName,
    string SourceFieldName,
    string ViewModelPropertyName,
    string? ElementTypeName = null,
    string? ElementTypeNamespace = null,
    IReadOnlyList<string>? ElementPropertyNames = null)
{
    public IReadOnlyList<string> ElementPropertyNames { get; } = ElementPropertyNames ?? [];
}

/// <summary>
/// A <c>View.Details</c> ListView - which becomes a <c>DataGrid</c> - and the ViewModel collection
/// its rows now live in.
/// </summary>
/// <remarks>
/// A row is a <c>string[]</c>, one cell per column, and column <i>i</i> binds to <c>[i]</c>. That
/// is exactly what a <c>ListViewItem</c> is: an ordered array of sub-item texts. Deriving named
/// properties from the column headers instead would invent domain names the original never wrote
/// (and has no answer for a blank or duplicated header), which is the opposite of what this
/// converter is for. Without this plan the generated <c>DataGridTextColumn</c>s carry a header and
/// <b>no binding at all</b>, so the grid can never show a row - not even after a hand migration.
/// </remarks>
/// <param name="ColumnFieldNames">
/// The <c>ColumnHeader</c> fields in declaration order; a column's index in this list is the
/// index it binds to.
/// </param>
public sealed record ListViewRowsPlan(
    string ControlFieldName,
    string ViewModelPropertyName,
    IReadOnlyList<string> ColumnFieldNames);

/// <summary>
/// A <c>CheckedListBox</c>, and the ViewModel collection whose items carry its tick boxes.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has no checkbox list, but it has an <c>ItemTemplate</c> - so the control stays a
/// <c>ListBox</c> and each row becomes a real <c>CheckBox</c> bound to a row object. The row type
/// is synthesized because the shape is not invented: a CheckedListBox item <em>is</em> a caption
/// and a tick, which is the control's own contract. (Contrast <see cref="ListViewRowsPlan"/>,
/// where deriving names from column headers would have invented domain vocabulary, so the row is
/// a <c>string[]</c> instead.)
/// </para>
/// <para>
/// The designer's <c>Items</c> entries become the collection's initial contents rather than
/// literal item elements - a templated ListBox binds its rows, it does not host them.
/// </para>
/// </remarks>
public sealed record CheckedListPlan(
    string ControlFieldName,
    string ViewModelPropertyName,
    string ElementTypeName,
    string ElementTypeNamespace,
    IReadOnlyList<string> Items);

/// <summary>
/// One of a <c>BindingNavigator</c>'s buttons, and the navigation it performed.
/// </summary>
/// <param name="MethodName">
/// The <c>BindingNavigatorFallback</c> method to call - the clamping lives there, not in the
/// generated code.
/// </param>
public sealed record BindingNavigatorButtonPlan(string ButtonFieldName, string MethodName);

/// <summary>
/// A <c>BindingNavigator</c> whose <c>BindingSource</c> a control is bound to, and the ViewModel
/// state the two now share.
/// </summary>
/// <remarks>
/// <para>
/// <c>BindingSource.Position</c> is the current record index, and the navigator and the grid were
/// two views of that one number. So it becomes one ViewModel property: the navigator's
/// <c>Position</c> and the bound control's <c>SelectedIndex</c> both bind to it two-way, and
/// moving either moves the other - which is what the WinForms pair did.
/// </para>
/// <para>
/// Planned from designer facts only. <c>bindingNavigator1.BindingSource = this.bindingSource1</c>
/// says which collection, and the navigator's <c>MoveFirstItem</c>/<c>MovePreviousItem</c>/
/// <c>MoveNextItem</c>/<c>MoveLastItem</c> properties say which button did what. A navigator whose
/// designer recorded no roles gets the bindings and no buttons, and the conversion says so - the
/// alternative would be guessing a button's job from its name.
/// </para>
/// </remarks>
public sealed record BindingNavigatorPlan(
    string ControlFieldName,
    string BoundControlFieldName,
    string CollectionPropertyName,
    string PositionPropertyName,
    IReadOnlyList<BindingNavigatorButtonPlan> Buttons);

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
    IReadOnlyList<DataSourceBindingPlan>? DataSourceBindings = null,
    /// <summary>
    /// Details-mode ListViews, whose DataGrid rows are their ListViewItems' sub-item texts.
    /// </summary>
    IReadOnlyList<ListViewRowsPlan>? ListViewRows = null,
    /// <summary>
    /// BindingNavigators wired to a BindingSource a control is bound to.
    /// </summary>
    IReadOnlyList<BindingNavigatorPlan>? BindingNavigators = null,
    /// <summary>
    /// CheckedListBoxes, whose per-item tick becomes a bound CheckBox in an ItemTemplate.
    /// </summary>
    IReadOnlyList<CheckedListPlan>? CheckedLists = null)
{
    public IReadOnlyList<DataSourceBindingPlan> DataSourceBindings { get; } = DataSourceBindings ?? [];

    public IReadOnlyList<ListViewRowsPlan> ListViewRows { get; } = ListViewRows ?? [];

    public IReadOnlyList<BindingNavigatorPlan> BindingNavigators { get; } = BindingNavigators ?? [];

    public IReadOnlyList<CheckedListPlan> CheckedLists { get; } = CheckedLists ?? [];

    /// <summary>
    /// Whether the generated View needs a typed field for its ViewModel rather than constructing
    /// one straight into <c>DataContext</c>.
    /// </summary>
    /// <remarks>
    /// Asked in one place so the code-behind emitter and <c>HandlerBodyRewriter</c> cannot
    /// disagree about whether the field the rewriter names actually exists - the same reason
    /// <see cref="CodeBehindHandlerPlan.IsUnfinished"/> is a single predicate. It is deliberately
    /// false for every Form that has neither collection, so the other generated Views stay byte
    /// for byte what they were.
    /// </remarks>
    public bool RequiresViewModelField =>
        DataSourceBindings.Count > 0 || ListViewRows.Count > 0 || CheckedLists.Count > 0;

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
        [.. AllEmittedRewrites.SelectMany(r => r.RequiredFallbackKeys)
            // An event can pull a template in too, without any body naming it: the paint surface's
            // Paint declares the args type the generated handler is *signed* with.
            .Concat(CodeBehindHandlers
                .SelectMany(h => h.Subscriptions)
                .Where(s => !s.Suppressed)
                .Select(s => s.Mapping.FallbackTemplateKey)
                .OfType<string>())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)];

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
            .Where(s => !s.Suppressed && !s.ChainedInConstructor && s.Mapping.XamlAttributeName is not null)
            .Select(s => (s.Mapping.XamlAttributeName!, s.HandlerMethodName));

    /// <summary>
    /// Subscriptions the generated constructor has to make with a <c>+=</c> on a control field.
    /// </summary>
    /// <remarks>
    /// Two reasons land here. One is a WinForms event whose Avalonia counterpart is not an element
    /// attribute at all - a bundled template's own CLR event, like the paint surface's
    /// <c>Paint</c>. The other is the loser of two WinForms events that mapped onto the same
    /// Avalonia one: only the *attribute* is exclusive, so the second handler is subscribed here
    /// instead of dropped. Disjoint from <see cref="XamlEventAttributesFor"/> by construction.
    /// Timers, project components and NotifyIcon are excluded because each already has an emission
    /// path of its own, and subscribing twice would run every handler twice.
    /// </remarks>
    public IEnumerable<(string FieldName, string AvaloniaEventName, string HandlerMethodName)> ConstructorEventSubscriptions
    {
        get
        {
            var handledElsewhere = Timers.Select(t => t.FieldName)
                .Concat(Components.Select(c => c.FieldName))
                .ToHashSet(StringComparer.Ordinal);

            return CodeBehindHandlers
                .SelectMany(h => h.Subscriptions)
                .Where(s => !s.Suppressed
                    && s.ControlFieldName is not null
                    && s.Mapping.AvaloniaEventName is not null
                    && s.ControlClrTypeName != "NotifyIcon"
                    && !handledElsewhere.Contains(s.ControlFieldName)
                    && (s.ChainedInConstructor || s.Mapping.SubscribeInCode))
                .Select(s => (s.ControlFieldName!, s.Mapping.AvaloniaEventName!, s.HandlerMethodName));
        }
    }

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
