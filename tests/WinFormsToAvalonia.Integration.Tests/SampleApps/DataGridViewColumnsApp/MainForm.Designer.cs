namespace DataGridViewColumnsApp
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
            this.dataGridView1 = new DataGridView();
            this.nameColumn = new DataGridViewTextBoxColumn();
            this.activeColumn = new DataGridViewCheckBoxColumn();
            this.categoryColumn = new DataGridViewComboBoxColumn();
            this.actionColumn = new DataGridViewButtonColumn();
            this.iconColumn = new DataGridViewImageColumn();
            this.linkColumn = new DataGridViewLinkColumn();
            this.toolStrip1 = new ToolStrip();
            this.dropDownButton1 = new ToolStripDropDownButton();
            this.dropDownItemA = new ToolStripMenuItem();
            this.splitButton1 = new ToolStripSplitButton();
            this.splitItemA = new ToolStripMenuItem();
            this.detailsListView = new ListView();
            this.listViewNameColumn = new ColumnHeader();
            this.SuspendLayout();
            //
            // nameColumn
            //
            this.nameColumn.HeaderText = "Name";
            this.nameColumn.Name = "nameColumn";
            //
            // activeColumn
            //
            this.activeColumn.HeaderText = "Active";
            this.activeColumn.Name = "activeColumn";
            //
            // categoryColumn
            //
            this.categoryColumn.HeaderText = "Category";
            this.categoryColumn.Name = "categoryColumn";
            //
            // actionColumn
            //
            this.actionColumn.HeaderText = "Action";
            this.actionColumn.Name = "actionColumn";
            this.actionColumn.Text = "Run";
            //
            // iconColumn
            //
            this.iconColumn.HeaderText = "Icon";
            this.iconColumn.Name = "iconColumn";
            //
            // linkColumn
            //
            this.linkColumn.HeaderText = "Link";
            this.linkColumn.Name = "linkColumn";
            //
            // dataGridView1
            //
            this.dataGridView1.Columns.AddRange(new DataGridViewColumn[] {
                this.nameColumn,
                this.activeColumn,
                this.categoryColumn,
                this.actionColumn,
                this.iconColumn,
                this.linkColumn});
            this.dataGridView1.Location = new System.Drawing.Point(12, 40);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(300, 150);
            this.dataGridView1.TabIndex = 0;
            //
            // dropDownItemA
            //
            this.dropDownItemA.Name = "dropDownItemA";
            this.dropDownItemA.Text = "Drop-down item A";
            //
            // dropDownButton1
            //
            this.dropDownButton1.DropDownItems.AddRange(new ToolStripItem[] {
                this.dropDownItemA});
            this.dropDownButton1.Name = "dropDownButton1";
            this.dropDownButton1.Text = "Layout";
            //
            // splitItemA
            //
            this.splitItemA.Name = "splitItemA";
            this.splitItemA.Text = "Split item A";
            //
            // splitButton1
            //
            this.splitButton1.DropDownItems.AddRange(new ToolStripItem[] {
                this.splitItemA});
            this.splitButton1.Name = "splitButton1";
            this.splitButton1.Text = "Run";
            //
            // toolStrip1
            //
            this.toolStrip1.Items.AddRange(new ToolStripItem[] {
                this.dropDownButton1,
                this.splitButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(320, 25);
            //
            // listViewNameColumn
            //
            this.listViewNameColumn.Text = "File";
            this.listViewNameColumn.Width = 200;
            //
            // detailsListView
            //
            this.detailsListView.Columns.AddRange(new ColumnHeader[] {
                this.listViewNameColumn});
            this.detailsListView.Location = new System.Drawing.Point(12, 200);
            this.detailsListView.Name = "detailsListView";
            this.detailsListView.Size = new System.Drawing.Size(300, 100);
            this.detailsListView.View = View.Details;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(320, 320);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.detailsListView);
            this.Name = "MainForm";
            this.Text = "DataGridView Columns Demo";
            this.ResumeLayout(false);
        }

        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn nameColumn;
        private DataGridViewCheckBoxColumn activeColumn;
        private DataGridViewComboBoxColumn categoryColumn;
        private DataGridViewButtonColumn actionColumn;
        private DataGridViewImageColumn iconColumn;
        private DataGridViewLinkColumn linkColumn;
        private ToolStrip toolStrip1;
        private ToolStripDropDownButton dropDownButton1;
        private ToolStripMenuItem dropDownItemA;
        private ToolStripSplitButton splitButton1;
        private ToolStripMenuItem splitItemA;
        private ListView detailsListView;
        private ColumnHeader listViewNameColumn;
    }
}
