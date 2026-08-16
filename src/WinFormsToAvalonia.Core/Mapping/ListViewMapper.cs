using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// WinForms' ListView is two controls in one, so it needs a per-instance decision rather than
/// a fixed <see cref="SimplePropertyMapper"/> entry: in <c>View.Details</c> mode it is a
/// column-oriented grid (Avalonia's <c>DataGrid</c>), in every other mode a flat item list
/// (Avalonia's <c>ListBox</c>).
/// </summary>
/// <remarks>
/// The presence of parsed <c>ColumnHeader</c> children counts as Details too: those come from
/// `this.listView1.Columns.AddRange(...)` (recognized by DesignerSyntaxWalker) and map to
/// <c>DataGridTextColumn</c>, which a ListBox cannot host - emitting them under a ListBox
/// would be an AVLN2000 build break, exactly the class of bug this mapper exists to avoid.
/// Item content is still not translated (same simplification as everywhere else): the target
/// control is emitted with its columns but no rows.
/// </remarks>
public sealed class ListViewMapper : IControlMapper
{
    public string WinFormsTypeName => "ListView";

    public MappedControl Map(ControlModel control)
    {
        var isDetails = control.Properties.TryGetValue("View", out var view)
            && view is PropertyValue.EnumMembers { MemberNames: var members }
            && members.Contains("Details");
        var hasColumns = control.Children.Any(c => c.ClrTypeName == "ColumnHeader");

        if (!isDetails && !hasColumns)
        {
            return new MappedControl(
                control.ClrTypeName, MappingStatus.Direct, "ListBox",
                new Dictionary<string, string>(StringComparer.Ordinal), null, []);
        }

        return new MappedControl(
            control.ClrTypeName,
            MappingStatus.Direct,
            "DataGrid",
            new Dictionary<string, string>(StringComparer.Ordinal),
            FallbackTemplateKey: null,
            Warnings: [],
            ChildWrapperElementNames: ["DataGrid.Columns"],
            RequiredNuGetPackage: "Avalonia.Controls.DataGrid");
    }
}
