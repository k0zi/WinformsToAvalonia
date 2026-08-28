using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms ToolStrip: Avalonia has no built-in toolbar control. A horizontal
/// StackPanel (same shape as ToolStripPanelFallback) so its now-parsed ToolStripButton/Label/
/// ComboBox/TextBox/ProgressBar items (see DefaultControlMappers) render as real children,
/// with a light background for a toolbar-like look.
/// </summary>
public class ToolStripFallback : StackPanel
{
    /// <remarks>
    /// The spacing and the centring are the strip: without them a horizontal StackPanel butts
    /// its children straight up against each other and stretches them to its full height, so
    /// the sample's two status labels rendered as the single word "ReadyAll-In-One WinForms
    /// control gallery" pinned to the top edge. WinForms lays these out with a margin per item
    /// and centres them in the strip; this is that, in the two properties Avalonia spells it
    /// with.
    /// </remarks>
    public ToolStripFallback()
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal;
        Spacing = 6;
        Background = Brushes.WhiteSmoke;

        Styles.Add(new Style(x => x.OfType<ToolStripFallback>().Child().Is<Control>())
        {
            Setters = { new Setter(VerticalAlignmentProperty, VerticalAlignment.Center) },
        });
    }
}
