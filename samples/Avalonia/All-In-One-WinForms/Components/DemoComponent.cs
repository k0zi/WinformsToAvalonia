using System;
using System.ComponentModel;

namespace All_In_One_WinForms.Components;

/// <summary>
/// Migrated form of the WinForms <c>DemoComponent</c>. Avalonia has no
/// <see cref="System.ComponentModel.Component"/>/designer-container model, so the component
/// becomes a plain class the View news up itself - everything the original exposed
/// (<see cref="Caption"/>, <see cref="TickCount"/>, <see cref="Ticked"/>, <see cref="Tick"/>)
/// is unchanged, which is all the PropertyGrid fallback and the Advanced tab need.
/// </summary>
public sealed class DemoComponent
{
    private int tickCount;

    [DefaultValue("Demo")]
    public string Caption { get; set; } = "Demo";

    public int TickCount => this.tickCount;

    public event EventHandler? Ticked;

    public void Tick()
    {
        this.tickCount++;
        Ticked?.Invoke(this, EventArgs.Empty);
    }
}
