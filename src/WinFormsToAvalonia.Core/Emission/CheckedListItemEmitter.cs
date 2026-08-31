using System.Text;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>
/// The row type behind a converted <c>CheckedListBox</c>: a caption and a tick.
/// </summary>
/// <remarks>
/// An <c>ObservableObject</c> rather than a plain class, and that is load-bearing rather than
/// idiom. The binding writes the tick back when the user clicks it either way - but a handler
/// calling <c>SetItemChecked</c> writes it from the other side, and without a change notification
/// the CheckBox on screen would keep showing the old state. WinForms redrew the item; this is how
/// Avalonia is told to.
/// </remarks>
public static class CheckedListItemEmitter
{
    public static string EmitItemType(CheckedListPlan plan)
    {
        var sb = new StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        Line("using CommunityToolkit.Mvvm.ComponentModel;");
        Line();
        Line($"namespace {plan.ElementTypeNamespace};");
        Line();
        Line($"/// <summary>One row of '{plan.ControlFieldName}', which was a WinForms CheckedListBox.</summary>");
        Line($"public sealed partial class {plan.ElementTypeName} : ObservableObject");
        Line("{");
        Line("    /// <summary>The caption - what the WinForms item's ToString() showed.</summary>");
        Line("    [ObservableProperty]");
        Line("    public partial string Text { get; set; } = string.Empty;");
        Line();
        Line("    /// <summary>The tick. This is the state WinForms kept separately from selection.</summary>");
        Line("    [ObservableProperty]");
        Line("    public partial bool IsChecked { get; set; }");
        Line("}");

        return sb.ToString();
    }
}
