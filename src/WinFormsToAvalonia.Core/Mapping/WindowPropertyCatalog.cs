namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// A property of the <c>Form</c> itself that has an exact <c>Avalonia.Controls.Window</c>
/// counterpart - the window-level sibling of <see cref="BindablePropertyCatalog"/>.
/// </summary>
/// <param name="AvaloniaPropertyName">The property to emit.</param>
/// <param name="ClrTypeName">
/// The Avalonia property's type, which decides whether a *read* needs the null-guard every
/// string read in this converter gets.
/// </param>
/// <param name="EnumTypeName">
/// Set when the value is an enum whose WinForms and Avalonia spellings differ
/// (<c>FormWindowState.Maximized</c> vs <c>WindowState.Maximized</c>). The member names in
/// <paramref name="EnumMemberNames"/> are the ones that mean the same thing in both.
/// </param>
public sealed record WindowProperty(
    string AvaloniaPropertyName,
    string ClrTypeName,
    string? EnumTypeName = null,
    IReadOnlySet<string>? EnumMemberNames = null);

/// <summary>
/// Deliberately small, and for the same reason <see cref="BindablePropertyCatalog"/> is: a
/// property is in here only when the two frameworks mean the same thing by it.
/// </summary>
/// <remarks>
/// <para>
/// The notable absentees are the size ones. WinForms' <c>Form.Size</c>/<c>Width</c>/<c>Height</c>
/// measure the *outer frame*, including the title bar and borders; Avalonia's are the window's
/// own size, and the difference is whatever the current desktop theme draws. There is no fixed
/// conversion, so translating one to the other would be a guess that silently resizes every
/// converted window - and <c>ClientSize</c>, the one that *is* comparable, is not what designer
/// code usually sets.
/// </para>
/// <para>
/// <c>FormBorderStyle</c>, <c>ControlBox</c>, <c>MaximizeBox</c> and <c>StartPosition</c> are out
/// for the sharper version of the same problem: Avalonia expresses those through
/// <c>SystemDecorations</c>, <c>CanResize</c> and <c>WindowStartupPosition</c>, and the mapping
/// between them is many-to-many rather than one-to-one.
/// </para>
/// </remarks>
public static class WindowPropertyCatalog
{
    private static readonly IReadOnlySet<string> WindowStateMembers =
        new HashSet<string>(StringComparer.Ordinal) { "Normal", "Maximized", "Minimized" };

    private static readonly IReadOnlyDictionary<string, WindowProperty> Properties =
        new Dictionary<string, WindowProperty>(StringComparer.Ordinal)
        {
            ["Text"] = new("Title", "string"),
            ["TopMost"] = new("Topmost", "bool"),
            ["ShowInTaskbar"] = new("ShowInTaskbar", "bool"),
            ["Opacity"] = new("Opacity", "double"),

            // FormWindowState has exactly these three members, and Avalonia's WindowState spells
            // all three identically. (Avalonia adds FullScreen, which WinForms has no name for -
            // that asymmetry only ever means a value we never produce.)
            ["WindowState"] = new("WindowState", "enum", "WindowState", WindowStateMembers),
        };

    public static bool TryGet(string winFormsPropertyName, out WindowProperty property) =>
        Properties.TryGetValue(winFormsPropertyName, out property!);
}
