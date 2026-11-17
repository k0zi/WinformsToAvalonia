namespace RuntimeControlsApp
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
            this.hostPanel = new Panel();
            this.addButton = new Button();
            this.statusStrip1 = new StatusStrip();
            this.SuspendLayout();
            //
            // hostPanel
            //
            this.hostPanel.Location = new System.Drawing.Point(12, 12);
            this.hostPanel.Name = "hostPanel";
            this.hostPanel.Size = new System.Drawing.Size(200, 150);
            //
            // addButton
            //
            this.addButton.Location = new System.Drawing.Point(12, 170);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(90, 25);
            this.addButton.Text = "Add";
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            //
            // statusStrip1
            //
            this.statusStrip1.Location = new System.Drawing.Point(0, 208);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(240, 22);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 230);
            this.Controls.Add(this.hostPanel);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.statusStrip1);
            this.Name = "MainForm";
            this.Text = "Runtime controls";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
        }

        private Panel hostPanel;
        private Button addButton;
        private StatusStrip statusStrip1;
    }
}
