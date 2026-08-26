namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Which Avalonia elements accept plain literal item children, and what each item is wrapped in.
/// </summary>
/// <remarks>
/// The same shape - and the same reasoning - as <see cref="AvaloniaStylePropertySupport"/>:
/// keyed on the *target* element name rather than the WinForms type, because that is what
/// decides whether the emitted XAML is legal. Only the two selection controls whose Avalonia
/// counterpart takes a matching item element are listed; a `ComboBoxItem` inside anything else
/// would be an AVLN error in the generated project.
///
/// Unknown element names accept no items, so a fallback control (`DomainUpDownFallback`) or a
/// future mapper target loses its designer-declared entries rather than risking a broken build -
/// the loss is reported as a warning by AxamlEmitter.
/// </remarks>
public static class AvaloniaItemsSupport
{
    private static readonly IReadOnlyDictionary<string, string> ItemElementByTarget =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ComboBox"] = "ComboBoxItem",
            ["ListBox"] = "ListBoxItem",
        };

    /// <summary>The element each literal item becomes, or null when the target takes none.</summary>
    public static string? ItemElementFor(string? avaloniaElementName) =>
        avaloniaElementName is not null && ItemElementByTarget.TryGetValue(avaloniaElementName, out var itemElement)
            ? itemElement
            : null;
}
