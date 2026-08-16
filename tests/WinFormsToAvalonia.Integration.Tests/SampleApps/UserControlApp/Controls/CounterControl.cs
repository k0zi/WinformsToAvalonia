using System;
using System.Windows.Forms;

namespace UserControlApp.Controls;

public partial class CounterControl : UserControl
{
    public CounterControl()
    {
        InitializeComponent();
    }

    private void incrementButton_Click(object? sender, EventArgs e)
    {
        this.counterLabel.Text = (int.Parse(this.counterLabel.Text) + 1).ToString();
    }
}
