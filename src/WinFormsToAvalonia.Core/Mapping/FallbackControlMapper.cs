using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Maps a WinForms control with no built-in Avalonia equivalent to one of the tool's
/// bundled fallback controls (the actual template files live in
/// WinFormsToAvalonia.FallbackControls and are resolved/copied by FallbackControlResolver
/// in Phase 8 - this mapper only records which template key applies).
/// </summary>
public sealed class FallbackControlMapper : IControlMapper
{
    private readonly string _fallbackTemplateKey;
    private readonly IReadOnlyList<(string WinFormsProperty, string AvaloniaAttribute, Func<PropertyValue, string?> Format)> _propertyMappings;

    public FallbackControlMapper(
        string winFormsTypeName,
        string fallbackTemplateKey,
        IReadOnlyList<(string WinFormsProperty, string AvaloniaAttribute, Func<PropertyValue, string?> Format)>? propertyMappings = null)
    {
        WinFormsTypeName = winFormsTypeName;
        _fallbackTemplateKey = fallbackTemplateKey;
        _propertyMappings = propertyMappings ?? [];
    }

    public string WinFormsTypeName { get; }

    public MappedControl Map(ControlModel control)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (winFormsProperty, avaloniaAttribute, format) in _propertyMappings)
        {
            if (control.Properties.TryGetValue(winFormsProperty, out var value) && format(value) is { } formatted)
            {
                attributes[avaloniaAttribute] = formatted;
            }
        }

        return new MappedControl(
            control.ClrTypeName,
            MappingStatus.Fallback,
            _fallbackTemplateKey,
            attributes,
            _fallbackTemplateKey,
            [$"'{control.ClrTypeName}' has no built-in Avalonia equivalent; using the bundled fallback control '{_fallbackTemplateKey}'."]);
    }
}
