namespace Widgets
{
    partial class SharedPanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.captionLabel = new System.Windows.Forms.Label();
            this.refreshButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.captionLabel.Location = new System.Drawing.Point(8, 8);
            this.captionLabel.Name = "captionLabel";
            this.captionLabel.Size = new System.Drawing.Size(160, 23);
            this.captionLabel.Text = "shared";
            this.refreshButton.Location = new System.Drawing.Point(8, 36);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(90, 28);
            this.refreshButton.Text = "Refresh";
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            this.Controls.Add(this.captionLabel);
            this.Controls.Add(this.refreshButton);
            this.Name = "SharedPanel";
            this.Size = new System.Drawing.Size(180, 72);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label captionLabel;
        private System.Windows.Forms.Button refreshButton;
    }
}
