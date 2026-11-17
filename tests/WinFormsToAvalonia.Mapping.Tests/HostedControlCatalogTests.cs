using System.Reflection;
using WinFormsToAvalonia.Core.Mapping;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// The hosted-control table, against WinForms itself.
/// </summary>
/// <remarks>
/// The whole conversion rests on one claim about this family: the hosted control is *always*
/// named in the constructor, because there is no constructor that omits it. That is checkable
/// here rather than believable, and it is the only side of this table that exists — the Avalonia
/// side has no counterpart at all, which is the point.
/// </remarks>
public class HostedControlCatalogTests
{
    [Theory]
    [MemberData(nameof(Hosts))]
    public void Host_CannotBeConstructedWithoutTheControlItHosts(string winFormsTypeName, int argumentIndex)
    {
        var type = WinFormsMetadata.FindType(winFormsTypeName);
        Assert.True(type is not null, $"WinForms does not define '{winFormsTypeName}'.");

        var constructors = type!.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.True(
            constructors.All(c => c.GetParameters().Length > 0),
            $"'{winFormsTypeName}' has a parameterless constructor, so a designer could create one "
            + "without naming a hosted control - the conversion would then have nothing to substitute.");

        Assert.True(
            constructors.Any(c =>
                c.GetParameters().Length > argumentIndex
                && c.GetParameters()[argumentIndex].ParameterType.Name == "Control"),
            $"No constructor of '{winFormsTypeName}' takes a Control at position {argumentIndex}.");
    }

    /// <summary>
    /// The three ToolStrip items that derive from a host are deliberately absent: they have
    /// parameterless constructors, a designer never passes them a control, and they already map
    /// to real Avalonia elements.
    /// </summary>
    [Theory]
    [InlineData("ToolStripComboBox")]
    [InlineData("ToolStripTextBox")]
    [InlineData("ToolStripProgressBar")]
    public void HostDerivedItem_WithItsOwnMapping_IsNotTreatedAsAHost(string winFormsTypeName)
    {
        Assert.False(HostedControlCatalog.TryGetHostedArgumentIndex(winFormsTypeName, out _));

        var type = WinFormsMetadata.FindType(winFormsTypeName);
        Assert.True(type is not null, $"WinForms does not define '{winFormsTypeName}'.");

        Assert.True(
            type!.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Any(c => c.GetParameters().Length == 0),
            $"'{winFormsTypeName}' is excluded from the catalog because it can be created without a "
            + "hosted control - and it cannot.");
    }

    public static TheoryData<string, int> Hosts()
    {
        var data = new TheoryData<string, int>();
        foreach (var (typeName, index) in HostedControlCatalog.All.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            data.Add(typeName, index);
        }

        return data;
    }
}
