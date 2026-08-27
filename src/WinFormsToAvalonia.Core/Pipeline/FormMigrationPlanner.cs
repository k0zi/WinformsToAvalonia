using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    private readonly ControlMappingRegistry _controlMappings;
    private readonly EventMappingRegistry _eventMappings;

    public FormMigrationPlanner(ControlMappingRegistry controlMappings, EventMappingRegistry eventMappings)
    {
        _controlMappings = controlMappings;
        _eventMappings = eventMappings;
    }

    /// <param name="formViews">
    /// Every Form in the project resolved to its generated View, so a handler that opens another
    /// Form can be translated. Empty means navigation stays un-migrated, which is the safe default.
    /// </param>
    /// <param name="artifactKind">
    /// Decides whether the host View is a Window. Only a Window can own a modal dialog, so a
    /// converted UserControl's handlers cannot translate `ShowDialog`.
    /// </param>
    /// <param name="viewSurface">
    /// What the project's converted Views expose to each other, resolved before any body is
    /// translated - including this artifact's own, which the pre-pass already planned.
    /// </param>
    public FormMigrationPlan Plan(
        FormModel formModel,
        CodeBehindModel codeBehind,
        IReadOnlyDictionary<string, FormViewInfo>? formViews = null,
        WinFormsArtifactKind artifactKind = WinFormsArtifactKind.Form,
        IReadOnlyDictionary<string, string>? projectComponentNamespaces = null,
        ViewSurfaceContext? viewSurface = null)
    {
        viewSurface ??= ViewSurfaceContext.None;
        var warnings = new List<string>();
        var subscriptionsByHandler = CollectSubscriptions(formModel, codeBehind, warnings);

        var codeBehindHandlers = new List<CodeBehindHandlerPlan>();
        var viewModelCommands = new List<ViewModelCommandPlan>();
        var boundProperties = new List<BoundPropertyPlan>();
        var seenBoundProperties = new HashSet<(string Control, string Property)>();

        // Helpers that a promoted command reaches. They have to move to the ViewModel with it -
        // and their control accesses count towards its bound properties, which is what lets a
        // `Log(...)` helper make `logTextBox.Text` bindable in the first place.
        var viewModelHelperNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (handlerMethodName, subscriptions) in subscriptionsByHandler)
        {
            var source = codeBehind.FindHandler(handlerMethodName);
            if (source is null && codeBehind.HandlerMethods.Count > 0)
            {
                warnings.Add(
                    $"handler method '{handlerMethodName}' is referenced by the designer but was not found in " +
                    $"'{Path.GetFileName(codeBehind.OriginalFilePath)}' - emitted as an empty stub.");
            }

            var reachedHelpers = new HashSet<string>(StringComparer.Ordinal);
            var effective = source is null ? null : Inlined(source, codeBehind, out reachedHelpers);
            if (TryPlanCommand(formModel, handlerMethodName, subscriptions, source, effective, out var command, out var reason))
            {
                viewModelCommands.Add(command);
                AddBoundProperties(formModel, effective!, boundProperties, seenBoundProperties);
                viewModelHelperNames.UnionWith(reachedHelpers);
                continue;
            }

            if (reason is not null)
            {
                warnings.Add($"Click handler '{handlerMethodName}' stays in code-behind: {reason}");
            }

            codeBehindHandlers.AddRange(PlanCodeBehindHandler(handlerMethodName, subscriptions, source, warnings));
        }

        // A designer-set DialogResult is a whole handler the designer never had to write.
        codeBehindHandlers.AddRange(PlanDialogResultButtons(formModel, artifactKind));

        // A handler that exists only to keep a promoted button's Enabled state in sync is a
        // CanExecute guard written imperatively - fold it into the command and drop it.
        DeriveCanExecuteGuards(formModel, codeBehind, codeBehindHandlers, viewModelCommands, boundProperties, seenBoundProperties, warnings);

        // Body translation runs last, over the finished decisions: which control properties are
        // bound - and therefore what a ViewModel body is even allowed to name - is only settled
        // once every handler has been classified.
        var rewriter = new HandlerBodyRewriter(_controlMappings);
        var navigation = new ViewNavigationContext(
            formViews ?? new Dictionary<string, FormViewInfo>(StringComparer.Ordinal),
            HostIsWindow: artifactKind != WinFormsArtifactKind.UserControl);

        // Planned *before* the rewrite, unlike the file dialogs below: these fields are something
        // a handler body may name, so the rewriter has to know they exist. (The file dialogs go
        // the other way - what to emit for them depends on what the rewrite did.)
        var timers = PlanTimers(formModel);
        var timerFields = timers.Select(t => t.FieldName).ToHashSet(StringComparer.Ordinal);
        var components = PlanComponents(
            formModel,
            codeBehind,
            projectComponentNamespaces ?? new Dictionary<string, string>(StringComparer.Ordinal),
            warnings);
        var componentFields = components.Select(c => c.FieldName).ToHashSet(StringComparer.Ordinal);

        // Helpers first: a handler body may call one, and whether that call translates depends on
        // whether the helper itself did.
        // Fields before helpers: the classic `SetBusy` / `isBusy` pair only translates if the
        // field it maintains exists on the Avalonia side too.
        var promotedFields = PlanHelperFields(codeBehind);
        var promotedFieldNames = promotedFields.Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        var promotedHelpers = PlanHelpers(
            codeBehind, formModel, navigation, timerFields, componentFields, promotedFieldNames, rewriter,
            viewSurface.ByType);
        var helperCalls = promotedHelpers.ToDictionary(
            h => h.Name,
            h => new HelperCallInfo(CountParameters(h.ParameterListText), h.IsAsync),
            StringComparer.Ordinal);

        // The helpers a promoted command reaches move to the ViewModel with it. Their bodies are
        // expressible there by construction: the promotion analysis above already merged each
        // helper's control accesses into the caller's and required every one to be bindable.
        var viewModelHelpers = PlanViewModelHelpers(codeBehind, formModel, boundProperties, viewModelHelperNames, rewriter);
        var viewModelHelperCalls = viewModelHelpers.ToDictionary(
            h => h.Name,
            h => new HelperCallInfo(CountParameters(h.ParameterListText), h.IsAsync),
            StringComparer.Ordinal);

        var rewrittenHandlers = ResolveDuplicateXamlAttributes(codeBehindHandlers, warnings)
            .Select(h => h.Rewrite is not null
                // Already synthesized (a designer-set DialogResult) - there is no original body
                // to translate, and re-running the rewriter would erase it.
                ? h
                : h with
                {
                    Rewrite = rewriter.RewriteForView(
                        h.OriginalBody, formModel, navigation, SignatureOf(h, codeBehind), timerFields, componentFields,
                        helperCalls, promotedFieldNames, viewSurface.ByType),
                })
            .ToList();

        // Which file dialogs a body opened inline is only known once the bodies are translated,
        // and it decides whether a separate picker method is still worth emitting.
        var inlinedDialogFields = rewrittenHandlers
            .SelectMany(h => h.Rewrite?.InlinedDialogFields ?? (IReadOnlySet<string>)new HashSet<string>())
            .ToHashSet(StringComparer.Ordinal);

        return new FormMigrationPlan(
            rewrittenHandlers,
            [.. viewModelCommands
                .Select(c => c with
                {
                    Rewrite = rewriter.RewriteForViewModel(c.OriginalBody, formModel, boundProperties, viewModelHelperCalls),
                })],
            boundProperties,
            timers,
            components,
            PlanFileDialogs(formModel, inlinedDialogFields),
            promotedFields,
            promotedHelpers,
            viewModelHelpers,
            viewSurface.Own,
            // A member that became real code must not *also* appear in the preserved comment
            // block: it would read as un-migrated while a compiling copy sits above it.
            [.. codeBehind.HelperMembers.Where(m =>
                !(m.Kind == HelperMemberKind.Method && helperCalls.ContainsKey(m.Name))
                && !(m.Kind == HelperMemberKind.Field && promotedFieldNames.Contains(m.Name))
                && !(m.Kind == HelperMemberKind.Property
                    && viewSurface.Own.Any(p => string.Equals(p.Name, m.Name, StringComparison.Ordinal))))],
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

    /// <summary>
    /// The helper methods a promoted command calls, translated against the ViewModel.
    /// </summary>
    /// <remarks>
    /// No fixed point here, unlike the View-side round. The promotion decision already answered
    /// the hard question - every control member the helper touches was merged into its caller's
    /// and checked against the bindable catalog - so a helper that reaches this point translates,
    /// and one that somehow does not simply drops out along with its caller's promotion.
    /// </remarks>
    private static List<PromotedHelperPlan> PlanViewModelHelpers(
        CodeBehindModel codeBehind,
        FormModel formModel,
        IReadOnlyList<BoundPropertyPlan> boundProperties,
        IReadOnlySet<string> names,
        HandlerBodyRewriter rewriter)
    {
        var promoted = new List<PromotedHelperPlan>();
        var byName = new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal);

        // Order-independent within a round, so a helper calling another needs a second pass.
        var candidates = codeBehind.HelperMembers
            .Where(m => names.Contains(m.Name)
                && m.Signature is not null
                && IsPlainDotNetSignature(m.Signature!)
                && !ReservedMemberNames.IsReserved(m.Name))
            .ToList();

        for (var round = 0; round < candidates.Count; round++)
        {
            var promotedThisRound = false;

            foreach (var candidate in candidates.Where(c => !byName.ContainsKey(c.Name)).ToList())
            {
                var signature = candidate.Signature!;
                var rewrite = rewriter.RewriteForViewModelHelper(signature, formModel, boundProperties, byName);

                if (rewrite.RemainingBody.Length > 0 || (rewrite.RequiresAsync && signature.ReturnTypeText != "void"))
                {
                    continue;
                }

                promoted.Add(new PromotedHelperPlan(
                    candidate.Name, signature.ReturnTypeText, signature.ParameterListText, rewrite,
                    signature.IsAsync || rewrite.RequiresAsync));
                byName[candidate.Name] = new HelperCallInfo(signature.ParameterNames.Count, signature.IsAsync || rewrite.RequiresAsync);
                promotedThisRound = true;
            }

            if (!promotedThisRound)
            {
                break;
            }
        }

        return promoted;
    }

    /// <summary>
    /// The Form's own private fields, carried over as real code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reason this is worth doing is the shape it unlocks rather than the fields themselves:
    /// the canonical WinForms helper is a <c>SetBusy(bool)</c> maintaining an <c>isBusy</c> flag,
    /// and without the field the helper cannot translate, so neither can any handler that calls it.
    /// </para>
    /// <para>
    /// Keyword types only, and only a literal initializer - the same bar a helper's parameters
    /// and a translated local have to clear, and for the same reason: a named type could be a
    /// WinForms type whose Avalonia counterpart is a different type entirely, and nothing here
    /// can tell without a semantic model.
    /// </para>
    /// </remarks>
    private static List<PromotedFieldPlan> PlanHelperFields(CodeBehindModel codeBehind) =>
    [
        .. codeBehind.HelperMembers
            .Where(m => m.Kind == HelperMemberKind.Field && m.Field is not null)
            .Where(m => IsKeywordTypeText(m.Field!.TypeText))
            .Where(m => m.Field!.InitializerText is null || IsSimpleLiteral(m.Field.InitializerText))
            .Select(m => new PromotedFieldPlan(m.Name, m.Field!.ModifiersText, m.Field.TypeText, m.Field.InitializerText)),
    ];

    /// <summary>A literal, and nothing that could reach for a WinForms API on the way.</summary>
    private static bool IsSimpleLiteral(string text) =>
        SyntaxFactory.ParseExpression(text) is LiteralExpressionSyntax or PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax };

    /// <summary>
    /// The public surface a converted View really carries: the properties of the original Form or
    /// UserControl whose accessor bodies translate <em>whole</em>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Whole-or-nothing for the same reason a helper is: at a use site - <c>dialog.EnteredText</c>
    /// - there is nowhere to put a remainder, so a half-translated property would read as migrated
    /// while quietly doing half its work. Both accessors have to make it, or neither does: a
    /// property that could be read but not written would silently change what assigning to it
    /// means.
    /// </para>
    /// <para>
    /// The body may name <b>only this artifact's own controls</b> - no timers, components, helpers
    /// or promoted fields. That is a real restriction and a deliberate one: it makes a property
    /// depend on nothing that planning decides, which is what lets the whole project's properties
    /// be resolved in one pass *before* any handler is translated. It also covers the shape that
    /// actually occurs - a property over a control property is what a WinForms UserControl's
    /// surface is made of.
    /// </para>
    /// <para>
    /// This runs from <c>ConversionPipeline</c>'s discovery pass rather than from <see cref="Plan"/>,
    /// and its result comes back in as <see cref="ViewSurfaceContext.Own"/>.
    /// </para>
    /// </remarks>
    public IReadOnlyList<PromotedPropertyPlan> PlanProperties(
        FormModel formModel,
        CodeBehindModel codeBehind,
        WinFormsArtifactKind artifactKind = WinFormsArtifactKind.Form)
    {
        var rewriter = new HandlerBodyRewriter(_controlMappings);
        var navigation = new ViewNavigationContext(
            new Dictionary<string, FormViewInfo>(StringComparer.Ordinal),
            HostIsWindow: artifactKind != WinFormsArtifactKind.UserControl);
        var nothing = new HashSet<string>(StringComparer.Ordinal);
        var noHelpers = new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal);

        var promoted = new List<PromotedPropertyPlan>();

        foreach (var candidate in codeBehind.HelperMembers
            .Where(m => m.Kind == HelperMemberKind.Property && m.Property is not null)
            // A named type could be a WinForms type whose Avalonia counterpart is a different type
            // entirely - the same bar a helper's return type has to clear, for the same reason.
            .Where(m => IsKeywordTypeText(m.Property!.TypeText))
            // A property called `Tag` or `Name` would hide the one the generated class already
            // inherits - CS0108 in the generated project, which this build cannot see.
            .Where(m => !ReservedMemberNames.IsReserved(m.Name)))
        {
            var property = candidate.Property!;

            if (!TryTranslateAccessor(property.GetterBodyText, isSetter: false, out var getter)
                || !TryTranslateAccessor(property.SetterBodyText, isSetter: true, out var setter))
            {
                continue;
            }

            promoted.Add(new PromotedPropertyPlan(
                candidate.Name, property.ModifiersText, property.TypeText, getter, setter));
        }

        return promoted;

        bool TryTranslateAccessor(string? body, bool isSetter, out RewrittenBody? rewritten)
        {
            rewritten = null;

            if (body is null)
            {
                return true;
            }

            // `value` reaches the body as an ordinary parameter local, which is exactly what it is.
            var signature = new HelperMethodSignature(
                isSetter ? "void" : "string",
                isSetter ? "(object value)" : "()",
                isSetter ? ["value"] : [],
                body,
                IsAsync: false);

            var result = rewriter.RewriteForHelper(
                signature, formModel, navigation, nothing, nothing, noHelpers, nothing);

            // A property cannot be async, and a remainder has nowhere to go.
            if (result.RemainingBody.Length > 0 || result.RequiresAsync)
            {
                return false;
            }

            rewritten = result;
            return true;
        }
    }

    /// <summary>
    /// Translates the code-behind helper methods, to a fixed point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A helper is promoted only when its <b>entire</b> body translates - never a prefix. The
    /// prefix rule is what makes a partly-migrated *handler* honest, because the un-migrated
    /// remainder sits in a comment directly below the code that did translate. A helper has no
    /// such place: at its call site there would be nothing at all, so a half-translated
    /// <c>SetBusy</c> would look migrated while silently skipping half its work.
    /// </para>
    /// <para>
    /// The loop is a fixed point because a helper may call another. A call to a helper that is
    /// not promoted *yet* simply fails to translate, so that helper waits for the next round;
    /// when nothing new promotes, the remainder never will. Recursion needs no special guard:
    /// a helper is never in the promoted table while its own body is being translated, so a
    /// self-call - or a mutually recursive pair - refuses on its own. The same loop settles
    /// <c>async</c>, which propagates the same way: a helper that awaits a message box makes
    /// every caller await it in turn.
    /// </para>
    /// </remarks>
    private static List<PromotedHelperPlan> PlanHelpers(
        CodeBehindModel codeBehind,
        FormModel formModel,
        ViewNavigationContext navigation,
        IReadOnlySet<string> timerFields,
        IReadOnlySet<string> componentFields,
        IReadOnlySet<string> promotedFields,
        HandlerBodyRewriter rewriter,
        IReadOnlyDictionary<string, IReadOnlyList<ViewPropertyInfo>> viewProperties)
    {
        var candidates = codeBehind.HelperMembers
            .Where(m => m.Kind == HelperMemberKind.Method && m.Signature is not null)
            .Where(m => IsPlainDotNetSignature(m.Signature!))
            // A helper called `Tag` or `Refresh` would land beside the member the generated class
            // already inherits - CS0108 in the generated project, which this build cannot see.
            .Where(m => !ReservedMemberNames.IsReserved(m.Name))
            .ToList();

        var promoted = new List<PromotedHelperPlan>();
        var byName = new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal);

        // Bounded by the number of candidates: each round promotes at least one, or stops.
        for (var round = 0; round < candidates.Count; round++)
        {
            var promotedThisRound = false;

            foreach (var candidate in candidates.Where(c => !byName.ContainsKey(c.Name)).ToList())
            {
                var signature = candidate.Signature!;
                var rewrite = rewriter.RewriteForHelper(
                    signature, formModel, navigation, timerFields, componentFields, byName, promotedFields,
                    viewProperties);

                // An empty body is complete in the sense that matters: there is nothing left over.
                if (rewrite.RemainingBody.Length > 0)
                {
                    continue;
                }

                var isAsync = signature.IsAsync || rewrite.RequiresAsync;

                // A helper that turns async has to become `async Task` so its callers can await
                // it - `async void` is not awaitable. That works for a void helper and only for a
                // void one: a value-returning helper would become `Task<T>`, whose result is
                // usable only inside an expression, which is exactly where this converter refuses
                // to await. So it is left un-promoted rather than emitted uncallable.
                if (isAsync && signature.ReturnTypeText != "void")
                {
                    continue;
                }

                promoted.Add(new PromotedHelperPlan(
                    candidate.Name, signature.ReturnTypeText, signature.ParameterListText, rewrite, isAsync));
                byName[candidate.Name] = new HelperCallInfo(signature.ParameterNames.Count, isAsync);
                promotedThisRound = true;
            }

            if (!promotedThisRound)
            {
                break;
            }
        }

        return promoted;
    }

    /// <summary>
    /// Whether a helper's signature is expressible on the Avalonia side without translation:
    /// keyword types only, and a non-async method returns something or nothing but never a Task
    /// this converter would have to reason about.
    /// </summary>
    /// <remarks>
    /// A named type is refused for the same reason a named local type is - it could be a WinForms
    /// type whose Avalonia counterpart is a different type entirely, and nothing here can tell the
    /// difference without a semantic model.
    /// </remarks>
    private static bool IsPlainDotNetSignature(HelperMethodSignature signature)
    {
        if (!IsKeywordTypeText(signature.ReturnTypeText))
        {
            return false;
        }

        var parsed = SyntaxFactory.ParseParameterList(signature.ParameterListText);
        return !parsed.ContainsDiagnostics
            && parsed.Parameters.All(p => p.Type is PredefinedTypeSyntax);
    }

    private static bool IsKeywordTypeText(string typeText) =>
        SyntaxFactory.ParseTypeName(typeText) is PredefinedTypeSyntax;

    private static int CountParameters(string parameterListText) =>
        SyntaxFactory.ParseParameterList(parameterListText).Parameters.Count;

    /// <summary>
    /// The non-visual components that are plain .NET types, emitted as real fields so a handler
    /// body can name them - which before this it could not, on a component the conversion simply
    /// dropped.
    /// </summary>
    /// <remarks>
    /// Evidence-driven like everything else here: a component gets a field only if something
    /// actually uses it - a designer-wired event, or a handler body that mentions it. Declaring
    /// the rest would add fields (and NuGet references, and platform constraints) for objects the
    /// converted app never touches.
    /// </remarks>
    /// <param name="projectComponentNamespaces">
    /// The project's own Components whose source this run carries over, and the namespace they
    /// land in. They get a field on exactly the same terms as the in-box ones - the only
    /// difference is where the type comes from.
    /// </param>
    private static List<ComponentFieldPlan> PlanComponents(
        FormModel formModel,
        CodeBehindModel codeBehind,
        IReadOnlyDictionary<string, string> projectComponentNamespaces,
        List<string> warnings)
    {
        var referenced = codeBehind.HandlerMethods
            .SelectMany(h => h.ReferencedControlFields)
            .ToHashSet(StringComparer.Ordinal);

        var components = new List<ComponentFieldPlan>();

        foreach (var component in formModel.Controls.Values.OrderBy(c => c.FieldName, StringComparer.Ordinal))
        {
            var isProjectComponent = false;
            if (!ComponentFieldCatalog.TryGet(component.ClrTypeName, out var kind))
            {
                if (!projectComponentNamespaces.TryGetValue(component.ClrTypeName, out var ownNamespace))
                {
                    continue;
                }

                kind = new ComponentFieldKind(ownNamespace);
                isProjectComponent = true;
            }

            // A designer-wired event is subscribed when *something* can name its args type - the
            // catalog for an in-box component, the component's own declaration for a carried one.
            var subscriptions = component.Events
                .Where(e => e.HandlerMethodName is not null
                    && (ComponentFieldCatalog.TryGetEvent(component.ClrTypeName, e.EventName, out _)
                        || isProjectComponent))
                .Select(e => (e.EventName, e.HandlerMethodName!))
                .ToList();

            if (subscriptions.Count == 0 && !referenced.Contains(component.FieldName))
            {
                continue;
            }

            var initializers = new List<string>();
            foreach (var (propertyName, value) in component.Properties.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (TryFormatCSharpLiteral(value, out var literal))
                {
                    initializers.Add($"{propertyName} = {literal}");
                }
                else
                {
                    warnings.Add(
                        $"Component '{component.FieldName}' ({component.ClrTypeName}): designer property "
                        + $"'{propertyName}' was not reproduced - only literal values are, and this one is not.");
                }
            }

            if (kind.WindowsOnly)
            {
                warnings.Add(
                    $"Component '{component.FieldName}' ({component.ClrTypeName}) is Windows-only. The generated "
                    + "View declares it and compiles everywhere, with the platform analyser suppressed for that "
                    + "file - but these calls throw on Linux and macOS.");
            }

            components.Add(new ComponentFieldPlan(
                component.FieldName,
                component.ClrTypeName,
                kind.Namespace,
                kind.NuGetPackage,
                kind.WindowsOnly,
                initializers,
                subscriptions));
        }

        return components;
    }

    /// <summary>
    /// A designer value as C# source. Only literals: these components are unchanged .NET types,
    /// so any property of theirs would be valid - but a value this converter cannot resolve to a
    /// literal (a resource lookup, a computed expression, an enum whose type the evaluator does
    /// not track) has no faithful spelling, and guessing one would configure the component wrong.
    /// </summary>
    private static bool TryFormatCSharpLiteral(PropertyValue value, out string text)
    {
        text = value switch
        {
            PropertyValue.Literal { Value: string s } => SymbolDisplay.FormatLiteral(s, quote: true),
            PropertyValue.Literal { Value: bool b } => b ? "true" : "false",
            PropertyValue.Literal { Value: int i } => i.ToString(CultureInfo.InvariantCulture),
            PropertyValue.Literal { Value: double d } => d.ToString("R", CultureInfo.InvariantCulture),
            PropertyValue.Literal { Value: float f } => f.ToString("R", CultureInfo.InvariantCulture) + "f",
            _ => "",
        };

        return text.Length > 0;
    }

    /// <param name="inlinedFields">
    /// Dialogs a handler body already opens inline. Emitting a separate method for those too
    /// would leave a dead one behind, since nothing would ever call it.
    /// </param>
    private static List<FileDialogPlan> PlanFileDialogs(FormModel formModel, IReadOnlySet<string> inlinedFields) =>
    [
        .. formModel.Controls.Values
            .Where(c => FileDialogCatalog.TryGet(c.ClrTypeName, out _) && !inlinedFields.Contains(c.FieldName))
            .Select(c =>
            {
                FileDialogCatalog.TryGet(c.ClrTypeName, out var kind);
                return new FileDialogPlan(
                    c.FieldName,
                    $"Show{NamingConventions.Capitalize(c.FieldName)}Async",
                    kind.PickerMethodName,
                    kind.OptionsTypeName);
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

    /// <param name="effective">
    /// <paramref name="source"/>'s own requirements merged with those of every helper it calls -
    /// what has to hold for the handler *and* its helpers to live on a ViewModel together.
    /// </param>
    private bool TryPlanCommand(
        FormModel formModel,
        string handlerMethodName,
        List<EventSubscriptionPlan> subscriptions,
        HandlerMethodModel? source,
        HandlerMethodModel? effective,
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

        if (source is null || effective is null)
        {
            reason = "its body could not be found in the code-behind file, so there is no evidence it is bindable.";
            return false;
        }

        // From here on the question is about the handler *and* every helper it calls, because
        // they move together or not at all.
        source = effective;

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

        if (source.NeedsTopLevel)
        {
            reason = "it uses an API whose Avalonia replacement hangs off the TopLevel (a message box, the "
                + "clipboard) - which the View has and a ViewModel does not.";
            return false;
        }

        if (source.TouchedFormMembers.Count > 0)
        {
            reason = $"it drives the Form itself ({string.Join(", ", source.TouchedFormMembers)}).";
            return false;
        }

        // What survives the merge is the helpers whose bodies could not be analysed at all - a
        // recursive one, or a shape DescribeHelperMethod refuses. Everything else has already
        // been folded into the facts above.
        if (source.CalledHelperMethods.Count > 0)
        {
            reason = $"it calls the code-behind helper(s) {string.Join(", ", source.CalledHelperMethods)}, "
                + "whose bodies this converter cannot analyse.";
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
                if (!IsBindableFromAViewModel(control.ClrTypeName, member))
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

    /// <summary>
    /// Whether a member a body names can be expressed against a ViewModel property.
    /// </summary>
    /// <remarks>
    /// Two ways in. Most are bindable properties outright. The rest are the control *methods*
    /// whose Avalonia counterpart is itself a bindable property - <c>AppendText</c> is a write to
    /// <c>Text</c> wearing a method's clothes, and <c>Hide()</c> a write to <c>IsVisible</c>.
    /// <c>Focus()</c> is not, and correctly keeps its handler in code-behind.
    /// </remarks>
    private static bool IsBindableFromAViewModel(string controlTypeName, string memberName) =>
        TryResolveBindable(controlTypeName, memberName, out _, out _);

    /// <param name="winFormsPropertyName">
    /// The WinForms *property* the member ultimately names, which is what a designer value has to
    /// be looked up under - `AppendText` has no designer value, `Text` does.
    /// </param>
    private static bool TryResolveBindable(
        string controlTypeName,
        string memberName,
        out BindablePropertyCatalog.BindableProperty bindable,
        out string winFormsPropertyName)
    {
        if (BindablePropertyCatalog.TryGet(controlTypeName, memberName, out bindable))
        {
            winFormsPropertyName = memberName;
            return true;
        }

        if (ControlMethodCatalog.TryGet(controlTypeName, memberName, out var method))
        {
            return BindablePropertyCatalog.TryGetByAvaloniaName(
                controlTypeName, method.AvaloniaMemberName, out bindable, out winFormsPropertyName);
        }

        winFormsPropertyName = "";
        return false;
    }

    /// <summary>
    /// A handler's own requirements plus those of every helper it calls, transitively.
    /// </summary>
    /// <remarks>
    /// Promotion asks whether a body could live on a ViewModel. A body that calls a helper is
    /// really asking that of both, since the helper has to move with it - so the helper's facts
    /// are merged in as if it were inlined. What is left in <c>CalledHelperMethods</c> afterwards
    /// is exactly the helpers that could not be analysed, which still block. Cycle-guarded, so a
    /// recursive helper contributes once and then reports itself as unanalysable.
    /// </remarks>
    private static HandlerMethodModel Inlined(
        HandlerMethodModel source, CodeBehindModel codeBehind, out HashSet<string> reachedHelpers)
    {
        reachedHelpers = new HashSet<string>(StringComparer.Ordinal);

        var usesSender = source.UsesSender;
        var usesEventArgs = source.UsesEventArgs;
        var createsOtherForms = source.CreatesOtherForms;
        var needsTopLevel = source.NeedsTopLevel;
        var touchedFormMembers = new SortedSet<string>(source.TouchedFormMembers, StringComparer.Ordinal);
        var unanalysable = new SortedSet<string>(StringComparer.Ordinal);
        var accesses = source.ControlMemberAccesses.ToDictionary(
            kvp => kvp.Key, kvp => new List<string>(kvp.Value), StringComparer.Ordinal);

        var pending = new Queue<string>(source.CalledHelperMethods);
        while (pending.Count > 0)
        {
            var name = pending.Dequeue();
            if (!reachedHelpers.Add(name))
            {
                continue;
            }

            // A reserved name counts as unanalysable here on purpose: the helper cannot be emitted
            // on either target, so promoting its caller would leave the call with nothing to reach.
            if (ReservedMemberNames.IsReserved(name)
                || codeBehind.HelperMembers.FirstOrDefault(m => m.Name == name) is not { Facts: { } facts, Signature: not null })
            {
                unanalysable.Add(name);
                continue;
            }

            usesSender |= facts.UsesSender;
            usesEventArgs |= facts.UsesEventArgs;
            createsOtherForms |= facts.CreatesOtherForms;
            needsTopLevel |= facts.NeedsTopLevel;
            touchedFormMembers.UnionWith(facts.TouchedFormMembers);

            foreach (var (fieldName, members) in facts.ControlMemberAccesses)
            {
                if (!accesses.TryGetValue(fieldName, out var merged))
                {
                    merged = [];
                    accesses[fieldName] = merged;
                }

                merged.AddRange(members.Where(m => !merged.Contains(m, StringComparer.Ordinal)));
            }

            foreach (var nested in facts.CalledHelperMethods)
            {
                pending.Enqueue(nested);
            }
        }

        return source with
        {
            UsesSender = usesSender,
            UsesEventArgs = usesEventArgs,
            CreatesOtherForms = createsOtherForms,
            NeedsTopLevel = needsTopLevel,
            TouchedFormMembers = [.. touchedFormMembers],
            CalledHelperMethods = [.. unanalysable],
            ControlMemberAccesses = accesses.ToDictionary(
                kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value, StringComparer.Ordinal),
        };
    }

    /// <summary>
    /// Folds `okButton.Enabled = &lt;condition&gt;;` handlers into the promoted command's
    /// <c>CanExecute</c>, which is what that WinForms idiom means in MVVM.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately narrow: the handler's <em>whole body</em> must be that one assignment, it must
    /// ignore sender/EventArgs, and the condition must translate completely against ViewModel
    /// properties. Under those conditions the handler is fully redundant - the two-way bindings
    /// already push the values, and [NotifyCanExecuteChangedFor] re-evaluates the guard - so it is
    /// removed along with its subscription rather than left to fight the Command for control of
    /// the button's IsEnabled.
    /// </para>
    /// <para>
    /// A handler that does anything <em>else</em> as well keeps its imperative
    /// `IsEnabled = ...` write and no guard is derived: silently splitting such a body in two
    /// would be the kind of unprovable rewrite this converter avoids everywhere else.
    /// </para>
    /// </remarks>
    private static void DeriveCanExecuteGuards(
        FormModel formModel,
        CodeBehindModel codeBehind,
        List<CodeBehindHandlerPlan> codeBehindHandlers,
        List<ViewModelCommandPlan> viewModelCommands,
        List<BoundPropertyPlan> boundProperties,
        HashSet<(string Control, string Property)> seenBoundProperties,
        List<string> warnings)
    {
        foreach (var handler in codeBehindHandlers.ToList())
        {
            var source = codeBehind.FindHandler(handler.OriginalMethodName);
            if (source is null || !TryParseEnabledAssignment(handler, source, out var buttonField, out var conditionText))
            {
                continue;
            }

            var commandIndex = viewModelCommands.FindIndex(c => c.ControlFieldName == buttonField);
            if (commandIndex < 0)
            {
                continue;
            }

            // If something already binds this button's IsEnabled, a derived guard would be a
            // second, competing owner of the same property - leave the handler alone instead.
            if (boundProperties.Any(p => p.ControlFieldName == buttonField && p.AvaloniaPropertyName == "IsEnabled"))
            {
                continue;
            }

            // Bind what the condition reads, on a copy: if the rewrite then fails, the plan is
            // left exactly as it was rather than carrying orphaned properties.
            var candidateBound = new List<BoundPropertyPlan>(boundProperties);
            var candidateSeen = new HashSet<(string, string)>(seenBoundProperties);

            AddBoundProperties(formModel, source, candidateBound, candidateSeen);

            // The `Enabled` write itself must not become a binding: CanExecute is what drives the
            // button's IsEnabled now, and a second binding would fight it - permanently, since
            // the handler that used to set the value is about to be removed.
            candidateBound.RemoveAll(p => p.ControlFieldName == buttonField && p.AvaloniaPropertyName == "IsEnabled");
            candidateSeen.Remove((buttonField, "IsEnabled"));

            if (!HandlerBodyRewriter.TryRewriteConditionForViewModel(
                    conditionText, formModel, candidateBound, out var condition))
            {
                continue;
            }

            var command = viewModelCommands[commandIndex];

            // Exactly the properties the *translated condition* names, so no unrelated property
            // ends up raising this command's CanExecuteChanged.
            var readProperties = candidateBound
                .Where(p => Regex.IsMatch(condition, $@"\b{Regex.Escape(p.ViewModelPropertyName)}\b"))
                .Select(p => p.ViewModelPropertyName)
                .ToHashSet(StringComparer.Ordinal);

            boundProperties.Clear();
            boundProperties.AddRange(candidateBound.Select(p =>
                readProperties.Contains(p.ViewModelPropertyName)
                    ? new BoundPropertyPlan(
                        p.ControlFieldName,
                        p.AvaloniaPropertyName,
                        p.ViewModelPropertyName,
                        p.ClrTypeName,
                        p.DefaultValueSuffix,
                        [.. p.NotifiesCommands.Append(command.CommandPropertyName)])
                    : p));

            seenBoundProperties.UnionWith(candidateSeen);
            viewModelCommands[commandIndex] = command with { CanExecuteExpression = condition };
            codeBehindHandlers.Remove(handler);

            warnings.Add(
                $"handler '{handler.OriginalMethodName}' only kept '{buttonField}.Enabled' in sync, so it became " +
                $"'{command.CommandPropertyName}'s CanExecute guard - the handler and its subscription are gone.");
        }
    }

    /// <summary>
    /// Matches a handler whose entire body is `someButton.Enabled = &lt;condition&gt;;`, ignoring
    /// both parameters - the exact shape that is a guard rather than an action.
    /// </summary>
    private static bool TryParseEnabledAssignment(
        CodeBehindHandlerPlan handler, HandlerMethodModel source, out string buttonField, out string conditionText)
    {
        buttonField = "";
        conditionText = "";

        if (source is not { UsesSender: false, UsesEventArgs: false } || handler.Subscriptions.Count != 1)
        {
            return false;
        }

        if (SyntaxFactory.ParseStatement("{\n" + source.BodyText + "\n}") is not BlockSyntax block
            || block.ContainsDiagnostics)
        {
            return false;
        }

        if (block.Statements is not [ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }])
        {
            return false;
        }

        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            || assignment.Left is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Enabled" } target)
        {
            return false;
        }

        buttonField = target.Expression switch
        {
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name } => name,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => "",
        };

        conditionText = assignment.Right.ToString();
        return buttonField.Length > 0;
    }

    /// <summary>
    /// What a handler's own parameters mean, for the body rewrite. The EventArgs parameter keeps
    /// its original name in the generated method, and the raising control is only unambiguous
    /// when exactly one subscription feeds the handler.
    /// </summary>
    /// <summary>
    /// WinForms' one piece of designer-declared behaviour: a control with a <c>DialogResult</c>
    /// closes its form with that result when clicked, without any handler existing. Avalonia has
    /// no such thing, so the handler is synthesized here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The result becomes a <c>bool</c> - <c>Close(true)</c> for OK/Yes, <c>Close(false)</c>
    /// otherwise - which is what makes the caller side (<c>await dlg.ShowDialog&lt;bool&gt;(this)</c>)
    /// expressible. A dialog closed by its title bar yields <c>default(bool)</c>, i.e. false,
    /// which is exactly what WinForms reports for that case too.
    /// </para>
    /// <para>
    /// Only for a control with no Click handler of its own: when the designer wired one, the
    /// button's behaviour is whatever that handler does, and prepending a Close would change it.
    /// And only on a Form - a converted UserControl has no window to close.
    /// </para>
    /// </remarks>
    private IEnumerable<CodeBehindHandlerPlan> PlanDialogResultButtons(FormModel formModel, WinFormsArtifactKind artifactKind)
    {
        if (artifactKind == WinFormsArtifactKind.UserControl)
        {
            yield break;
        }

        foreach (var control in formModel.Controls.Values)
        {
            if (control.Events.Any(e => e.EventName == "Click")
                || !control.Properties.TryGetValue("DialogResult", out var value)
                || value is not PropertyValue.EnumMembers { MemberNames: [var resultName] }
                || _controlMappings.Map(control).Status != MappingStatus.Direct)
            {
                continue;
            }

            var mapping = _eventMappings.ResolveControlEvent(control.ClrTypeName, "Click");
            if (mapping.XamlAttributeName is null)
            {
                continue;
            }

            var methodName = $"{control.FieldName}_Click";
            var closesWithSuccess = DialogResultCatalog.ClosesWithSuccess(resultName);

            yield return new CodeBehindHandlerPlan(
                methodName,
                mapping.AvaloniaEventArgsTypeName,
                IsAsync: false,
                methodName,
                OriginalBody: "",
                [new EventSubscriptionPlan(control.FieldName, control.ClrTypeName, "Click", mapping, methodName)],
                RewrittenBody.Synthesized($"Close({(closesWithSuccess ? "true" : "false")});"));
        }
    }


    private static HandlerSignature SignatureOf(CodeBehindHandlerPlan handler, CodeBehindModel codeBehind)
    {
        var source = codeBehind.FindHandler(handler.OriginalMethodName);
        // A null field name is a Form-level subscription, which has no raising control - so it
        // contributes nothing here rather than a nameless entry.
        var controlFields = handler.Subscriptions
            .Select(s => s.ControlFieldName)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new HandlerSignature(
            source?.EventArgsParameterName,
            handler.EventArgsTypeName,
            controlFields);
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
                // A member is either a bindable property outright, or a control *method* whose
                // Avalonia counterpart is one - `AppendText` binds `Text`, `Hide()` binds
                // `IsVisible`. Both have to produce the same [ObservableProperty], or a helper
                // that only ever calls the method would leave nothing for its body to name.
                if (!TryResolveBindable(control.ClrTypeName, member, out var bindable, out var propertyName)
                    || !seen.Add((fieldName, bindable.AvaloniaPropertyName)))
                {
                    continue;
                }

                boundProperties.Add(new BoundPropertyPlan(
                    fieldName,
                    bindable.AvaloniaPropertyName,
                    $"{NamingConventions.Capitalize(fieldName)}{bindable.AvaloniaPropertyName}",
                    bindable.ClrTypeName,
                    DeriveInitializer(control, propertyName, bindable.DefaultValueSuffix)));
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
