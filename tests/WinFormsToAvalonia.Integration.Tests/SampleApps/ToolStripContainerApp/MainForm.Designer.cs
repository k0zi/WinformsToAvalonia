namespace ToolStripContainerApp
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
            this.toolStripContainer1 = new ToolStripContainer();
            this.SuspendLayout();
            //
            // toolStripContainer1
            //
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(240, 140);
            this.toolStripContainer1.TabIndex = 0;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 140);
            this.Controls.Add(this.toolStripContainer1);
            this.Name = "MainForm";
            this.Text = "ToolStripContainer Demo";
            this.ResumeLayout(false);
        }

        private ToolStripContainer toolStripContainer1;
    }
}
