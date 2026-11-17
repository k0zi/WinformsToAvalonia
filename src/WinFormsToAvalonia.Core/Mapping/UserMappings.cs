using System.Text.Json;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// One control type the user mapped in a <c>--mappings</c> file - a third-party control this
/// converter cannot know about.
/// </summary>
/// <param name="SourceType">The type as the designer writes it, without namespace - <c>TextEdit</c>.</param>
/// <param name="As">
/// A WinForms control to convert it as (<c>TextBox</c>): the control is re-typed before anything
/// downstream looks at it, so every table - mapper, events, bindable properties, handler bodies -
/// answers for it exactly as for the real one. The strong form, and the recommended one.
/// </param>
/// <param name="Element">
/// Or an Avalonia element to emit for it directly, with <paramref name="Properties"/> naming which
/// designer property becomes which attribute. Nothing else about the control is known, so nothing
/// else is translated.
/// </param>
/// <param name="Properties">
/// With <paramref name="As"/>: the vendor's property names renamed to the WinForms control's
/// (<c>EditValue</c> → <c>Text</c>). With <paramref name="Element"/>: designer property → attribute.
/// </param>
/// <param name="Events">With <paramref name="As"/>: the vendor's event names renamed to the WinForms control's.</param>
public sealed record UserMapping(
    string SourceType,
    string? As,
    string? Element,
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyDictionary<string, string> Events);

