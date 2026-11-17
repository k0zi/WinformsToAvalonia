namespace LambdaApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    // Already taken: the lambda on goButton.Click has to be lifted under another name.
    private void goButton_Click(object? sender, EventArgs e)
    {
        this.statusLabel.Text = "Unused";
    }
}
