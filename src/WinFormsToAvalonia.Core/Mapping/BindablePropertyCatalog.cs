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
    /// <param name="AvaloniaTypeName">
    /// The type the Avalonia property <em>really</em> is, where that differs from
    /// <paramref name="ClrTypeName"/>. Null means they agree.
    /// </param>
    /// <remarks>
    /// The two types are not the same question, which is why both are here.
    /// <paramref name="ClrTypeName"/> is what the generated ViewModel property is declared as, and
    /// a <c>{Binding}</c> converts on its way to the element. A translated <em>code-behind</em>
    /// statement has no binding in between: it touches the Avalonia member directly, so reading
    /// <c>Button.Content</c> (an <c>object?</c>) into a string is a compile error in the generated
    /// project unless the read says so. See <see cref="ReadExpression"/>.
    /// </remarks>
    public readonly record struct BindableProperty(
        string AvaloniaPropertyName,
        string ClrTypeName,
        string DefaultValueSuffix = "",
        string? AvaloniaTypeName = null);

    /// <summary>
    /// How a code-behind read of <paramref name="access"/> has to be written so it yields
    /// <see cref="BindableProperty.ClrTypeName"/> - the type the original WinForms expression had.
    /// </summary>
    /// <remarks>
    /// Every conversion here is faithful, not convenient. WinForms' string properties never return
    /// null where Avalonia's are nullable, so <c>?? string.Empty</c> *is* the WinForms behaviour.
    /// <c>Content</c> holds whatever this conversion put there, which for a converted
    /// <c>Text</c> is a string. A two-state <c>IsChecked</c> is never null - the three-state case
    /// is refused at the call site rather than coalesced, since WinForms reports Indeterminate as
    /// <c>true</c> and <c>?? false</c> would say the opposite.
    /// </remarks>
    public static string ReadExpression(string access, BindableProperty property) =>
        (property.AvaloniaTypeName, property.ClrTypeName) switch
        {
            ("object", "string") => $"({access} as string ?? string.Empty)",
            ("bool?", "bool") => $"({access} ?? false)",
            (_, "string") => $"({access} ?? string.Empty)",
            _ => access,
        };

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
        ["LinkLabel"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;", "object") },
        ["Button"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;", "object") },
        ["CheckBox"] = new(StringComparer.Ordinal)
        {
            ["Checked"] = new("IsChecked", "bool", AvaloniaTypeName: "bool?"),
            ["Text"] = new("Content", "string", " = string.Empty;", "object"),
        },
        ["RadioButton"] = new(StringComparer.Ordinal)
        {
            ["Checked"] = new("IsChecked", "bool", AvaloniaTypeName: "bool?"),
            ["Text"] = new("Content", "string", " = string.Empty;", "object"),
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
        // DateTime?, not DateTimeOffset?: Avalonia's CalendarDatePicker.SelectedDate is a
        // DateTime?, and a two-way binding between the two silently does nothing at run time -
        // the kind of disagreement only the generated project can see.
        ["DateTimePicker"] = new(StringComparer.Ordinal) { ["Value"] = new("SelectedDate", "DateTime?") },
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
            ["Text"] = new("Header", "string", " = string.Empty;", "object"),
            ["Checked"] = new("IsChecked", "bool"),
        },
        ["ToolStripButton"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;", "object") },
        ["ToolStripDropDownButton"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;", "object") },
        ["ToolStripSplitButton"] = new(StringComparer.Ordinal) { ["Text"] = new("Content", "string", " = string.Empty;", "object") },
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

    /// <summary>
    /// The same lookup from the other end: is *this Avalonia property* one this catalog considers
    /// bindable on this control? Needed because a control method can translate into a property
    /// write (<c>AppendText</c> into <c>Text</c>), and what decides whether that survives on a
    /// ViewModel is the property, not the method it came from.
    /// </summary>
    /// <param name="winFormsPropertyName">
    /// The WinForms property this entry is keyed under - <c>Visible</c> for Avalonia's
    /// <c>IsVisible</c>. Needed because a designer value is stored under the WinForms name.
    /// </param>
    public static bool TryGetByAvaloniaName(
        string winFormsControlTypeName,
        string avaloniaPropertyName,
        out BindableProperty property,
        out string winFormsPropertyName)
    {
        var candidates = ByControlType.TryGetValue(winFormsControlTypeName, out var typeProperties)
            ? typeProperties.Concat(UniversalProperties)
            : UniversalProperties;

        foreach (var (name, candidate) in candidates)
        {
            if (candidate.AvaloniaPropertyName == avaloniaPropertyName)
            {
                property = candidate;
                winFormsPropertyName = name;
                return true;
            }
        }

        property = default;
        winFormsPropertyName = "";
        return false;
    }
}
