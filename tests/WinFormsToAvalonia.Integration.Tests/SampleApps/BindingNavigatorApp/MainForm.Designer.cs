namespace BindingNavigatorApp
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
            this.bindingSource1 = new BindingSource(this.components);
            this.bindingNavigator1 = new BindingNavigator(this.components);
            this.bindingNavigatorMoveFirstItem = new ToolStripButton();
            this.bindingNavigatorMovePreviousItem = new ToolStripButton();
            this.bindingNavigatorMoveNextItem = new ToolStripButton();
            this.bindingNavigatorMoveLastItem = new ToolStripButton();
            this.bindingNavigatorAddNewItem = new ToolStripButton();
            this.tracksGrid = new DataGridView();
            this.titleColumn = new DataGridViewTextBoxColumn();
            this.artistColumn = new DataGridViewTextBoxColumn();
            this.SuspendLayout();
            //
            // titleColumn
            //
            this.titleColumn.DataPropertyName = "Title";
            this.titleColumn.HeaderText = "Title";
            this.titleColumn.Name = "titleColumn";
            //
            // artistColumn
            //
            this.artistColumn.DataPropertyName = "Artist";
            this.artistColumn.HeaderText = "Artist";
            this.artistColumn.Name = "artistColumn";
            //
            // tracksGrid
            //
            this.tracksGrid.Columns.AddRange(new DataGridViewColumn[] {
                this.titleColumn,
                this.artistColumn});
            this.tracksGrid.DataSource = this.bindingSource1;
            this.tracksGrid.Location = new System.Drawing.Point(12, 12);
            this.tracksGrid.Name = "tracksGrid";
            this.tracksGrid.Size = new System.Drawing.Size(400, 160);
            //
            // bindingNavigatorMoveFirstItem
            //
            this.bindingNavigatorMoveFirstItem.Name = "bindingNavigatorMoveFirstItem";
            this.bindingNavigatorMoveFirstItem.Text = "Move first";
            //
            // bindingNavigatorMovePreviousItem
            //
            this.bindingNavigatorMovePreviousItem.Name = "bindingNavigatorMovePreviousItem";
            this.bindingNavigatorMovePreviousItem.Text = "Move previous";
            //
            // bindingNavigatorMoveNextItem
            //
            this.bindingNavigatorMoveNextItem.Name = "bindingNavigatorMoveNextItem";
            this.bindingNavigatorMoveNextItem.Text = "Move next";
            //
            // bindingNavigatorMoveLastItem
            //
            this.bindingNavigatorMoveLastItem.Name = "bindingNavigatorMoveLastItem";
            this.bindingNavigatorMoveLastItem.Text = "Move last";
            //
            // bindingNavigatorAddNewItem
            //
            this.bindingNavigatorAddNewItem.Name = "bindingNavigatorAddNewItem";
            this.bindingNavigatorAddNewItem.Text = "Add new";
            //
            // bindingNavigator1
            //
            this.bindingNavigator1.AddNewItem = this.bindingNavigatorAddNewItem;
            this.bindingNavigator1.BindingSource = this.bindingSource1;
            this.bindingNavigator1.Items.AddRange(new ToolStripItem[] {
                this.bindingNavigatorMoveFirstItem,
                this.bindingNavigatorMovePreviousItem,
                this.bindingNavigatorMoveNextItem,
                this.bindingNavigatorMoveLastItem,
                this.bindingNavigatorAddNewItem});
            this.bindingNavigator1.Location = new System.Drawing.Point(12, 184);
            this.bindingNavigator1.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.bindingNavigator1.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.bindingNavigator1.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.bindingNavigator1.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.bindingNavigator1.Name = "bindingNavigator1";
            this.bindingNavigator1.Size = new System.Drawing.Size(400, 25);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(430, 230);
            this.Controls.Add(this.tracksGrid);
            this.Controls.Add(this.bindingNavigator1);
            this.Name = "MainForm";
            this.Text = "Binding navigator";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
        }

        private BindingSource bindingSource1;
        private BindingNavigator bindingNavigator1;
        private ToolStripButton bindingNavigatorMoveFirstItem;
        private ToolStripButton bindingNavigatorMovePreviousItem;
        private ToolStripButton bindingNavigatorMoveNextItem;
        private ToolStripButton bindingNavigatorMoveLastItem;
        private ToolStripButton bindingNavigatorAddNewItem;
        private DataGridView tracksGrid;
        private DataGridViewTextBoxColumn titleColumn;
        private DataGridViewTextBoxColumn artistColumn;
    }
}
