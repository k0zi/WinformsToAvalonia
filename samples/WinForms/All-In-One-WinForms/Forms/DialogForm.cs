namespace AllInOneWinForms.Forms;

/// <summary>
/// A second Form, opened with <see cref="Form.ShowDialog()"/> from the main window - it makes
/// the owning handler navigation code, which is exactly the kind of handler that can never be
/// promoted to a ViewModel command.
/// </summary>
public partial class DialogForm : Form
{
    public DialogForm()
    {
        InitializeComponent();
    }

    public string EnteredText => this.inputTextBox.Text;

    private void okButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    private void cancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
