using System;
using System.Windows.Forms;

namespace UserControlApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void applyButton_Click(object? sender, EventArgs e)
    {
        // Writing and reading a property of the hosted UserControl's generated View.
        this.counterControl1.Caption = this.titleLabel.Text;
        this.titleLabel.Text = this.counterControl1.Caption;
    }
}
