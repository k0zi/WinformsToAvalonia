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
        ["LinkLabel"] = new(StringComparer.Ordinal) { ["Text"] = new("Text", "string", " = string.Empty;") },
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
