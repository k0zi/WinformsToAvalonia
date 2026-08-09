namespace WarehouseApp.Forms;

partial class StockTransferForm
{
    private System.ComponentModel.IContainer components = null!;
    private Panel entryPanel = null!;
    private Label fromLabel = null!;
    private ComboBox fromWarehouseComboBox = null!;
    private Button swapButton = null!;
    private Label toLabel = null!;
    private ComboBox toWarehouseComboBox = null!;
    private Label productLabel = null!;
    private ComboBox productComboBox = null!;
    private Label quantityLabel = null!;
    private NumericUpDown quantityNumericUpDown = null!;
    private Button addLineButton = null!;

    private DataGridView linesGrid = null!;
    private DataGridViewTextBoxColumn lineProductColumn = null!;
    private DataGridViewTextBoxColumn lineFromColumn = null!;
    private DataGridViewTextBoxColumn lineToColumn = null!;
    private DataGridViewTextBoxColumn lineQuantityColumn = null!;

    private Panel actionPanel = null!;
    private Button removeLineButton = null!;
    private Button postButton = null!;
    private Label statusLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.SuspendLayout();

        this.fromLabel = new System.Windows.Forms.Label();
        this.fromLabel.Text = "From:";
        this.fromLabel.Location = new System.Drawing.Point(12, 15);
        this.fromLabel.AutoSize = true;
        this.fromWarehouseComboBox = new System.Windows.Forms.ComboBox();
        this.fromWarehouseComboBox.Location = new System.Drawing.Point(60, 12);
        this.fromWarehouseComboBox.Width = 170;
        this.fromWarehouseComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.swapButton = new System.Windows.Forms.Button();
        this.swapButton.Text = "⇄";
        this.swapButton.Location = new System.Drawing.Point(238, 11);
        this.swapButton.Size = new System.Drawing.Size(32, 24);
        this.swapButton.Click += (_, _) => this.SwapWarehouses();
        this.toLabel = new System.Windows.Forms.Label();
        this.toLabel.Text = "To:";
        this.toLabel.Location = new System.Drawing.Point(278, 15);
        this.toLabel.AutoSize = true;
        this.toWarehouseComboBox = new System.Windows.Forms.ComboBox();
        this.toWarehouseComboBox.Location = new System.Drawing.Point(310, 12);
        this.toWarehouseComboBox.Width = 170;
        this.toWarehouseComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

        this.productLabel = new System.Windows.Forms.Label();
        this.productLabel.Text = "Product:";
        this.productLabel.Location = new System.Drawing.Point(12, 50);
        this.productLabel.AutoSize = true;
        this.productComboBox = new System.Windows.Forms.ComboBox();
        this.productComboBox.Location = new System.Drawing.Point(80, 46);
        this.productComboBox.Width = 250;
        this.productComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.quantityLabel = new System.Windows.Forms.Label();
        this.quantityLabel.Text = "Quantity:";
        this.quantityLabel.Location = new System.Drawing.Point(345, 50);
        this.quantityLabel.AutoSize = true;
        this.quantityNumericUpDown = new System.Windows.Forms.NumericUpDown();
        this.quantityNumericUpDown.Location = new System.Drawing.Point(410, 46);
        this.quantityNumericUpDown.Width = 70;
        this.quantityNumericUpDown.Minimum = 1;
        this.quantityNumericUpDown.Maximum = 10000;
        this.quantityNumericUpDown.Value = 1;
        this.addLineButton = new System.Windows.Forms.Button();
        this.addLineButton.Text = "Add Line";
        this.addLineButton.Location = new System.Drawing.Point(495, 44);
        this.addLineButton.Size = new System.Drawing.Size(100, 28);
        this.addLineButton.Click += this.addLineButton_Click;

        this.entryPanel = new System.Windows.Forms.Panel();
        this.entryPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.entryPanel.Height = 90;
        this.entryPanel.Controls.Add(this.fromLabel);
        this.entryPanel.Controls.Add(this.fromWarehouseComboBox);
        this.entryPanel.Controls.Add(this.swapButton);
        this.entryPanel.Controls.Add(this.toLabel);
        this.entryPanel.Controls.Add(this.toWarehouseComboBox);
        this.entryPanel.Controls.Add(this.productLabel);
        this.entryPanel.Controls.Add(this.productComboBox);
        this.entryPanel.Controls.Add(this.quantityLabel);
        this.entryPanel.Controls.Add(this.quantityNumericUpDown);
        this.entryPanel.Controls.Add(this.addLineButton);

        this.lineProductColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineProductColumn.Name = "Product";
        this.lineProductColumn.HeaderText = "Product";
        this.lineProductColumn.Width = 200;
        this.lineFromColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineFromColumn.Name = "From";
        this.lineFromColumn.HeaderText = "From";
        this.lineFromColumn.Width = 140;
        this.lineToColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineToColumn.Name = "To";
        this.lineToColumn.HeaderText = "To";
        this.lineToColumn.Width = 140;
        this.lineQuantityColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineQuantityColumn.Name = "Quantity";
        this.lineQuantityColumn.HeaderText = "Quantity";
        this.lineQuantityColumn.Width = 90;
        this.lineQuantityColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.linesGrid = new System.Windows.Forms.DataGridView();
        this.linesGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this.linesGrid.AutoGenerateColumns = false;
        this.linesGrid.AllowUserToAddRows = false;
        this.linesGrid.ReadOnly = true;
        this.linesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.linesGrid.MultiSelect = false;
        this.linesGrid.RowHeadersVisible = false;
        this.linesGrid.Columns.AddRange(this.lineProductColumn, this.lineFromColumn, this.lineToColumn, this.lineQuantityColumn);

        this.removeLineButton = new System.Windows.Forms.Button();
        this.removeLineButton.Text = "Remove Line";
        this.removeLineButton.Location = new System.Drawing.Point(12, 10);
        this.removeLineButton.Size = new System.Drawing.Size(110, 28);
        this.removeLineButton.Click += (_, _) => this.RemoveSelectedLine();
        this.postButton = new System.Windows.Forms.Button();
        this.postButton.Text = "Post Transfer";
        this.postButton.Location = new System.Drawing.Point(600, 10);
        this.postButton.Size = new System.Drawing.Size(120, 32);
        this.postButton.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        this.postButton.Click += async (_, _) => await this.PostTransferAsync();
        this.statusLabel = new System.Windows.Forms.Label();
        this.statusLabel.Location = new System.Drawing.Point(140, 16);
        this.statusLabel.AutoSize = true;
        this.statusLabel.ForeColor = System.Drawing.Color.SeaGreen;

        this.actionPanel = new System.Windows.Forms.Panel();
        this.actionPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.actionPanel.Height = 52;
        this.actionPanel.Controls.Add(this.removeLineButton);
        this.actionPanel.Controls.Add(this.postButton);
        this.actionPanel.Controls.Add(this.statusLabel);

        this.ClientSize = new System.Drawing.Size(760, 460);
        this.Controls.Add(this.linesGrid);
        this.Controls.Add(this.actionPanel);
        this.Controls.Add(this.entryPanel);
        this.Text = "Stock Transfer — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}