using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

public class BindablePropertyCatalogTests
{
    /// <summary>
    /// The catalog and the mapping registry both name an Avalonia property for the same WinForms
    /// one, from two separate tables - and they must agree. A disagreement is not cosmetic: the
    /// mapper decides which element is emitted, so a catalog entry naming a property that element
    /// does not have becomes an AVLN error (as a {Binding}) or a compile error (as a translated
    /// handler statement) in the *generated* project, which the tool's own build cannot catch.
    /// </summary>
    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Catalog_AgreesWithTheControlMapper_AboutTheAvaloniaPropertyName(
        string winFormsTypeName, string winFormsPropertyName, string catalogAvaloniaName)
    {
        var control = new ControlModel { FieldName = "field1", ClrTypeName = winFormsTypeName };
        control.Properties[winFormsPropertyName] = new PropertyValue.Literal("probe");

        var mapped = new ControlMappingRegistry().Map(control);
        if (mapped.Status != MappingStatus.Direct)
        {
            return; // Fallback/Unsupported targets are governed by FallbackControlMemberSupport.
        }

        // The mapper only emits attributes for properties it knows; a type it has no entry for
        // simply carries the property through the universal path, which is nothing to compare.
        var mapperNames = mapped.Attributes.Keys.ToList();
        if (mapperNames.Count == 0)
        {
            return;
        }

        Assert.True(
            mapperNames.Contains(catalogAvaloniaName),
            $"BindablePropertyCatalog maps {winFormsTypeName}.{winFormsPropertyName} to " +
            $"'{catalogAvaloniaName}', but the mapper emits it as '{string.Join("/", mapperNames)}' " +
            $"on a '{mapped.AvaloniaElementName}'.");
    }

    public static TheoryData<string, string, string> CatalogEntries()
    {
        var data = new TheoryData<string, string, string>();

        // One row per (control type, WinForms property) the catalog claims is bindable, using the
        // property names the catalog itself reports rather than a hand-copied list.
        foreach (var (typeName, propertyName) in ProbedPairs)
        {
            if (BindablePropertyCatalog.TryGet(typeName, propertyName, out var bindable))
            {
                data.Add(typeName, propertyName, bindable.AvaloniaPropertyName);
            }
        }

        return data;
    }

    /// <summary>
    /// The (type, property) pairs worth probing: every WinForms property name the catalog can
    /// return, against every control type the registry maps. Universal properties
    /// (Enabled/Visible) are excluded - no mapper declares them, they are emitted by the
    /// universal pass instead.
    /// </summary>
    private static IEnumerable<(string TypeName, string PropertyName)> ProbedPairs =>
        from typeName in new ControlMappingRegistry().Mappers.Keys
        from propertyName in new[] { "Text", "Checked", "Value", "SelectedItem", "SelectedIndex" }
        select (typeName, propertyName);
}
