using Avalonia.Controls;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms ToolStripContainer: builds the same fixed 5-region layout WinForms'
/// own ToolStripContainer constructor creates automatically - four docked
/// ToolStripPanelFallback strips around a central ToolStripContentPanelFallback - and exposes
/// each region as a public property, mirroring the WinForms API shape so code-behind written
/// against `toolStripContainer1.ContentPanel`/`.TopToolStripPanel`/etc. has a direct
/// equivalent to migrate to.
/// </summary>
/// <remarks>
/// The five regions are settable, which is the only reason the conversion can fill them: XAML
/// property-element syntax (<c>&lt;ToolStripContainerFallback.ContentPanel&gt;</c>) assigns a
/// panel, and a get-only property could not receive one. Each setter rebuilds the child list
/// rather than swapping in place, because a DockPanel lays its children out in order and the
/// content panel has to stay last - that is what gives it the space the strips did not take
/// (<c>LastChildFill</c>). Rebuilding is a handful of children and cannot get the order wrong.
/// </remarks>
public class ToolStripContainerFallback : DockPanel
{
    private ToolStripPanelFallback _topToolStripPanel = new();
    private ToolStripPanelFallback _bottomToolStripPanel = new();
    private ToolStripPanelFallback _leftToolStripPanel = new() { Orientation = Avalonia.Layout.Orientation.Vertical };
    private ToolStripPanelFallback _rightToolStripPanel = new() { Orientation = Avalonia.Layout.Orientation.Vertical };
    private ToolStripContentPanelFallback _contentPanel = new();

    public ToolStripContainerFallback() => Rebuild();

    public ToolStripPanelFallback TopToolStripPanel
    {
        get => _topToolStripPanel;
        set => Replace(ref _topToolStripPanel, value);
    }

    public ToolStripPanelFallback BottomToolStripPanel
    {
        get => _bottomToolStripPanel;
        set => Replace(ref _bottomToolStripPanel, value);
    }

    public ToolStripPanelFallback LeftToolStripPanel
    {
        get => _leftToolStripPanel;
        set => Replace(ref _leftToolStripPanel, value);
    }

    public ToolStripPanelFallback RightToolStripPanel
    {
        get => _rightToolStripPanel;
        set => Replace(ref _rightToolStripPanel, value);
    }

    public ToolStripContentPanelFallback ContentPanel
    {
        get => _contentPanel;
        set => Replace(ref _contentPanel, value);
    }

    private void Replace<T>(ref T slot, T panel)
        where T : Control
    {
        if (panel is null || ReferenceEquals(slot, panel))
        {
            return;
        }

        slot = panel;
        Rebuild();
    }

    /// <summary>
    /// Docks every region and puts the content panel last, which is where a DockPanel gives the
    /// remaining space.
    /// </summary>
    private void Rebuild()
    {
        Children.Clear();

        DockPanel.SetDock(_topToolStripPanel, Dock.Top);
        DockPanel.SetDock(_bottomToolStripPanel, Dock.Bottom);
        DockPanel.SetDock(_leftToolStripPanel, Dock.Left);
        DockPanel.SetDock(_rightToolStripPanel, Dock.Right);

        Children.Add(_topToolStripPanel);
        Children.Add(_bottomToolStripPanel);
        Children.Add(_leftToolStripPanel);
        Children.Add(_rightToolStripPanel);
        Children.Add(_contentPanel);
    }
}
