namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The (deliberately small) set of WinForms control properties that have a direct, two-way
/// bindable Avalonia counterpart. This is the whole vocabulary a handler body is allowed to
/// use if it wants to be promoted from an event-driven code-behind method to a ViewModel
/// [RelayCommand]: a body that only reads and writes these can be expressed against ViewModel
/// properties, anything else needs the control object itself and therefore stays in code-behind.
/// </summary>
/// <summary>
/// How a value has to be rewritten to cross from WinForms to Avalonia, when the two frameworks
/// mean the same thing by different values.
/// </summary>
/// <remarks>
/// A rename is the ordinary case and needs nothing here. This is for the entries where the shapes
/// differ - and it is deliberately an enum rather than a pair of format strings: each member is a
/// specific, argued equivalence, and a new one should have to be written down and defended rather
/// than expressed in passing.
/// </remarks>
public enum BindableValueShape
{
    /// <summary>The value crosses unchanged.</summary>
    Same,

    /// <summary>
    /// WinForms' <c>WordWrap</c> bool against Avalonia's <c>TextWrapping</c> enum. Two-valued on
    /// both sides - <c>Wrap</c> and <c>NoWrap</c> are the only members a converted TextBox can
    /// hold - so the round trip is exact.
    /// </summary>
    BoolAsTextWrapping,
}

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
        string? AvaloniaTypeName = null,
        BindableValueShape ValueShape = BindableValueShape.Same);

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
    /// <summary>
    /// How a code-behind <em>write</em> of <paramref name="value"/> has to be spelled. Only the
    /// entries whose value shape differs need one; everything else assigns as it stands.
    /// </summary>
    public static string WriteExpression(string value, BindableProperty property) =>
        property.ValueShape switch
        {
            BindableValueShape.BoolAsTextWrapping => $"({value}) ? TextWrapping.Wrap : TextWrapping.NoWrap",
            _ => value,
        };

    public static string ReadExpression(string access, BindableProperty property) =>
        property.ValueShape switch
        {
            BindableValueShape.BoolAsTextWrapping => $"({access} == TextWrapping.Wrap)",
            _ => ReadByType(access, property),
        };

    private static string ReadByType(string access, BindableProperty property) =>
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

    /// <summary>
    /// The text-entry family. Everything here is a plain rename that Avalonia's <c>TextBox</c>
    /// really has - checked, entry by entry, in WinFormsToAvalonia.Mapping.Tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Declared before <c>ByControlType</c>: static initializers run in source order.
    /// </para>
    /// <para>
    /// <c>WordWrap</c> is the odd one and the interesting one. WinForms holds a <c>bool</c> where
    /// Avalonia holds a <c>TextWrapping</c> enum - the same idea in a different shape - so it is
    /// the first entry that needs the value itself rewritten, not just the name. See
    /// <see cref="BindableProperty.ValueShape"/>.
    /// </para>
    /// </remarks>
    private static Dictionary<string, BindableProperty> TextBoxProperties { get; } =
        new(StringComparer.Ordinal)
        {
            ["Text"] = new("Text", "string", " = string.Empty;"),
            ["Multiline"] = new("AcceptsReturn", "bool"),
            ["ReadOnly"] = new("IsReadOnly", "bool"),
            ["MaxLength"] = new("MaxLength", "int"),
            ["SelectionStart"] = new("SelectionStart", "int"),
            ["WordWrap"] = new(
                "TextWrapping", "bool", ValueShape: BindableValueShape.BoolAsTextWrapping,
                AvaloniaTypeName: "TextWrapping"),
        };

    private static readonly Dictionary<string, Dictionary<string, BindableProperty>> ByControlType = new(StringComparer.Ordinal)
    {
        ["TextBox"] = TextBoxProperties,
        ["MaskedTextBox"] = TextBoxProperties,
        ["RichTextBox"] = TextBoxProperties,
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

    /// <summary>
    /// Universal properties that a particular target does <em>not</em> have after all.
    /// </summary>
    /// <remarks>
    /// Every WinForms <c>Control</c> has <c>Enabled</c>, so the universal table offers it for
    /// everything - but a DataGrid column is not a control. It is a description of a column, it
    /// lives in <c>DataGrid.Columns</c> rather than in the visual tree, and it has an
    /// <c>IsVisible</c> but no <c>IsEnabled</c>. Offering one produced a <c>{Binding}</c> against
    /// a property that does not exist, which is an AVLN2000 in the generated project.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> UniversalExclusions =
        new[]
        {
            "ColumnHeader",
            "DataGridViewButtonColumn",
            "DataGridViewCheckBoxColumn",
            "DataGridViewComboBoxColumn",
            "DataGridViewImageColumn",
            "DataGridViewLinkColumn",
            "DataGridViewTextBoxColumn",
        }.ToDictionary(
            t => t,
            _ => (IReadOnlySet<string>)new HashSet<string>(StringComparer.Ordinal) { "Enabled" },
            StringComparer.Ordinal);

    public static bool TryGet(string winFormsControlTypeName, string propertyName, out BindableProperty property)
    {
        if (ByControlType.TryGetValue(winFormsControlTypeName, out var typeProperties)
            && typeProperties.TryGetValue(propertyName, out property))
        {
            return true;
        }

        if (UniversalExclusions.TryGetValue(winFormsControlTypeName, out var excluded)
            && excluded.Contains(propertyName))
        {
            property = default;
            return false;
        }

        return UniversalProperties.TryGetValue(propertyName, out property);
    }

    /// <summary>
    /// The type-specific entries, as (WinForms type, WinForms property, mapping).
    /// </summary>
    /// <remarks>
    /// Exposed for WinFormsToAvalonia.Mapping.Tests, which checks both halves of every entry
    /// against Avalonia: that the property exists on the element the mapper emits, and that
    /// <see cref="BindableProperty.AvaloniaTypeName"/> is what it really is. Getting the second
    /// wrong is a CS0266 in the generated project - it happened, more than once, before this was
    /// checkable.
    /// </remarks>
    public static IEnumerable<(string WinFormsTypeName, string PropertyName, BindableProperty Property)> TypeSpecificEntries =>
        ByControlType.SelectMany(t => t.Value.Select(p => (t.Key, p.Key, p.Value)));

    /// <summary>The entries that apply to every control, whatever its type.</summary>
    public static IEnumerable<(string PropertyName, BindableProperty Property)> UniversalEntries =>
        UniversalProperties.Select(e => (e.Key, e.Value));

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
