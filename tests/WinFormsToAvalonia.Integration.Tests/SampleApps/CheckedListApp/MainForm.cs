namespace CheckedListApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void tickButton_Click(object sender, EventArgs e)
    {
        // Written from code rather than by the user clicking, which is the direction that needs
        // the row type to raise a change notification - otherwise the box on screen never moves.
        this.optionsList.SetItemChecked(1, true);
    }
}
