namespace WinFormsToAvalonia.Core.Mapping.Vendors;

/// <summary>
/// The vendor controls and base classes this converter knows without being told - DevExpress and
/// Telerik - and the lookup a designer's written type name is resolved through.
/// </summary>
/// <remarks>
/// <para>
/// Built on the same idea as a <c>--mappings</c> <c>"as"</c> entry: a vendor control is converted
/// as a WinForms control the registry already maps, so every table downstream - mappers, events,
/// bindable properties, handler bodies - answers for it unchanged, and no new Avalonia element is
/// ever introduced here. What it adds over a mappings file is everything that file cannot say: the
/// vendor's nested designer vocabulary (<c>Properties.ReadOnly</c>), its collections, and the
/// structures <c>VendorDesignerNormalizer</c> rebuilds (a GridView's columns, a bar's links).
/// </para>
/// <para>
/// Deliberately <em>not</em> in <c>ControlMappingRegistry</c>: that registry is keyed by simple
/// WinForms names, documented in <c>docs/Controls.md</c> row by row, and refuses a duplicate - and a
/// project's own UserControl may well be called <c>GridView</c>.
/// </para>
/// </remarks>
public sealed class VendorCatalog
{
    private readonly Dictionary<string, VendorControl> _byFullName;
    private readonly ILookup<string, VendorControl> _bySimpleName;
    private readonly Dictionary<string, VendorBaseType> _baseTypes;

    public VendorCatalog(IEnumerable<VendorControl> controls, IEnumerable<VendorBaseType> baseTypes)
    {
        _byFullName = controls.ToDictionary(c => c.FullName, StringComparer.Ordinal);
        _bySimpleName = _byFullName.Values.ToLookup(c => c.SimpleName, StringComparer.Ordinal);
        _baseTypes = baseTypes.ToDictionary(b => b.FullName, StringComparer.Ordinal);
    }

    /// <summary>DevExpress and Telerik.</summary>
    public static VendorCatalog BuiltIn { get; } = new(
        [.. DevExpressPack.Controls, .. TelerikPack.Controls],
        [.. DevExpressPack.BaseTypes, .. TelerikPack.BaseTypes]);

    /// <summary>Knows nothing - what a walk without vendor support is given.</summary>
    public static VendorCatalog Empty { get; } = new([], []);

    public IEnumerable<VendorControl> Controls => _byFullName.Values;

    public IEnumerable<VendorBaseType> BaseTypes => _baseTypes.Values;

    public bool IsEmpty => _byFullName.Count == 0 && _baseTypes.Count == 0;

    /// <summary>
    /// This catalog minus every type the user mapped themselves: a <c>--mappings</c> entry for
    /// <c>TextEdit</c>, of either kind, replaces the built-in one.
    /// </summary>
    public VendorCatalog Without(IEnumerable<string> simpleNames)
    {
        var excluded = simpleNames.ToHashSet(StringComparer.Ordinal);
        return excluded.Count == 0
            ? this
            : new VendorCatalog(
                _byFullName.Values.Where(c => !excluded.Contains(c.SimpleName)),
                _baseTypes.Values.Where(b => !excluded.Contains(b.SimpleName)));
    }

    public bool TryGet(string fullName, out VendorControl control) =>
        _byFullName.TryGetValue(fullName, out control!);

    /// <summary>
    /// The entry a designer's <c>new T(...)</c> names. A qualified name has to match exactly; an
    /// unqualified one (the file has a <c>using</c>) matches the entry in a namespace the file
    /// imports, or the only entry of that name - never a guess between two vendors' types.
    /// </summary>
    public VendorControl? Resolve(string writtenTypeName, IReadOnlyCollection<string> usings)
    {
        var name = writtenTypeName.StartsWith("global::", StringComparison.Ordinal)
            ? writtenTypeName["global::".Length..]
            : writtenTypeName;

        if (name.Contains('.'))
        {
            return _byFullName.GetValueOrDefault(name);
        }

        var candidates = _bySimpleName[name].ToList();
        var imported = candidates.Where(c => usings.Contains(c.Namespace)).ToList();
        return imported.Count == 1 ? imported[0]
            : candidates.Count == 1 ? candidates[0]
            : null;
    }

    /// <summary>The entry a project class's simple base-list name stands for, when only one does.</summary>
    public VendorControl? ResolveSimpleName(string simpleName) =>
        _bySimpleName[simpleName].ToList() is [var only] ? only : null;

    /// <summary>A vendor Form or UserControl base class, by the simple name a base list gives.</summary>
    public VendorBaseType? BaseTypeNamed(string simpleName) =>
        _baseTypes.Values.FirstOrDefault(b => string.Equals(b.SimpleName, simpleName, StringComparison.Ordinal));

    /// <summary>
    /// The base types <c>DesignerFileLocator.Locate</c> takes: the user's first (from a
    /// <c>--mappings</c> file), then this catalog's. The project's own classes still win over
    /// both - the locator only fills in what syntax could not see.
    /// </summary>
    public IReadOnlyDictionary<string, string> ExternalBaseTypes(IReadOnlyDictionary<string, UserMapping> userMappings)
    {
        var merged = new Dictionary<string, string>(UserMappings.BaseTypes(userMappings), StringComparer.Ordinal);
        foreach (var baseType in _baseTypes.Values.OrderBy(b => b.FullName, StringComparer.Ordinal))
        {
            merged.TryAdd(baseType.SimpleName, baseType.As);
        }

        return merged;
    }
}
