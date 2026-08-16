namespace ComplexApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void saveButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Saved!");
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        statusTimer.Start();
    }
}
