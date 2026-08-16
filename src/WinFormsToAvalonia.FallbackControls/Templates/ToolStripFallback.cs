using Avalonia.Controls;
using Avalonia.Media;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms ToolStrip: Avalonia has no built-in toolbar control. A horizontal
/// StackPanel (same shape as ToolStripPanelFallback) so its now-parsed ToolStripButton/Label/
/// ComboBox/TextBox/ProgressBar items (see DefaultControlMappers) render as real children,
/// with a light background for a toolbar-like look.
/// </summary>
public class ToolStripFallback : StackPanel
{
    public ToolStripFallback()
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal;
        Background = Brushes.WhiteSmoke;
    }
}
