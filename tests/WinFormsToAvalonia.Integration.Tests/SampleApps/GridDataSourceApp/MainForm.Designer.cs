namespace GridDataSourceApp
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
            this.autoGrid = new DataGridView();
            this.boundGrid = new DataGridView();
            this.nameColumn = new DataGridViewTextBoxColumn();
            this.lockButton = new Button();
            this.SuspendLayout();
            //
            // autoGrid
            //
            this.autoGrid.Location = new System.Drawing.Point(12, 12);
            this.autoGrid.Name = "autoGrid";
            this.autoGrid.Size = new System.Drawing.Size(300, 120);
            //
            // nameColumn
            //
            this.nameColumn.DataPropertyName = "Name";
            this.nameColumn.HeaderText = "Name";
            this.nameColumn.Name = "nameColumn";
            //
            // boundGrid
            //
            this.boundGrid.AutoGenerateColumns = false;
            this.boundGrid.Columns.AddRange(new DataGridViewColumn[] { this.nameColumn });
            this.boundGrid.Location = new System.Drawing.Point(12, 140);
            this.boundGrid.Name = "boundGrid";
            this.boundGrid.Size = new System.Drawing.Size(300, 120);
            //
            // lockButton
            //
            this.lockButton.Location = new System.Drawing.Point(12, 270);
            this.lockButton.Name = "lockButton";
            this.lockButton.Size = new System.Drawing.Size(90, 25);
            this.lockButton.Text = "Lock";
            this.lockButton.Click += new System.EventHandler(this.lockButton_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(330, 310);
            this.Controls.Add(this.autoGrid);
            this.Controls.Add(this.boundGrid);
            this.Controls.Add(this.lockButton);
            this.Name = "MainForm";
            this.Text = "Grid data source";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
        }

        private DataGridView autoGrid;
        private DataGridView boundGrid;
        private DataGridViewTextBoxColumn nameColumn;
        private Button lockButton;
    }
}
