namespace MenuStripApp
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
            this.menuStrip1 = new MenuStrip();
            this.fileMenuItem = new ToolStripMenuItem();
            this.exitMenuItem = new ToolStripMenuItem();
            this.okButton = new Button();
            this.SuspendLayout();
            //
            // exitMenuItem
            //
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new EventHandler(this.exitMenuItem_Click);
            //
            // fileMenuItem
            //
            this.fileMenuItem.Text = "File";
            this.fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.exitMenuItem});
            //
            // menuStrip1
            //
            this.menuStrip1.Items.AddRange(new ToolStripItem[] {
                this.fileMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(240, 24);
            this.menuStrip1.TabIndex = 0;
            //
            // okButton
            //
            this.okButton.Location = new System.Drawing.Point(10, 40);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 140);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "MenuStrip Demo";
            this.ResumeLayout(false);
        }

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem exitMenuItem;
        private Button okButton;
    }
}
