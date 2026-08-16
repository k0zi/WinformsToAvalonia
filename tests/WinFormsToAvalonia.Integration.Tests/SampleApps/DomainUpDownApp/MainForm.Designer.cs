namespace DomainUpDownApp
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
            this.domainUpDown1 = new DomainUpDown();
            this.SuspendLayout();
            //
            // domainUpDown1
            //
            this.domainUpDown1.Location = new System.Drawing.Point(12, 12);
            this.domainUpDown1.Name = "domainUpDown1";
            this.domainUpDown1.Size = new System.Drawing.Size(100, 23);
            this.domainUpDown1.TabIndex = 0;
            this.domainUpDown1.Wrap = true;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 140);
            this.Controls.Add(this.domainUpDown1);
            this.Name = "MainForm";
            this.Text = "DomainUpDown Demo";
            this.ResumeLayout(false);
        }

        private DomainUpDown domainUpDown1;
    }
}
