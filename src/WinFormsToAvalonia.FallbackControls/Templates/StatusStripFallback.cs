using Avalonia.Controls;
using Avalonia.Media;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms StatusStrip: Avalonia has no built-in status bar. A horizontal
/// StackPanel (same shape as ToolStripPanelFallback) so its now-parsed ToolStripStatusLabel
/// items (see DefaultControlMappers) render as real children, with a light background for a
/// status-bar-like look.
/// </summary>
public class StatusStripFallback : StackPanel
{
    public StatusStripFallback()
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal;
        Background = Brushes.WhiteSmoke;
    }
}
