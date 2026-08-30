using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// WinForms' CheckedListBox is a ListBox whose items each carry a checkbox, and Avalonia has no
/// such control. A <c>ListBox</c> is still the right element - but the per-item check state is
/// the whole point of the control, so this says so rather than mapping it and moving on.
/// </summary>
/// <remarks>
/// <c>SelectionMode="Multiple"</c> is the closest thing Avalonia offers to "several items are
/// ticked at once", and it is genuinely closer than the single-selection default a plain ListBox
/// mapping produced. It is not the same thing: WinForms tracks checked and selected separately,
/// so a handler reading <c>CheckedItems</c> has nothing to read here. Hence the warning - the
/// registry's job is to be honest about what it converted, and this was silent before.
/// </remarks>
public sealed class CheckedListBoxMapper : IControlMapper
{
    public string WinFormsTypeName => "CheckedListBox";

    public MappedControl Map(ControlModel control) => new(
        control.ClrTypeName,
        MappingStatus.Direct,
        "ListBox",
        new Dictionary<string, string>(StringComparer.Ordinal) { ["SelectionMode"] = "Multiple" },
        FallbackTemplateKey: null,
        Warnings:
        [
            $"'{control.FieldName}' is a CheckedListBox, which becomes a multi-selection ListBox: "
            + "Avalonia has no per-item checkbox list, so ticking is approximated by selection and "
            + "CheckedItems/CheckedIndices have no equivalent. Bind an ItemTemplate containing a "
            + "CheckBox if the check state has to be a value of its own.",
        ]);
}
