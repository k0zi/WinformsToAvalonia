using Microsoft.CodeAnalysis.CSharp;
using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Decides, for one Form, where every event handler goes: an event-driven method on the
/// generated View (the default) or a CommunityToolkit [RelayCommand] on the ViewModel (the
/// evidence-backed exception). Runs once per Form and produces the single
/// <see cref="FormMigrationPlan"/> that AxamlEmitter, ViewCodeBehindEmitter and
/// ViewModelEmitter all read, so the three can never disagree about where a handler ended up
/// or which properties are bound.
/// </summary>
/// <remarks>
/// <para>
/// The promotion rule is intentionally strict, because the failure modes are asymmetric: a
/// handler wrongly left in code-behind is merely un-MVVM, while a handler wrongly promoted
/// lands in a class that cannot reach the control it was written against. A handler is promoted
/// only when it is a genuine "user invoked this control" event (a Button/menu Click), it ignores
/// both `sender` and the EventArgs, it drives no Form member, opens no other Form, calls no
/// helper method, and every control member it touches is a two-way bindable value property
/// (<see cref="BindablePropertyCatalog"/>) on a Direct-mapped control.
/// </para>
/// <para>
/// Every promoted command's bound properties are planned here too, so a ViewModel property is
/// only ever generated together with the {Binding} that feeds it.
/// </para>
/// </remarks>
public sealed class FormMigrationPlanner
{
    private static readonly IReadOnlyDictionary<string, (string PickerMethod, string OptionsTypeName)> FileDialogTemplates =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["OpenFileDialog"] = ("OpenFilePickerAsync", "FilePickerOpenOptions"),
            ["SaveFileDialog"] = ("SaveFilePickerAsync", "FilePickerSaveOptions"),
            ["FolderBrowserDialog"] = ("OpenFolderPickerAsync", "FolderPickerOpenOptions"),
        };

    private readonly ControlMappingRegistry _controlMappings;
    private readonly EventMappingRegistry _eventMappings;

    public FormMigrationPlanner(ControlMappingRegistry controlMappings, EventMappingRegistry eventMappings)
    {
        _controlMappings = controlMappings;
        _eventMappings = eventMappings;
    }

    public FormMigrationPlan Plan(FormModel formModel, CodeBehindModel codeBehind)
    {
        var warnings = new List<string>();
        var subscriptionsByHandler = CollectSubscriptions(formModel, codeBehind, warnings);

        var codeBehindHandlers = new List<CodeBehindHandlerPlan>();
        var viewModelCommands = new List<ViewModelCommandPlan>();
        var boundProperties = new List<BoundPropertyPlan>();
        var seenBoundProperties = new HashSet<(string Control, string Property)>();

        foreach (var (handlerMethodName, subscriptions) in subscriptionsByHandler)
        {
            var source = codeBehind.FindHandler(handlerMethodName);
            if (source is null && codeBehind.HandlerMethods.Count > 0)
            {
                warnings.Add(
                    $"handler method '{handlerMethodName}' is referenced by the designer but was not found in " +
                    $"'{Path.GetFileName(codeBehind.OriginalFilePath)}' - emitted as an empty stub.");
            }

            if (TryPlanCommand(formModel, handlerMethodName, subscriptions, source, out var command, out var reason))
            {
                viewModelCommands.Add(command);
                AddBoundProperties(formModel, source!, boundProperties, seenBoundProperties);
                continue;
            }

            if (reason is not null)
            {
                warnings.Add($"Click handler '{handlerMethodName}' stays in code-behind: {reason}");
            }

            codeBehindHandlers.AddRange(PlanCodeBehindHandler(handlerMethodName, subscriptions, source, warnings));
        }

        return new FormMigrationPlan(
            ResolveDuplicateXamlAttributes(codeBehindHandlers, warnings),
            viewModelCommands,
            boundProperties,
            PlanTimers(formModel),
            PlanFileDialogs(formModel),
            codeBehind.HelperMembers,
            codeBehind.ConstructorExtraStatements,
            warnings);
    }

    /// <summary>
    /// WinForms distinguishes events that Avalonia merges: a PictureBox's <c>Click</c> and
    /// <c>MouseDown</c> both become <c>PointerPressed</c>. Emitting both would be a duplicate XML
    /// attribute on the same element, which fails the Avalonia XAML parser (AVLN1001) and breaks
    /// the generated build - so at most one subscription per (element, Avalonia event) survives.
    /// Exact mappings win over approximations (an approximation is exactly the mapping that
    /// carries <see cref="EventMapping.Guidance"/>), and the dropped handler is reported so it can
    /// be chained by hand.
    /// </summary>
    private static List<CodeBehindHandlerPlan> ResolveDuplicateXamlAttributes(
        List<CodeBehindHandlerPlan> handlers,
        List<string> warnings)
    {
        var claimedBy = new Dictionary<(string? Control, string Attribute), EventSubscriptionPlan>();
        var suppressed = new HashSet<(string? Control, string WinFormsEvent)>();

        var candidates = handlers
            .SelectMany(h => h.Subscriptions)
            .Where(s => s.Mapping.XamlAttributeName is not null)
            .OrderBy(s => s.Mapping.Guidance is null ? 0 : 1);

        foreach (var subscription in candidates)
        {
            var key = (subscription.ControlFieldName, subscription.Mapping.XamlAttributeName!);
            if (claimedBy.TryGetValue(key, out var winner))
            {
                suppressed.Add((subscription.ControlFieldName, subscription.WinFormsEventName));
                var owner = subscription.ControlFieldName is null ? "the Form" : $"'{subscription.ControlFieldName}'";
                warnings.Add(
                    $"{owner} subscribes both '{winner.WinFormsEventName}' and '{subscription.WinFormsEventName}', which map to the " +
                    $"same Avalonia event '{key.Item2}' - only '{winner.HandlerMethodName}' is subscribed; " +
                    $"call '{subscription.HandlerMethodName}' from it by hand.");
                continue;
            }

            claimedBy[key] = subscription;
        }

        if (suppressed.Count == 0)
        {
            return handlers;
        }

        return
        [
            .. handlers.Select(h => h with
            {
                Subscriptions = [.. h.Subscriptions.Select(s =>
                    suppressed.Contains((s.ControlFieldName, s.WinFormsEventName)) ? s with { Suppressed = true } : s)],
            }),
        ];
    }

    /// <summary>
    /// Only Timer components that actually have a Tick handler get a DispatcherTimer - the same
    /// evidence-driven rule as everywhere else. Interval/Enabled are ordinary designer literals
    /// DesignerSyntaxWalker already captured.
    /// </summary>
    private static List<TimerFieldPlan> PlanTimers(FormModel formModel)
    {
        var timers = new List<TimerFieldPlan>();

        foreach (var component in formModel.Controls.Values.Where(c => c.ClrTypeName == "Timer"))
        {
            var tick = component.Events.FirstOrDefault(e => e.EventName == "Tick" && e.HandlerMethodName is not null);
            if (tick is null)
            {
                continue;
            }

            var interval = component.Properties.TryGetValue("Interval", out var intervalValue)
                && intervalValue is PropertyValue.Literal { Value: int milliseconds }
                    ? milliseconds
                    : 1000;
            var startImmediately = component.Properties.TryGetValue("Enabled", out var enabledValue)
                && enabledValue is PropertyValue.Literal { Value: true };

            timers.Add(new TimerFieldPlan(component.FieldName, tick.HandlerMethodName!, interval, startImmediately));
        }

        return timers;
    }

    private static List<FileDialogPlan> PlanFileDialogs(FormModel formModel) =>
    [
        .. formModel.Controls.Values
            .Where(c => FileDialogTemplates.ContainsKey(c.ClrTypeName))
            .Select(c =>
            {
                var (pickerMethod, optionsType) = FileDialogTemplates[c.ClrTypeName];
                return new FileDialogPlan(c.FieldName, $"Show{NamingConventions.Capitalize(c.FieldName)}Async", pickerMethod, optionsType);
            }),
    ];

    /// <summary>
    /// Groups every `+=` this Form has - designer-captured control and Form events, plus the
    /// hand-written subscriptions CodeBehindAnalyzer found in the code-behind - by the method
    /// that handles them. One method wired to several controls yields one group with several
    /// subscriptions, which is exactly the shape that later forces a signature split.
    /// </summary>
    private Dictionary<string, List<EventSubscriptionPlan>> CollectSubscriptions(
        FormModel formModel,
        CodeBehindModel codeBehind,
        List<string> warnings)
    {
        var byHandler = new Dictionary<string, List<EventSubscriptionPlan>>(StringComparer.Ordinal);

        void Add(EventSubscriptionPlan subscription)
        {
            if (!byHandler.TryGetValue(subscription.HandlerMethodName, out var list))
            {
                list = [];
                byHandler[subscription.HandlerMethodName] = list;
            }

            var alreadyPresent = list.Any(s =>
                string.Equals(s.ControlFieldName, subscription.ControlFieldName, StringComparison.Ordinal)
                && string.Equals(s.WinFormsEventName, subscription.WinFormsEventName, StringComparison.Ordinal));

            if (!alreadyPresent)
            {
                list.Add(subscription);
            }
        }

        foreach (var binding in formModel.FormEvents)
        {
            if (binding.HandlerMethodName is not { } handlerName)
            {
                warnings.Add($"the Form's '{binding.EventName}' event uses an inline lambda handler, which is not migrated automatically.");
                continue;
            }

            Add(new EventSubscriptionPlan(null, "Form", binding.EventName, _eventMappings.ResolveFormEvent(binding.EventName), handlerName));
        }

        foreach (var control in formModel.Controls.Values)
        {
            foreach (var binding in control.Events)
            {
                if (binding.HandlerMethodName is not { } handlerName)
                {
                    warnings.Add($"'{control.FieldName}.{binding.EventName}' uses an inline lambda handler, which is not migrated automatically.");
                    continue;
                }

                Add(new EventSubscriptionPlan(
                    control.FieldName,
                    control.ClrTypeName,
                    binding.EventName,
                    _eventMappings.ResolveControlEvent(control.ClrTypeName, binding.EventName),
                    handlerName));
            }
        }

        // Hand-written subscriptions only need to make the target method exist: the statement that
        // wires them lives inside another handler's preserved body, so re-emitting a subscription
        // here would double-wire it.
        foreach (var subscription in codeBehind.RuntimeEventSubscriptions)
        {
            if (!byHandler.ContainsKey(subscription.HandlerMethodName))
            {
                byHandler[subscription.HandlerMethodName] = [];
            }
        }

        return byHandler;
    }

    private bool TryPlanCommand(
        FormModel formModel,
        string handlerMethodName,
        List<EventSubscriptionPlan> subscriptions,
        HandlerMethodModel? source,
        out ViewModelCommandPlan command,
        out string? reason)
    {
        command = null!;
        reason = null;

        if (subscriptions.Count != 1 || !subscriptions[0].Mapping.IsCommandCandidate)
        {
            // Not a command-shaped event at all - not worth reporting, it was never a candidate.
            if (subscriptions.Count > 1 && subscriptions.Any(s => s.Mapping.IsCommandCandidate))
            {
                reason = $"it is wired to {subscriptions.Count} controls, so it needs the 'sender' that told them apart.";
            }

            return false;
        }

        var subscription = subscriptions[0];

        if (subscription.ControlFieldName is null
            || !formModel.Controls.TryGetValue(subscription.ControlFieldName, out var owner)
            || _controlMappings.Map(owner).Status != MappingStatus.Direct)
        {
            reason = $"its control has no direct Avalonia element with a Command property to bind to.";
            return false;
        }

        if (source is null)
        {
            reason = "its body could not be found in the code-behind file, so there is no evidence it is bindable.";
            return false;
        }

        if (source.UsesSender)
        {
            reason = "its body uses the 'sender' parameter, which an ICommand does not provide.";
            return false;
        }

        if (source.UsesEventArgs)
        {
            reason = $"its body uses the '{source.EventArgsTypeName}' argument, which an ICommand does not provide.";
            return false;
        }

        if (source.CreatesOtherForms)
        {
            reason = "it opens another Form/Dialog, which needs a navigation or dialog service before it can move to a ViewModel.";
            return false;
        }

        if (source.TouchedFormMembers.Count > 0)
        {
            reason = $"it drives the Form itself ({string.Join(", ", source.TouchedFormMembers)}).";
            return false;
        }

        if (source.CalledHelperMethods.Count > 0)
        {
            reason = $"it calls the code-behind helper(s) {string.Join(", ", source.CalledHelperMethods)}.";
            return false;
        }

        foreach (var (fieldName, members) in source.ControlMemberAccesses)
        {
            if (!formModel.Controls.TryGetValue(fieldName, out var control))
            {
                reason = $"it touches '{fieldName}', which is not a designer control.";
                return false;
            }

            if (_controlMappings.Map(control).Status != MappingStatus.Direct)
            {
                reason = $"'{fieldName}' ({control.ClrTypeName}) has no direct Avalonia element to bind against.";
                return false;
            }

            foreach (var member in members)
            {
                if (!BindablePropertyCatalog.TryGet(control.ClrTypeName, member, out _))
                {
                    reason = $"it uses '{fieldName}.{member}', which has no bindable Avalonia equivalent.";
                    return false;
                }
            }
        }

        command = new ViewModelCommandPlan(
            NamingConventions.DeriveCommandName(handlerMethodName, subscription.WinFormsEventName),
            subscription.ControlFieldName!,
            handlerMethodName,
            source.BodyText,
            source.IsAsync);

        return true;
    }

    private static void AddBoundProperties(
        FormModel formModel,
        HandlerMethodModel source,
        List<BoundPropertyPlan> boundProperties,
        HashSet<(string Control, string Property)> seen)
    {
        foreach (var (fieldName, members) in source.ControlMemberAccesses)
        {
            if (!formModel.Controls.TryGetValue(fieldName, out var control))
            {
                continue;
            }

            foreach (var member in members)
            {
                if (!BindablePropertyCatalog.TryGet(control.ClrTypeName, member, out var bindable)
                    || !seen.Add((fieldName, bindable.AvaloniaPropertyName)))
                {
                    continue;
                }

                boundProperties.Add(new BoundPropertyPlan(
                    fieldName,
                    bindable.AvaloniaPropertyName,
                    $"{NamingConventions.Capitalize(fieldName)}{bindable.AvaloniaPropertyName}",
                    bindable.ClrTypeName,
                    DeriveInitializer(control, member, bindable.DefaultValueSuffix)));
            }
        }
    }

    /// <summary>
    /// Once a property is bound, its designer-set literal moves out of the AXAML attribute (a
    /// {Binding} takes that attribute's place) and becomes the ViewModel property's initial
    /// value, so the window still starts up looking the way the designer defined it. Only
    /// string and bool literals are carried over; anything else keeps the catalog default.
    /// </summary>
    private static string DeriveInitializer(ControlModel control, string winFormsPropertyName, string catalogDefault) =>
        control.Properties.TryGetValue(winFormsPropertyName, out var value) && value is PropertyValue.Literal literal
            ? literal.Value switch
            {
                string text => $" = {SymbolDisplay.FormatLiteral(text, quote: true)};",
                bool flag => $" = {(flag ? "true" : "false")};",
                _ => catalogDefault,
            }
            : catalogDefault;

    /// <summary>
    /// One generated method per distinct Avalonia signature. A handler shared by controls whose
    /// Click maps differently (a Button's real Click vs. a Label's PointerPressed) cannot be a
    /// single C# method, so it is split and each part keeps the Avalonia event in its name.
    /// </summary>
    private static IEnumerable<CodeBehindHandlerPlan> PlanCodeBehindHandler(
        string handlerMethodName,
        List<EventSubscriptionPlan> subscriptions,
        HandlerMethodModel? source,
        List<string> warnings)
    {
        var body = source?.BodyText ?? "";
        var isAsync = source?.IsAsync ?? false;

        foreach (var subscription in subscriptions.Where(s => s.Mapping.AvaloniaEventName is null))
        {
            var owner = subscription.ControlFieldName is null ? "the Form" : $"'{subscription.ControlFieldName}'";
            warnings.Add(
                $"{owner} subscribes '{subscription.WinFormsEventName}', which has no Avalonia equivalent - " +
                $"'{handlerMethodName}' is emitted but never subscribed. {subscription.Mapping.Guidance}");
        }

        var groups = subscriptions
            .Where(s => s.Mapping.AvaloniaEventName is not null)
            .GroupBy(s => (s.Mapping.AvaloniaEventArgsTypeName, s.Mapping.AvaloniaEventName))
            .ToList();

        if (groups.Count == 0)
        {
            // Either an unmapped event or a hand-written subscription: emit the method, wire nothing.
            yield return new CodeBehindHandlerPlan(
                handlerMethodName,
                "EventArgs",
                isAsync,
                handlerMethodName,
                body,
                []);
            yield break;
        }

        var needsSplit = groups.Count > 1;
        if (needsSplit)
        {
            warnings.Add(
                $"handler '{handlerMethodName}' is shared by controls whose Avalonia events have different signatures - " +
                $"split into {string.Join(", ", groups.Select(g => $"'{handlerMethodName}_{g.Key.AvaloniaEventName}'"))}.");
        }

        foreach (var group in groups)
        {
            var methodName = needsSplit ? $"{handlerMethodName}_{group.Key.AvaloniaEventName}" : handlerMethodName;
            yield return new CodeBehindHandlerPlan(
                methodName,
                group.Key.AvaloniaEventArgsTypeName,
                isAsync,
                handlerMethodName,
                body,
                [.. group.Select(s => s with { HandlerMethodName = methodName })]);
        }
    }
}
