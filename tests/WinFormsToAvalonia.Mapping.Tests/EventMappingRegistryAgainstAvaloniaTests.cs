using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// Every event the registry claims an element raises, and every args type it names, checked
/// against Avalonia.
/// </summary>
/// <remarks>
/// <para>
/// An event attribute the element does not define is an AVLN2000; an args type spelled wrong is a
/// CS0246 in the generated handler's signature. Both are failures of the <em>generated</em>
/// project, which is the only place they show up.
/// </para>
/// <para>
/// The generic entries are checked against Avalonia's <c>Control</c> on purpose. A generic entry
/// is a claim about <em>every</em> control - the emitter will put it on whatever element the
/// designer wired it to - so it is only true if the base type has it. An event that lives on one
/// specific type belongs in the per-control-type overrides, where it is checked against that
/// type; that is the difference between "MouseDown is always PointerPressed" and "a NumericUpDown
/// has a ValueChanged".
/// </para>
/// </remarks>
public class EventMappingRegistryAgainstAvaloniaTests
{
    [Theory]
    [MemberData(nameof(GenericControlEvents))]
    public void GenericEvent_ExistsOnEveryControl(string winFormsEventName, string avaloniaEventName)
    {
        var control = AvaloniaMetadata.FindElement("Control");
        Assert.True(control is not null, "Avalonia has no Control type - something is very wrong.");

        Assert.True(
            FindEventAnywhere(control!, avaloniaEventName) is not null,
            $"'{winFormsEventName}' is mapped generically to '{avaloniaEventName}', but Avalonia's "
            + "Control does not raise it - so the emitter would put it on whatever element the "
            + "designer wired it to, including ones that do not have it. It belongs in the "
            + "per-control-type overrides instead.");
    }

    [Theory]
    [MemberData(nameof(OverriddenControlEvents))]
    public void OverriddenEvent_ExistsOnThatControlsElement(
        string winFormsTypeName, string winFormsEventName, string avaloniaElementName, string avaloniaEventName)
    {
        var element = AvaloniaMetadata.FindElement(avaloniaElementName);
        Assert.True(element is not null, $"Avalonia has no '{avaloniaElementName}' element at all.");

        Assert.True(
            FindEventAnywhere(element!, avaloniaEventName) is not null,
            $"'{winFormsTypeName}.{winFormsEventName}' is emitted as '{avaloniaEventName}' on a "
            + $"'{avaloniaElementName}', which does not raise it.");
    }

    /// <summary>
    /// The args type ends up in the generated handler's signature, so it has to be the type the
    /// event really carries - not merely a type that exists. Avalonia renames these between
    /// majors, and the result is a CS0246 in the generated project and nowhere else.
    /// </summary>
    [Theory]
    [MemberData(nameof(ArgsTypes))]
    public void EventArgsType_IsWhatTheEventCarries(
        string ownerElementName, string avaloniaEventName, string claimedArgsTypeName, string source)
    {
        var owner = AvaloniaMetadata.FindElement(ownerElementName);
        Assert.True(owner is not null, $"Avalonia has no '{ownerElementName}' element at all.");

        var actual = ArgsTypeOf(owner!, avaloniaEventName);
        Assert.True(
            actual is not null,
            $"'{ownerElementName}' does not raise '{avaloniaEventName}' - covered by the event tests.");

        Assert.True(
            string.Equals(actual, claimedArgsTypeName, StringComparison.Ordinal),
            $"{source} declares its handler with '{claimedArgsTypeName}', but "
            + $"'{ownerElementName}.{avaloniaEventName}' carries a '{actual}'.");
    }

    /// <summary>Form-level events land on the generated Window, not on a control.</summary>
    [Theory]
    [MemberData(nameof(FormEvents))]
    public void FormEvent_ExistsOnWindow(string winFormsEventName, string avaloniaEventName)
    {
        var window = AvaloniaMetadata.FindElement("Window");
        Assert.True(window is not null, "Avalonia has no Window type - something is very wrong.");

        Assert.True(
            FindEventAnywhere(window!, avaloniaEventName) is not null,
            $"Form.{winFormsEventName} is emitted as '{avaloniaEventName}' on a Window, which does "
            + "not raise it.");
    }

    public static TheoryData<string, string> GenericControlEvents()
    {
        var data = new TheoryData<string, string>();
        var registry = new EventMappingRegistry();

        foreach (var (declaredType, eventName) in Probable().Where(p => p.ControlTypeName is null))
        {
            _ = declaredType;
            var mapping = registry.ResolveControlEvent("Panel", eventName);

            // An attached event is declared by its owner, not by Control - covered by the
            // owner-qualified name in ArgsTypeNames and by the emitter's own attached handling.
            if (mapping.AvaloniaEventName is not { } avaloniaName || mapping.SubscribeInCode
                || mapping.AttachedOwnerTypeName is not null)
            {
                continue;
            }

            data.Add(eventName, avaloniaName);
        }

        return data;
    }

