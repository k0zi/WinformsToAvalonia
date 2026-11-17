using System;
using System.Windows.Forms;

namespace UserControlApp.Controls;

public partial class CounterControl : UserControl
{
    public CounterControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The public surface a WinForms UserControl is normally made of - a property over one of its
    /// own controls. Both accessors translate, so the generated View carries it for real and the
    /// hosting Form's handler can name it.
    /// </summary>
    public string Caption
    {
        get => this.counterLabel.Text;
        set => this.counterLabel.Text = value;
    }

    /// <summary>Getter-only, and expression-bodied - the other two shapes a property comes in.</summary>
    public int Count => int.Parse(this.counterLabel.Text);

    /// <summary>
    /// The negative case: the getter would translate on its own, the setter never can. A property
    /// is whole-or-nothing, so neither half is emitted and this stays a comment - otherwise
    /// assigning to it would silently do nothing.
    /// </summary>
    public string Tooltip
    {
        get => this.counterLabel.Text;
        set => this.counterLabel.AccessibleDescription = value;
    }

    private void incrementButton_Click(object? sender, EventArgs e)
    {
        this.counterLabel.Text = (int.Parse(this.counterLabel.Text) + 1).ToString();
    }
}
