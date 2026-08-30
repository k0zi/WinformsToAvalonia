using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// Every element and attribute the control mappers emit, checked against Avalonia itself.
/// </summary>
/// <remarks>
/// An element name Avalonia does not have, or an attribute that element does not define, is an
/// AVLN2000 in the <em>generated</em> project - which builds long after this repo's own does, and
/// only on someone else's machine. This is the check that moves that failure here.
/// </remarks>
public class ControlMapperTests
{
    [Theory]
    [MemberData(nameof(SimpleMappers))]
    public void Mapper_EmitsAnElementAvaloniaHas(string winFormsTypeName, string avaloniaElementName)
    {
        Assert.True(
            AvaloniaMetadata.FindElement(avaloniaElementName) is not null,
            $"'{winFormsTypeName}' maps to '{avaloniaElementName}', which no loaded Avalonia assembly "
            + $"defines. Assemblies searched: {string.Join(", ", AvaloniaMetadata.LoadedAssemblyNames)}.");
    }

    /// <summary>
    /// The attribute has to exist on the element the same mapper names - the two halves are
    /// declared side by side and can still disagree, because nothing compiles them together.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeclaredAttributes))]
    public void Mapper_EmitsAttributesTheElementDefines(
        string winFormsTypeName, string avaloniaElementName, string attributeName, string source)
    {
        var element = AvaloniaMetadata.FindElement(avaloniaElementName);
        Assert.True(element is not null, $"Avalonia has no '{avaloniaElementName}' element at all.");

        // An attached property (`Canvas.Left`, `Grid.Row`) is declared by its owner, not by the
        // element it sits on, so it is checked against that owner instead.
        var (owner, memberName) = SplitAttached(element!, attributeName);

        Assert.True(
            AvaloniaMetadata.FindProperty(owner, memberName) is not null,
            $"'{winFormsTypeName}.{source}' is emitted as '{attributeName}' on a '{avaloniaElementName}', "
            + $"which has no such property.");
    }

    /// <summary>
    /// The bespoke mappers - ListView, the DataGrid template columns - build their attributes in
    /// code rather than from a table, so they are probed the only way that covers them: mapped
    /// with every property shape the designer can produce, and whatever comes out is checked.
    /// </summary>
    [Theory]
    [MemberData(nameof(ProbedAttributes))]
    public void Mapper_ProbedWithRealDesignerValues_EmitsAttributesTheElementDefines(
        string winFormsTypeName, string avaloniaElementName, string attributeName)
    {
        var element = AvaloniaMetadata.FindElement(avaloniaElementName);
        Assert.True(element is not null, $"Avalonia has no '{avaloniaElementName}' element at all.");

        var (owner, memberName) = SplitAttached(element!, attributeName);

        Assert.True(
            AvaloniaMetadata.FindProperty(owner, memberName) is not null,
            $"Mapping '{winFormsTypeName}' emitted '{attributeName}' on a '{avaloniaElementName}', "
            + "which has no such property.");
    }

    public static TheoryData<string, string> SimpleMappers()
    {
        var data = new TheoryData<string, string>();

        foreach (var (typeName, mapper) in OrderedMappers)
        {
            if (mapper is SimplePropertyMapper simple)
            {
                data.Add(typeName, simple.AvaloniaElementName);
            }
        }

        return data;
    }

    public static TheoryData<string, string, string, string> DeclaredAttributes()
    {
        var data = new TheoryData<string, string, string, string>();

        foreach (var (typeName, mapper) in OrderedMappers)
        {
            if (mapper is not SimplePropertyMapper simple)
            {
                continue;
            }

            foreach (var (winFormsProperty, attribute) in simple.DeclaredAttributes)
            {
                data.Add(typeName, simple.AvaloniaElementName, attribute, winFormsProperty ?? "(fixed)");
            }
        }

        return data;
    }

    public static TheoryData<string, string, string> ProbedAttributes()
    {
        var data = new TheoryData<string, string, string>();
        var seen = new HashSet<(string, string, string)>();

        foreach (var (typeName, mapper) in OrderedMappers)
        {
            foreach (var (element, attribute) in ProbeMapper(mapper, typeName))
            {
                if (seen.Add((typeName, element, attribute)))
                {
                    data.Add(typeName, element, attribute);
                }
            }
        }

        return data;
    }

