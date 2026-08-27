using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// <see cref="BindablePropertyCatalog"/> held up against the Avalonia it describes.
/// </summary>
/// <remarks>
/// <para>
/// Core.Tests already checks that the catalog and the mappers agree with <em>each other</em>.
/// They can agree and both be wrong: nothing in this repo compiles against Avalonia, so an entry
/// naming a property that does not exist - or giving it the wrong type - passes every check there
/// is and then breaks the generated project on someone else's machine.
/// </para>
/// <para>
/// The type half is the one that bit hardest. Each entry carries two: the ViewModel property's
/// type, which a <c>{Binding}</c> converts on its way to the element, and the Avalonia member's
/// own. They are different questions, and a translated code-behind statement asks the second -
/// <c>if (checkBox1.IsChecked)</c> on a <c>bool?</c> is a CS0266.
/// </para>
/// </remarks>
public class BindablePropertyCatalogAgainstAvaloniaTests
{
    [Theory]
    [MemberData(nameof(Entries))]
    public void CatalogEntry_NamesAPropertyTheElementHas(
        string winFormsTypeName, string winFormsPropertyName, string avaloniaElementName, string avaloniaPropertyName)
    {
        var element = AvaloniaMetadata.FindElement(avaloniaElementName);
        Assert.True(element is not null, $"Avalonia has no '{avaloniaElementName}' element at all.");

        Assert.True(
            AvaloniaMetadata.FindProperty(element!, avaloniaPropertyName) is not null,
            $"'{winFormsTypeName}.{winFormsPropertyName}' is catalogued as '{avaloniaPropertyName}' on a "
            + $"'{avaloniaElementName}', which has no such property.");
    }

    /// <summary>
    /// A read is emitted as the type the WinForms expression had, and the catalog is what decides
    /// how. If it disagrees with the real member, the conversion is wrong in one of two ways: a
    /// missing conversion is a compile error, a needless one is dead code around a value that was
    /// never nullable.
    /// </summary>
    [Theory]
    [MemberData(nameof(Entries))]
    public void CatalogEntry_DescribesTheAvaloniaTypeCorrectly(
        string winFormsTypeName,
        string winFormsPropertyName,
        string avaloniaElementName,
        string avaloniaPropertyName)
    {
        var element = AvaloniaMetadata.FindElement(avaloniaElementName);
        Assert.True(element is not null, $"Avalonia has no '{avaloniaElementName}' element at all.");

        var property = AvaloniaMetadata.FindProperty(element!, avaloniaPropertyName);
        Assert.True(property is not null, $"'{avaloniaElementName}' has no '{avaloniaPropertyName}'.");

        var found = BindablePropertyCatalog.TryGet(winFormsTypeName, winFormsPropertyName, out var entry);
        Assert.True(found, $"'{winFormsTypeName}.{winFormsPropertyName}' vanished from the catalog.");

        // Null means "the two agree"; the ViewModel type is then also the Avalonia one.
        var claimed = entry.AvaloniaTypeName ?? entry.ClrTypeName;
        var actual = AvaloniaMetadata.SpellType(property!.PropertyType);

        // An enum is spelled by its own name on both sides, and the catalog says "enum" for the
        // ones it maps member-for-member - there is nothing to compare beyond that it is one.
        if (claimed == "enum")
        {
            Assert.True(
                AvaloniaMetadata.IsEnumOrNullableEnum(property.PropertyType),
                $"'{winFormsTypeName}.{winFormsPropertyName}' is catalogued as an enum, but "
                + $"'{avaloniaElementName}.{avaloniaPropertyName}' is a {actual}.");
            return;
        }

        // Reference-type nullability is an attribute, not part of the type, and it changes
        // nothing about whether an assignment compiles - `object?` and `object` are the same
        // member. Value-type nullability is the opposite: it is the type, and it is exactly what
        // a read has to convert away. So the `?` is only significant on a value type.
        if (!AvaloniaMetadata.IsNullableValueType(property.PropertyType))
        {
            claimed = claimed.TrimEnd('?');
        }

        Assert.True(
            claimed == actual,
            $"'{winFormsTypeName}.{winFormsPropertyName}' claims '{avaloniaElementName}."
            + $"{avaloniaPropertyName}' is a {claimed}, but it is a {actual}. A code-behind read is "
            + "written from this, so the generated project would not compile.");
    }

    /// <summary>
    /// One row per (WinForms type, property) the catalog answers for, resolved to the element the
    /// mapper actually emits for that type - the two tables are only meaningful together.
    /// </summary>
    public static TheoryData<string, string, string, string> Entries()
    {
        var data = new TheoryData<string, string, string, string>();
        var registry = new ControlMappingRegistry();

        foreach (var (typeName, propertyName) in AllCatalogPairs(registry))
        {
            if (!BindablePropertyCatalog.TryGet(typeName, propertyName, out var entry))
            {
                continue;
            }

            var mapped = registry.Map(new ControlModel { FieldName = "field1", ClrTypeName = typeName });

            // A fallback or unsupported target is governed by FallbackControlMemberSupport, which
            // has its own test - there is no Avalonia element here to check against.
            if (mapped.Status != MappingStatus.Direct || mapped.AvaloniaElementName is not { } element)
            {
                continue;
            }

            data.Add(typeName, propertyName, element, entry.AvaloniaPropertyName);
        }

        return data;
    }

    /// <summary>
    /// Every entry the catalog holds: its type-specific ones, plus the universal ones against
    /// every control type the registry maps - because that is exactly the set a handler body can
    /// reach.
    /// </summary>
    private static IEnumerable<(string TypeName, string PropertyName)> AllCatalogPairs(ControlMappingRegistry registry) =>
        BindablePropertyCatalog.TypeSpecificEntries
            .Select(e => (e.WinFormsTypeName, e.PropertyName))
            .Concat(
                from typeName in registry.Mappers.Keys
                from universal in BindablePropertyCatalog.UniversalEntries
                select (typeName, universal.PropertyName))
            .Distinct()
            .OrderBy(p => p.Item1, StringComparer.Ordinal)
            .ThenBy(p => p.Item2, StringComparer.Ordinal);
}
