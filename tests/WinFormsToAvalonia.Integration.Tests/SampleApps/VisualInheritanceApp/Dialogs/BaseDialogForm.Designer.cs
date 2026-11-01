namespace VisualInheritanceApp.Dialogs;

partial class BaseDialogForm
{
    private System.ComponentModel.IContainer components = null;
    protected Label headerLabel;
    protected Button okButton;
    protected Button cancelButton;

    private void InitializeComponent()
    {
        this.headerLabel = new Label();
        this.okButton = new Button();
        this.cancelButton = new Button();
        this.SuspendLayout();
        //
        // headerLabel
        //
        this.headerLabel.Location = new Point(12, 9);
        this.headerLabel.Name = "headerLabel";
        this.headerLabel.Size = new Size(260, 20);
        this.headerLabel.Text = "Dialog";
        //
        // okButton
        //
        this.okButton.Location = new Point(116, 120);
        this.okButton.Name = "okButton";
        this.okButton.Size = new Size(75, 23);
        this.okButton.TabIndex = 10;
        this.okButton.Text = "OK";
        this.okButton.Click += new EventHandler(this.okButton_Click);
        //
        // cancelButton
        //
        this.cancelButton.DialogResult = DialogResult.Cancel;
        this.cancelButton.Location = new Point(197, 120);
        this.cancelButton.Name = "cancelButton";
        this.cancelButton.Size = new Size(75, 23);
        this.cancelButton.TabIndex = 11;
        this.cancelButton.Text = "Cancel";
        //
        // BaseDialogForm
        //
        this.AcceptButton = this.okButton;
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.CancelButton = this.cancelButton;
        this.ClientSize = new Size(284, 155);
        this.Controls.Add(this.headerLabel);
        this.Controls.Add(this.okButton);
        this.Controls.Add(this.cancelButton);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.Name = "BaseDialogForm";
        this.Text = "Dialog";
        this.ResumeLayout(false);
    }
}
