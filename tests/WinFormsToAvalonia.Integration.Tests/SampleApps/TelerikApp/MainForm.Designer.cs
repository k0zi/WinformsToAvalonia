namespace TelerikApp
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            Telerik.WinControls.UI.RadListDataItem radListDataItem1 = new Telerik.WinControls.UI.RadListDataItem();
            Telerik.WinControls.UI.RadListDataItem radListDataItem2 = new Telerik.WinControls.UI.RadListDataItem();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn1 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewCheckBoxColumn gridViewCheckBoxColumn1 = new Telerik.WinControls.UI.GridViewCheckBoxColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn1 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.TableViewDefinition tableViewDefinition1 = new Telerik.WinControls.UI.TableViewDefinition();
            Telerik.WinControls.UI.RadTreeNode radTreeNode1 = new Telerik.WinControls.UI.RadTreeNode();
            Telerik.WinControls.UI.RadTreeNode radTreeNode2 = new Telerik.WinControls.UI.RadTreeNode();
            this.radMenu1 = new Telerik.WinControls.UI.RadMenu();
            this.fileMenuItem = new Telerik.WinControls.UI.RadMenuItem();
            this.openMenuItem = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuSeparatorItem1 = new Telerik.WinControls.UI.RadMenuSeparatorItem();
            this.exitMenuItem = new Telerik.WinControls.UI.RadMenuItem();
            this.radCommandBar1 = new Telerik.WinControls.UI.RadCommandBar();
            this.commandBarRowElement1 = new Telerik.WinControls.UI.CommandBarRowElement();
            this.commandBarStripElement1 = new Telerik.WinControls.UI.CommandBarStripElement();
            this.newButton = new Telerik.WinControls.UI.CommandBarButton();
            this.commandBarSeparator1 = new Telerik.WinControls.UI.CommandBarSeparator();
            this.helpButton = new Telerik.WinControls.UI.CommandBarButton();
            this.nameLabel = new Telerik.WinControls.UI.RadLabel();
            this.nameBox = new Telerik.WinControls.UI.RadTextBox();
            this.agreeCheck = new Telerik.WinControls.UI.RadCheckBox();
            this.colorList = new Telerik.WinControls.UI.RadDropDownList();
            this.countSpin = new Telerik.WinControls.UI.RadSpinEditor();
            this.okButton = new Telerik.WinControls.UI.RadButton();
            this.progress = new Telerik.WinControls.UI.RadProgressBar();
            this.pages = new Telerik.WinControls.UI.RadPageView();
            this.gridPage = new Telerik.WinControls.UI.RadPageViewPage();
            this.ordersGrid = new Telerik.WinControls.UI.RadGridView();
            this.treePage = new Telerik.WinControls.UI.RadPageViewPage();
            this.folderTree = new Telerik.WinControls.UI.RadTreeView();
            this.optionsGroup = new Telerik.WinControls.UI.RadGroupBox();
            this.fastRadio = new Telerik.WinControls.UI.RadRadioButton();
            this.slowRadio = new Telerik.WinControls.UI.RadRadioButton();
            this.statusStrip = new Telerik.WinControls.UI.RadStatusStrip();
            this.statusLabel = new Telerik.WinControls.UI.RadLabelElement();
            ((System.ComponentModel.ISupportInitialize)(this.radMenu1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radCommandBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameLabel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.agreeCheck)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.colorList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.countSpin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.okButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.progress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pages)).BeginInit();
            this.pages.SuspendLayout();
            this.gridPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ordersGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ordersGrid.MasterTemplate)).BeginInit();
            this.treePage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.folderTree)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.optionsGroup)).BeginInit();
            this.optionsGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fastRadio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.slowRadio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusStrip)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // radMenu1
            // 
            this.radMenu1.Items.AddRange(new Telerik.WinControls.RadItem[] {
            this.fileMenuItem});
            this.radMenu1.Location = new System.Drawing.Point(0, 0);
            this.radMenu1.Name = "radMenu1";
            this.radMenu1.Size = new System.Drawing.Size(600, 20);
            this.radMenu1.TabIndex = 0;
            // 
            // fileMenuItem
            // 
            this.fileMenuItem.Items.AddRange(new Telerik.WinControls.RadItem[] {
            this.openMenuItem,
            this.radMenuSeparatorItem1,
            this.exitMenuItem});
            this.fileMenuItem.Name = "fileMenuItem";
            this.fileMenuItem.Text = "&File";
            // 
            // openMenuItem
            // 
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Text = "Open";
            this.openMenuItem.Click += new System.EventHandler(this.openMenuItem_Click);
            // 
            // radMenuSeparatorItem1
            // 
            this.radMenuSeparatorItem1.Name = "radMenuSeparatorItem1";
            this.radMenuSeparatorItem1.Text = "radMenuSeparatorItem1";
            this.radMenuSeparatorItem1.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Text = "Exit";
            // 
            // radCommandBar1
            // 
            this.radCommandBar1.Location = new System.Drawing.Point(0, 20);
            this.radCommandBar1.Name = "radCommandBar1";
            this.radCommandBar1.Rows.AddRange(new Telerik.WinControls.UI.CommandBarRowElement[] {
            this.commandBarRowElement1});
            this.radCommandBar1.Size = new System.Drawing.Size(600, 30);
            this.radCommandBar1.TabIndex = 1;
            // 
            // commandBarRowElement1
            // 
            this.commandBarRowElement1.MinSize = new System.Drawing.Size(25, 25);
            this.commandBarRowElement1.Name = "commandBarRowElement1";
            this.commandBarRowElement1.Strips.AddRange(new Telerik.WinControls.UI.CommandBarStripElement[] {
            this.commandBarStripElement1});
            // 
            // commandBarStripElement1
            // 
            this.commandBarStripElement1.DisplayName = "commandBarStripElement1";
            this.commandBarStripElement1.Items.AddRange(new Telerik.WinControls.UI.RadCommandBarBaseItem[] {
            this.newButton,
            this.commandBarSeparator1,
            this.helpButton});
            this.commandBarStripElement1.Name = "commandBarStripElement1";
            // 
            // newButton
            // 
            this.newButton.DisplayName = "New";
            this.newButton.DrawText = true;
            this.newButton.Name = "newButton";
            this.newButton.Text = "New";
            this.newButton.Click += new System.EventHandler(this.newButton_Click);
            // 
            // commandBarSeparator1
            // 
            this.commandBarSeparator1.DisplayName = "commandBarSeparator1";
            this.commandBarSeparator1.Name = "commandBarSeparator1";
            this.commandBarSeparator1.VisibleInOverflowMenu = false;
            // 
            // helpButton
            // 
            this.helpButton.DisplayName = "Help";
            this.helpButton.DrawText = true;
            this.helpButton.Name = "helpButton";
            this.helpButton.Text = "Help";
            // 
            // nameLabel
            // 
            this.nameLabel.Location = new System.Drawing.Point(12, 62);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(38, 18);
            this.nameLabel.TabIndex = 2;
            this.nameLabel.Text = "Name:";
            // 
            // nameBox
            // 
            this.nameBox.Location = new System.Drawing.Point(80, 60);
            this.nameBox.Name = "nameBox";
            this.nameBox.NullText = "Your name";
            this.nameBox.Size = new System.Drawing.Size(180, 20);
            this.nameBox.TabIndex = 3;
            this.nameBox.TextChanged += new System.EventHandler(this.nameBox_TextChanged);
            // 
            // agreeCheck
            // 
            this.agreeCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.agreeCheck.Location = new System.Drawing.Point(80, 86);
            this.agreeCheck.Name = "agreeCheck";
            this.agreeCheck.Size = new System.Drawing.Size(100, 18);
            this.agreeCheck.TabIndex = 4;
            this.agreeCheck.Text = "I agree";
            this.agreeCheck.ToggleStateChanged += new Telerik.WinControls.UI.StateChangedEventHandler(this.agreeCheck_ToggleStateChanged);
            // 
            // colorList
            // 
            this.colorList.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            radListDataItem1.Text = "Red";
            radListDataItem2.Text = "Blue";
            this.colorList.Items.Add(radListDataItem1);
            this.colorList.Items.Add(radListDataItem2);
            this.colorList.Location = new System.Drawing.Point(80, 110);
            this.colorList.Name = "colorList";
            this.colorList.Size = new System.Drawing.Size(125, 20);
            this.colorList.TabIndex = 5;
            // 
            // countSpin
            // 
            this.countSpin.Location = new System.Drawing.Point(80, 136);
            this.countSpin.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.countSpin.Name = "countSpin";
            this.countSpin.Size = new System.Drawing.Size(100, 20);
            this.countSpin.TabIndex = 6;
            this.countSpin.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(80, 162);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(100, 24);
            this.okButton.TabIndex = 7;
            this.okButton.Text = "OK";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(80, 192);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(180, 20);
            this.progress.TabIndex = 8;
            this.progress.Value1 = 40;
            // 
            // pages
            // 
            this.pages.Controls.Add(this.gridPage);
            this.pages.Controls.Add(this.treePage);
            this.pages.Location = new System.Drawing.Point(290, 60);
            this.pages.Name = "pages";
            this.pages.SelectedPage = this.gridPage;
            this.pages.Size = new System.Drawing.Size(300, 250);
            this.pages.TabIndex = 9;
            // 
            // gridPage
            // 
            this.gridPage.Controls.Add(this.ordersGrid);
            this.gridPage.ItemSize = new System.Drawing.SizeF(45F, 28F);
            this.gridPage.Location = new System.Drawing.Point(10, 37);
            this.gridPage.Name = "gridPage";
            this.gridPage.Size = new System.Drawing.Size(279, 202);
            this.gridPage.Text = "Orders";
            // 
            // ordersGrid
            // 
            this.ordersGrid.Location = new System.Drawing.Point(0, 0);
            // 
            // 
            // 
            this.ordersGrid.MasterTemplate.AllowAddNewRow = false;
            gridViewTextBoxColumn1.FieldName = "Customer";
            gridViewTextBoxColumn1.HeaderText = "Customer";
            gridViewTextBoxColumn1.Name = "colCustomer";
            gridViewTextBoxColumn1.Width = 120;
            gridViewCheckBoxColumn1.FieldName = "Paid";
            gridViewCheckBoxColumn1.HeaderText = "Paid";
            gridViewCheckBoxColumn1.Name = "colPaid";
            gridViewDecimalColumn1.FieldName = "Total";
            gridViewDecimalColumn1.HeaderText = "Total";
            gridViewDecimalColumn1.IsVisible = false;
            gridViewDecimalColumn1.Name = "colTotal";
            this.ordersGrid.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewTextBoxColumn1,
            gridViewCheckBoxColumn1,
            gridViewDecimalColumn1});
            this.ordersGrid.MasterTemplate.ViewDefinition = tableViewDefinition1;
            this.ordersGrid.Name = "ordersGrid";
            this.ordersGrid.Size = new System.Drawing.Size(279, 202);
            this.ordersGrid.TabIndex = 0;
            // 
            // treePage
            // 
            this.treePage.Controls.Add(this.folderTree);
            this.treePage.ItemSize = new System.Drawing.SizeF(52F, 28F);
            this.treePage.Location = new System.Drawing.Point(10, 37);
            this.treePage.Name = "treePage";
            this.treePage.Size = new System.Drawing.Size(279, 202);
            this.treePage.Text = "Folders";
            // 
            // folderTree
            // 
            this.folderTree.Location = new System.Drawing.Point(0, 0);
            this.folderTree.Name = "folderTree";
            radTreeNode1.Expanded = true;
            radTreeNode1.Name = "Node1";
            radTreeNode2.Name = "Node2";
            radTreeNode2.Text = "Child";
            radTreeNode1.Nodes.AddRange(new Telerik.WinControls.UI.RadTreeNode[] {
            radTreeNode2});
            radTreeNode1.Text = "Root";
            this.folderTree.Nodes.AddRange(new Telerik.WinControls.UI.RadTreeNode[] {
            radTreeNode1});
            this.folderTree.Size = new System.Drawing.Size(279, 202);
            this.folderTree.TabIndex = 0;
            // 
            // optionsGroup
            // 
            this.optionsGroup.Controls.Add(this.slowRadio);
            this.optionsGroup.Controls.Add(this.fastRadio);
            this.optionsGroup.HeaderText = "Speed";
            this.optionsGroup.Location = new System.Drawing.Point(12, 220);
            this.optionsGroup.Name = "optionsGroup";
            this.optionsGroup.Size = new System.Drawing.Size(200, 80);
            this.optionsGroup.TabIndex = 10;
            // 
            // fastRadio
            // 
            this.fastRadio.CheckState = System.Windows.Forms.CheckState.Checked;
            this.fastRadio.Location = new System.Drawing.Point(10, 24);
            this.fastRadio.Name = "fastRadio";
            this.fastRadio.Size = new System.Drawing.Size(80, 18);
            this.fastRadio.TabIndex = 0;
            this.fastRadio.Text = "Fast";
            // 
            // slowRadio
            // 
            this.slowRadio.Location = new System.Drawing.Point(10, 48);
            this.slowRadio.Name = "slowRadio";
            this.slowRadio.Size = new System.Drawing.Size(80, 18);
            this.slowRadio.TabIndex = 1;
            this.slowRadio.Text = "Slow";
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new Telerik.WinControls.RadItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 376);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(600, 24);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 11;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusStrip.SetSpring(this.statusLabel, false);
            this.statusLabel.Text = "Ready";
            this.statusLabel.TextWrap = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.optionsGroup);
            this.Controls.Add(this.pages);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.countSpin);
            this.Controls.Add(this.colorList);
            this.Controls.Add(this.agreeCheck);
            this.Controls.Add(this.nameBox);
            this.Controls.Add(this.nameLabel);
            this.Controls.Add(this.radCommandBar1);
            this.Controls.Add(this.radMenu1);
            this.Name = "MainForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.Text = "Telerik demo";
            this.ThemeName = "Fluent";
            ((Telerik.WinControls.Primitives.FillPrimitive)(this.okButton.GetChildAt(0).GetChildAt(0))).BackColor = System.Drawing.Color.SteelBlue;
            ((System.ComponentModel.ISupportInitialize)(this.radMenu1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radCommandBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameLabel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.agreeCheck)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.colorList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.countSpin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.okButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.progress)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pages)).EndInit();
            this.pages.ResumeLayout(false);
            this.gridPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ordersGrid.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ordersGrid)).EndInit();
            this.treePage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.folderTree)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.optionsGroup)).EndInit();
            this.optionsGroup.ResumeLayout(false);
            this.optionsGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fastRadio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.slowRadio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusStrip)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Telerik.WinControls.UI.RadMenu radMenu1;
        private Telerik.WinControls.UI.RadMenuItem fileMenuItem;
        private Telerik.WinControls.UI.RadMenuItem openMenuItem;
        private Telerik.WinControls.UI.RadMenuSeparatorItem radMenuSeparatorItem1;
        private Telerik.WinControls.UI.RadMenuItem exitMenuItem;
        private Telerik.WinControls.UI.RadCommandBar radCommandBar1;
        private Telerik.WinControls.UI.CommandBarRowElement commandBarRowElement1;
        private Telerik.WinControls.UI.CommandBarStripElement commandBarStripElement1;
        private Telerik.WinControls.UI.CommandBarButton newButton;
        private Telerik.WinControls.UI.CommandBarSeparator commandBarSeparator1;
        private Telerik.WinControls.UI.CommandBarButton helpButton;
        private Telerik.WinControls.UI.RadLabel nameLabel;
        private Telerik.WinControls.UI.RadTextBox nameBox;
        private Telerik.WinControls.UI.RadCheckBox agreeCheck;
        private Telerik.WinControls.UI.RadDropDownList colorList;
        private Telerik.WinControls.UI.RadSpinEditor countSpin;
        private Telerik.WinControls.UI.RadButton okButton;
        private Telerik.WinControls.UI.RadProgressBar progress;
        private Telerik.WinControls.UI.RadPageView pages;
        private Telerik.WinControls.UI.RadPageViewPage gridPage;
        private Telerik.WinControls.UI.RadGridView ordersGrid;
        private Telerik.WinControls.UI.RadPageViewPage treePage;
        private Telerik.WinControls.UI.RadTreeView folderTree;
        private Telerik.WinControls.UI.RadGroupBox optionsGroup;
        private Telerik.WinControls.UI.RadRadioButton fastRadio;
        private Telerik.WinControls.UI.RadRadioButton slowRadio;
        private Telerik.WinControls.UI.RadStatusStrip statusStrip;
        private Telerik.WinControls.UI.RadLabelElement statusLabel;
    }
}
