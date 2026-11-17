using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// What WinForms' <c>control.BackColor</c> reads: the control's own colour, or - when it never set
/// one - its parent's, and so on up to the window.
/// </summary>
/// <remarks>
/// <para>
/// WinForms' <c>BackColor</c> is an <em>ambient</em> property: a Label inside a blue panel answers
/// blue. Avalonia's <c>Background</c> is not inherited - unset, it reads <c>null</c> - so
/// <c>label.ForeColor = label.BackColor</c>, the classic way to hide a label's text, needs this walk
/// to mean the same thing. It stops at the first element that draws a background of its own -
/// not at a fully transparent one, which the conversion gives a clickable element (a TextBlock
/// with no background cannot be hit) and which lets the parent's colour show through anyway.
/// </para>
/// <para>
/// At the top it answers what the window's theme paints, where WinForms would answer
/// <c>SystemColors.Control</c>: the colour a form shows when nobody set one, in either framework.
/// </para>
/// <para>
/// A static helper rather than a control, pulled in by a converted handler body.
/// </para>
/// </remarks>
public static class AmbientBackColorFallback
{
    public static IBrush? Of(Visual control)
    {
        for (var current = control; current is not null; current = current.GetVisualParent())
        {
            var brush = current switch
            {
                Panel panel => panel.Background,
                TemplatedControl templated => templated.Background,
                TextBlock text => text.Background,
                Border border => border.Background,
                _ => null,
            };

            // A fully transparent solid brush is not a colour of its own: it is what the conversion
            // gives an element only so the pointer can hit it, and what shows is the parent's.
            if (brush is not null and not ISolidColorBrush { Color.A: 0 })
            {
                return brush;
            }
        }

        return null;
    }
}
