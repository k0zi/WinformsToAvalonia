using System.ComponentModel;

namespace All_In_One_WinForms.Components;

/// <summary>
/// A hand-written, designer-hostable component - the third artifact kind
/// (Form / UserControl / Component) a WinForms project can contain.
/// </summary>
public partial class DemoComponent : Component
{
    private int tickCount;

    public DemoComponent()
    {
        InitializeComponent();
    }

    public DemoComponent(IContainer container)
    {
        container.Add(this);
        InitializeComponent();
    }

    [DefaultValue("Demo")]
    public string Caption { get; set; } = "Demo";

    public int TickCount => this.tickCount;

    public event EventHandler? Ticked;

    public void Tick()
    {
        this.tickCount++;
        Ticked?.Invoke(this, EventArgs.Empty);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
    }

    private System.ComponentModel.IContainer? components;
}
