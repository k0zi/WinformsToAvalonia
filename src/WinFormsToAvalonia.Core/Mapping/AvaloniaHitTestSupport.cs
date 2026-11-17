namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Which emitted elements are invisible to the pointer until they are given a background - and
/// which events make that matter.
/// </summary>
/// <remarks>
/// <para>
/// A WinForms control receives mouse and drag events anywhere inside its bounds. An Avalonia
/// element is only hit where it draws: a <c>Panel</c> or <c>Border</c> with no <c>Background</c>
/// draws nothing and is hit nowhere, and a <c>TextBlock</c> is hit on its glyphs and nowhere else.
/// Every WinForms container becomes one of the first, every Label the second, and the designer only
/// writes <c>BackColor</c> when it differs from the default - so a Panel with a <c>MouseDown</c>
/// handler, a drop target, or a clickable Label converted into an element whose handlers (nearly)
/// never ran. It compiled, started, and passed every test.
/// </para>
/// <para>
/// Measured, not assumed: a headless hit test at the centre of a converted drop target landed on
/// nothing until it had a background, and at the centre of a one-word TextBlock likewise.
/// <c>Background="Transparent"</c> is the fix - hit everywhere, drawing nothing. The startup tests
/// repeat that hit test on every converted drop target, and
/// <c>WinFormsToAvalonia.Mapping.Tests</c> checks every element listed here has a Background.
/// </para>
/// </remarks>
public static class AvaloniaHitTestSupport
{
    /// <summary>Elements that are only hit where they paint a background.</summary>
    public static IReadOnlySet<string> NeedBackgroundToBeHit { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "Canvas", "StackPanel", "Grid", "DockPanel", "WrapPanel", "Panel", "Border", "TextBlock",
    };

    /// <summary>Avalonia events that only reach an element the pointer can hit.</summary>
    public static IReadOnlySet<string> PointerEvents { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "PointerPressed", "PointerReleased", "PointerMoved", "PointerEntered", "PointerExited",
        "PointerWheelChanged", "Tapped", "DoubleTapped",
        "DragEnter", "DragOver", "DragLeave", "Drop",
    };

    /// <summary>The attached property that makes an element a drop target at all.</summary>
    public const string AllowDropAttribute = "DragDrop.AllowDrop";
}
