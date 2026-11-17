namespace DialogContractApp
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
            this.components = new System.ComponentModel.Container();
            this.maximizeButton = new System.Windows.Forms.Button();
            this.titleButton = new System.Windows.Forms.Button();
            this.configureButton = new System.Windows.Forms.Button();
            this.toggleClockButton = new System.Windows.Forms.Button();
            this.stopClockButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.clockTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            //
            // maximizeButton
            //
            this.maximizeButton.Location = new System.Drawing.Point(12, 12);
            this.maximizeButton.Name = "maximizeButton";
            this.maximizeButton.Size = new System.Drawing.Size(110, 28);
            this.maximizeButton.TabIndex = 0;
            this.maximizeButton.Text = "Maximize";
            this.maximizeButton.Click += new System.EventHandler(this.maximizeButton_Click);
            //
            // titleButton
            //
            this.titleButton.Location = new System.Drawing.Point(128, 12);
            this.titleButton.Name = "titleButton";
            this.titleButton.Size = new System.Drawing.Size(110, 28);
            this.titleButton.TabIndex = 1;
            this.titleButton.Text = "Show title";
            this.titleButton.Click += new System.EventHandler(this.titleButton_Click);
            //
            // configureButton
            //
            this.configureButton.Location = new System.Drawing.Point(12, 46);
            this.configureButton.Name = "configureButton";
            this.configureButton.Size = new System.Drawing.Size(110, 28);
            this.configureButton.TabIndex = 2;
            this.configureButton.Text = "Configure";
            this.configureButton.Click += new System.EventHandler(this.configureButton_Click);
            //
            // toggleClockButton
            //
            this.toggleClockButton.Location = new System.Drawing.Point(128, 46);
            this.toggleClockButton.Name = "toggleClockButton";
            this.toggleClockButton.Size = new System.Drawing.Size(110, 28);
            this.toggleClockButton.TabIndex = 3;
            this.toggleClockButton.Text = "Toggle clock";
            this.toggleClockButton.Click += new System.EventHandler(this.toggleClockButton_Click);
            //
            // stopClockButton
            //
            this.stopClockButton.Location = new System.Drawing.Point(244, 46);
            this.stopClockButton.Name = "stopClockButton";
            this.stopClockButton.Size = new System.Drawing.Size(110, 28);
            this.stopClockButton.TabIndex = 4;
            this.stopClockButton.Text = "Stop clock";
            this.stopClockButton.Click += new System.EventHandler(this.stopClockButton_Click);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 84);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(342, 23);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.Text = "Ready";
            //
            // clockTimer
            //
            this.clockTimer.Interval = 1000;
            this.clockTimer.Tick += new System.EventHandler(this.clockTimer_Tick);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(366, 120);
            this.Controls.Add(this.maximizeButton);
            this.Controls.Add(this.titleButton);
            this.Controls.Add(this.configureButton);
            this.Controls.Add(this.toggleClockButton);
            this.Controls.Add(this.stopClockButton);
            this.Controls.Add(this.statusLabel);
            this.Name = "MainForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Text = "Dialog contract";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button maximizeButton;
        private System.Windows.Forms.Button titleButton;
        private System.Windows.Forms.Button configureButton;
        private System.Windows.Forms.Button toggleClockButton;
        private System.Windows.Forms.Button stopClockButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Timer clockTimer;
    }
}
