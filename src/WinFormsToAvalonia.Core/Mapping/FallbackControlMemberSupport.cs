namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Which catalog members - properties and methods alike - each bundled fallback control actually
/// exposes.
/// </summary>
/// <remarks>
/// <para>
/// Everything else about fallback controls is deliberately conservative - they get no styling, no
/// event wiring, no item children - because a template does not necessarily have the property a
/// mapping names, and a wrong attribute is an AVLN error in the generated project. Bindable
/// properties are the one case where that caution can be lifted safely: these templates are
/// *ours*, shipped in this repo, so what they expose is a known fact rather than a guess.
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
        new HashSet<string>(StringComparer.Ordinal) { "Text", "Clear", "SelectAll" };

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
}
