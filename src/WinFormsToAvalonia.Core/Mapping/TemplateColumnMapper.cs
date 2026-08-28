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
/// records the column, and even when it records a <c>DataPropertyName</c> the cell member it
/// belongs to is not decidable from it - so the emitted element carries an XML comment naming
/// what is known and asking for the rest.
/// </remarks>
public sealed class TemplateColumnMapper : IControlMapper
{
    /// <summary>
    /// What the human still has to write, naming the row-model property when the designer
    /// recorded one.
    /// </summary>
    /// <remarks>
    /// A template column is refused a generated binding on purpose, unlike the text and checkbox
    /// columns: <c>DataGridTemplateColumn</c> has no <c>Binding</c> property at all, and which
    /// member of the cell element the value belongs to is not decidable from
    /// <c>DataPropertyName</c> alone - a ComboBox cell could want <c>SelectedItem</c> or
    /// <c>SelectedValue</c>; a button or link cell shows the column's own <c>Text</c> unless
    /// <c>UseColumnTextForButtonValue</c> says otherwise; an <c>Image.Source</c> wants an
    /// <c>IImage</c> where a WinForms row holds a <c>System.Drawing.Image</c> or a byte array.
    /// So the name is reported rather than guessed at.
    /// </remarks>
    private static string BindingTodo(ControlModel control) =>
        control.Properties.TryGetValue("DataPropertyName", out var value)
            && value is PropertyValue.Literal { Value: string path }
                ? $"TODO(Winforms2Avalonia): bind this cell to the row model's '{path}' - which member " +
                  "of the cell element it belongs to depends on the column's own settings, so it is " +
                  "not generated."
                : "TODO(Winforms2Avalonia): bind this cell to the row model - this column has no " +
                  "DataPropertyName in Designer.cs, so there is no property name to carry over.";

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
                    Comment: BindingTodo(control),
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
