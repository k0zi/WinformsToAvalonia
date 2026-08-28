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

    /// <summary>
    /// The NotifyIcon events a generated View's constructor can subscribe on the TrayIcon.
    /// </summary>
    /// <remarks>
    /// Stated here as well as in <c>EventMappingRegistry</c>'s per-type overrides because a
    /// <c>SubscribeInCode</c> mapping is skipped by every one of the checks in
    /// <c>EventMappingRegistryAgainstAvaloniaTests</c> - they cannot know what type declares the
    /// event. This table names the type, so it is the one that can be held up against the real
    /// <c>TrayIcon</c>. Two tables for one fact, so they come with a test that they agree.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, string> Events =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Click"] = "Clicked",
        };

    public static bool TryGetEvent(string winFormsEventName, out string avaloniaEventName) =>
        Events.TryGetValue(winFormsEventName, out avaloniaEventName!);

    /// <summary>Every event entry, for the same reason as <see cref="AllEntries"/>.</summary>
    public static IEnumerable<(string WinFormsEventName, string AvaloniaEventName)> AllEventEntries =>
        Events.Select(e => (e.Key, e.Value));

    public static bool TryGet(string winFormsPropertyName, out string avaloniaPropertyName) =>
        Properties.TryGetValue(winFormsPropertyName, out avaloniaPropertyName!);

    /// <summary>
    /// Every entry, for the checks in WinFormsToAvalonia.Mapping.Tests - this converter never
    /// references Avalonia, so nothing else here can tell whether a TrayIcon has these.
    /// </summary>
    public static IEnumerable<(string WinFormsPropertyName, string AvaloniaPropertyName)> AllEntries =>
        Properties.Select(e => (e.Key, e.Value));
}
