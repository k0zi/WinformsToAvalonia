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
    private static IReadOnlySet<string> TextBoxMembers { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Text", "Clear", "SelectAll",

            // Inherited from TemplatedControl by way of TextBox, so a font really can be written
            // to one of these - which is what lets a FontDialog result reach a RichTextBox.
            "FontFamily", "FontSize", "FontWeight", "FontStyle",

            // TextBox's own, which is what a WinForms WordWrap becomes.
            "TextWrapping", "AcceptsReturn", "IsReadOnly", "MaxLength", "SelectionStart",
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> MembersByTemplateKey =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            // Both derive from Avalonia's TextBox, so these are inherited and real.
            ["RichTextBoxFallback"] = TextBoxMembers,
            ["MaskedTextBoxFallback"] = TextBoxMembers,
        };

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
