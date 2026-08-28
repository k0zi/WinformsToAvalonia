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

    /// <summary>The bundled template this mapper emits - the fallback counterpart of
    /// <see cref="SimplePropertyMapper.AvaloniaElementName"/>, exposed for the same reason.</summary>
    public string FallbackTemplateKey => _fallbackTemplateKey;

    /// <summary>Every attribute this mapper can emit, with the WinForms property it comes from.</summary>
    public IReadOnlyList<(string WinFormsProperty, string AvaloniaAttribute)> DeclaredAttributes =>
        [.. _propertyMappings.Select(m => (m.WinFormsProperty, m.AvaloniaAttribute))];

    public MappedControl Map(ControlModel control)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (winFormsProperty, avaloniaAttribute, format) in _propertyMappings)
        {
            if (control.Properties.TryGetValue(winFormsProperty, out var value) && format(value) is { } formatted)
            {
                // Same as SimplePropertyMapper: a caption's mnemonic marker is notation, not text.
                attributes[avaloniaAttribute] = winFormsProperty == "Text"
                    ? WinFormsMnemonics.Convert(formatted, WinFormsMnemonicCatalog.For(control.ClrTypeName))
                    : formatted;
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
