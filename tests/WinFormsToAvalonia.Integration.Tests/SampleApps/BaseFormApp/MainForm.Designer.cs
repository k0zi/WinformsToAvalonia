namespace BaseFormApp
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
            this.okButton = new Button();
            this.statusLabel = new Label();
            this.SuspendLayout();
            //
            // okButton
            //
            this.okButton.Location = new System.Drawing.Point(12, 12);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(96, 28);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 50);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(200, 20);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Ready";
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(260, 90);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.statusLabel);
            this.Name = "MainForm";
            this.Text = "Base Form Demo";
            this.ResumeLayout(false);
        }

        private Button okButton;
        private Label statusLabel;
    }
}
