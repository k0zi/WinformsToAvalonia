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
            // SettingsForm
            //
            this.ClientSize = new System.Drawing.Size(220, 60);
            this.Controls.Add(this.closeButton);
            this.Name = "SettingsForm";
            this.Text = "Settings";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button closeButton;
    }
}
