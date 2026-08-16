namespace NestedFormApp.Forms;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void okButton_Click(object? sender, EventArgs e)
    {
        Close();
    }
}
