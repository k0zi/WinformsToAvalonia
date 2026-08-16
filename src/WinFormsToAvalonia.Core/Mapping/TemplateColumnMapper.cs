using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Maps a DataGridView column type that has no attribute-only Avalonia counterpart to a
/// <c>DataGridTemplateColumn</c> carrying a generated <c>CellTemplate</c>.
/// </summary>
/// <remarks>
/// Avalonia's DataGrid ships exactly three usable column types - <c>DataGridTextColumn</c>,
/// <c>DataGridCheckBoxColumn</c> and <c>DataGridTemplateColumn</c>. There is no
/// <c>DataGridComboBoxColumn</c> (a mapping to one is an AVLN2000 build break), and no button/
/// image/link column at all, so every one of those WinForms column types has to become a
/// template column instead. The generated cell template is deliberately unbound: Designer.cs
/// records the column, never which row-model property it displays, so the emitted element
/// carries an XML comment telling the human to add the binding.
/// </remarks>
public sealed class TemplateColumnMapper : IControlMapper
{
    private const string BindingTodoComment =
        "TODO(Winforms2Avalonia): bind this cell to the row model - Designer.cs records the " +
        "column but not its DataPropertyName-to-view-model mapping.";

    private readonly string _cellElementName;
    private readonly IReadOnlyList<(string WinFormsProperty, string AvaloniaAttribute, Func<PropertyValue, string?> Format)> _cellPropertyMappings;

    /// <param name="cellElementName">The Avalonia element placed inside the cell DataTemplate.</param>
    /// <param name="cellPropertyMappings">
    /// WinForms properties copied onto the *cell* element rather than onto the column (e.g. a
    /// DataGridViewButtonColumn's Text, which is the caption every button in the column shows).
    /// </param>
    public TemplateColumnMapper(
        string winFormsTypeName,
        string cellElementName,
        IReadOnlyList<(string WinFormsProperty, string AvaloniaAttribute, Func<PropertyValue, string?> Format)>? cellPropertyMappings = null)
    {
        WinFormsTypeName = winFormsTypeName;
        _cellElementName = cellElementName;
        _cellPropertyMappings = cellPropertyMappings ?? [];
    }

    public string WinFormsTypeName { get; }

    public MappedControl Map(ControlModel control)
    {
        var columnAttributes = new Dictionary<string, string>(StringComparer.Ordinal);
        if (control.Properties.TryGetValue("HeaderText", out var headerText)
            && PropertyValueFormatters.AsText(headerText) is { } header)
        {
            columnAttributes["Header"] = header;
        }

        var cellAttributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (winFormsProperty, avaloniaAttribute, format) in _cellPropertyMappings)
        {
            if (control.Properties.TryGetValue(winFormsProperty, out var value) && format(value) is { } formatted)
            {
                cellAttributes[avaloniaAttribute] = formatted;
            }
        }

        var cellTemplate = new AxamlElementSpec(
            "DataGridTemplateColumn.CellTemplate",
            Children:
            [
                new AxamlElementSpec(
                    "DataTemplate",
                    Comment: BindingTodoComment,
                    Children: [new AxamlElementSpec(_cellElementName, cellAttributes)]),
            ]);

        return new MappedControl(
            control.ClrTypeName,
            MappingStatus.Direct,
            "DataGridTemplateColumn",
            columnAttributes,
            FallbackTemplateKey: null,
            Warnings: [],
            RequiredNuGetPackage: "Avalonia.Controls.DataGrid",
            SupportsName: false,
            NestedElements: [cellTemplate]);
    }
}