    public static TheoryData<string, string, string, string> OverriddenControlEvents()
    {
        var data = new TheoryData<string, string, string, string>();
        var mappingRegistry = new ControlMappingRegistry();
        var eventRegistry = new EventMappingRegistry();

        foreach (var (typeName, eventName) in Probable().Where(p => p.ControlTypeName is not null))
        {
            var mapping = eventRegistry.ResolveControlEvent(typeName!, eventName);
            if (mapping.AvaloniaEventName is not { } avaloniaName || mapping.SubscribeInCode)
            {
                continue;
            }

            var mapped = mappingRegistry.Map(new ControlModel { FieldName = "field1", ClrTypeName = typeName! });
            if (mapped.Status != MappingStatus.Direct || mapped.AvaloniaElementName is not { } element)
            {
                continue;
            }

            data.Add(typeName!, eventName, element, avaloniaName);
        }

        return data;
    }

    /// <summary>
    /// One row per (owner element, Avalonia event) the registry can produce, with the args type it
    /// claims. The owner is where the event is declared: the element for an ordinary one, the
    /// attached class for a <c>DragDrop.Drop</c>, and Window for a Form-level one.
    /// </summary>
    public static TheoryData<string, string, string, string> ArgsTypes()
    {
        var data = new TheoryData<string, string, string, string>();
        var seen = new HashSet<(string, string)>();
        var mappingRegistry = new ControlMappingRegistry();
        var eventRegistry = new EventMappingRegistry();

        foreach (var (typeName, eventName) in Probable())
        {
            var mapping = eventRegistry.ResolveControlEvent(typeName ?? "Panel", eventName);
            var owner = mapping.AttachedOwnerTypeName ?? ElementOf(mappingRegistry, typeName);
            Add(owner, mapping, $"{typeName ?? "Control"}.{eventName}");
        }

        foreach (var eventName in EventMappingRegistry.FormEventNames)
        {
            Add("Window", eventRegistry.ResolveFormEvent(eventName), $"Form.{eventName}");
        }

        return data;

        void Add(string? owner, EventMapping mapping, string source)
        {
            // An unmapped event has no handler to sign; a component event carries a plain .NET
            // args type that survived the conversion untouched, so Avalonia has no opinion on it.
            if (owner is null || mapping.AvaloniaEventName is not { } avaloniaName || mapping.SubscribeInCode)
            {
                return;
            }

            if (seen.Add((owner, avaloniaName)))
            {
                data.Add(owner, avaloniaName, mapping.AvaloniaEventArgsTypeName, source);
            }
        }
    }

    /// <summary>The element a WinForms type maps to, or Control for a generic entry.</summary>
    private static string? ElementOf(ControlMappingRegistry registry, string? winFormsTypeName)
    {
        if (winFormsTypeName is null)
        {
            return "Control";
        }

        var mapped = registry.Map(new ControlModel { FieldName = "field1", ClrTypeName = winFormsTypeName });
        return mapped.Status == MappingStatus.Direct ? mapped.AvaloniaElementName : null;
    }

    /// <summary>
    /// The args type an event actually carries: from the delegate's second parameter for a CLR
    /// event, or from <c>RoutedEvent&lt;TArgs&gt;</c> for an attached one.
    /// </summary>
    private static string? ArgsTypeOf(Type owner, string eventName)
    {
        if (AvaloniaMetadata.FindEvent(owner, eventName)?.EventHandlerType is { } handler)
        {
            var invoke = AvaloniaMetadata.FindMethod(handler, "Invoke", 2);
            if (invoke is not null)
            {
                return invoke.GetParameters()[1].ParameterType.Name;
            }

            // EventHandler<T> reaches its args through the generic argument instead.
            if (handler.IsGenericType)
            {
                return handler.GetGenericArguments()[0].Name;
            }
        }

        var field = AvaloniaMetadata.FindField(owner, eventName + "Event");
        if (field?.FieldType is { IsGenericType: true } routed)
        {
            return routed.GetGenericArguments()[0].Name;
        }

        return null;
    }

    public static TheoryData<string, string> FormEvents()
    {
        var data = new TheoryData<string, string>();
        var registry = new EventMappingRegistry();

        foreach (var eventName in EventMappingRegistry.FormEventNames.Order(StringComparer.Ordinal))
        {
            if (registry.ResolveFormEvent(eventName).AvaloniaEventName is { } avaloniaName)
            {
                data.Add(eventName, avaloniaName);
            }
        }

        return data;
    }

    private static IEnumerable<(string? ControlTypeName, string EventName)> Probable() =>
        EventMappingRegistry.ProbableControlEvents
            .OrderBy(p => p.ControlTypeName ?? "", StringComparer.Ordinal)
            .ThenBy(p => p.EventName, StringComparer.Ordinal);

    /// <summary>
    /// An event as XAML can reach it: a CLR event, or - for an attached one like
    /// <c>DragDrop.Drop</c> - the static <c>RoutedEvent</c> field that declares it. Avalonia
    /// exposes attached events only as fields, so looking for a CLR event alone reports every one
    /// of them as missing.
    /// </summary>
    private static object? FindEventAnywhere(Type type, string eventName) =>
        AvaloniaMetadata.FindEvent(type, eventName)
        ?? (object?)AvaloniaMetadata.FindProperty(type, eventName)
        ?? AvaloniaMetadata.FindField(type, eventName + "Event");
}
