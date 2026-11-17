namespace FileDialogOptionsApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void openButton_Click(object? sender, EventArgs e)
    {
        if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.statusLabel.Text = this.openFileDialog1.FileName;
        }
    }

    private void saveButton_Click(object? sender, EventArgs e)
    {
        if (this.saveFileDialog1.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        this.statusLabel.Text = this.saveFileDialog1.FileName;
    }

    private void folderButton_Click(object? sender, EventArgs e)
    {
        // A bare call: nothing is done with the result, so it is not inlined - the folder dialog
        // reaches the generated helper method instead, which has to open it with the same options.
        this.folderBrowserDialog1.ShowDialog();
    }
}
