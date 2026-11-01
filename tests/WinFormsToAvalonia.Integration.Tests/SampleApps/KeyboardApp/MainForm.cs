namespace KeyboardApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void nameBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            this.statusLabel.Text = "Submitted";
            e.Handled = true;
        }

        if (e.Control && e.KeyCode == Keys.Delete)
        {
            this.nameBox.Clear();
        }

        if (e.KeyData == (Keys.Control | Keys.S))
        {
            this.statusLabel.Text = "Saved";
        }

        if (e.KeyCode != Keys.Escape && !e.Shift)
        {
            this.statusLabel.Text = "Typing";
        }
    }

    private void amountBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        // The classic digit filter: let control keys through, refuse anything that is not a digit.
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
        {
            e.Handled = true;
        }

        if (e.KeyChar == '-')
        {
            this.statusLabel.Text = "No negatives";
        }
    }

    private void codeBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        // Enter arrives as a KeyPress in WinForms and as no TextInput at all in Avalonia - this
        // one has to stay for a human, who will move it to KeyDown.
        if (e.KeyChar == '\r')
        {
            this.statusLabel.Text = "Entered";
        }
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Modifiers == Keys.Alt)
        {
            this.statusLabel.Text = "Alt";
        }
    }
}
