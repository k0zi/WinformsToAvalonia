namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The WinForms members that only mean anything in a multiple-document interface.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has no MDI at all - no child-window surface, no <c>LayoutMdi</c>, nothing to arrange.
/// That is not a gap this converter can close: MDI is a window-management model, and Avalonia's is
/// separate top-level windows.
/// </para>
/// <para>
/// The table exists so the conversion can <em>say</em> so. Before it, an MDI application converted
/// in total silence: <c>this.IsMdiContainer = true;</c> was a Form property with no counterpart, so
/// it was dropped without a word, and <c>LayoutMdi(...)</c> in a handler simply stopped the body
/// with no explanation of why. A developer reading the output had no way to tell that the thing
/// their application is built around had not been converted.
/// </para>
/// </remarks>
public static class MdiMemberCatalog
{
    private static readonly IReadOnlySet<string> Members = new HashSet<string>(StringComparer.Ordinal)
    {
        // Designer/form properties.
        "IsMdiContainer",
        "MdiParent",

        // Read or called from handler bodies.
        "MdiChildren",
        "ActiveMdiChild",
        "LayoutMdi",
    };

    public static bool IsMdiMember(string memberName) => Members.Contains(memberName);

    /// <summary>Every name, for the test that holds them up against WinForms' own metadata.</summary>
    public static IEnumerable<string> All => Members;

    /// <summary>What the conversion tells a reader when it finds one.</summary>
    public static string GuidanceFor(string formClassName, IEnumerable<string> foundMembers) =>
        $"'{formClassName}' uses MDI ({string.Join(", ", foundMembers)}). Avalonia has no multiple-document "
        + "interface at all - no child-window surface and no LayoutMdi - so none of it is converted, and the "
        + "handlers that arrange or enumerate child windows are left whole. The usual replacements are separate "
        + "top-level Windows, a TabControl if the children were really just pages, or a docking library.";
}
