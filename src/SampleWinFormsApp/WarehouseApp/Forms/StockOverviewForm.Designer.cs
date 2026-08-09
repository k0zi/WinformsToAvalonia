using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class StockOverviewForm
{
    private System.ComponentModel.IContainer components = null!;
    private ToolStrip mainToolStrip = null!;
    private ToolStripLabel warehouseFilterLabel = null!;
    private ToolStripComboBox warehouseFilterComboBox = null!;
    private ToolStripButton refreshButton = null!;

    private Panel thresholdPanel = null!;
    private Label thresholdLabel = null!;
    private TrackBar lowStockTrackBar = null!;
    private Label thresholdValueLabel = null!;

    private SplitContainer mainSplitContainer = null!;
    private DataGridView stockGrid = null!;
    private BindingSource bindingSourceControl = null!;
    private DataGridViewTextBoxColumn productColumn = null!;
    private DataGridViewTextBoxColumn warehouseColumn = null!;
    private DataGridViewTextBoxColumn onHandColumn = null!;
    private DataGridViewTextBoxColumn reservedColumn = null!;
    private DataGridViewTextBoxColumn reorderColumn = null!;

    private Panel summaryPanel = null!;
    private Label summaryTitleLabel = null!;
    private GaugeControl overallGauge = null!;
    private StatusBadgeControl selectedStatusBadge = null!;

    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel recordCountLabel = null!;

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

        this.mainToolStrip = new System.Windows.Forms.ToolStrip();
        this.warehouseFilterLabel = new System.Windows.Forms.ToolStripLabel("Warehouse:");
        this.warehouseFilterComboBox = new System.Windows.Forms.ToolStripComboBox();
        this.warehouseFilterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.warehouseFilterComboBox.Width = 180;
        this.refreshButton = new System.Windows.Forms.ToolStripButton("Refresh", null, async (_, _) => await this.LoadStockAsync());
        this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.mainToolStrip.Items.Add(this.warehouseFilterLabel);
        this.mainToolStrip.Items.Add(this.warehouseFilterComboBox);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(this.refreshButton);
        this.mainToolStrip.Dock = System.Windows.Forms.DockStyle.Top;

        this.thresholdLabel = new System.Windows.Forms.Label();
        this.thresholdLabel.Text = "Low-stock alert level (% of reorder level):";
        this.thresholdLabel.AutoSize = true;
        this.thresholdLabel.Location = new System.Drawing.Point(8, 8);
        this.lowStockTrackBar = new System.Windows.Forms.TrackBar();
        this.lowStockTrackBar.Minimum = 50;
        this.lowStockTrackBar.Maximum = 200;
        this.lowStockTrackBar.Value = 100;
        this.lowStockTrackBar.TickFrequency = 25;
        this.lowStockTrackBar.Location = new System.Drawing.Point(8, 28);
        this.lowStockTrackBar.Width = 260;
        this.thresholdValueLabel = new System.Windows.Forms.Label();
        this.thresholdValueLabel.Text = "100%";
        this.thresholdValueLabel.AutoSize = true;
        this.thresholdValueLabel.Location = new System.Drawing.Point(275, 34);
        this.lowStockTrackBar.ValueChanged += (_, _) =>
        {
            this.thresholdValueLabel.Text = $"{this.lowStockTrackBar.Value}%";
            this.RefreshRowStatuses();
        };
        this.thresholdPanel = new System.Windows.Forms.Panel();
        this.thresholdPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.thresholdPanel.Height = 62;
        this.thresholdPanel.Controls.Add(this.thresholdLabel);
        this.thresholdPanel.Controls.Add(this.lowStockTrackBar);
        this.thresholdPanel.Controls.Add(this.thresholdValueLabel);

        this.bindingSourceControl = new System.Windows.Forms.BindingSource();
        this.productColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.productColumn.Name = "Product";
        this.productColumn.HeaderText = "Product";
        this.productColumn.DataPropertyName = "ProductName";
        this.productColumn.Width = 200;
        this.warehouseColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.warehouseColumn.Name = "Warehouse";
        this.warehouseColumn.HeaderText = "Warehouse";
        this.warehouseColumn.DataPropertyName = "WarehouseName";
        this.warehouseColumn.Width = 150;
        this.onHandColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.onHandColumn.Name = "OnHand";
        this.onHandColumn.HeaderText = "On Hand";
        this.onHandColumn.DataPropertyName = "OnHand";
        this.onHandColumn.Width = 80;
        this.onHandColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.reservedColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.reservedColumn.Name = "Reserved";
        this.reservedColumn.HeaderText = "Reserved";
        this.reservedColumn.DataPropertyName = "Reserved";
        this.reservedColumn.Width = 80;
        this.reservedColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.reorderColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.reorderColumn.Name = "Reorder";
        this.reorderColumn.HeaderText = "Reorder Lvl";
        this.reorderColumn.DataPropertyName = "ReorderLevel";
        this.reorderColumn.Width = 90;
        this.reorderColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;

        this.stockGrid = new System.Windows.Forms.DataGridView();
        this.stockGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this.stockGrid.AutoGenerateColumns = false;
        this.stockGrid.AllowUserToAddRows = false;
        this.stockGrid.AllowUserToDeleteRows = false;
        this.stockGrid.ReadOnly = true;
        this.stockGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.stockGrid.MultiSelect = false;
        this.stockGrid.RowHeadersVisible = false;
        this.stockGrid.DataSource = this.bindingSourceControl;
        this.stockGrid.Columns.AddRange(this.productColumn, this.warehouseColumn, this.onHandColumn, this.reservedColumn, this.reorderColumn);
        this.stockGrid.CellFormatting += this.StockGrid_CellFormatting;
        this.stockGrid.SelectionChanged += this.StockGrid_SelectionChanged;

        this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
        this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainSplitContainer.SplitterDistance = 620;
        this.mainSplitContainer.Panel1.Controls.Add(this.stockGrid);
        this.mainSplitContainer.Panel1.Controls.Add(this.thresholdPanel);

        this.summaryPanel = new System.Windows.Forms.Panel();
        this.summaryPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.summaryPanel.Padding = new System.Windows.Forms.Padding(12);
        this.summaryTitleLabel = new System.Windows.Forms.Label();
        this.summaryTitleLabel.Text = "Overall Capacity";
        this.summaryTitleLabel.AutoSize = true;
        this.summaryTitleLabel.Location = new System.Drawing.Point(12, 8);
        this.summaryTitleLabel.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        this.overallGauge = new WarehouseApp.Controls.GaugeControl();
        this.overallGauge.Location = new System.Drawing.Point(12, 30);
        this.overallGauge.Unit = "%";
        this.selectedStatusBadge = new WarehouseApp.Controls.StatusBadgeControl();
        this.selectedStatusBadge.Location = new System.Drawing.Point(12, 190);
        this.selectedStatusBadge.Text = "Select a row";
        this.selectedStatusBadge.BadgeStyle = BadgeStyle.Neutral;
        this.summaryPanel.Controls.Add(this.summaryTitleLabel);
        this.summaryPanel.Controls.Add(this.overallGauge);
        this.summaryPanel.Controls.Add(this.selectedStatusBadge);
        this.mainSplitContainer.Panel2.Controls.Add(this.summaryPanel);

        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.recordCountLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.recordCountLabel.Spring = true;
        this.recordCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.statusStrip.Items.Add(this.recordCountLabel);

        this.ClientSize = new System.Drawing.Size(900, 560);
        this.Controls.Add(this.mainSplitContainer);
        this.Controls.Add(this.mainToolStrip);
        this.Controls.Add(this.statusStrip);
        this.Text = "Stock Overview — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
