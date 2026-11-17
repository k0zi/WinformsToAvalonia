using VisualInheritanceApp.Dialogs;

namespace VisualInheritanceApp;

public partial class SettingsForm : ChildDialogBase
{
    public SettingsForm()
    {
        InitializeComponent();
    }

    private void resetButton_Click(object? sender, EventArgs e)
    {
        this.nameTextBox.Text = string.Empty;
    }
}
