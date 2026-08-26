namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The (deliberately small) set of WinForms control properties that have a direct, two-way
/// bindable Avalonia counterpart. This is the whole vocabulary a handler body is allowed to
/// use if it wants to be promoted from an event-driven code-behind method to a ViewModel
/// [RelayCommand]: a body that only reads and writes these can be expressed against ViewModel
/// properties, anything else needs the control object itself and therefore stays in code-behind.
/// </summary>
public static class BindablePropertyCatalog
{
    /// <param name="AvaloniaPropertyName">The Avalonia property to bind on the mapped element.</param>
    /// <param name="ClrTypeName">The generated [ObservableProperty] type.</param>
    /// <param name="DefaultValueSuffix">Initializer appended to the generated partial property declaration, e.g. " = string.Empty;".</param>
    public readonly record struct BindableProperty(string AvaloniaPropertyName, string ClrTypeName, string DefaultValueSuffix = "");

    /// <summary>Properties every Control exposes, whatever its concrete type.</summary>
    private static readonly Dictionary<string, BindableProperty> UniversalProperties = new(StringComparer.Ordinal)
    {
        ["Enabled"] = new("IsEnabled", "bool", " = true;"),
        ["Visible"] = new("IsVisible", "bool", " = true;"),
    };

    private static readonly Dictionary<string, Dictionary<string, BindableProperty>> ByControlType = new(StringComparer.Ordinal)
    {
        ["TextBox"] = new(StringComparer.Ordinal) { ["Text"] = new("Text", "string", " = string.Empty;") },
        ["MaskedTextBox"] = new(StringComparer.Ordinal) { ["Text"] = new("Text", "string", " = string.Empty;") },
        ["RichTextBox"] = new(StringComparer.Ordinal) { ["Text"] = new("Text", "string", " = string.Empty;") },
        ["Label"] = new(StringComparer.Ordinal) { ["Text"] = new("Text", "string", " = string.Empty;") },
        // Content, not Text: a LinkLabel maps to a HyperlinkButton, which has no Text property.
        // Getting this wrong is an AVLN2000 in the *generated* project - see the consistency test
        // in BindablePropertyCatalogTests, which is what this table is checked against.
        ["LinkLabel"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;") },
        ["Button"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;") },
        ["CheckBox"] = new(StringComparer.Ordinal)
        {
            ["Checked"] = new("IsChecked", "bool"),
            ["Text"] = new("Content", "string", " = string.Empty;"),
        },
        ["RadioButton"] = new(StringComparer.Ordinal)
        {
            ["Checked"] = new("IsChecked", "bool"),
            ["Text"] = new("Content", "string", " = string.Empty;"),
        },
        ["NumericUpDown"] = new(StringComparer.Ordinal) { ["Value"] = new("Value", "decimal?") },
        ["ProgressBar"] = new(StringComparer.Ordinal) { ["Value"] = new("Value", "double") },
        ["TrackBar"] = new(StringComparer.Ordinal) { ["Value"] = new("Value", "double") },
        ["ComboBox"] = new(StringComparer.Ordinal)
        {
            ["SelectedItem"] = new("SelectedItem", "object?"),
            ["SelectedIndex"] = new("SelectedIndex", "int"),
            ["Text"] = new("Text", "string", " = string.Empty;"),
        },
        ["ListBox"] = new(StringComparer.Ordinal)
        {
            ["SelectedItem"] = new("SelectedItem", "object?"),
            ["SelectedIndex"] = new("SelectedIndex", "int"),
        },
        ["DateTimePicker"] = new(StringComparer.Ordinal) { ["Value"] = new("SelectedDate", "DateTimeOffset?") },
        ["CheckedListBox"] = new(StringComparer.Ordinal)
        {
            ["SelectedItem"] = new("SelectedItem", "object?"),
            ["SelectedIndex"] = new("SelectedIndex", "int"),
        },

        // ToolStrip items. Every one of these is already Direct-mapped to a real Avalonia element
        // (see DefaultControlMappers), so their values were always bindable in principle - they
        // were simply missing from this table, which is what makes a property writable from a
        // translated handler or bindable from a promoted command.
        ["ToolStripMenuItem"] = new(StringComparer.Ordinal)
        {
            ["Text"] = new("Header", "string", " = string.Empty;"),
            ["Checked"] = new("IsChecked", "bool"),
        },
        ["ToolStripButton"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;") },
        ["ToolStripDropDownButton"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;") },
        ["ToolStripSplitButton"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;") },
        ["ToolStripLabel"] = new(StringComparer.Ordinal) { ["Text"] = new("Text", "string", " = string.Empty;") },
        ["ToolStripStatusLabel"] = new(StringComparer.Ordinal) { ["Text"] = new("Text", "string", " = string.Empty;") },
        ["ToolStripTextBox"] = new(StringComparer.Ordinal) { ["Text"] = new("Text", "string", " = string.Empty;") },
        ["ToolStripComboBox"] = new(StringComparer.Ordinal)
        {
            ["SelectedItem"] = new("SelectedItem", "object?"),
            ["SelectedIndex"] = new("SelectedIndex", "int"),
        },
        ["ToolStripProgressBar"] = new(StringComparer.Ordinal) { ["Value"] = new("Value", "double") },
    };

    public static bool TryGet(string winFormsControlTypeName, string propertyName, out BindableProperty property)
    {
        if (ByControlType.TryGetValue(winFormsControlTypeName, out var typeProperties)
            && typeProperties.TryGetValue(propertyName, out property))
        {
            return true;
        }

        return UniversalProperties.TryGetValue(propertyName, out property);
    }
}
