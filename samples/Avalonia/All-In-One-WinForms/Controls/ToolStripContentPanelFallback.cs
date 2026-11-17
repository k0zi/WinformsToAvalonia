using Avalonia.Controls;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms ToolStripContentPanel: a plain, chrome-less content-hosting region
/// (WinForms' own ToolStripContentPanel has no distinguishing visuals either - it's just the
/// panel a ToolStripContainer wraps around the form's real content). Used standalone or as the
/// center region of ToolStripContainerFallback.
/// </summary>
/// <remarks>
/// A <c>Canvas</c>, like every other container this conversion emits: the controls WinForms put
/// on a content panel carry absolute coordinates, and a Grid would stack them all in cell 0,0.
/// It was a Grid while nothing was ever placed on it, which cost nothing and was wrong the
/// moment something was.
/// </remarks>
public class ToolStripContentPanelFallback : Canvas
{
}
