namespace DevExpressApp
{
    partial class DetailsForm
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
            this.ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.saveItem = new DevExpress.XtraBars.BarButtonItem();
            this.printItem = new DevExpress.XtraBars.BarButtonItem();
            this.readyItem = new DevExpress.XtraBars.BarStaticItem();
            this.homePage = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.filePageGroup = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.ribbonStatusBar = new DevExpress.XtraBars.Ribbon.RibbonStatusBar();
            this.layoutControl = new DevExpress.XtraLayout.LayoutControl();
            this.cityEdit = new DevExpress.XtraEditors.TextEdit();
            this.zipEdit = new DevExpress.XtraEditors.TextEdit();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.cityItem = new DevExpress.XtraLayout.LayoutControlItem();
            this.zipItem = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.split = new DevExpress.XtraEditors.SplitContainerControl();
            this.tree = new DevExpress.XtraTreeList.TreeList();
            this.treeColumn = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.notesMemo = new DevExpress.XtraEditors.MemoEdit();
            ((System.ComponentModel.ISupportInitialize)(this.ribbon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl)).BeginInit();
            this.layoutControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cityEdit.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.zipEdit.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cityItem)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.zipItem)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tree)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.notesMemo.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // ribbon
            // 
            this.ribbon.ExpandCollapseItem.Id = 0;
            this.ribbon.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.ribbon.ExpandCollapseItem,
            this.ribbon.SearchEditItem,
            this.saveItem,
            this.printItem,
            this.readyItem});
            this.ribbon.Location = new System.Drawing.Point(0, 0);
            this.ribbon.MaxItemId = 4;
            this.ribbon.Name = "ribbon";
            this.ribbon.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
            this.homePage});
            this.ribbon.Size = new System.Drawing.Size(560, 158);
            this.ribbon.StatusBar = this.ribbonStatusBar;
            // 
            // saveItem
            // 
            this.saveItem.Caption = "Save";
            this.saveItem.Id = 1;
            this.saveItem.Name = "saveItem";
            this.saveItem.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.saveItem_ItemClick);
            // 
            // printItem
            // 
            this.printItem.Caption = "Print";
            this.printItem.Id = 2;
            this.printItem.Name = "printItem";
            this.printItem.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.printItem_ItemClick);
            // 
            // readyItem
            // 
            this.readyItem.Caption = "Ready";
            this.readyItem.Id = 3;
            this.readyItem.Name = "readyItem";
            // 
            // homePage
            // 
            this.homePage.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.filePageGroup});
            this.homePage.Name = "homePage";
            this.homePage.Text = "Home";
            // 
            // filePageGroup
            // 
            this.filePageGroup.ItemLinks.Add(this.saveItem);
            this.filePageGroup.ItemLinks.Add(this.printItem, true);
            this.filePageGroup.Name = "filePageGroup";
            this.filePageGroup.Text = "File";
            // 
            // ribbonStatusBar
            // 
            this.ribbonStatusBar.ItemLinks.Add(this.readyItem);
            this.ribbonStatusBar.Location = new System.Drawing.Point(0, 418);
            this.ribbonStatusBar.Name = "ribbonStatusBar";
            this.ribbonStatusBar.Ribbon = this.ribbon;
            this.ribbonStatusBar.Size = new System.Drawing.Size(560, 22);
            // 
            // layoutControl
            // 
            this.layoutControl.Controls.Add(this.cityEdit);
            this.layoutControl.Controls.Add(this.zipEdit);
            this.layoutControl.Location = new System.Drawing.Point(0, 158);
            this.layoutControl.Name = "layoutControl";
            this.layoutControl.Root = this.Root;
            this.layoutControl.Size = new System.Drawing.Size(280, 120);
            this.layoutControl.TabIndex = 2;
            // 
            // cityEdit
            // 
            this.cityEdit.Location = new System.Drawing.Point(45, 12);
            this.cityEdit.MenuManager = this.ribbon;
            this.cityEdit.Name = "cityEdit";
            this.cityEdit.Size = new System.Drawing.Size(223, 20);
            this.cityEdit.StyleController = this.layoutControl;
            this.cityEdit.TabIndex = 4;
            // 
            // zipEdit
            // 
            this.zipEdit.Location = new System.Drawing.Point(45, 36);
            this.zipEdit.MenuManager = this.ribbon;
            this.zipEdit.Name = "zipEdit";
            this.zipEdit.Size = new System.Drawing.Size(223, 20);
            this.zipEdit.StyleController = this.layoutControl;
            this.zipEdit.TabIndex = 5;
            // 
            // Root
            // 
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.cityItem,
            this.zipItem,
            this.emptySpaceItem1});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(280, 120);
            this.Root.TextVisible = false;
            // 
            // cityItem
            // 
            this.cityItem.Control = this.cityEdit;
            this.cityItem.Location = new System.Drawing.Point(0, 0);
            this.cityItem.Name = "cityItem";
            this.cityItem.Size = new System.Drawing.Size(260, 24);
            this.cityItem.Text = "City";
            this.cityItem.TextSize = new System.Drawing.Size(21, 13);
            // 
            // zipItem
            // 
            this.zipItem.Control = this.zipEdit;
            this.zipItem.Location = new System.Drawing.Point(0, 24);
            this.zipItem.Name = "zipItem";
            this.zipItem.Size = new System.Drawing.Size(260, 24);
            this.zipItem.Text = "Zip";
            this.zipItem.TextSize = new System.Drawing.Size(21, 13);
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(0, 48);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(260, 52);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // split
            // 
            this.split.Location = new System.Drawing.Point(280, 158);
            this.split.Name = "split";
            this.split.Panel1.Controls.Add(this.tree);
            this.split.Panel1.Text = "Panel1";
            this.split.Panel2.Controls.Add(this.notesMemo);
            this.split.Panel2.Text = "Panel2";
            this.split.Size = new System.Drawing.Size(280, 260);
            this.split.SplitterPosition = 140;
            this.split.TabIndex = 3;
            // 
            // tree
            // 
            this.tree.Columns.AddRange(new DevExpress.XtraTreeList.Columns.TreeListColumn[] {
            this.treeColumn});
            this.tree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tree.Location = new System.Drawing.Point(0, 0);
            this.tree.Name = "tree";
            this.tree.Size = new System.Drawing.Size(140, 260);
            this.tree.TabIndex = 0;
            // 
            // treeColumn
            // 
            this.treeColumn.Caption = "Name";
            this.treeColumn.FieldName = "Name";
            this.treeColumn.Name = "treeColumn";
            this.treeColumn.Visible = true;
            this.treeColumn.VisibleIndex = 0;
            // 
            // notesMemo
            // 
            this.notesMemo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.notesMemo.Location = new System.Drawing.Point(0, 0);
            this.notesMemo.MenuManager = this.ribbon;
            this.notesMemo.Name = "notesMemo";
            this.notesMemo.Size = new System.Drawing.Size(134, 260);
            this.notesMemo.TabIndex = 0;
            // 
            // DetailsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 440);
            this.Controls.Add(this.split);
            this.Controls.Add(this.layoutControl);
            this.Controls.Add(this.ribbonStatusBar);
            this.Controls.Add(this.ribbon);
            this.Name = "DetailsForm";
            this.Ribbon = this.ribbon;
            this.StatusBar = this.ribbonStatusBar;
            this.Text = "Details";
            ((System.ComponentModel.ISupportInitialize)(this.ribbon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl)).EndInit();
            this.layoutControl.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cityEdit.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.zipEdit.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cityItem)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.zipItem)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tree)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.notesMemo.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private DevExpress.XtraBars.Ribbon.RibbonControl ribbon;
        private DevExpress.XtraBars.BarButtonItem saveItem;
        private DevExpress.XtraBars.BarButtonItem printItem;
        private DevExpress.XtraBars.BarStaticItem readyItem;
        private DevExpress.XtraBars.Ribbon.RibbonPage homePage;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup filePageGroup;
        private DevExpress.XtraBars.Ribbon.RibbonStatusBar ribbonStatusBar;
        private DevExpress.XtraLayout.LayoutControl layoutControl;
        private DevExpress.XtraEditors.TextEdit cityEdit;
        private DevExpress.XtraEditors.TextEdit zipEdit;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlItem cityItem;
        private DevExpress.XtraLayout.LayoutControlItem zipItem;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraEditors.SplitContainerControl split;
        private DevExpress.XtraTreeList.TreeList tree;
        private DevExpress.XtraTreeList.Columns.TreeListColumn treeColumn;
        private DevExpress.XtraEditors.MemoEdit notesMemo;
    }
}
