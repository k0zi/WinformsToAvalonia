using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms ToolStripPanel: a single docking strip that lays out toolbars in a
/// row or column depending on which side of a form/container it's placed on. Avalonia's
/// StackPanel already provides exactly that layout behavior (and its own Orientation
/// property), so this just fixes the default to Horizontal - the common top/bottom-docked
/// case; set Orientation="Vertical" explicitly for a left/right-docked strip.
/// </summary>
public class ToolStripPanelFallback : StackPanel
{
    /// <remarks>
    /// The spacing and the centring are the strip: without them a horizontal StackPanel butts
    /// its children straight up against each other and stretches them to its full height, so
    /// the sample's two status labels rendered as the single word "ReadyAll-In-One WinForms
    /// control gallery" pinned to the top edge. WinForms lays these out with a margin per item
    /// and centres them in the strip; this is that, in the two properties Avalonia spells it
    /// with.
    /// </remarks>
    public ToolStripPanelFallback()
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal;
        Spacing = 6;

        Styles.Add(new Style(x => x.OfType<ToolStripPanelFallback>().Child().Is<Control>())
        {
            Setters = { new Setter(VerticalAlignmentProperty, VerticalAlignment.Center) },
        });
    }
}
