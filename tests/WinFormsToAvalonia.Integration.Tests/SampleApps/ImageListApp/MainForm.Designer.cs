namespace ImageListApp
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.iconList = new ImageList(this.components);
            this.menuStrip1 = new MenuStrip();
            this.fileMenuItem = new ToolStripMenuItem();
            this.openMenuItem = new ToolStripMenuItem();
            this.saveMenuItem = new ToolStripMenuItem();
            this.treeView1 = new TreeView();
            this.SuspendLayout();
            //
            // iconList
            //
            this.iconList.ImageStream = ((ImageListStreamer)(resources.GetObject("iconList.ImageStream")));
            this.iconList.TransparentColor = System.Drawing.Color.Transparent;
            //
            // openMenuItem
            //
            this.openMenuItem.ImageIndex = 0;
            this.openMenuItem.Text = "Open";
            //
            // saveMenuItem
            //
            this.saveMenuItem.ImageIndex = 2;
            this.saveMenuItem.Text = "Save";
            //
            // fileMenuItem
            //
            this.fileMenuItem.Text = "File";
            this.fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.openMenuItem,
                this.saveMenuItem});
            //
            // menuStrip1
            //
            this.menuStrip1.ImageList = this.iconList;
            this.menuStrip1.Items.AddRange(new ToolStripItem[] {
                this.fileMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(240, 24);
            this.menuStrip1.TabIndex = 0;
            //
            // treeView1
            //
            this.treeView1.ImageList = this.iconList;
            this.treeView1.ImageIndex = 1;
            this.treeView1.Location = new System.Drawing.Point(10, 40);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(200, 80);
            this.treeView1.TabIndex = 1;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 140);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "ImageList Demo";
            this.ResumeLayout(false);
        }

        private ImageList iconList;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem openMenuItem;
        private ToolStripMenuItem saveMenuItem;
        private TreeView treeView1;
    }
}