/// <summary>A <c>--mappings</c> file that cannot be used, with every reason why.</summary>
public sealed class UserMappingsException(string filePath, IReadOnlyList<string> errors)
    : Exception($"'{filePath}' is not a usable mappings file: {string.Join(" ", errors)}")
{
    public string FilePath { get; } = filePath;

    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>
/// Reads a <c>--mappings</c> JSON file:
/// <code>
/// {
///   "controls": {
///     "TextEdit":     { "as": "TextBox", "properties": { "EditValue": "Text" }, "events": { "EditValueChanged": "TextChanged" } },
///     "GaugeControl": { "element": "ProgressBar", "properties": { "Value": "Value", "Maximum": "Maximum" } }
///   }
/// }
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// A third-party control (DevExpress, Telerik, Infragistics, ...) used to have exactly one outcome:
/// "No mapping registered", no element, a warning. The user usually knows what it is - a text box
/// with a different name - and this is where they can say so.
/// </para>
/// <para>
/// Refuses the whole file on any error rather than skipping an entry: a mapping that silently did
/// not apply is the failure this exists to remove. An <c>as</c> target is checked against the
/// built-in mappers; an <c>element</c> cannot be checked at all - the tool never references
/// Avalonia - so a misspelt one is a build error in the generated project, which the docs say.
/// </para>
/// </remarks>
public static class UserMappings
{
    /// <summary>
    /// The <c>as</c> targets that are base classes rather than controls: a vendor's own <c>Form</c>
    /// or <c>UserControl</c> (<c>MetroForm</c>, <c>C1RibbonForm</c>, <c>XtraForm</c>), which is
    /// what decides whether a designer class is converted at all.
    /// </summary>
    public static IReadOnlySet<string> BaseTypeTargets { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "Form", "UserControl",
    };

    /// <summary>The base-type mappings, as <see cref="Parsing.DesignerFileLocator"/> takes them.</summary>
    public static IReadOnlyDictionary<string, string> BaseTypes(IReadOnlyDictionary<string, UserMapping> mappings) =>
        mappings.Values
            .Where(m => m.As is { } target && BaseTypeTargets.Contains(target))
            .ToDictionary(m => m.SourceType, m => m.As!, StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, UserMapping> Load(string filePath, ControlMappingRegistry builtIn)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(filePath), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new UserMappingsException(filePath, [e.Message]);
        }

        using (document)
        {
            var errors = new List<string>();
            var mappings = new Dictionary<string, UserMapping>(StringComparer.Ordinal);

            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("controls", out var controls)
                || controls.ValueKind != JsonValueKind.Object)
            {
                throw new UserMappingsException(filePath, ["The root must be an object with a \"controls\" object."]);
            }

            foreach (var control in controls.EnumerateObject())
            {
                var type = control.Name;
                if (control.Value.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"'{type}' must be an object.");
                    continue;
                }

                string? Text(string name) =>
                    control.Value.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

                IReadOnlyDictionary<string, string> Map(string name)
                {
                    var map = new Dictionary<string, string>(StringComparer.Ordinal);
                    if (!control.Value.TryGetProperty(name, out var v))
                    {
                        return map;
                    }

                    if (v.ValueKind != JsonValueKind.Object)
                    {
                        errors.Add($"'{type}.{name}' must be an object of name-to-name strings.");
                        return map;
                    }

                    foreach (var entry in v.EnumerateObject())
                    {
                        if (entry.Value.ValueKind == JsonValueKind.String && entry.Value.GetString() is { Length: > 0 } target)
                        {
                            map[entry.Name] = target;
                        }
                        else
                        {
                            errors.Add($"'{type}.{name}.{entry.Name}' must be a non-empty string.");
                        }
                    }

                    return map;
                }

                foreach (var unknown in control.Value.EnumerateObject()
                             .Select(p => p.Name)
                             .Where(n => n is not ("as" or "element" or "properties" or "events")))
                {
                    errors.Add($"'{type}' has an unknown key '{unknown}' (expected as, element, properties, events).");
                }

                var asType = Text("as");
                var element = Text("element");
                var properties = Map("properties");
                var events = Map("events");

                if ((asType is null) == (element is null))
                {
                    errors.Add($"'{type}' needs exactly one of \"as\" (a WinForms control) or \"element\" (an Avalonia element).");
                    continue;
                }

                if (builtIn.Mappers.ContainsKey(type))
                {
                    errors.Add($"'{type}' is a control this converter already maps; a mappings file only adds types.");
                    continue;
                }

                if (asType is not null && BaseTypeTargets.Contains(asType) && (properties.Count > 0 || events.Count > 0))
                {
                    errors.Add($"'{type}': a base-class mapping (\"as\": \"{asType}\") takes no properties or events - "
                        + "the derived Form's own designer properties are read as the Form's.");
                    continue;
                }

                if (asType is not null && !BaseTypeTargets.Contains(asType) && !builtIn.Mappers.ContainsKey(asType))
                {
                    errors.Add($"'{type}': \"as\": \"{asType}\" is not a WinForms control this converter maps (see list-mappings).");
                    continue;
                }

                if (element is not null && !IsXmlName(element))
                {
                    errors.Add($"'{type}': \"element\": \"{element}\" is not an element name.");
                    continue;
                }

                if (element is not null && events.Count > 0)
                {
                    errors.Add($"'{type}': \"events\" needs \"as\" - an element mapping knows nothing about the control's events.");
                    continue;
                }

                mappings[type] = new UserMapping(type, asType, element, properties, events);
            }

            if (errors.Count > 0)
            {
                throw new UserMappingsException(filePath, errors);
            }

            return mappings;
        }
    }

    /// <summary>The mapper an <c>element</c> mapping becomes.</summary>
    public static IControlMapper ToMapper(UserMapping mapping) =>
        new SimplePropertyMapper(
            mapping.SourceType,
            mapping.Element!,
            [.. mapping.Properties.Select(p => (p.Key, p.Value, (Func<PropertyValue, string?>)AsAttributeValue))]);

    /// <summary>
    /// A designer value as an attribute, by its own shape - the user named the attribute, so its
    /// type is not known here. Anything that is not a plain value is declined, which the mapper reports.
    /// </summary>
    public static string? AsAttributeValue(PropertyValue value) => value switch
    {
        PropertyValue.Literal { Value: bool } => PropertyValueFormatters.AsBool(value),
        PropertyValue.Literal { Value: string } => PropertyValueFormatters.AsText(value),
        PropertyValue.Literal => PropertyValueFormatters.AsNumber(value),
        PropertyValue.ColorValue => PropertyValueFormatters.AsBrush(value),
        PropertyValue.EnumMembers { MemberNames.Count: 1 } => PropertyValueFormatters.AsEnumMember(value),
        _ => null,
    };

    /// <summary>
    /// Applies an <c>as</c> mapping to one control: renamed properties and events, then the
    /// WinForms type. The written type is kept, as for the project's own subclasses.
    /// </summary>
    public static void Retype(ControlModel control, UserMapping mapping)
    {
        foreach (var (from, to) in mapping.Properties)
        {
            if (control.Properties.Remove(from, out var value))
            {
                control.Properties[to] = value;
            }
        }

        for (var i = 0; i < control.Events.Count; i++)
        {
            if (mapping.Events.TryGetValue(control.Events[i].EventName, out var renamed))
            {
                control.Events[i] = control.Events[i] with { EventName = renamed };
            }
        }

        control.DeclaredClrTypeName = control.ClrTypeName;
        control.ClrTypeName = mapping.As!;
    }

    private static bool IsXmlName(string name) =>
        name.Length > 0
        && (char.IsLetter(name[0]) || name[0] == '_')
        && name.All(c => char.IsLetterOrDigit(c) || c is '_' or '.');
}
