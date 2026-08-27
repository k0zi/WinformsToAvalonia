namespace Shell
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.titleButton = new System.Windows.Forms.Button();
            this.sharedPanel1 = new Widgets.SharedPanel();
            this.SuspendLayout();
            this.titleLabel.Location = new System.Drawing.Point(12, 12);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(200, 23);
            this.titleLabel.Text = "shell";
            this.titleButton.Location = new System.Drawing.Point(12, 40);
            this.titleButton.Name = "titleButton";
            this.titleButton.Size = new System.Drawing.Size(90, 28);
            this.titleButton.Text = "Set";
            this.titleButton.Click += new System.EventHandler(this.titleButton_Click);
            this.sharedPanel1.Location = new System.Drawing.Point(12, 80);
            this.sharedPanel1.Name = "sharedPanel1";
            this.sharedPanel1.Size = new System.Drawing.Size(180, 72);
            this.ClientSize = new System.Drawing.Size(240, 170);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.titleButton);
            this.Controls.Add(this.sharedPanel1);
            this.Name = "MainForm";
            this.Text = "Shell";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Button titleButton;
        private Widgets.SharedPanel sharedPanel1;
    }
}
