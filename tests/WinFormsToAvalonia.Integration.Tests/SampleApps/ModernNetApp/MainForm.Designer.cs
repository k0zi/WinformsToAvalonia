namespace ModernNetApp
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
            this.userControl1 = new ModernNetApp.Controls.MyUserControl();
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
            // userControl1
            //
            this.userControl1.Location = new System.Drawing.Point(12, 50);
            this.userControl1.Name = "userControl1";
            this.userControl1.Size = new System.Drawing.Size(200, 100);
            this.userControl1.TabIndex = 1;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(320, 240);
            this.Controls.Add(this.userControl1);
            this.Controls.Add(this.okButton);
            this.Name = "MainForm";
            this.Text = "Main Form";
            this.ResumeLayout(false);
        }

        private Button okButton;
        private ModernNetApp.Controls.MyUserControl userControl1;
    }
}
