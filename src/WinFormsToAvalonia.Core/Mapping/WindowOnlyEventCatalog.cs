namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The Avalonia events a Form's own subscriptions map to that <b>only a Window declares</b>.
/// </summary>
/// <remarks>
/// <para>
/// Form-level subscriptions are emitted as attributes on the AXAML root element, and that is
/// fine as long as the root is a <c>Window</c>. Under <c>--with-web</c> the main Form's View is
/// rooted at a <c>UserControl</c> instead (Avalonia's browser backend has no windowing platform
/// at all), and an <c>Opened=</c> or <c>Closing=</c> attribute there is a compile error - so
/// these move to the generated wrapper Window, which forwards them into the View.
/// </para>
/// <para>
/// Everything <c>EventMappingRegistry</c>'s Form table produces that is <i>not</i> listed here -
/// <c>Loaded</c>, <c>SizeChanged</c> - is declared by <c>Control</c> and stays on the View, where
/// it also keeps working in the browser. <c>WindowOnlyEventCatalogTests</c> checks both halves
/// against Avalonia's own metadata.
/// </para>
/// </remarks>
public static class WindowOnlyEventCatalog
{
    private static readonly IReadOnlySet<string> Events = new HashSet<string>(StringComparer.Ordinal)
    {
        "Opened",
        "Closing",
        "Closed",
        "Activated",
        "Deactivated",
        "PositionChanged",
        "ScalingChanged",
    };

    public static bool IsWindowOnly(string avaloniaEventName) => Events.Contains(avaloniaEventName);

    public static IEnumerable<string> All => Events.OrderBy(e => e, StringComparer.Ordinal);
}
