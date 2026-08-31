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
            this.contentLabel = new System.Windows.Forms.Label();
            this.dockedStrip = new ToolStrip();
            this.SuspendLayout();
            //
            // contentLabel - put into a nested region. Both region kinds are exercised here: the
            // content panel below and the top strip panel, which become different templates.
            //
            this.contentLabel.Location = new System.Drawing.Point(8, 8);
            this.contentLabel.Name = "contentLabel";
            this.contentLabel.Size = new System.Drawing.Size(180, 20);
            this.contentLabel.Text = "Inside the content panel";
            //
            // toolStripContainer1
            //
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(240, 140);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.contentLabel);
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.dockedStrip);
            //
            // dockedStrip
            //
            this.dockedStrip.Name = "dockedStrip";
            this.dockedStrip.Size = new System.Drawing.Size(240, 25);
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
        private System.Windows.Forms.Label contentLabel;
        private ToolStrip dockedStrip;
    }
}
