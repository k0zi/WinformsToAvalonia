namespace FormNavigationApp
{
    partial class MainForm
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
            this.settingsButton = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.confirmButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // settingsButton
            //
            this.settingsButton.Location = new System.Drawing.Point(12, 12);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(96, 28);
            this.settingsButton.TabIndex = 0;
            this.settingsButton.Text = "Settings";
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            //
            // helpButton
            //
            this.helpButton.Location = new System.Drawing.Point(114, 12);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(96, 28);
            this.helpButton.TabIndex = 1;
            this.helpButton.Text = "Help";
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            //
            // confirmButton
            //
            this.confirmButton.Location = new System.Drawing.Point(216, 12);
            this.confirmButton.Name = "confirmButton";
            this.confirmButton.Size = new System.Drawing.Size(96, 28);
            this.confirmButton.TabIndex = 2;
            this.confirmButton.Text = "Confirm";
            this.confirmButton.Click += new System.EventHandler(this.confirmButton_Click);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 50);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(300, 20);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "Ready";
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(324, 82);
            this.Controls.Add(this.settingsButton);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.confirmButton);
            this.Controls.Add(this.statusLabel);
            this.Name = "MainForm";
            this.Text = "Form Navigation Demo";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button settingsButton;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button confirmButton;
        private System.Windows.Forms.Label statusLabel;
    }
}
