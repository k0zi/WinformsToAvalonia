namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="ItemElementName">The element each literal item becomes.</param>
/// <param name="ItemContentAttributeName">
/// The attribute the item's text goes in, or null when the element carries it as text content.
/// </param>
/// <param name="CollectionPropertyName">
/// The property element the items are wrapped in (<c>Items</c> -> <c>&lt;X.Items&gt;</c>), or null
/// when they are direct children of the control.
/// </param>
/// <param name="XmlnsPrefix">
/// An xmlns the item element needs, declared on the wrapper rather than on the document root -
/// it is needed by exactly one target, and hoisting it would rewrite every generated view's root
/// attribute list (which the golden fixture pins) for a prefix almost no file uses.
/// </param>
public sealed record AvaloniaItemsTarget(
    string ItemElementName,
    string? ItemContentAttributeName = null,
    string? CollectionPropertyName = null,
    string? XmlnsPrefix = null,
    string? XmlnsValue = null);

/// <summary>
/// Which targets accept plain literal item children, and what shape each one wants them in.
/// </summary>
/// <remarks>
/// <para>
/// Keyed on the *target* - an Avalonia element name, or a bundled template's key - because that
/// is what decides whether the emitted XAML is legal. A `ComboBoxItem` inside anything else would
/// be an AVLN error in the generated project.
/// </para>
/// <para>
/// Unknown targets accept no items and the loss is reported as a warning by AxamlEmitter, so a
/// new mapper target is safe by default and opts in by being listed.
/// </para>
/// </remarks>
public static class AvaloniaItemsSupport
{
    private static readonly IReadOnlyDictionary<string, AvaloniaItemsTarget> ItemsByTarget =
        new Dictionary<string, AvaloniaItemsTarget>(StringComparer.Ordinal)
        {
            // Avalonia's own selection controls: a real item element, carrying its caption.
            ["ComboBox"] = new("ComboBoxItem", ItemContentAttributeName: "Content"),
            ["ListBox"] = new("ListBoxItem", ItemContentAttributeName: "Content"),

            // A bundled template, so keyed by template key rather than element name.
            // DomainUpDownFallback.Items is an AvaloniaList<string>: a get-only collection
            // property XAML can populate, holding bare strings rather than item elements.
            ["DomainUpDownFallback"] = new(
                "sys:String",
                CollectionPropertyName: "Items",
                XmlnsPrefix: "sys",
                XmlnsValue: "using:System"),
        };

    /// <summary>How the target takes literal items, or null when it takes none.</summary>
    public static AvaloniaItemsTarget? For(string? target) =>
        target is not null && ItemsByTarget.TryGetValue(target, out var itemsTarget) ? itemsTarget : null;

    /// <summary>Every claim, so WinFormsToAvalonia.Mapping.Tests can check each against Avalonia.</summary>
    public static IEnumerable<(string Target, AvaloniaItemsTarget Items)> AllEntries =>
        ItemsByTarget.Select(e => (e.Key, e.Value));
}
