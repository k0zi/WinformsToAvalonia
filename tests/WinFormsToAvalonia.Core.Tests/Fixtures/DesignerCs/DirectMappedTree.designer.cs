namespace Demo
{
    partial class DirectMappedTreeForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.innerButton = new System.Windows.Forms.Button();
            this.innerLabel = new System.Windows.Forms.Label();
            this.topTextBox = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            //
            // panel1
            //
            this.panel1.Controls.Add(this.innerButton);
            this.panel1.Controls.Add(this.innerLabel);
            this.panel1.Location = new System.Drawing.Point(10, 10);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 100);
            this.panel1.TabIndex = 0;
            //
            // innerButton
            //
            this.innerButton.Location = new System.Drawing.Point(8, 8);
            this.innerButton.Name = "innerButton";
            this.innerButton.Size = new System.Drawing.Size(75, 23);
            this.innerButton.TabIndex = 0;
            this.innerButton.Text = "OK";
            //
            // innerLabel
            //
            this.innerLabel.Location = new System.Drawing.Point(8, 40);
            this.innerLabel.Name = "innerLabel";
            this.innerLabel.Size = new System.Drawing.Size(100, 23);
            this.innerLabel.TabIndex = 1;
            this.innerLabel.Text = "Status";
            //
            // topTextBox
            //
            this.topTextBox.Location = new System.Drawing.Point(10, 120);
            this.topTextBox.Name = "topTextBox";
            this.topTextBox.Size = new System.Drawing.Size(200, 23);
            this.topTextBox.TabIndex = 1;
            //
            // DirectMappedTreeForm
            //
            this.ClientSize = new System.Drawing.Size(230, 160);
            this.Controls.Add(this.topTextBox);
            this.Controls.Add(this.panel1);
            this.Name = "DirectMappedTreeForm";
            this.Text = "Direct Mapped Tree Demo";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button innerButton;
        private System.Windows.Forms.Label innerLabel;
        private System.Windows.Forms.TextBox topTextBox;
    }
}
