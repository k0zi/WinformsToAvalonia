using Avalonia.Controls;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms ToolStripContainer: builds the same fixed 5-region layout WinForms'
/// own ToolStripContainer constructor creates automatically - four docked
/// ToolStripPanelFallback strips around a central ToolStripContentPanelFallback - and exposes
/// each region as a public property, mirroring the WinForms API shape so code-behind written
/// against `toolStripContainer1.ContentPanel`/`.TopToolStripPanel`/etc. has a direct
/// equivalent to migrate to. Nested content (originally added via e.g.
/// `this.toolStripContainer1.ContentPanel.Controls.Add(x)`) is not migrated automatically -
/// that three-level member-access chain isn't parsed yet (see docs/known-limitations.md); add
/// children to the exposed regions by hand.
/// </summary>
public class ToolStripContainerFallback : DockPanel
{
    public ToolStripPanelFallback TopToolStripPanel { get; }

    public ToolStripPanelFallback BottomToolStripPanel { get; }

    public ToolStripPanelFallback LeftToolStripPanel { get; }

    public ToolStripPanelFallback RightToolStripPanel { get; }

    public ToolStripContentPanelFallback ContentPanel { get; }

    public ToolStripContainerFallback()
    {
        TopToolStripPanel = new ToolStripPanelFallback();
        DockPanel.SetDock(TopToolStripPanel, Dock.Top);

        BottomToolStripPanel = new ToolStripPanelFallback();
        DockPanel.SetDock(BottomToolStripPanel, Dock.Bottom);

        LeftToolStripPanel = new ToolStripPanelFallback { Orientation = Avalonia.Layout.Orientation.Vertical };
        DockPanel.SetDock(LeftToolStripPanel, Dock.Left);

        RightToolStripPanel = new ToolStripPanelFallback { Orientation = Avalonia.Layout.Orientation.Vertical };
        DockPanel.SetDock(RightToolStripPanel, Dock.Right);

        ContentPanel = new ToolStripContentPanelFallback();

        // Order matters for DockPanel: docked children first, the undocked last child fills
        // the remaining space (LastChildFill defaults to true).
        Children.Add(TopToolStripPanel);
        Children.Add(BottomToolStripPanel);
        Children.Add(LeftToolStripPanel);
        Children.Add(RightToolStripPanel);
        Children.Add(ContentPanel);
    }
}
