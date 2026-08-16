namespace UserControlApp
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
            this.counterControl1 = new UserControlApp.Controls.CounterControl();
            this.titleLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // counterControl1
            //
            this.counterControl1.Location = new System.Drawing.Point(12, 40);
            this.counterControl1.Name = "counterControl1";
            this.counterControl1.Size = new System.Drawing.Size(180, 36);
            //
            // titleLabel
            //
            this.titleLabel.Location = new System.Drawing.Point(12, 12);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(200, 20);
            this.titleLabel.Text = "Hosting a UserControl";
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 100);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.counterControl1);
            this.Name = "MainForm";
            this.Text = "UserControl host";
            this.ResumeLayout(false);
        }

        private UserControlApp.Controls.CounterControl counterControl1;
        private System.Windows.Forms.Label titleLabel;
    }
}
