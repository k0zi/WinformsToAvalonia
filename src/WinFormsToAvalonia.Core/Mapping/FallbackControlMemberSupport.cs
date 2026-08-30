namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Which catalog members - properties and methods alike - each bundled fallback control actually
/// exposes.
/// </summary>
/// <remarks>
/// <para>
/// Everything else about fallback controls is deliberately conservative - no designer styling, no
/// event wiring, no item children - because a template does not necessarily have the property a
/// mapping names, and a wrong attribute is an AVLN error in the generated project. What a
/// *translated body* touches is the one case where that caution can be lifted safely: these
/// templates are ours, shipped in this repo, so what they expose is a known fact rather than a
/// guess. That covers the style groups too, via
/// <see cref="AvaloniaStylePropertySupport.MemberNamesOf"/> - a group is writable on a template
/// only when every member it is made of is listed here.
/// </para>
/// <para>
/// Only the entries below are known. Anything absent behaves exactly as before - the member is
/// neither bound, written nor called - so a new template is safe by default and opts in by being
/// listed. Keyed by Avalonia member name, matching what the catalogs return.
/// </para>
/// </remarks>
public static class FallbackControlMemberSupport
{
    /// <summary>
    /// What every template that derives from Avalonia's <c>Control</c> has, whatever else it adds.
    /// </summary>
    /// <remarks>
    /// These are the members <see cref="BindablePropertyCatalog.UniversalProperties"/> and
    /// <see cref="ControlMethodCatalog"/>'s universal methods translate to - the counterpart of
    /// "every WinForms Control has Enabled, Visible, Focus() and Refresh()". Every entry below
    /// unions this in, because a table that lists only a template's *own* members refuses
    /// `richTextBox1.Visible = false;` for no reason anyone could defend: the template is a
    /// StackPanel, a DockPanel, a Grid, a UserControl or a TextBox, and all five have them.
    /// Verified against Avalonia's metadata by FallbackControlMemberSupportTests.
    /// </remarks>
    private static IReadOnlySet<string> ControlMembers { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "IsVisible", "IsEnabled", "Focus", "InvalidateVisual",
        };

    /// <summary>
    /// What every <c>TemplatedControl</c>- and <c>UserControl</c>-derived template carries: the
    /// whole styling surface, so a designer's BackColor and Font reach it.
    /// </summary>
    private static IReadOnlySet<string> TemplatedStyleMembers { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Background", "Foreground", "Padding",
            "FontFamily", "FontSize", "FontWeight", "FontStyle",
        };

    /// <summary>A Panel declares Background and nothing else of the styling surface.</summary>
    private static IReadOnlySet<string> PanelStyleMembers { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "Background" };

    private static IReadOnlySet<string> TextBoxMembers { get; } =
        new HashSet<string>(ControlMembers.Concat(TemplatedStyleMembers), StringComparer.Ordinal)
        {
            "Text", "Clear", "SelectAll",

            // TextBox's own, which is what a WinForms WordWrap becomes.
            "TextWrapping", "AcceptsReturn", "IsReadOnly", "MaxLength", "SelectionStart",
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> MembersByTemplateKey =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            // Derives from Avalonia's TextBox, so these are inherited and real.
            ["RichTextBoxFallback"] = TextBoxMembers,

            // A template's *own* properties, on top of what Control gives it. These were
            // invisible for as long as this table existed, so every handler line touching one
            // refused - not because there was nowhere to translate it, but because nobody had
            // written the name down.
            // UserControl-derived, so the whole styling surface as well.
            ["PropertyGridFallback"] = Plus(TemplatedStyleMembers, "SelectedObject"),
            ["PrintPreviewControlFallback"] = Plus(TemplatedStyleMembers),
            ["WebBrowserFallback"] = Plus(TemplatedStyleMembers),

            // Panel-derived (StackPanel / DockPanel / Grid): a Background and nothing else of the
            // styling surface. Listing it is what stops a designer's BackColor being dropped on
            // the technicality that a template key is not an Avalonia element name.
            ["DomainUpDownFallback"] = Plus(PanelStyleMembers, "SelectedIndex", "Wrap"),
            // Position and Count are bound by the conversion itself, not by a handler body: no
            // WinForms BindingNavigator has either (the BindingSource does), so nothing on the
            // WinForms side can name them. They are listed because the *emitter* asks this same
            // table before it writes a binding onto a fallback - see AxamlEmitter's
            // FilterBindableForTarget, which silently drops one the template does not expose.
            ["BindingNavigatorFallback"] = Plus(PanelStyleMembers, "Position", "Count"),
            ["StatusStripFallback"] = Plus(PanelStyleMembers),
            ["ToolStripContainerFallback"] = Plus(PanelStyleMembers),
            ["ToolStripContentPanelFallback"] = Plus(PanelStyleMembers),
            ["ToolStripFallback"] = Plus(PanelStyleMembers),
            ["ToolStripPanelFallback"] = Plus(PanelStyleMembers),

            // ErrorProviderFallback is deliberately absent: it is an attached-property holder,
            // not a Control, so none of the above exists on it.
        };

    private static IReadOnlySet<string> Plus(IReadOnlySet<string> styleMembers, params string[] ownMembers) =>
        new HashSet<string>(ControlMembers.Concat(styleMembers).Concat(ownMembers), StringComparer.Ordinal);

    public static bool Exposes(string? fallbackTemplateKey, string avaloniaMemberName) =>
        fallbackTemplateKey is not null
        && MembersByTemplateKey.TryGetValue(fallbackTemplateKey, out var members)
        && members.Contains(avaloniaMemberName);

    /// <summary>
    /// Every claim this table makes, as (template key, Avalonia member).
    /// </summary>
    /// <remarks>
    /// Exposed so WinFormsToAvalonia.Mapping.Tests can check each one against the base class the
    /// template really derives from - these members are inherited, and "inherited" is a fact about
    /// Avalonia that this repo cannot otherwise see.
    /// </remarks>
    public static IEnumerable<(string TemplateKey, string MemberName)> AllEntries =>
        MembersByTemplateKey.SelectMany(t => t.Value.Select(m => (t.Key, m)));
}
