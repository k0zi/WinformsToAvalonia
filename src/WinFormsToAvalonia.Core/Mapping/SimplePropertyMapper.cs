using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Table-driven <see cref="IControlMapper"/> for WinForms controls that have a direct
/// Avalonia equivalent: a fixed target element name plus a declarative list of (WinForms
/// property -> Avalonia attribute) transforms, an optional list of fixed attributes that
/// aren't sourced from any WinForms property (e.g. HScrollBar/VScrollBar's Orientation,
/// which is implied by the type itself, not a property on it), an optional child-wrapper
/// element chain (for targets like TabItem that can't host multiple direct children, or
/// ToolStripDropDownButton, whose items live two levels down in Button.Flyout > MenuFlyout),
/// and an optional required NuGet package (for targets like DataGrid that ship outside the
/// core Avalonia packages). Controls that need real per-instance logic (e.g. SplitContainer's
/// Panel1/Panel2) still get their own bespoke <see cref="IControlMapper"/> implementation -
/// see docs/known-limitations.md for what isn't covered yet.
/// </summary>
public sealed class SimplePropertyMapper : IControlMapper
{
    private readonly string _avaloniaElementName;
    private readonly IReadOnlyList<(string WinFormsProperty, string AvaloniaAttribute, Func<PropertyValue, string?> Format)> _propertyMappings;
    private readonly IReadOnlyList<(string AvaloniaAttribute, string Value)> _fixedAttributes;
    private readonly IReadOnlyList<string>? _childWrapperElementNames;
    private readonly string? _requiredNuGetPackage;
    private readonly bool _supportsName;

    public SimplePropertyMapper(
        string winFormsTypeName,
        string avaloniaElementName,
        IReadOnlyList<(string WinFormsProperty, string AvaloniaAttribute, Func<PropertyValue, string?> Format)> propertyMappings,
        IReadOnlyList<string>? childWrapperElementNames = null,
        string? requiredNuGetPackage = null,
        IReadOnlyList<(string AvaloniaAttribute, string Value)>? fixedAttributes = null,
        bool supportsName = true)
    {
        WinFormsTypeName = winFormsTypeName;
        _avaloniaElementName = avaloniaElementName;
        _propertyMappings = propertyMappings;
        _childWrapperElementNames = childWrapperElementNames;
        _requiredNuGetPackage = requiredNuGetPackage;
        _fixedAttributes = fixedAttributes ?? [];
        _supportsName = supportsName;
    }

    public string WinFormsTypeName { get; }

    /// <summary>The Avalonia element this mapper emits.</summary>
    /// <remarks>
    /// Exposed - like <see cref="DeclaredAttributes"/> - so the mapping tables can be checked
    /// against Avalonia's real API: this converter never references Avalonia, so nothing else in
    /// this repo can tell whether the name and the attributes below actually exist. See
    /// WinFormsToAvalonia.Mapping.Tests.
    /// </remarks>
    public string AvaloniaElementName => _avaloniaElementName;

    /// <summary>
    /// Every Avalonia attribute this mapper can emit, with the WinForms property each comes from
    /// (null for a fixed attribute implied by the type itself).
    /// </summary>
    public IReadOnlyList<(string? WinFormsProperty, string AvaloniaAttribute)> DeclaredAttributes =>
    [
        .. _fixedAttributes.Select(a => ((string?)null, a.AvaloniaAttribute)),
        .. _propertyMappings.Select(m => ((string?)m.WinFormsProperty, m.AvaloniaAttribute)),
    ];

    public MappedControl Map(ControlModel control)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (avaloniaAttribute, value) in _fixedAttributes)
        {
            attributes[avaloniaAttribute] = value;
        }

        foreach (var (winFormsProperty, avaloniaAttribute, format) in _propertyMappings)
        {
            if (control.Properties.TryGetValue(winFormsProperty, out var value)
                && format(value) is { } formatted)
            {
                attributes[avaloniaAttribute] = formatted;
            }
        }

        return new MappedControl(
            control.ClrTypeName, MappingStatus.Direct, _avaloniaElementName, attributes, null, [],
            _childWrapperElementNames, _requiredNuGetPackage, _supportsName);
    }
}
