namespace WarehouseApp.Forms;

partial class SalesOrdersListForm
{
    private ToolStripComboBox statusFilterComboBox = null!;
    private DataGridViewTextBoxColumn orderNumberColumn = null!;
    private DataGridViewTextBoxColumn customerColumn = null!;
    private DataGridViewTextBoxColumn orderDateColumn = null!;
    private DataGridViewTextBoxColumn statusColumn = null!;

    private void InitializeComponent()
    {
        this.Text = "Sales Orders — WarehouseApp";

        this.statusFilterComboBox = new System.Windows.Forms.ToolStripComboBox();
        this.statusFilterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.statusFilterComboBox.Width = 140;
        this.InsertFilterComboBox("Status:", this.statusFilterComboBox);

        this.orderNumberColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.orderNumberColumn.Name = "OrderNumber";
        this.orderNumberColumn.HeaderText = "Order #";
        this.orderNumberColumn.DataPropertyName = "OrderNumber";
        this.orderNumberColumn.Width = 110;
        this.customerColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.customerColumn.Name = "Customer";
        this.customerColumn.HeaderText = "Customer";
        this.customerColumn.Width = 180;
        this.orderDateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.orderDateColumn.Name = "OrderDate";
        this.orderDateColumn.HeaderText = "Order Date";
        this.orderDateColumn.DataPropertyName = "OrderDate";
        this.orderDateColumn.Width = 100;
        this.orderDateColumn.DefaultCellStyle.Format = "d";
        this.statusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.statusColumn.Name = "Status";
        this.statusColumn.HeaderText = "Status";
        this.statusColumn.DataPropertyName = "Status";
        this.statusColumn.Width = 130;

        this.Grid.Columns.AddRange(this.orderNumberColumn, this.customerColumn, this.orderDateColumn, this.statusColumn);
        this.Grid.CellFormatting += this.Grid_CellFormatting;
    }
}