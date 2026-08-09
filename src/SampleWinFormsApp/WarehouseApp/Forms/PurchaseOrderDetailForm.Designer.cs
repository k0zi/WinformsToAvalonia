using System.Drawing.Printing;
using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class PurchaseOrderDetailForm
{
    private TableLayoutPanel headerTableLayoutPanel = null!;
    private Label orderNumberLabel = null!;
    private Label orderNumberValueLabel = null!;
    private Label supplierLabel = null!;
    private ComboBox supplierComboBox = null!;
    private Label orderDateLabel = null!;
    private DateTimePicker orderDatePicker = null!;
    private Label expectedDateLabel = null!;
    private DateTimePicker expectedDatePicker = null!;
    private Label statusLabel = null!;
    private ComboBox statusComboBox = null!;
    private StatusBadgeControl statusBadge = null!;
    private Label notesLabel = null!;
    private TextBox notesTextBox = null!;

    private Panel lineEntryPanel = null!;
    private Label productLabel = null!;
    private AutocompleteSearchBox productSearchBox = null!;
    private Label qtyLabel = null!;
    private NumericUpDown qtyNumericUpDown = null!;
    private Label priceLabel = null!;
    private NumericUpDown unitPriceNumericUpDown = null!;
    private Button addLineButton = null!;

    private DataGridView linesGrid = null!;
    private DataGridViewTextBoxColumn lineProductColumn = null!;
    private DataGridViewTextBoxColumn lineQtyColumn = null!;
    private DataGridViewTextBoxColumn linePriceColumn = null!;
    private DataGridViewTextBoxColumn lineTotalColumn = null!;

    private Button printButton = null!;
    private PrintDocument printDocument = null!;
    private PrintDialog printDialog = null!;

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.headerTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
        this.headerTableLayoutPanel.Location = new System.Drawing.Point(12, 12);
        this.headerTableLayoutPanel.Size = new System.Drawing.Size(560, 190);
        this.headerTableLayoutPanel.ColumnCount = 2;
        this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110));
        this.headerTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
        {
            this.headerTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30));
        }

        this.orderNumberLabel = new System.Windows.Forms.Label();
        this.orderNumberLabel.Text = "Order #:";
        this.orderNumberLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.orderNumberLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.orderNumberValueLabel = new System.Windows.Forms.Label();
        this.orderNumberValueLabel.Text = "(new)";
        this.orderNumberValueLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.orderNumberValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.orderNumberValueLabel.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        this.supplierLabel = new System.Windows.Forms.Label();
        this.supplierLabel.Text = "Supplier:";
        this.supplierLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.supplierLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.supplierComboBox = new System.Windows.Forms.ComboBox();
        this.supplierComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.supplierComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.supplierComboBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this.orderDateLabel = new System.Windows.Forms.Label();
        this.orderDateLabel.Text = "Order Date:";
        this.orderDateLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.orderDateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.orderDatePicker = new System.Windows.Forms.DateTimePicker();
        this.orderDatePicker.Dock = System.Windows.Forms.DockStyle.Fill;
        this.orderDatePicker.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this.expectedDateLabel = new System.Windows.Forms.Label();
        this.expectedDateLabel.Text = "Expected:";
        this.expectedDateLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.expectedDateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.expectedDatePicker = new System.Windows.Forms.DateTimePicker();
        this.expectedDatePicker.Dock = System.Windows.Forms.DockStyle.Fill;
        this.expectedDatePicker.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this.statusLabel = new System.Windows.Forms.Label();
        this.statusLabel.Text = "Status:";
        this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.statusComboBox = new System.Windows.Forms.ComboBox();
        this.statusComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.statusComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.statusComboBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this.notesLabel = new System.Windows.Forms.Label();
        this.notesLabel.Text = "Notes:";
        this.notesLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.notesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.notesTextBox = new System.Windows.Forms.TextBox();
        this.notesTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.notesTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
        this.notesTextBox.Multiline = true;
        this.notesTextBox.Height = 40;

        this.headerTableLayoutPanel.Controls.Add(this.orderNumberLabel, 0, 0);
        this.headerTableLayoutPanel.Controls.Add(this.orderNumberValueLabel, 1, 0);
        this.headerTableLayoutPanel.Controls.Add(this.supplierLabel, 0, 1);
        this.headerTableLayoutPanel.Controls.Add(this.supplierComboBox, 1, 1);
        this.headerTableLayoutPanel.Controls.Add(this.orderDateLabel, 0, 2);
        this.headerTableLayoutPanel.Controls.Add(this.orderDatePicker, 1, 2);
        this.headerTableLayoutPanel.Controls.Add(this.expectedDateLabel, 0, 3);
        this.headerTableLayoutPanel.Controls.Add(this.expectedDatePicker, 1, 3);
        this.headerTableLayoutPanel.Controls.Add(this.statusLabel, 0, 4);
        this.headerTableLayoutPanel.Controls.Add(this.statusComboBox, 1, 4);
        this.headerTableLayoutPanel.Controls.Add(this.notesLabel, 0, 5);
        this.headerTableLayoutPanel.Controls.Add(this.notesTextBox, 1, 5);

        this.statusBadge = new WarehouseApp.Controls.StatusBadgeControl();
        this.statusBadge.Location = new System.Drawing.Point(590, 12);
        this.statusBadge.Text = "Draft";
        this.statusBadge.BadgeStyle = BadgeStyle.Neutral;

        this.productLabel = new System.Windows.Forms.Label();
        this.productLabel.Text = "Product:";
        this.productLabel.Location = new System.Drawing.Point(12, 215);
        this.productLabel.AutoSize = true;
        this.productSearchBox = new WarehouseApp.Controls.AutocompleteSearchBox();
        this.productSearchBox.Location = new System.Drawing.Point(12, 232);
        this.productSearchBox.Width = 250;
        this.productSearchBox.DisplayMember = "Name";
        this.productSearchBox.SelectedItemChanged += this.ProductSearchBox_SelectedItemChanged;
        this.qtyLabel = new System.Windows.Forms.Label();
        this.qtyLabel.Text = "Qty:";
        this.qtyLabel.Location = new System.Drawing.Point(272, 215);
        this.qtyLabel.AutoSize = true;
        this.qtyNumericUpDown = new System.Windows.Forms.NumericUpDown();
        this.qtyNumericUpDown.Location = new System.Drawing.Point(272, 232);
        this.qtyNumericUpDown.Width = 70;
        this.qtyNumericUpDown.Minimum = 1;
        this.qtyNumericUpDown.Maximum = 10000;
        this.qtyNumericUpDown.Value = 1;
        this.priceLabel = new System.Windows.Forms.Label();
        this.priceLabel.Text = "Unit Price:";
        this.priceLabel.Location = new System.Drawing.Point(352, 215);
        this.priceLabel.AutoSize = true;
        this.unitPriceNumericUpDown = new System.Windows.Forms.NumericUpDown();
        this.unitPriceNumericUpDown.Location = new System.Drawing.Point(352, 232);
        this.unitPriceNumericUpDown.Width = 90;
        this.unitPriceNumericUpDown.Minimum = 0;
        this.unitPriceNumericUpDown.Maximum = 100000;
        this.unitPriceNumericUpDown.DecimalPlaces = 2;
        this.addLineButton = new System.Windows.Forms.Button();
        this.addLineButton.Text = "Add Line";
        this.addLineButton.Location = new System.Drawing.Point(452, 230);
        this.addLineButton.Size = new System.Drawing.Size(90, 28);
        this.addLineButton.Click += this.addLineButton_Click;

        this.lineEntryPanel = new System.Windows.Forms.Panel();
        this.lineEntryPanel.Location = new System.Drawing.Point(0, 205);
        this.lineEntryPanel.Size = new System.Drawing.Size(700, 65);
        this.lineEntryPanel.Controls.Add(this.productLabel);
        this.lineEntryPanel.Controls.Add(this.productSearchBox);
        this.lineEntryPanel.Controls.Add(this.qtyLabel);
        this.lineEntryPanel.Controls.Add(this.qtyNumericUpDown);
        this.lineEntryPanel.Controls.Add(this.priceLabel);
        this.lineEntryPanel.Controls.Add(this.unitPriceNumericUpDown);
        this.lineEntryPanel.Controls.Add(this.addLineButton);

        this.lineProductColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineProductColumn.Name = "Product";
        this.lineProductColumn.HeaderText = "Product";
        this.lineProductColumn.Width = 220;
        this.lineProductColumn.ReadOnly = true;
        this.lineQtyColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineQtyColumn.Name = "Qty";
        this.lineQtyColumn.HeaderText = "Qty";
        this.lineQtyColumn.Width = 70;
        this.lineQtyColumn.ReadOnly = true;
        this.lineQtyColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.linePriceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.linePriceColumn.Name = "Price";
        this.linePriceColumn.HeaderText = "Unit Price";
        this.linePriceColumn.Width = 90;
        this.linePriceColumn.ReadOnly = true;
        this.linePriceColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.linePriceColumn.DefaultCellStyle.Format = "C2";
        this.lineTotalColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.lineTotalColumn.Name = "Total";
        this.lineTotalColumn.HeaderText = "Line Total";
        this.lineTotalColumn.Width = 100;
        this.lineTotalColumn.ReadOnly = true;
        this.lineTotalColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.lineTotalColumn.DefaultCellStyle.Format = "C2";
        this.linesGrid = new System.Windows.Forms.DataGridView();
        this.linesGrid.Location = new System.Drawing.Point(12, 275);
        this.linesGrid.Size = new System.Drawing.Size(680, 200);
        this.linesGrid.AutoGenerateColumns = false;
        this.linesGrid.AllowUserToAddRows = false;
        this.linesGrid.ReadOnly = true;
        this.linesGrid.RowHeadersVisible = false;
        this.linesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.linesGrid.Columns.AddRange(this.lineProductColumn, this.lineQtyColumn, this.linePriceColumn, this.lineTotalColumn);

        this.printDocument = new System.Drawing.Printing.PrintDocument();
        this.printDocument.PrintPage += this.PrintDocument_PrintPage;
        this.printDialog = new System.Windows.Forms.PrintDialog();
        this.printDialog.Document = this.printDocument;
        this.printButton = new System.Windows.Forms.Button();
        this.printButton.Text = "Print...";
        this.printButton.Location = new System.Drawing.Point(12, 485);
        this.printButton.Size = new System.Drawing.Size(100, 28);
        this.printButton.Click += this.printButton_Click;

        this.ClientSize = new System.Drawing.Size(704, 560);
        this.Controls.Add(this.headerTableLayoutPanel);
        this.Controls.Add(this.statusBadge);
        this.Controls.Add(this.lineEntryPanel);
        this.Controls.Add(this.linesGrid);
        this.Controls.Add(this.printButton);
        this.Text = "Purchase Order Detail — WarehouseApp";

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