    /// <summary>
    /// Maps one control repeatedly - once per (property name, value shape) pair - and collects
    /// every attribute that came out. A formatter refuses a value of the wrong shape, so probing
    /// with only one shape would silently cover a fraction of the table.
    /// </summary>
    private static IEnumerable<(string Element, string Attribute)> ProbeMapper(IControlMapper mapper, string typeName)
    {
        foreach (var propertyName in ProbedPropertyNames)
        {
            foreach (var value in ProbedValues)
            {
                var control = new ControlModel { FieldName = "field1", ClrTypeName = typeName };
                control.Properties[propertyName] = value;

                var mapped = mapper.Map(control);
                if (mapped.Status != MappingStatus.Direct || mapped.AvaloniaElementName is not { } element)
                {
                    continue;
                }

                foreach (var attribute in mapped.Attributes.Keys)
                {
                    yield return (element, attribute);
                }
            }
        }
    }

    /// <summary>
    /// `Canvas.Left` is a Canvas property, not a property of whatever carries it. Anything else
    /// stays on the element itself.
    /// </summary>
    private static (Type Owner, string MemberName) SplitAttached(Type element, string attributeName)
    {
        var separator = attributeName.IndexOf('.');
        if (separator < 0)
        {
            return (element, attributeName);
        }

        var ownerName = attributeName[..separator];
        var memberName = attributeName[(separator + 1)..];
        var owner = AvaloniaMetadata.FindElement(ownerName);

        Assert.True(owner is not null, $"'{attributeName}' names '{ownerName}', which Avalonia does not define.");
        return (owner!, memberName);
    }

    private static IEnumerable<(string TypeName, IControlMapper Mapper)> OrderedMappers =>
        new ControlMappingRegistry().Mappers
            .OrderBy(m => m.Key, StringComparer.Ordinal)
            .Select(m => (m.Key, m.Value));

    /// <summary>
    /// The WinForms property names worth probing with: every one any table-driven mapper declares,
    /// so the bespoke mappers are probed with the same vocabulary rather than a guessed list.
    /// </summary>
    private static IReadOnlyList<string> ProbedPropertyNames { get; } =
    [
        .. new ControlMappingRegistry().Mappers.Values
            .OfType<SimplePropertyMapper>()
            .SelectMany(m => m.DeclaredAttributes)
            .Select(a => a.WinFormsProperty)
            .OfType<string>()
            // "Format" is what a DateTimePicker switches its target element on, the same way
            // "View" is for a ListView - without it a bespoke mapper's other branch is never
            // probed, and its element and attributes are never held up against Avalonia at all.
            .Concat(["Text", "Checked", "Value", "Name", "Size", "Location", "View", "Columns", "Format"])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal),
    ];

    /// <summary>One value of every shape ExpressionEvaluator can produce from a designer file.</summary>
    private static IReadOnlyList<PropertyValue> ProbedValues { get; } =
    [
        new PropertyValue.Literal("probe"),
        new PropertyValue.Literal(7),
        new PropertyValue.Literal(true),
        new PropertyValue.Literal(1.5d),
        new PropertyValue.PointValue(3, 4),
        new PropertyValue.SizeValue(10, 20),
        new PropertyValue.PaddingValue(1, 2, 3, 4),
        new PropertyValue.ColorValue("Red", null, null, null, null),
        new PropertyValue.ColorValue(null, 255, 16, 32, 48),
        new PropertyValue.FontValue("Segoe UI", 9f, ["Bold"]),
        new PropertyValue.EnumMembers(["Fixed3D"]),

        // The enum members the per-instance mappers actually branch on. A mapper that picks a
        // different element for one of these is only checked against Avalonia if the probe
        // reaches that branch.
        new PropertyValue.EnumMembers(["Time"]),
        new PropertyValue.EnumMembers(["Custom"]),
        new PropertyValue.EnumMembers(["Details"]),
        new PropertyValue.EnumMembers(["MiddleRight"]),
        new PropertyValue.EnumMembers(["None"]),

        new PropertyValue.ControlReference("otherField"),
    ];
}
