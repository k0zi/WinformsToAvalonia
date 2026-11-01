namespace WindowChromeApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void optionsButton_Click(object? sender, EventArgs e)
    {
        new OptionsForm().ShowDialog(this);
    }
}
