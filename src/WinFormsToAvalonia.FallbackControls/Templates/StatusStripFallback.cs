using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms StatusStrip: Avalonia has no built-in status bar. A horizontal
/// StackPanel (same shape as ToolStripPanelFallback) so its now-parsed ToolStripStatusLabel
/// items (see DefaultControlMappers) render as real children, with a light background for a
/// status-bar-like look.
/// </summary>
public class StatusStripFallback : StackPanel
{
    /// <remarks>
    /// The spacing and the centring are the strip: without them a horizontal StackPanel butts
    /// its children straight up against each other and stretches them to its full height, so
    /// the sample's two status labels rendered as the single word "ReadyAll-In-One WinForms
    /// control gallery" pinned to the top edge. WinForms lays these out with a margin per item
    /// and centres them in the strip; this is that, in the two properties Avalonia spells it
    /// with.
    /// </remarks>
    public StatusStripFallback()
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal;
        Spacing = 6;
        Background = Brushes.WhiteSmoke;

        Styles.Add(new Style(x => x.OfType<StatusStripFallback>().Child().Is<Control>())
        {
            Setters = { new Setter(VerticalAlignmentProperty, VerticalAlignment.Center) },
        });
    }
}
