using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.FallbackControls;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// Every claim <see cref="AvaloniaItemsSupport"/> makes about how a target takes literal items,
/// held up against the thing that would actually have to accept it.
/// </summary>
/// <remarks>
/// This table had no test at all, and it emits <em>elements</em> - the failure mode is an
/// AVLN2000 in the generated project, which nothing in this repo's own build can see. An item
/// element Avalonia does not have, or a collection property the target does not declare, is
/// exactly that.
/// </remarks>
public class AvaloniaItemsSupportTests
{
    public static TheoryData<string> Targets()
    {
        var data = new TheoryData<string>();
        foreach (var (target, _) in AvaloniaItemsSupport.AllEntries)
        {
            data.Add(target);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Targets))]
    public void Target_TakesTheItemsItClaims(string target)
    {
        var items = AvaloniaItemsSupport.For(target)!;

        // A bundled template: the type is ours, so the source is the authority.
        if (FallbackControlCatalog.All.TryGetValue(target, out var template))
        {
            var source = FallbackControlCatalog.ReadTemplateSource(template.ResourceLogicalName);

            Assert.True(
                items.CollectionPropertyName is not null,
                $"'{target}' is a bundled template, whose items can only go in a named collection "
                + "property - a template key is not an element XAML can put children under.");

            Assert.True(
                Regex.IsMatch(source, $@"\b{Regex.Escape(items.CollectionPropertyName!)}\s*{{\s*get;"),
                $"'{target}' claims a '{items.CollectionPropertyName}' collection property, but its "
                + "template declares no such get-only property - the emitted property element would "
                + "be an AVLN2000 in the generated project.");

            return;
        }

        // A real Avalonia element: metadata is the authority.
        var element = AvaloniaMetadata.FindElement(target);
        Assert.True(element is not null, $"'{target}' is neither an Avalonia element nor a bundled template.");

        var itemElement = AvaloniaMetadata.FindElement(items.ItemElementName);
        Assert.True(
            itemElement is not null,
            $"'{target}' wraps each item in '{items.ItemElementName}', which Avalonia does not have.");

        Assert.True(
            items.ItemContentAttributeName is null
                || AvaloniaMetadata.FindProperty(itemElement!, items.ItemContentAttributeName) is not null,
            $"'{items.ItemElementName}' has no '{items.ItemContentAttributeName}' to carry the item's caption.");
    }

    /// <summary>
    /// An item element carrying a prefix needs that prefix declared, and one without must not
    /// claim a declaration nobody would emit.
    /// </summary>
    [Theory]
    [MemberData(nameof(Targets))]
    public void ItemElementPrefix_MatchesTheDeclarationItNeeds(string target)
    {
        var items = AvaloniaItemsSupport.For(target)!;
        var prefix = items.ItemElementName.Contains(':', StringComparison.Ordinal)
            ? items.ItemElementName[..items.ItemElementName.IndexOf(':', StringComparison.Ordinal)]
            : null;

        Assert.Equal(prefix, items.XmlnsPrefix);
        Assert.Equal(prefix is null, items.XmlnsValue is null);
    }
}
