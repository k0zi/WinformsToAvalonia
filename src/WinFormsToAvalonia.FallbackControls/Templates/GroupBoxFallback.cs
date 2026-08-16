using Avalonia;
using Avalonia.Controls;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms GroupBox: Avalonia has no built-in bordered/headered panel that
/// also behaves like a plain Panel for child placement, so this is a Canvas subclass -
/// children keep using Canvas.Left/Canvas.Top exactly like every other converted container
/// - with a Header property carrying the original WinForms Text for reference.
/// </summary>
/// <remarks>
/// No decorative border/header chrome is drawn: Avalonia's Panel seals Render, so a
/// Canvas subclass can't hand-draw one the way a plain Control could. Style this the same
/// way you would any other Avalonia control (e.g. a Styles selector targeting
/// GroupBoxFallback) once you migrate it further.
/// </remarks>
public class GroupBoxFallback : Canvas
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<GroupBoxFallback, string?>(nameof(Header));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
}
