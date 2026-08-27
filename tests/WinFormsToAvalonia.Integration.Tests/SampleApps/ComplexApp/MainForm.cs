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

    // Avalonia raises SelectionChanged while the AXAML is still being populated - a TabControl
    // selects its first tab as it initializes - so this runs once inside InitializeComponent,
    // before a single x:Name field exists. It compiled, and then took the app down at startup.
    private void tabControl1_SelectedIndexChanged(object? sender, EventArgs e)
    {
        this.nameLabel.Text = this.tabControl1.SelectedTab?.Text ?? string.Empty;
    }
}
