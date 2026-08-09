using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class WarehousesForm
{
    private System.ComponentModel.IContainer components = null!;
    private ToolStrip mainToolStrip = null!;
    private ToolStripButton newWarehouseButton = null!;
    private ToolStripButton newZoneButton = null!;
    private ToolStripButton newShelfButton = null!;
    private ToolStripButton deleteNodeButton = null!;
    private ToolStripButton refreshButton = null!;

    private Panel treeViewPanel = null!;
    private TreeView locationsTreeView = null!;
    private ImageList locationImageList = null!;
    private Splitter splitter = null!;

    private Panel detailFillPanel = null!;
    private Panel gaugePanel = null!;
    private GaugeControl capacityGauge = null!;
    private Label selectedNameLabel = null!;
    private Label capacityDetailLabel = null!;
    private ListView shelfContentsListView = null!;
    private ColumnHeader productColumnHeader = null!;
    private ColumnHeader onHandColumnHeader = null!;
    private ColumnHeader reservedColumnHeader = null!;

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

        this.locationImageList = new System.Windows.Forms.ImageList();
        this.locationImageList.ImageSize = new System.Drawing.Size(16, 16);
        this.locationImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
        this.locationImageList.Images.Add("Warehouse", Common.AppIcons.CreateGlyph("⌂", System.Drawing.Color.SlateGray, 16));
        this.locationImageList.Images.Add("Zone", Common.AppIcons.CreateGlyph("▤", System.Drawing.Color.DarkGoldenrod, 16));
        this.locationImageList.Images.Add("Shelf", Common.AppIcons.CreateGlyph("▫", System.Drawing.Color.SteelBlue, 16));

        this.mainToolStrip = new System.Windows.Forms.ToolStrip();
        this.newWarehouseButton = new System.Windows.Forms.ToolStripButton("New Warehouse", null, (_, _) => this.AddWarehouse());
        this.newWarehouseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.newZoneButton = new System.Windows.Forms.ToolStripButton("New Zone", null, (_, _) => this.AddZone());
        this.newZoneButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.newShelfButton = new System.Windows.Forms.ToolStripButton("New Shelf", null, (_, _) => this.AddShelf());
        this.newShelfButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.deleteNodeButton = new System.Windows.Forms.ToolStripButton("Delete", null, async (_, _) => await this.DeleteSelectedNodeAsync());
        this.deleteNodeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.refreshButton = new System.Windows.Forms.ToolStripButton("Refresh", null, async (_, _) => await this.LoadTreeAsync());
        this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.mainToolStrip.Items.Add(this.newWarehouseButton);
        this.mainToolStrip.Items.Add(this.newZoneButton);
        this.mainToolStrip.Items.Add(this.newShelfButton);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(this.deleteNodeButton);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(this.refreshButton);
        this.mainToolStrip.Dock = System.Windows.Forms.DockStyle.Top;

        this.locationsTreeView = new System.Windows.Forms.TreeView();
        this.locationsTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.locationsTreeView.ImageList = this.locationImageList;
        this.locationsTreeView.HideSelection = false;
        this.locationsTreeView.AfterSelect += this.LocationsTreeView_AfterSelect;

        this.treeViewPanel = new System.Windows.Forms.Panel();
        this.treeViewPanel.Dock = System.Windows.Forms.DockStyle.Left;
        this.treeViewPanel.Width = 300;
        this.treeViewPanel.Controls.Add(this.locationsTreeView);

        this.splitter = new System.Windows.Forms.Splitter();
        this.splitter.Dock = System.Windows.Forms.DockStyle.Left;
        this.splitter.Width = 4;

        this.gaugePanel = new System.Windows.Forms.Panel();
        this.gaugePanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.gaugePanel.Height = 170;
        this.gaugePanel.Padding = new System.Windows.Forms.Padding(12);
        this.selectedNameLabel = new System.Windows.Forms.Label();
        this.selectedNameLabel.Text = "Select a warehouse or location";
        this.selectedNameLabel.Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
        this.selectedNameLabel.Location = new System.Drawing.Point(12, 8);
        this.selectedNameLabel.AutoSize = true;
        this.capacityGauge = new WarehouseApp.Controls.GaugeControl();
        this.capacityGauge.Location = new System.Drawing.Point(12, 34);
        this.capacityGauge.Unit = "%";
        this.capacityDetailLabel = new System.Windows.Forms.Label();
        this.capacityDetailLabel.Location = new System.Drawing.Point(180, 60);
        this.capacityDetailLabel.AutoSize = true;
        this.capacityDetailLabel.ForeColor = System.Drawing.Color.Gray;

        this.gaugePanel.Controls.Add(this.selectedNameLabel);
        this.gaugePanel.Controls.Add(this.capacityGauge);
        this.gaugePanel.Controls.Add(this.capacityDetailLabel);

        this.productColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.productColumnHeader.Text = "Product";
        this.productColumnHeader.Width = 220;
        this.onHandColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.onHandColumnHeader.Text = "On Hand";
        this.onHandColumnHeader.Width = 90;
        this.reservedColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.reservedColumnHeader.Text = "Reserved";
        this.reservedColumnHeader.Width = 90;
        this.shelfContentsListView = new System.Windows.Forms.ListView();
        this.shelfContentsListView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.shelfContentsListView.View = System.Windows.Forms.View.Details;
        this.shelfContentsListView.FullRowSelect = true;
        this.shelfContentsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.productColumnHeader, this.onHandColumnHeader, this.reservedColumnHeader });

        this.detailFillPanel = new System.Windows.Forms.Panel();
        this.detailFillPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.detailFillPanel.Controls.Add(this.shelfContentsListView);
        this.detailFillPanel.Controls.Add(this.gaugePanel);

        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.recordCountLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.recordCountLabel.Spring = true;
        this.recordCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.statusStrip.Items.Add(this.recordCountLabel);

        // Splitter usage requires this exact add order: the panel it resizes, then
        // the Splitter itself, then the panel that fills the remaining space.
        this.Controls.Add(this.treeViewPanel);
        this.Controls.Add(this.splitter);
        this.Controls.Add(this.detailFillPanel);
        this.Controls.Add(this.mainToolStrip);
        this.Controls.Add(this.statusStrip);

        this.ClientSize = new System.Drawing.Size(880, 560);
        this.Text = "Warehouses & Locations — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}