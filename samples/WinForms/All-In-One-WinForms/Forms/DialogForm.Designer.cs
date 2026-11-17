namespace AllInOneWinForms.Forms
{
    partial class DialogForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.promptLabel = new Label();
            this.inputTextBox = new TextBox();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.SuspendLayout();
            //
            // promptLabel
            //
            this.promptLabel.Location = new System.Drawing.Point(12, 15);
            this.promptLabel.Name = "promptLabel";
            this.promptLabel.Size = new System.Drawing.Size(120, 20);
            this.promptLabel.TabIndex = 0;
            this.promptLabel.Text = "Enter a value:";
            //
            // inputTextBox
            //
            this.inputTextBox.Location = new System.Drawing.Point(138, 12);
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.Size = new System.Drawing.Size(210, 23);
            this.inputTextBox.TabIndex = 1;
            //
            // okButton
            //
            this.okButton.Location = new System.Drawing.Point(192, 58);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 26);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "OK";
            this.okButton.Click += new EventHandler(this.okButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Location = new System.Drawing.Point(273, 58);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 26);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.Click += new EventHandler(this.cancelButton_Click);
            //
            // DialogForm
            //
            this.ClientSize = new System.Drawing.Size(360, 96);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.inputTextBox);
            this.Controls.Add(this.promptLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogForm";
            this.Text = "Dialog";
            this.ResumeLayout(false);
        }

        private Label promptLabel;
        private TextBox inputTextBox;
        private Button okButton;
        private Button cancelButton;
    }
}
