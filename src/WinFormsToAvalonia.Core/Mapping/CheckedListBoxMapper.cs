using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// WinForms' CheckedListBox is a ListBox whose items each carry a checkbox. Avalonia has no such
/// control, but it has an <c>ItemTemplate</c> - so this stays a <c>ListBox</c> and
/// <c>CheckedListPlan</c> gives each row a real <c>CheckBox</c> bound to a synthesized row type.
/// </summary>
/// <remarks>
/// <c>SelectionMode="Multiple"</c> used to be emitted here, as the closest thing Avalonia offered
/// to "several items are ticked at once". It is gone, and its going is the point: WinForms tracks
/// checked and selected separately, so approximating one with the other was only ever defensible
/// while the tick had nowhere else to live. Now it has, and a converted CheckedListBox selects the
/// way the original did.
/// </remarks>
public sealed class CheckedListBoxMapper : IControlMapper
{
    public string WinFormsTypeName => "CheckedListBox";

    public MappedControl Map(ControlModel control) => new(
        control.ClrTypeName,
        MappingStatus.Direct,
        "ListBox",
        new Dictionary<string, string>(StringComparer.Ordinal),
        FallbackTemplateKey: null,
        Warnings:
        [
            $"'{control.FieldName}' is a CheckedListBox: Avalonia has no such control, so it becomes a "
            + "ListBox whose ItemTemplate holds a CheckBox, bound to a generated row type in Models/. "
            + "SetItemChecked/GetItemChecked translate onto it. CheckedItems and CheckedIndices do not - "
            + "they are WinForms collections with no counterpart - so read the collection instead.",
        ]);
}
