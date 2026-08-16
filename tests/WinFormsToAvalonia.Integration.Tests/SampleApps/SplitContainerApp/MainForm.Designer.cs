namespace SplitContainerApp
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
            this.splitContainer1 = new SplitContainer();
            this.leftButton = new Button();
            this.rightLabel = new Label();
            this.SuspendLayout();
            //
            // leftButton
            //
            this.leftButton.Location = new System.Drawing.Point(10, 10);
            this.leftButton.Name = "leftButton";
            this.leftButton.Size = new System.Drawing.Size(75, 23);
            this.leftButton.TabIndex = 0;
            this.leftButton.Text = "Left";
            //
            // rightLabel
            //
            this.rightLabel.Location = new System.Drawing.Point(10, 10);
            this.rightLabel.Name = "rightLabel";
            this.rightLabel.Size = new System.Drawing.Size(100, 23);
            this.rightLabel.TabIndex = 0;
            this.rightLabel.Text = "Right";
            //
            // splitContainer1
            //
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Panel1.Controls.Add(this.leftButton);
            this.splitContainer1.Panel2.Controls.Add(this.rightLabel);
            this.splitContainer1.Size = new System.Drawing.Size(400, 200);
            this.splitContainer1.TabIndex = 0;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(400, 200);
            this.Controls.Add(this.splitContainer1);
            this.Name = "MainForm";
            this.Text = "SplitContainer Demo";
            this.ResumeLayout(false);
        }

        private SplitContainer splitContainer1;
        private Button leftButton;
        private Label rightLabel;
    }
}
