using Avalonia.Controls;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms ToolStripContentPanel: a plain, chrome-less content-hosting region
/// (WinForms' own ToolStripContentPanel has no distinguishing visuals either - it's just the
/// panel a ToolStripContainer wraps around the form's real content). A Grid subclass so
/// children placed on it behave like any other Avalonia panel; used standalone or as the
/// center region of ToolStripContainerFallback.
/// </summary>
public class ToolStripContentPanelFallback : Grid
{
}
