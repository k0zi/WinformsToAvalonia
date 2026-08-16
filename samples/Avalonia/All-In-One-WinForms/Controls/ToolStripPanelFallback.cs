using Avalonia.Controls;

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
    public ToolStripPanelFallback()
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal;
    }
}
