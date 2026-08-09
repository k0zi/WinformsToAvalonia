namespace WarehouseApp.Common;

public static class InputBoxHelper
{
    public static string? Show(IWin32Window owner, string title, string label, string defaultValue = "")
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(360, 130),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var promptLabel = new Label { Text = label, Location = new Point(12, 15), AutoSize = true };
        var textBox = new TextBox { Text = defaultValue, Location = new Point(12, 38), Width = 336 };
        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(190, 75), Size = new Size(75, 28) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(273, 75), Size = new Size(75, 28) };

        form.Controls.Add(promptLabel);
        form.Controls.Add(textBox);
        form.Controls.Add(okButton);
        form.Controls.Add(cancelButton);
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        return form.ShowDialog(owner) == DialogResult.OK ? textBox.Text.Trim() : null;
    }
}
