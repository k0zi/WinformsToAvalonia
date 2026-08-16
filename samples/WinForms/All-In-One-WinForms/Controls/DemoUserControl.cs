using System.ComponentModel;

namespace AllInOneWinForms.Controls;

/// <summary>
/// A composite UserControl - the second artifact kind a WinForms project can contain.
/// </summary>
public partial class DemoUserControl : UserControl
{
    public DemoUserControl()
    {
        InitializeComponent();
    }

    [DefaultValue("Demo user control")]
    public string Caption
    {
        get => this.captionLabel.Text;
        set => this.captionLabel.Text = value;
    }

    private void incrementButton_Click(object? sender, EventArgs e)
    {
        this.counterLabel.Text = (int.Parse(this.counterLabel.Text) + 1).ToString();
    }
}
