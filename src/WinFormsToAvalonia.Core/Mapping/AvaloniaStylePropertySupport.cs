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
            // Everything a TemplatedControl has except Padding, which the mapper owns outright -
            // see DefaultControlMappers. Two writers for one attribute would emit it twice, and a
            // duplicate XML attribute does not merge, it fails to parse.
            ["GroupBox"] = AvaloniaStyleProperties.Background
                | AvaloniaStyleProperties.Foreground
                | AvaloniaStyleProperties.Font,
            ["HyperlinkButton"] = AvaloniaStyleProperties.Templated,
            ["ListBox"] = AvaloniaStyleProperties.Templated,
            ["MaskedTextBox"] = AvaloniaStyleProperties.Templated,
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
            ["TimePicker"] = AvaloniaStyleProperties.Templated,
            ["TreeView"] = AvaloniaStyleProperties.Templated,
        };

    public static AvaloniaStyleProperties For(string? avaloniaElementName) =>
        avaloniaElementName is not null && ByElementName.TryGetValue(avaloniaElementName, out var supported)
            ? supported
            : AvaloniaStyleProperties.None;

    public static bool Supports(string? avaloniaElementName, AvaloniaStyleProperties property) =>
        For(avaloniaElementName).HasFlag(property);

    /// <summary>
    /// Every element this table has an opinion about. Exposed so WinFormsToAvalonia.Mapping.Tests
    /// can hold each claim up against the real element - the whole reason the table exists is
    /// that Avalonia spreads these properties across unrelated base types, and nothing in this
    /// repo can see them.
    /// </summary>
    public static IEnumerable<(string AvaloniaElementName, AvaloniaStyleProperties Supported)> AllEntries =>
        ByElementName.Select(e => (e.Key, e.Value));

    /// <summary>
    /// The style surface of a <b>bundled fallback template</b>, derived from what it exposes.
    /// </summary>
    /// <remarks>
    /// A fallback's element name is its template key, so <see cref="For"/> - keyed on real
    /// Avalonia element names - answers <see cref="AvaloniaStyleProperties.None"/> for it, and a
    /// designer's BackColor on a converted ToolStrip was dropped on that technicality. The
    /// templates ship in this repo, so what they have is a known fact:
    /// <see cref="FallbackControlMemberSupport"/> records it member by member, and a group is
    /// writable exactly when every member it is made of is there. That rule already governed
    /// handler bodies; this is the same answer, in one place, for the emitter too.
    /// </remarks>
    public static AvaloniaStyleProperties ForFallbackTemplate(string? fallbackTemplateKey)
    {
        var supported = AvaloniaStyleProperties.None;

        foreach (var group in new[]
        {
            AvaloniaStyleProperties.Background,
            AvaloniaStyleProperties.Foreground,
            AvaloniaStyleProperties.Font,
            AvaloniaStyleProperties.Padding,
            AvaloniaStyleProperties.TextDecorations,
        })
        {
            var members = MemberNamesOf(group);
            if (members.Count > 0 && members.All(m => FallbackControlMemberSupport.Exposes(fallbackTemplateKey, m)))
            {
                supported |= group;
            }
        }

        return supported;
    }

    /// <summary>
    /// The Avalonia properties one style group is actually made of - what a *fallback* template
    /// has to expose for the group to be writable on it, since that table is keyed by member.
    /// </summary>
    public static IReadOnlyList<string> MemberNamesOf(AvaloniaStyleProperties property) =>
        property switch
        {
            AvaloniaStyleProperties.Background => ["Background"],
            AvaloniaStyleProperties.Foreground => ["Foreground"],
            AvaloniaStyleProperties.Font => ["FontFamily", "FontSize", "FontWeight", "FontStyle"],
            AvaloniaStyleProperties.Padding => ["Padding"],
            AvaloniaStyleProperties.TextDecorations => ["TextDecorations"],

            // A combination is not one writable thing, so it names nothing.
            _ => [],
        };
}
