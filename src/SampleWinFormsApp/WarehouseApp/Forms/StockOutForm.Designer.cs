using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class StockOutForm
{
    private System.ComponentModel.IContainer components = null!;
    private Panel entryPanel = null!;
    private Label warehouseLabel = null!;
    private ComboBox warehouseComboBox = null!;
    private Label productLabel = null!;
    private ComboBox productComboBox = null!;
    private Label quantityLabel = null!;
    private NumericStepperControl quantityStepper = null!;
    private Button addLineButton = null!;

    private DataGridView linesGrid = null!;
    private DataGridViewTextBoxColumn lineProductColumn = null!;
    private DataGridViewTextBoxColumn lineWarehouseColumn = null!;
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

        this.warehouseLabel = new System.Windows.Forms.Label();
        this.warehouseLabel.Text = "Warehouse:";
        this.warehouseLabel.Location = new System.Drawing.Point(12, 15);
        this.warehouseLabel.AutoSize = true;
        this.warehouseComboBox = new System.Windows.Forms.ComboBox();
        this.warehouseComboBox.Location = new System.Drawing.Point(100, 12);
        this.warehouseComboBox.Width = 180;
        this.warehouseComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.productLabel = new System.Windows.Forms.Label();
        this.productLabel.Text = "Product:";
        this.productLabel.Location = new System.Drawing.Point(300, 15);
        this.productLabel.AutoSize = true;
        this.productComboBox = new System.Windows.Forms.ComboBox();
        this.productComboBox.Location = new System.Drawing.Point(370, 12);
        this.productComboBox.Width = 220;
        this.productComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.quantityLabel = new System.Windows.Forms.Label();
        this.quantityLabel.Text = "Quantity:";
        this.quantityLabel.Location = new System.Drawing.Point(12, 50);
        this.quantityLabel.AutoSize = true;
        this.quantityStepper = new WarehouseApp.Controls.NumericStepperControl();
        this.quantityStepper.Location = new System.Drawing.Point(100, 44);
        this.quantityStepper.Minimum = 1;
        this.quantityStepper.Maximum = 10000;
        this.quantityStepper.Value = 1;
        this.addLineButton = new System.Windows.Forms.Button();
        this.addLineButton.Text = "Add Line";
        this.addLineButton.Location = new System.Drawing.Point(260, 44);
        this.addLineButton.Size = new System.Drawing.Size(100, 28);
        this.addLineButton.Click += this.addLineButton_Click;

        this.entryPanel = new System.Windows.Forms.Panel();
        this.entryPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.entryPanel.Height = 90;
        this.entryPanel.Controls.Add(this.warehouseLabel);
        this.entryPanel.Controls.Add(this.warehouseComboBox);
        this.entryPanel.Controls.Add(this.productLabel);
        this.entryPanel.Controls.Add(this.productComboBox);
        this.entryPanel.Controls.Add(this.quantityLabel);
        this.entryPanel.Controls.Add(this.quantityStepper);
        this.entryPanel.Controls.Add(this.addLineButton);

        this.lineProductColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineProductColumn.Name = "Product";
        this.lineProductColumn.HeaderText = "Product";
        this.lineProductColumn.Width = 220;
        this.lineWarehouseColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineWarehouseColumn.Name = "Warehouse";
        this.lineWarehouseColumn.HeaderText = "Warehouse";
        this.lineWarehouseColumn.Width = 160;
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
        this.linesGrid.Columns.AddRange(this.lineProductColumn, this.lineWarehouseColumn, this.lineQuantityColumn);

        this.removeLineButton = new System.Windows.Forms.Button();
        this.removeLineButton.Text = "Remove Line";
        this.removeLineButton.Location = new System.Drawing.Point(12, 10);
        this.removeLineButton.Size = new System.Drawing.Size(110, 28);
        this.removeLineButton.Click += (_, _) => this.RemoveSelectedLine();
        this.postButton = new System.Windows.Forms.Button();
        this.postButton.Text = "Post Issue";
        this.postButton.Location = new System.Drawing.Point(600, 10);
        this.postButton.Size = new System.Drawing.Size(120, 32);
        this.postButton.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        this.postButton.Click += async (_, _) => await this.PostIssueAsync();
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

        this.ClientSize = new System.Drawing.Size(740, 460);
        this.Controls.Add(this.linesGrid);
        this.Controls.Add(this.actionPanel);
        this.Controls.Add(this.entryPanel);
        this.Text = "Stock Out — Goods Issue — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}