namespace TelerikApp
{
    partial class EditorForm
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
            this.radRibbonBar1 = new Telerik.WinControls.UI.RadRibbonBar();
            this.homeTab = new Telerik.WinControls.UI.RibbonTab();
            this.clipboardGroup = new Telerik.WinControls.UI.RadRibbonBarGroup();
            this.copyButton = new Telerik.WinControls.UI.RadButtonElement();
            this.pasteButton = new Telerik.WinControls.UI.RadButtonElement();
            this.radSplitContainer1 = new Telerik.WinControls.UI.RadSplitContainer();
            this.splitPanel1 = new Telerik.WinControls.UI.SplitPanel();
            this.radLayoutControl1 = new Telerik.WinControls.UI.RadLayoutControl();
            this.cityBox = new Telerik.WinControls.UI.RadTextBox();
            this.layoutControlItem1 = new Telerik.WinControls.UI.LayoutControlItem();
            this.splitPanel2 = new Telerik.WinControls.UI.SplitPanel();
            this.notesBox = new Telerik.WinControls.UI.RadTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.radRibbonBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radSplitContainer1)).BeginInit();
            this.radSplitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel1)).BeginInit();
            this.splitPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radLayoutControl1)).BeginInit();
            this.radLayoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cityBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel2)).BeginInit();
            this.splitPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.notesBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // radRibbonBar1
            // 
            this.radRibbonBar1.CommandTabs.AddRange(new Telerik.WinControls.RadItem[] {
            this.homeTab});
            this.radRibbonBar1.Location = new System.Drawing.Point(0, 0);
            this.radRibbonBar1.Name = "radRibbonBar1";
            this.radRibbonBar1.Size = new System.Drawing.Size(560, 160);
            this.radRibbonBar1.TabIndex = 0;
            this.radRibbonBar1.Text = "Editor";
            // 
            // homeTab
            // 
            this.homeTab.IsSelected = true;
            this.homeTab.Items.AddRange(new Telerik.WinControls.RadItem[] {
            this.clipboardGroup});
            this.homeTab.Name = "homeTab";
            this.homeTab.Text = "Home";
            // 
            // clipboardGroup
            // 
            this.clipboardGroup.Items.AddRange(new Telerik.WinControls.RadItem[] {
            this.copyButton,
            this.pasteButton});
            this.clipboardGroup.Name = "clipboardGroup";
            this.clipboardGroup.Text = "Clipboard";
            // 
            // copyButton
            // 
            this.copyButton.Name = "copyButton";
            this.copyButton.Text = "Copy";
            this.copyButton.Click += new System.EventHandler(this.copyButton_Click);
            // 
            // pasteButton
            // 
            this.pasteButton.Name = "pasteButton";
            this.pasteButton.Text = "Paste";
            // 
            // radSplitContainer1
            // 
            this.radSplitContainer1.Controls.Add(this.splitPanel1);
            this.radSplitContainer1.Controls.Add(this.splitPanel2);
            this.radSplitContainer1.Location = new System.Drawing.Point(0, 160);
            this.radSplitContainer1.Name = "radSplitContainer1";
            this.radSplitContainer1.Size = new System.Drawing.Size(560, 240);
            this.radSplitContainer1.SplitterWidth = 4;
            this.radSplitContainer1.TabIndex = 1;
            this.radSplitContainer1.TabStop = false;
            // 
            // splitPanel1
            // 
            this.splitPanel1.Controls.Add(this.radLayoutControl1);
            this.splitPanel1.Location = new System.Drawing.Point(0, 0);
            this.splitPanel1.Name = "splitPanel1";
            this.splitPanel1.Size = new System.Drawing.Size(278, 240);
            this.splitPanel1.TabIndex = 0;
            this.splitPanel1.TabStop = false;
            // 
            // radLayoutControl1
            // 
            this.radLayoutControl1.Controls.Add(this.cityBox);
            this.radLayoutControl1.Items.AddRange(new Telerik.WinControls.RadItem[] {
            this.layoutControlItem1});
            this.radLayoutControl1.Location = new System.Drawing.Point(0, 0);
            this.radLayoutControl1.Name = "radLayoutControl1";
            this.radLayoutControl1.Size = new System.Drawing.Size(278, 240);
            this.radLayoutControl1.TabIndex = 0;
            // 
            // cityBox
            // 
            this.cityBox.Location = new System.Drawing.Point(80, 4);
            this.cityBox.Name = "cityBox";
            this.cityBox.Size = new System.Drawing.Size(194, 20);
            this.cityBox.TabIndex = 0;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.AssociatedControl = this.cityBox;
            this.layoutControlItem1.Bounds = new System.Drawing.Rectangle(0, 0, 278, 28);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Text = "City";
            this.layoutControlItem1.TextFixedSize = 76;
            this.layoutControlItem1.TextSizeMode = Telerik.WinControls.UI.LayoutItemTextSizeMode.Fixed;
            // 
            // splitPanel2
            // 
            this.splitPanel2.Controls.Add(this.notesBox);
            this.splitPanel2.Location = new System.Drawing.Point(282, 0);
            this.splitPanel2.Name = "splitPanel2";
            this.splitPanel2.Size = new System.Drawing.Size(278, 240);
            this.splitPanel2.TabIndex = 1;
            this.splitPanel2.TabStop = false;
            // 
            // notesBox
            // 
            this.notesBox.AutoSize = false;
            this.notesBox.Location = new System.Drawing.Point(4, 4);
            this.notesBox.Multiline = true;
            this.notesBox.Name = "notesBox";
            this.notesBox.Size = new System.Drawing.Size(270, 232);
            this.notesBox.TabIndex = 0;
            // 
            // EditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 400);
            this.Controls.Add(this.radSplitContainer1);
            this.Controls.Add(this.radRibbonBar1);
            this.MainMenuStrip = null;
            this.Name = "EditorForm";
            this.Text = "Editor";
            ((System.ComponentModel.ISupportInitialize)(this.radRibbonBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radSplitContainer1)).EndInit();
            this.radSplitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel1)).EndInit();
            this.splitPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radLayoutControl1)).EndInit();
            this.radLayoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cityBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel2)).EndInit();
            this.splitPanel2.ResumeLayout(false);
            this.splitPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.notesBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Telerik.WinControls.UI.RadRibbonBar radRibbonBar1;
        private Telerik.WinControls.UI.RibbonTab homeTab;
        private Telerik.WinControls.UI.RadRibbonBarGroup clipboardGroup;
        private Telerik.WinControls.UI.RadButtonElement copyButton;
        private Telerik.WinControls.UI.RadButtonElement pasteButton;
        private Telerik.WinControls.UI.RadSplitContainer radSplitContainer1;
        private Telerik.WinControls.UI.SplitPanel splitPanel1;
        private Telerik.WinControls.UI.RadLayoutControl radLayoutControl1;
        private Telerik.WinControls.UI.RadTextBox cityBox;
        private Telerik.WinControls.UI.LayoutControlItem layoutControlItem1;
        private Telerik.WinControls.UI.SplitPanel splitPanel2;
        private Telerik.WinControls.UI.RadTextBox notesBox;
    }
}
