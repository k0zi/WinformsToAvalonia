namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// What a handler body may say about a <c>NotifyIcon</c> that this conversion turned into an
/// app-level <c>TrayIcon</c>.
/// </summary>
/// <remarks>
/// <para>
/// A NotifyIcon has no per-View element in Avalonia - it lives in <c>App.axaml</c>'s
/// <c>TrayIcon.Icons</c> - so <c>notifyIcon1.Visible = false;</c> had nothing to name.
/// <c>AvaloniaProjectScaffolder</c> now emits a static accessor per emitted icon, and this table
/// is the small vocabulary that reaches it. Showing and hiding the tray icon is what a WinForms
/// app does with one, which is why it is worth naming at all.
/// </para>
/// <para>
/// Only for an icon whose file the conversion actually resolved. An unresolved one is emitted as
/// a commented-out block, so there is no accessor and nothing to write to - see
/// <c>AvaloniaProjectScaffolder.BuildTrayIconAccessors</c>.
/// </para>
/// </remarks>
public static class TrayIconMemberCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Properties =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Visible"] = "IsVisible",

            // WinForms' Text is the hover tooltip, which is exactly what ToolTipText is.
            ["Text"] = "ToolTipText",
        };

    public static bool TryGet(string winFormsPropertyName, out string avaloniaPropertyName) =>
        Properties.TryGetValue(winFormsPropertyName, out avaloniaPropertyName!);

    /// <summary>
    /// Every entry, for the checks in WinFormsToAvalonia.Mapping.Tests - this converter never
    /// references Avalonia, so nothing else here can tell whether a TrayIcon has these.
    /// </summary>
    public static IEnumerable<(string WinFormsPropertyName, string AvaloniaPropertyName)> AllEntries =>
        Properties.Select(e => (e.Key, e.Value));
}
