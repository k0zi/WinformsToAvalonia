namespace ModernNetApp.Controls
{
    partial class MyUserControl
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
            this.nameLabel = new Label();
            this.SuspendLayout();
            //
            // nameLabel
            //
            this.nameLabel.Location = new System.Drawing.Point(8, 8);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(100, 23);
            this.nameLabel.TabIndex = 0;
            this.nameLabel.Text = "Name:";
            //
            // MyUserControl
            //
            this.Controls.Add(this.nameLabel);
            this.Name = "MyUserControl";
            this.Size = new System.Drawing.Size(200, 100);
            this.ResumeLayout(false);
        }

        private Label nameLabel;
    }
}
