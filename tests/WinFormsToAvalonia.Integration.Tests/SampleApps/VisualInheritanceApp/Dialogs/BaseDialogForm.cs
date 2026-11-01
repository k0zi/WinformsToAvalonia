namespace VisualInheritanceApp.Dialogs;

/// <summary>
/// A dialog shell with its own designer: a header and an OK/Cancel pair. Its OK handler lives
/// here, in the base class - the derived form never mentions it.
/// </summary>
public partial class BaseDialogForm : Form
{
    public BaseDialogForm()
    {
        InitializeComponent();
    }

    private void okButton_Click(object? sender, EventArgs e)
    {
        this.headerLabel.Text = "Saved";
    }
}
