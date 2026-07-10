using Converter.Plugin.Abstractions;

namespace Converter.Generator.Mapping;

/// <summary>
/// Resolves plugin-provided control/property/event mappings for one form's control tree in
/// a single async pre-pass, run once before generation starts. Generation itself
/// (AxamlGenerator/ViewModelGenerator/StyleGenerator) stays fully synchronous and consumes
/// the resulting <see cref="PluginMappingOverrides"/> as a plain, already-computed,
/// read-only lookup - this is what lets plugin interfaces be async (they may do I/O)
/// without forcing the generators to become async themselves, which would be a large,
/// invasive change breaking every existing synchronous test call site for a level of
/// async-ness the generators otherwise have no need for.
/// </summary>
public sealed class MappingResolver
{
    /// <summary>Zero plugins configured - ResolveForFormAsync short-circuits with no tree walk at all.</summary>
    public static readonly MappingResolver Empty = new([], [], []);

    private readonly IReadOnlyList<IControlMapper> _controlMappers;
    private readonly IReadOnlyList<IPropertyTranslator> _propertyTranslators;
    private readonly IReadOnlyList<IEventMapper> _eventMappers;

    public MappingResolver(
        IEnumerable<IControlMapper> controlMappers,
        IEnumerable<IPropertyTranslator> propertyTranslators,
        IEnumerable<IEventMapper> eventMappers)
    {
        _controlMappers = controlMappers.OrderByDescending(m => m.Priority).ToList();
        _propertyTranslators = propertyTranslators.OrderByDescending(m => m.Priority).ToList();
        _eventMappers = eventMappers.OrderByDescending(m => m.Priority).ToList();
    }

    public bool HasPlugins => _controlMappers.Count > 0 || _propertyTranslators.Count > 0 || _eventMappers.Count > 0;

    /// <summary>
    /// Walks the whole control tree for one form once, asynchronously, resolving the first
    /// (highest-priority) plugin match for each control/property/event. Nodes/properties/
    /// events with no plugin match simply have no entry in the result - the generators fall
    /// back to the built-in static registry for those, exactly as they did before plugins
    /// existed.
    /// </summary>
    public async Task<PluginMappingOverrides> ResolveForFormAsync(
        ControlNode root, MappingContext context, CancellationToken cancellationToken = default)
    {
        if (!HasPlugins)
        {
            return PluginMappingOverrides.Empty;
        }

        var overrides = new PluginMappingOverrides();
        await ResolveRecursiveAsync(root, context, overrides, cancellationToken);
        return overrides;
    }

    private async Task ResolveRecursiveAsync(
        ControlNode control, MappingContext context, PluginMappingOverrides overrides, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var controlMapper = _controlMappers.FirstOrDefault(m => m.CanMap(control));
        if (controlMapper != null)
        {
            overrides.ControlMappings[control] = await controlMapper.MapAsync(control, context);
        }

        if (_propertyTranslators.Count > 0)
        {
            foreach (var (propertyName, propertyValue) in control.Properties)
            {
                var translator = _propertyTranslators.FirstOrDefault(
                    t => t.CanTranslate(control.ControlType, propertyName, propertyValue.Value));

                if (translator == null)
                {
                    continue;
                }

                var translationContext = new TranslationContext { Control = control, Options = context.Options, Services = context.Services };
                overrides.PropertyTranslations[(control, propertyName)] =
                    await translator.TranslateAsync(control.ControlType, propertyName, propertyValue.Value, translationContext);
            }
        }

        if (_eventMappers.Count > 0)
        {
            foreach (var eventName in control.EventHandlers.Keys)
            {
                var eventMapper = _eventMappers.FirstOrDefault(m => m.CanMap(eventName, control));
                if (eventMapper == null)
                {
                    continue;
                }

                overrides.EventMappings[(control, eventName)] = await eventMapper.MapAsync(eventName, control, context);
            }
        }

        foreach (var child in control.Children)
        {
            await ResolveRecursiveAsync(child, context, overrides, cancellationToken);
        }
    }
}

/// <summary>
/// Plugin mapping results for one form's control tree, keyed by reference identity on
/// <see cref="ControlNode"/> - safe because the exact same ControlNode instances flow
/// unmodified from parsing straight through to generation for a given form (no copying
/// happens in between).
/// </summary>
public sealed class PluginMappingOverrides
{
    public static readonly PluginMappingOverrides Empty = new();

    public Dictionary<ControlNode, ControlMappingResult> ControlMappings { get; } = new(ReferenceEqualityComparer.Instance);
    public Dictionary<(ControlNode Control, string PropertyName), PropertyTranslationResult> PropertyTranslations { get; } = [];
    public Dictionary<(ControlNode Control, string EventName), EventMappingResult> EventMappings { get; } = [];
}
