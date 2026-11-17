namespace DevExpressApp
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
            this.components = new System.ComponentModel.Container();
            this.barManager1 = new DevExpress.XtraBars.BarManager(this.components);
            this.toolsBar = new DevExpress.XtraBars.Bar();
            this.menuBar = new DevExpress.XtraBars.Bar();
            this.statusBar = new DevExpress.XtraBars.Bar();
            this.barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            this.fileSubItem = new DevExpress.XtraBars.BarSubItem();
            this.openItem = new DevExpress.XtraBars.BarButtonItem();
            this.exitItem = new DevExpress.XtraBars.BarButtonItem();
            this.statusItem = new DevExpress.XtraBars.BarStaticItem();
            this.nameLabel = new DevExpress.XtraEditors.LabelControl();
            this.nameEdit = new DevExpress.XtraEditors.TextEdit();
            this.notesMemo = new DevExpress.XtraEditors.MemoEdit();
            this.ageSpin = new DevExpress.XtraEditors.SpinEdit();
            this.activeCheck = new DevExpress.XtraEditors.CheckEdit();
            this.colorCombo = new DevExpress.XtraEditors.ComboBoxEdit();
            this.darkToggle = new DevExpress.XtraEditors.ToggleSwitch();
            this.saveButton = new DevExpress.XtraEditors.SimpleButton();
            this.progress = new DevExpress.XtraEditors.ProgressBarControl();
            this.sizeGroup = new DevExpress.XtraEditors.RadioGroup();
            this.tabs = new DevExpress.XtraTab.XtraTabControl();
            this.gridPage = new DevExpress.XtraTab.XtraTabPage();
            this.peopleGrid = new DevExpress.XtraGrid.GridControl();
            this.peopleView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colAge = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colActive = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemCheckEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.optionsPage = new DevExpress.XtraTab.XtraTabPage();
            this.optionsGroup = new DevExpress.XtraEditors.GroupControl();
            this.contactCard1 = new DevExpressApp.ContactCard();
            ((System.ComponentModel.ISupportInitialize)(this.barManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameEdit.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.notesMemo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ageSpin.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.activeCheck.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.colorCombo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.darkToggle.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.progress.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sizeGroup.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tabs)).BeginInit();
            this.tabs.SuspendLayout();
            this.gridPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.peopleGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.peopleView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemCheckEdit1)).BeginInit();
            this.optionsPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.optionsGroup)).BeginInit();
            this.optionsGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // barManager1
            // 
            this.barManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] {
            this.toolsBar,
            this.menuBar,
            this.statusBar});
            this.barManager1.DockControls.Add(this.barDockControlTop);
            this.barManager1.DockControls.Add(this.barDockControlBottom);
            this.barManager1.DockControls.Add(this.barDockControlLeft);
            this.barManager1.DockControls.Add(this.barDockControlRight);
            this.barManager1.Form = this;
            this.barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.fileSubItem,
            this.openItem,
            this.exitItem,
            this.statusItem});
            this.barManager1.MainMenu = this.menuBar;
            this.barManager1.MaxItemId = 4;
            this.barManager1.StatusBar = this.statusBar;
            // 
            // toolsBar
            // 
            this.toolsBar.BarName = "Tools";
            this.toolsBar.DockCol = 0;
            this.toolsBar.DockRow = 1;
            this.toolsBar.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            this.toolsBar.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(this.openItem),
            new DevExpress.XtraBars.LinkPersistInfo(this.exitItem, true)});
            this.toolsBar.Text = "Tools";
            // 
            // menuBar
            // 
            this.menuBar.BarName = "Main menu";
            this.menuBar.DockCol = 0;
            this.menuBar.DockRow = 0;
            this.menuBar.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            this.menuBar.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(this.fileSubItem)});
            this.menuBar.OptionsBar.MultiLine = true;
            this.menuBar.OptionsBar.UseWholeRow = true;
            this.menuBar.Text = "Main menu";
            // 
            // statusBar
            // 
            this.statusBar.BarName = "Status bar";
            this.statusBar.CanDockStyle = DevExpress.XtraBars.BarCanDockStyle.Bottom;
            this.statusBar.DockCol = 0;
            this.statusBar.DockRow = 0;
            this.statusBar.DockStyle = DevExpress.XtraBars.BarDockStyle.Bottom;
            this.statusBar.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(this.statusItem)});
            this.statusBar.OptionsBar.AllowQuickCustomization = false;
            this.statusBar.OptionsBar.DrawDragBorder = false;
            this.statusBar.OptionsBar.UseWholeRow = true;
            this.statusBar.Text = "Status bar";
            // 
            // barDockControlTop
            // 
            this.barDockControlTop.CausesValidation = false;
            this.barDockControlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.barDockControlTop.Location = new System.Drawing.Point(0, 0);
            this.barDockControlTop.Manager = this.barManager1;
            this.barDockControlTop.Size = new System.Drawing.Size(640, 51);
            // 
            // barDockControlBottom
            // 
            this.barDockControlBottom.CausesValidation = false;
            this.barDockControlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.barDockControlBottom.Location = new System.Drawing.Point(0, 458);
            this.barDockControlBottom.Manager = this.barManager1;
            this.barDockControlBottom.Size = new System.Drawing.Size(640, 22);
            // 
            // barDockControlLeft
            // 
            this.barDockControlLeft.CausesValidation = false;
            this.barDockControlLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.barDockControlLeft.Location = new System.Drawing.Point(0, 51);
            this.barDockControlLeft.Manager = this.barManager1;
            this.barDockControlLeft.Size = new System.Drawing.Size(0, 407);
            // 
            // barDockControlRight
            // 
            this.barDockControlRight.CausesValidation = false;
            this.barDockControlRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.barDockControlRight.Location = new System.Drawing.Point(640, 51);
            this.barDockControlRight.Manager = this.barManager1;
            this.barDockControlRight.Size = new System.Drawing.Size(0, 407);
            // 
            // fileSubItem
            // 
            this.fileSubItem.Caption = "&File";
            this.fileSubItem.Id = 0;
            this.fileSubItem.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(this.openItem),
            new DevExpress.XtraBars.LinkPersistInfo(this.exitItem, true)});
            this.fileSubItem.Name = "fileSubItem";
            // 
            // openItem
            // 
            this.openItem.Caption = "Open";
            this.openItem.Id = 1;
            this.openItem.Name = "openItem";
            this.openItem.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.openItem_ItemClick);
            // 
            // exitItem
            // 
            this.exitItem.Caption = "Exit";
            this.exitItem.Id = 2;
            this.exitItem.Name = "exitItem";
            this.exitItem.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.exitItem_ItemClick);
            // 
            // statusItem
            // 
            this.statusItem.Caption = "Ready";
            this.statusItem.Id = 3;
            this.statusItem.Name = "statusItem";
            // 
            // nameLabel
            // 
            this.nameLabel.Location = new System.Drawing.Point(12, 67);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(31, 13);
            this.nameLabel.TabIndex = 4;
            this.nameLabel.Text = "Name:";
            // 
            // nameEdit
            // 
            this.nameEdit.EditValue = "Ada";
            this.nameEdit.Location = new System.Drawing.Point(80, 64);
            this.nameEdit.MenuManager = this.barManager1;
            this.nameEdit.Name = "nameEdit";
            this.nameEdit.Properties.MaxLength = 40;
            this.nameEdit.Properties.NullValuePrompt = "Your name";
            this.nameEdit.Size = new System.Drawing.Size(200, 20);
            this.nameEdit.TabIndex = 5;
            this.nameEdit.EditValueChanged += new System.EventHandler(this.nameEdit_EditValueChanged);
            // 
            // notesMemo
            // 
            this.notesMemo.Location = new System.Drawing.Point(80, 90);
            this.notesMemo.MenuManager = this.barManager1;
            this.notesMemo.Name = "notesMemo";
            this.notesMemo.Properties.WordWrap = true;
            this.notesMemo.Size = new System.Drawing.Size(200, 60);
            this.notesMemo.TabIndex = 6;
            // 
            // ageSpin
            // 
            this.ageSpin.EditValue = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.ageSpin.Location = new System.Drawing.Point(80, 156);
            this.ageSpin.MenuManager = this.barManager1;
            this.ageSpin.Name = "ageSpin";
            this.ageSpin.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.ageSpin.Properties.MaxValue = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.ageSpin.Size = new System.Drawing.Size(100, 20);
            this.ageSpin.TabIndex = 7;
            // 
            // activeCheck
            // 
            this.activeCheck.EditValue = true;
            this.activeCheck.Location = new System.Drawing.Point(80, 182);
            this.activeCheck.MenuManager = this.barManager1;
            this.activeCheck.Name = "activeCheck";
            this.activeCheck.Properties.Caption = "Active";
            this.activeCheck.Size = new System.Drawing.Size(100, 19);
            this.activeCheck.TabIndex = 8;
            this.activeCheck.CheckedChanged += new System.EventHandler(this.activeCheck_CheckedChanged);
            // 
            // colorCombo
            // 
            this.colorCombo.EditValue = "Red";
            this.colorCombo.Location = new System.Drawing.Point(80, 207);
            this.colorCombo.MenuManager = this.barManager1;
            this.colorCombo.Name = "colorCombo";
            this.colorCombo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.colorCombo.Properties.Items.AddRange(new object[] {
            "Red",
            "Green",
            "Blue"});
            this.colorCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.colorCombo.Size = new System.Drawing.Size(100, 20);
            this.colorCombo.TabIndex = 9;
            // 
            // darkToggle
            // 
            this.darkToggle.Location = new System.Drawing.Point(80, 233);
            this.darkToggle.MenuManager = this.barManager1;
            this.darkToggle.Name = "darkToggle";
            this.darkToggle.Properties.OffText = "Off";
            this.darkToggle.Properties.OnText = "On";
            this.darkToggle.Size = new System.Drawing.Size(95, 18);
            this.darkToggle.TabIndex = 10;
            this.darkToggle.Toggled += new System.EventHandler(this.darkToggle_Toggled);
            // 
            // saveButton
            // 
            this.saveButton.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.saveButton.Appearance.Options.UseFont = true;
            this.saveButton.Location = new System.Drawing.Point(80, 259);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(100, 23);
            this.saveButton.TabIndex = 11;
            this.saveButton.Text = "Save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // progress
            // 
            this.progress.EditValue = 3;
            this.progress.Location = new System.Drawing.Point(80, 288);
            this.progress.MenuManager = this.barManager1;
            this.progress.Name = "progress";
            this.progress.Properties.Maximum = 40;
            this.progress.Size = new System.Drawing.Size(200, 18);
            this.progress.TabIndex = 12;
            // 
            // sizeGroup
            // 
            this.sizeGroup.EditValue = 1;
            this.sizeGroup.Location = new System.Drawing.Point(80, 312);
            this.sizeGroup.MenuManager = this.barManager1;
            this.sizeGroup.Name = "sizeGroup";
            this.sizeGroup.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem(0, "Small"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(1, "Medium"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(2, "Large")});
            this.sizeGroup.Size = new System.Drawing.Size(120, 75);
            this.sizeGroup.TabIndex = 13;
            // 
            // tabs
            // 
            this.tabs.Location = new System.Drawing.Point(300, 64);
            this.tabs.Name = "tabs";
            this.tabs.SelectedTabPage = this.gridPage;
            this.tabs.Size = new System.Drawing.Size(328, 380);
            this.tabs.TabIndex = 14;
            this.tabs.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
            this.gridPage,
            this.optionsPage});
            // 
            // gridPage
            // 
            this.gridPage.Controls.Add(this.peopleGrid);
            this.gridPage.Name = "gridPage";
            this.gridPage.Size = new System.Drawing.Size(326, 355);
            this.gridPage.Text = "People";
            // 
            // peopleGrid
            // 
            this.peopleGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.peopleGrid.Location = new System.Drawing.Point(0, 0);
            this.peopleGrid.MainView = this.peopleView;
            this.peopleGrid.MenuManager = this.barManager1;
            this.peopleGrid.Name = "peopleGrid";
            this.peopleGrid.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemCheckEdit1});
            this.peopleGrid.Size = new System.Drawing.Size(326, 355);
            this.peopleGrid.TabIndex = 0;
            this.peopleGrid.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.peopleView});
            // 
            // peopleView
            // 
            this.peopleView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colName,
            this.colAge,
            this.colActive,
            this.colId});
            this.peopleView.GridControl = this.peopleGrid;
            this.peopleView.Name = "peopleView";
            this.peopleView.OptionsBehavior.Editable = false;
            this.peopleView.OptionsView.ShowGroupPanel = false;
            this.peopleView.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.peopleView_FocusedRowChanged);
            // 
            // colName
            // 
            this.colName.Caption = "Name";
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.Visible = true;
            this.colName.VisibleIndex = 0;
            // 
            // colAge
            // 
            this.colAge.Caption = "Age";
            this.colAge.FieldName = "Age";
            this.colAge.Name = "colAge";
            this.colAge.Visible = true;
            this.colAge.VisibleIndex = 2;
            // 
            // colActive
            // 
            this.colActive.Caption = "Active";
            this.colActive.ColumnEdit = this.repositoryItemCheckEdit1;
            this.colActive.FieldName = "Active";
            this.colActive.Name = "colActive";
            this.colActive.Visible = true;
            this.colActive.VisibleIndex = 1;
            // 
            // colId
            // 
            this.colId.Caption = "Id";
            this.colId.FieldName = "Id";
            this.colId.Name = "colId";
            // 
            // repositoryItemCheckEdit1
            // 
            this.repositoryItemCheckEdit1.AutoHeight = false;
            this.repositoryItemCheckEdit1.Name = "repositoryItemCheckEdit1";
            // 
            // optionsPage
            // 
            this.optionsPage.Controls.Add(this.optionsGroup);
            this.optionsPage.Name = "optionsPage";
            this.optionsPage.Size = new System.Drawing.Size(326, 355);
            this.optionsPage.Text = "Options";
            // 
            // optionsGroup
            // 
            this.optionsGroup.Controls.Add(this.contactCard1);
            this.optionsGroup.Location = new System.Drawing.Point(8, 8);
            this.optionsGroup.Name = "optionsGroup";
            this.optionsGroup.Size = new System.Drawing.Size(310, 200);
            this.optionsGroup.TabIndex = 0;
            this.optionsGroup.Text = "Contact";
            // 
            // contactCard1
            // 
            this.contactCard1.Location = new System.Drawing.Point(10, 30);
            this.contactCard1.Name = "contactCard1";
            this.contactCard1.Size = new System.Drawing.Size(280, 80);
            this.contactCard1.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 480);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.sizeGroup);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.darkToggle);
            this.Controls.Add(this.colorCombo);
            this.Controls.Add(this.activeCheck);
            this.Controls.Add(this.ageSpin);
            this.Controls.Add(this.notesMemo);
            this.Controls.Add(this.nameEdit);
            this.Controls.Add(this.nameLabel);
            this.Controls.Add(this.barDockControlLeft);
            this.Controls.Add(this.barDockControlRight);
            this.Controls.Add(this.barDockControlBottom);
            this.Controls.Add(this.barDockControlTop);
            this.IconOptions.ShowIcon = false;
            this.Name = "MainForm";
            this.Text = "DevExpress demo";
            ((System.ComponentModel.ISupportInitialize)(this.barManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameEdit.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.notesMemo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ageSpin.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.activeCheck.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.colorCombo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.darkToggle.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.progress.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sizeGroup.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tabs)).EndInit();
            this.tabs.ResumeLayout(false);
            this.gridPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.peopleGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.peopleView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemCheckEdit1)).EndInit();
            this.optionsPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.optionsGroup)).EndInit();
            this.optionsGroup.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraBars.BarManager barManager1;
        private DevExpress.XtraBars.Bar toolsBar;
        private DevExpress.XtraBars.Bar menuBar;
        private DevExpress.XtraBars.Bar statusBar;
        private DevExpress.XtraBars.BarDockControl barDockControlTop;
        private DevExpress.XtraBars.BarDockControl barDockControlBottom;
        private DevExpress.XtraBars.BarDockControl barDockControlLeft;
        private DevExpress.XtraBars.BarDockControl barDockControlRight;
        private DevExpress.XtraBars.BarSubItem fileSubItem;
        private DevExpress.XtraBars.BarButtonItem openItem;
        private DevExpress.XtraBars.BarButtonItem exitItem;
        private DevExpress.XtraBars.BarStaticItem statusItem;
        private DevExpress.XtraEditors.LabelControl nameLabel;
        private DevExpress.XtraEditors.TextEdit nameEdit;
        private DevExpress.XtraEditors.MemoEdit notesMemo;
        private DevExpress.XtraEditors.SpinEdit ageSpin;
        private DevExpress.XtraEditors.CheckEdit activeCheck;
        private DevExpress.XtraEditors.ComboBoxEdit colorCombo;
        private DevExpress.XtraEditors.ToggleSwitch darkToggle;
        private DevExpress.XtraEditors.SimpleButton saveButton;
        private DevExpress.XtraEditors.ProgressBarControl progress;
        private DevExpress.XtraEditors.RadioGroup sizeGroup;
        private DevExpress.XtraTab.XtraTabControl tabs;
        private DevExpress.XtraTab.XtraTabPage gridPage;
        private DevExpress.XtraGrid.GridControl peopleGrid;
        private DevExpress.XtraGrid.Views.Grid.GridView peopleView;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colAge;
        private DevExpress.XtraGrid.Columns.GridColumn colActive;
        private DevExpress.XtraGrid.Columns.GridColumn colId;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repositoryItemCheckEdit1;
        private DevExpress.XtraTab.XtraTabPage optionsPage;
        private DevExpress.XtraEditors.GroupControl optionsGroup;
        private DevExpressApp.ContactCard contactCard1;
    }
}
