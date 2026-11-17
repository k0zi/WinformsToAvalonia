namespace StatusStripApp
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
            this.statusStrip1 = new StatusStrip();
            this.readyStatusLabel = new ToolStripStatusLabel();
            this.SuspendLayout();
            //
            // readyStatusLabel
            //
            this.readyStatusLabel.Text = "Ready";
            //
            // statusStrip1
            //
            this.statusStrip1.Items.AddRange(new ToolStripItem[] {
                this.readyStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 115);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(240, 25);
            this.statusStrip1.TabIndex = 0;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 140);
            this.Controls.Add(this.statusStrip1);
            this.Name = "MainForm";
            this.Text = "StatusStrip Demo";
            this.ResumeLayout(false);
        }

        private StatusStrip statusStrip1;
        private ToolStripStatusLabel readyStatusLabel;
    }
}
