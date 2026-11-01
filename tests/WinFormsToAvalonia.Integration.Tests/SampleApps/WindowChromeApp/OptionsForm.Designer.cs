namespace WindowChromeApp;

partial class OptionsForm
{
    private System.ComponentModel.IContainer components = null;
    private Label nameLabel;
    private TextBox nameTextBox;
    private Button okButton;
    private Button cancelButton;

    private void InitializeComponent()
    {
        this.nameLabel = new Label();
        this.nameTextBox = new TextBox();
        this.okButton = new Button();
        this.cancelButton = new Button();
        this.SuspendLayout();
        //
        // nameLabel - not a tab stop in practice, but WinForms still numbers it.
        //
        this.nameLabel.Location = new Point(12, 15);
        this.nameLabel.Name = "nameLabel";
        this.nameLabel.Size = new Size(60, 15);
        this.nameLabel.TabIndex = 2;
        this.nameLabel.Text = "Name:";
        //
        // nameTextBox - first in the tab order although it is added second.
        //
        this.nameTextBox.Location = new Point(80, 12);
        this.nameTextBox.Name = "nameTextBox";
        this.nameTextBox.Size = new Size(180, 23);
        this.nameTextBox.TabIndex = 0;
        //
        // okButton - the AcceptButton, so Enter clicks it.
        //
        this.okButton.DialogResult = DialogResult.OK;
        this.okButton.Location = new Point(104, 50);
        this.okButton.Name = "okButton";
        this.okButton.Size = new Size(75, 23);
        this.okButton.TabIndex = 1;
        this.okButton.Text = "OK";
        //
        // cancelButton - the CancelButton, so Escape clicks it; skipped by Tab.
        //
        this.cancelButton.DialogResult = DialogResult.Cancel;
        this.cancelButton.Location = new Point(185, 50);
        this.cancelButton.Name = "cancelButton";
        this.cancelButton.Size = new Size(75, 23);
        this.cancelButton.TabIndex = 3;
        this.cancelButton.TabStop = false;
        this.cancelButton.Text = "Cancel";
        //
        // OptionsForm - a classic fixed dialog.
        //
        this.AcceptButton = this.okButton;
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.CancelButton = this.cancelButton;
        this.ClientSize = new Size(272, 85);
        this.Controls.Add(this.nameLabel);
        this.Controls.Add(this.nameTextBox);
        this.Controls.Add(this.okButton);
        this.Controls.Add(this.cancelButton);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "OptionsForm";
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Options";
        this.ResumeLayout(false);
    }
}
