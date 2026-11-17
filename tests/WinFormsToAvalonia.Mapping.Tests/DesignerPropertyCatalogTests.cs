using System.Reflection;
using WinFormsToAvalonia.Core.Mapping;
using Xunit;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// Every entry in <see cref="DesignerPropertyCatalog"/> silences a report. A misspelt one would
/// silence nothing and look as though it did, so each name is held up against WinForms itself.
/// </summary>
public class DesignerPropertyCatalogTests
{
    /// <summary>The types an <see cref="DesignerPropertyCatalog.AnyType"/> entry has to be a property of one of.</summary>
    private static readonly string[] AnyTypeCandidates = ["Control", "Form", "ButtonBase"];

    public static TheoryData<string, string> Entries()
    {
        var data = new TheoryData<string, string>();
        foreach (var (typeName, properties) in DesignerPropertyCatalog.Entries)
        {
            foreach (var propertyName in properties.Keys)
            {
                data.Add(typeName, propertyName);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Entry_NamesARealWinFormsProperty(string typeName, string propertyName)
    {
        var owners = typeName == DesignerPropertyCatalog.AnyType ? AnyTypeCandidates : [typeName];

        var found = owners
            .Select(WinFormsMetadata.FindType)
            .Any(type => type?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) is not null);

        Assert.True(found, $"No WinForms property '{propertyName}' on {string.Join(" or ", owners)}.");
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Entry_SaysWhy(string typeName, string propertyName)
    {
        Assert.False(string.IsNullOrWhiteSpace(DesignerPropertyCatalog.Entries[typeName][propertyName].Reason));
    }
    /// <summary>
    /// The reason a carried-over helper was limited to keyword types is that a named type could be a
    /// WinForms type with a different Avalonia counterpart. The named types now accepted are claimed
    /// to be no such thing - so none of them may be a type WinForms declares.
    /// </summary>
    [Fact]
    public void PlainDotNetTypes_AreNotWinFormsTypes()
    {
        foreach (var name in PlainDotNetTypeCatalog.NamedTypes)
        {
            Assert.Null(WinFormsMetadata.FindType(name));
        }
    }
}
