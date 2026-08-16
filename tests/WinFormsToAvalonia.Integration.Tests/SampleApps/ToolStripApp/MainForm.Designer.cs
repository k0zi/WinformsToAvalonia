namespace ToolStripApp
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
            this.toolStrip1 = new ToolStrip();
            this.newToolStripButton = new ToolStripButton();
            this.SuspendLayout();
            //
            // newToolStripButton
            //
            this.newToolStripButton.Text = "New";
            //
            // toolStrip1
            //
            this.toolStrip1.Items.AddRange(new ToolStripItem[] {
                this.newToolStripButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(240, 25);
            this.toolStrip1.TabIndex = 0;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 140);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MainForm";
            this.Text = "ToolStrip Demo";
            this.ResumeLayout(false);
        }

        private ToolStrip toolStrip1;
        private ToolStripButton newToolStripButton;
    }
}
