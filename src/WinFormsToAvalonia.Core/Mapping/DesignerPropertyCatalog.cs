using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="Reason">Why there is nothing to carry.</param>
/// <param name="OnlyWhen">
/// Set when only some values carry nothing - a CheckBox's <c>CheckState</c> repeats
/// <c>Checked</c> unless it is <c>Indeterminate</c>, which is information of its own.
/// </param>
public sealed record NothingToCarry(string Reason, Func<PropertyValue, bool>? OnlyWhen = null);

/// <summary>
/// Designer properties that are set routinely and have nothing to carry into an Avalonia
/// project - so not reporting them is the faithful answer, not an omission.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart of <see cref="PropertyBag"/>'s record of what was looked up: everything a
/// designer sets is either asked about by some stage of the conversion, listed here, or reported
/// by name. Keep this list to what genuinely has no meaning on the Avalonia side - a property that
/// <em>could</em> be translated but is not yet belongs in the report, which is how anyone learns
/// it is missing.
/// </para>
/// <para>
/// <c>WinFormsToAvalonia.Mapping.Tests</c> checks every name is a real property of the WinForms
/// type it is listed under, so a typo cannot quietly hide nothing.
/// </para>
/// </remarks>
public static class DesignerPropertyCatalog
{
    /// <summary>The key for entries that apply to every type.</summary>
    public const string AnyType = "*";

    /// <summary>Type name (or <see cref="AnyType"/>) → property → why it carries nothing.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, NothingToCarry>> Entries { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, NothingToCarry>>(StringComparer.Ordinal)
        {
            [AnyType] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["Name"] = new("The designer field becomes the element's x:Name; the Name string only repeats it."),
                ["AutoScaleDimensions"] = new(
                    "WinForms' DPI scaling baseline. Avalonia lays out in device-independent pixels and scales the "
                    + "whole window itself."),
                ["AutoScaleMode"] = new("See AutoScaleDimensions."),
                ["UseVisualStyleBackColor"] = new(
                    "Asks for the OS theme's button background - which the Avalonia theme decides anyway."),
                ["AutoSize"] = new(
                    "The designer also records the Size it computed, and that is the size emitted - the Canvas "
                    + "layout pins every control to its recorded bounds."),
            },
            ["Form"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["MainMenuStrip"] = new(
                    "Names the menu WinForms merges MDI children's menus into; the menu itself is already placed."),
            },
            ["TabPage"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["Location"] = new(
                    "A page's bounds are the TabControl's client area, not a position anyone chose - and on a "
                    + "TabItem they would size the tab header (AxamlEmitter.HostsItems)."),
                ["Size"] = new("See Location."),
            },
            ["ContextMenuStrip"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["Size"] = new("Computed by the designer from the items; a ContextMenu sizes itself the same way."),
            },
            ["MenuStrip"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["Text"] = new("A strip's Text is never displayed."),
            },
            ["ToolStrip"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["Text"] = new("A strip's Text is never displayed."),
            },
            ["StatusStrip"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["Text"] = new("A strip's Text is never displayed."),
            },
            ["TableLayoutPanel"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["ColumnCount"] = new(
                    "Its children are emitted at the absolute positions the grid gave them, so the grid's shape "
                    + "has nothing left to lay out."),
                ["RowCount"] = new("See ColumnCount."),
            },
            ["ImageList"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["ImageSize"] = new("The extracted images keep the size the ImageStream stored them at."),
                ["ColorDepth"] = new("The extracted images are 32-bit PNGs whatever depth the list used."),
            },
            ["ErrorProvider"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["ContainerControl"] = new("Designer plumbing telling the provider which form it belongs to."),
            },
            ["PrintDialog"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["UseEXDialog"] = new("Picks between two Windows print dialogs; the conversion shows neither."),
                ["Document"] = new(
                    "The converted call prints the document the handler names (PrintAsync on it), so the "
                    + "dialog's link to it has nothing left to do."),
            },
            ["NotifyIcon"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["Visible"] = new(
                    "A TrayIcon is visible by default - only a designer-hidden icon would be lost, and that is "
                    + "reported.",
                    value => value is PropertyValue.Literal { Value: true }),
            },
            ["ListView"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["FullRowSelect"] = new(
                    "Avalonia's ListBox and DataGrid always select the whole row.",
                    value => value is PropertyValue.Literal { Value: true }),
            },
            ["CheckedListBox"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["CheckOnClick"] = new(
                    "Each row's tick is a real CheckBox in the ItemTemplate, which toggles on the first click - "
                    + "exactly what CheckOnClick asks for.",
                    value => value is PropertyValue.Literal { Value: true }),
            },
            ["CheckBox"] = new Dictionary<string, NothingToCarry>(StringComparer.Ordinal)
            {
                ["CheckState"] = new(
                    "Written alongside Checked, which carries the same value - except Indeterminate, which is "
                    + "reported.",
                    value => PropertyValueFormatters.AsEnumMember(value) is "Checked" or "Unchecked"),
            },
        };

    public static bool HasNothingToCarry(string typeName, string propertyName, PropertyValue value) =>
        Applies(AnyType, propertyName, value) || Applies(typeName, propertyName, value);

    private static bool Applies(string typeName, string propertyName, PropertyValue value) =>
        Entries.TryGetValue(typeName, out var byProperty)
        && byProperty.TryGetValue(propertyName, out var entry)
        && (entry.OnlyWhen is null || entry.OnlyWhen(value));
}
