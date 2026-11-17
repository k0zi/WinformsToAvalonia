namespace FormNavigationApp.Dialogs
{
    partial class SettingsForm
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
            this.closeButton = new System.Windows.Forms.Button();
            this.acceptButton = new System.Windows.Forms.Button();
            this.rejectButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // closeButton
            //
            this.closeButton.Location = new System.Drawing.Point(12, 12);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(96, 28);
            this.closeButton.TabIndex = 0;
            this.closeButton.Text = "Close";
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            //
            // acceptButton - designer-declared behaviour: closes the form with OK, no handler.
            //
            this.acceptButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.acceptButton.Location = new System.Drawing.Point(114, 12);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Size = new System.Drawing.Size(90, 28);
            this.acceptButton.TabIndex = 1;
            this.acceptButton.Text = "OK";
            //
            // rejectButton
            //
            this.rejectButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.rejectButton.Location = new System.Drawing.Point(114, 46);
            this.rejectButton.Name = "rejectButton";
            this.rejectButton.Size = new System.Drawing.Size(90, 28);
            this.rejectButton.TabIndex = 2;
            this.rejectButton.Text = "Cancel";
            //
            // SettingsForm
            //
            this.ClientSize = new System.Drawing.Size(220, 90);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.acceptButton);
            this.Controls.Add(this.rejectButton);
            this.Name = "SettingsForm";
            this.Text = "Settings";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button acceptButton;
        private System.Windows.Forms.Button rejectButton;
    }
}
