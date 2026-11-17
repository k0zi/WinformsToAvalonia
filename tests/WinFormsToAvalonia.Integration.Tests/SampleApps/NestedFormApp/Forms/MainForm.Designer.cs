namespace NestedFormApp.Forms
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
            this.SuspendLayout();
            //
            // okButton
            //
            this.okButton.Location = new System.Drawing.Point(12, 12);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.Click += new EventHandler(this.okButton_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(320, 240);
            this.Controls.Add(this.okButton);
            this.Name = "MainForm";
            this.Text = "Main Form";
            this.ResumeLayout(false);
        }

        private Button okButton;
    }
}
