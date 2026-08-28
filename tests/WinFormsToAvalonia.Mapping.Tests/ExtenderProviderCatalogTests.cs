using WinFormsToAvalonia.Core.Mapping;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// The extender-provider table, against both frameworks it makes a claim about.
/// </summary>
/// <remarks>
/// This also closes a gap that predates the table: <c>ToolTip.Tip</c> is emitted from the emitter
/// rather than from a mapper's attribute list, so <c>ControlMapperTests</c> never saw it and
/// nothing in this project had ever checked that it exists.
/// </remarks>
public class ExtenderProviderCatalogTests
{
    /// <summary>The WinForms half: the provider really has that two-argument setter.</summary>
    [Theory]
    [MemberData(nameof(Setters))]
    public void Setter_ExistsOnTheWinFormsProvider(string ownerTypeName, string methodName)
    {
        var owner = WinFormsMetadata.FindType(ownerTypeName);
        Assert.True(owner is not null, $"WinForms does not define '{ownerTypeName}'.");

        var setter = owner!
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == 2);

        Assert.True(
            setter is not null,
            $"'{ownerTypeName}' has no two-argument '{methodName}' - the walker would never match it.");

        Assert.Equal("Control", setter!.GetParameters()[0].ParameterType.Name);
    }

    /// <summary>
    /// The Avalonia half: the attribute is a real attached property, so it can legally be set on
    /// a different element than the one that declares it.
    /// </summary>
    [Theory]
    [MemberData(nameof(Attributes))]
    public void Attribute_IsARealAttachedProperty(string attributeName)
    {
        var parts = attributeName.Split('.');
        Assert.Equal(2, parts.Length);

        var owner = AvaloniaMetadata.FindElement(parts[0]);
        Assert.True(owner is not null, $"Avalonia does not define '{parts[0]}'.");

        Assert.True(
            AvaloniaMetadata.FindAttachedProperty(owner!, parts[1]) is not null,
            $"'{attributeName}' is not an AttachedProperty on '{parts[0]}' - setting it from XAML "
            + "on another element would not compile in the generated project.");
    }

    /// <summary>
    /// Two rows may not park their values under the same key, or the second provider would
    /// overwrite the first on any control both extended.
    /// </summary>
    [Fact]
    public void PropertyKeys_AreDistinct()
    {
        var duplicated = ExtenderProviderCatalog.Setters
            .GroupBy(e => e.PropertyKey, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicated.Count == 0, $"Property keys used twice: {string.Join(", ", duplicated)}");
    }

    public static TheoryData<string, string> Setters()
    {
        var data = new TheoryData<string, string>();
        foreach (var entry in ExtenderProviderCatalog.Setters)
        {
            data.Add(entry.OwnerClrTypeName, entry.SetterMethodName);
        }

        return data;
    }

    public static TheoryData<string> Attributes()
    {
        var data = new TheoryData<string>();
        foreach (var entry in ExtenderProviderCatalog.Setters)
        {
            data.Add(entry.AvaloniaAttributeName);
        }

        return data;
    }
}
