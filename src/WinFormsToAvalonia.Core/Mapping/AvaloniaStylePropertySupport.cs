namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>The visual-style attribute groups an Avalonia element is able to carry.</summary>
[Flags]
public enum AvaloniaStyleProperties
{
    None = 0,

    /// <summary>Background.</summary>
    Background = 1,

    /// <summary>Foreground.</summary>
    Foreground = 2,

    /// <summary>FontFamily / FontSize / FontWeight / FontStyle.</summary>
    Font = 4,

    /// <summary>Padding.</summary>
    Padding = 8,

    /// <summary>TextDecorations (Underline / Strikethrough).</summary>
    TextDecorations = 16,

    /// <summary>Everything a TemplatedControl exposes.</summary>
    Templated = Background | Foreground | Font | Padding,
}

/// <summary>
/// Which visual-style attributes each Avalonia element this converter emits actually has -
/// the styling counterpart of <see cref="BindablePropertyCatalog"/>, and deliberately the same
/// shape: a small, explicit, hand-maintained table rather than a derived rule.
/// </summary>
/// <remarks>
/// This table is a correctness requirement, not an optimization. Every WinForms
/// <c>Control</c> has <c>BackColor</c>/<c>ForeColor</c>/<c>Font</c>/<c>Padding</c>, but their
/// Avalonia counterparts are spread across unrelated base types: a <c>Panel</c> (what every
/// WinForms container maps to) has a Background but no Foreground or font properties at all,
/// and an <c>Image</c> (the <c>PictureBox</c> target) has none of them. Emitting an attribute
/// the target element does not define is an Avalonia XAML *compile* error (AVLN2000) in the
/// generated project - the same failure class the fallback-control event-wiring rule exists to
/// avoid (see docs/known-limitations.md).
///
/// Unknown element names resolve to <see cref="AvaloniaStyleProperties.None"/> on purpose: a
/// new mapper target emits no styling until it is listed here, which loses fidelity but can
/// never break the generated build. Prefixed elements (a generated UserControl View emitted as
/// <c>uc0:FooView</c>) take that path too - the WinForms UserControl's own designer already
/// styles the control's insides, so restyling the host element would fight it.
/// </remarks>
public static class AvaloniaStylePropertySupport
{
    private static readonly IReadOnlyDictionary<string, AvaloniaStyleProperties> ByElementName =
        new Dictionary<string, AvaloniaStyleProperties>(StringComparer.Ordinal)
        {
            // Conversion roots.
            ["Window"] = AvaloniaStyleProperties.Templated,
            ["UserControl"] = AvaloniaStyleProperties.Templated,

            // TextBlock is not a TemplatedControl but defines the same styling surface itself,
            // plus the TextDecorations that carry WinForms' Underline/Strikeout font styles.
            ["TextBlock"] = AvaloniaStyleProperties.Templated | AvaloniaStyleProperties.TextDecorations,

            // Panels: a Background, and nothing else. Every WinForms container control
            // (Panel/GroupBox/TableLayoutPanel/FlowLayoutPanel/TabPage/...) lands here via the
            // project's fixed Canvas-everywhere layout strategy.
            ["Canvas"] = AvaloniaStyleProperties.Background,
            ["Grid"] = AvaloniaStyleProperties.Background,

            // Image derives straight from Control - no styling surface whatsoever.
            ["Image"] = AvaloniaStyleProperties.None,

            // TemplatedControls.
            ["Button"] = AvaloniaStyleProperties.Templated,
            ["Calendar"] = AvaloniaStyleProperties.Templated,
            ["CalendarDatePicker"] = AvaloniaStyleProperties.Templated,
            ["CheckBox"] = AvaloniaStyleProperties.Templated,
            ["ComboBox"] = AvaloniaStyleProperties.Templated,
            ["DataGrid"] = AvaloniaStyleProperties.Templated,
            ["GridSplitter"] = AvaloniaStyleProperties.Templated,
            ["HyperlinkButton"] = AvaloniaStyleProperties.Templated,
            ["ListBox"] = AvaloniaStyleProperties.Templated,
            ["Menu"] = AvaloniaStyleProperties.Templated,
            ["MenuItem"] = AvaloniaStyleProperties.Templated,
            ["NumericUpDown"] = AvaloniaStyleProperties.Templated,
            ["ProgressBar"] = AvaloniaStyleProperties.Templated,
            ["RadioButton"] = AvaloniaStyleProperties.Templated,
            ["ScrollBar"] = AvaloniaStyleProperties.Templated,
            ["Separator"] = AvaloniaStyleProperties.Templated,
            ["Slider"] = AvaloniaStyleProperties.Templated,
            ["SplitButton"] = AvaloniaStyleProperties.Templated,
            ["TabControl"] = AvaloniaStyleProperties.Templated,
            ["TabItem"] = AvaloniaStyleProperties.Templated,
            ["TextBox"] = AvaloniaStyleProperties.Templated,
            ["TreeView"] = AvaloniaStyleProperties.Templated,
        };

    public static AvaloniaStyleProperties For(string? avaloniaElementName) =>
        avaloniaElementName is not null && ByElementName.TryGetValue(avaloniaElementName, out var supported)
            ? supported
            : AvaloniaStyleProperties.None;

    public static bool Supports(string? avaloniaElementName, AvaloniaStyleProperties property) =>
        For(avaloniaElementName).HasFlag(property);
}
