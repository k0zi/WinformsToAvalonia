using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class DashboardForm
{
    private System.ComponentModel.IContainer components = null!;

    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem fileMenuItem = null!;
    private ToolStripMenuItem logoutMenuItem = null!;
    private ToolStripMenuItem exitMenuItem = null!;
    private ToolStripMenuItem viewMenuItem = null!;
    private ToolStripMenuItem refreshMenuItem = null!;
    private ToolStripMenuItem helpMenuItem = null!;
    private ToolStripMenuItem aboutMenuItem = null!;

    private ToolStrip mainToolStrip = null!;
    private ToolStripButton refreshToolStripButton = null!;
    private ToolStripSeparator toolStripSeparator1 = null!;
    private ToolStripButton logoutToolStripButton = null!;

    private FlowLayoutPanel tilesFlowPanel = null!;
    private GaugeControl capacityGauge = null!;
    private Label capacityLabel = null!;
    private Panel sidePanel = null!;

    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel userStatusLabel = null!;
    private ToolStripStatusLabel clockStatusLabel = null!;
    private ToolStripProgressBar statusProgressBar = null!;

    private NotifyIcon notifyIcon = null!;
    private ContextMenuStrip notifyIconContextMenu = null!;
    private ToolStripMenuItem trayShowMenuItem = null!;
    private ToolStripMenuItem trayExitMenuItem = null!;

    private System.Windows.Forms.Timer clockTimer = null!;
    private ToolTip tileToolTip = null!;

    private CardTileControl productsTile = null!;
    private CardTileControl productDetailTile = null!;
    private CardTileControl categoriesTile = null!;
    private CardTileControl suppliersTile = null!;
    private CardTileControl customersTile = null!;
    private CardTileControl warehousesTile = null!;
    private CardTileControl stockOverviewTile = null!;
    private CardTileControl stockInTile = null!;
    private CardTileControl stockOutTile = null!;
    private CardTileControl stockTransferTile = null!;
    private CardTileControl stockAdjustmentTile = null!;
    private CardTileControl purchaseOrdersTile = null!;
    private CardTileControl purchaseOrderDetailTile = null!;
    private CardTileControl salesOrdersTile = null!;
    private CardTileControl salesOrderDetailTile = null!;
    private CardTileControl usersTile = null!;
    private CardTileControl reportsTile = null!;
    private CardTileControl settingsTile = null!;

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

        this.menuStrip = new System.Windows.Forms.MenuStrip();
        this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem("&File");
        this.logoutMenuItem = new System.Windows.Forms.ToolStripMenuItem("&Log Out");
        this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("E&xit");
        this.viewMenuItem = new System.Windows.Forms.ToolStripMenuItem("&View");
        this.refreshMenuItem = new System.Windows.Forms.ToolStripMenuItem("&Refresh");
        this.helpMenuItem = new System.Windows.Forms.ToolStripMenuItem("&Help");
        this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem("&About");

        this.logoutMenuItem.Click += this.logoutMenuItem_Click;
        this.exitMenuItem.Click += this.exitMenuItem_Click;
        this.refreshMenuItem.Click += this.refreshToolStripButton_Click;
        this.aboutMenuItem.Click += this.aboutMenuItem_Click;

        this.fileMenuItem.DropDownItems.Add(this.logoutMenuItem);
        this.fileMenuItem.DropDownItems.Add(new System.Windows.Forms.ToolStripSeparator());
        this.fileMenuItem.DropDownItems.Add(this.exitMenuItem);
        this.viewMenuItem.DropDownItems.Add(this.refreshMenuItem);
        this.helpMenuItem.DropDownItems.Add(this.aboutMenuItem);

        this.menuStrip.Items.Add(this.fileMenuItem);
        this.menuStrip.Items.Add(this.viewMenuItem);
        this.menuStrip.Items.Add(this.helpMenuItem);
        this.menuStrip.Dock = System.Windows.Forms.DockStyle.Top;

        this.mainToolStrip = new System.Windows.Forms.ToolStrip();
        this.refreshToolStripButton = new System.Windows.Forms.ToolStripButton("Refresh");
        this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
        this.logoutToolStripButton = new System.Windows.Forms.ToolStripButton("Log Out");
        this.refreshToolStripButton.Click += this.refreshToolStripButton_Click;
        this.logoutToolStripButton.Click += this.logoutMenuItem_Click;
        this.mainToolStrip.Items.Add(this.refreshToolStripButton);
        this.mainToolStrip.Items.Add(this.toolStripSeparator1);
        this.mainToolStrip.Items.Add(this.logoutToolStripButton);
        this.mainToolStrip.Dock = System.Windows.Forms.DockStyle.Top;

        this.tilesFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
        this.tilesFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tilesFlowPanel.AutoScroll = true;
        this.tilesFlowPanel.Padding = new System.Windows.Forms.Padding(16);
        this.tilesFlowPanel.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);

        this.productsTile = new WarehouseApp.Controls.CardTileControl();
        this.productsTile.Title = "Products";
        this.productsTile.Subtitle = "Browse & manage catalog";
        this.productsTile.Glyph = '▣';
        this.productsTile.AccentColor = System.Drawing.Color.SteelBlue;
        this.productsTile.Margin = new System.Windows.Forms.Padding(8);
        this.productDetailTile = new WarehouseApp.Controls.CardTileControl();
        this.productDetailTile.Title = "New Product";
        this.productDetailTile.Subtitle = "Add a product";
        this.productDetailTile.Glyph = '➕';
        this.productDetailTile.AccentColor = System.Drawing.Color.SeaGreen;
        this.productDetailTile.Margin = new System.Windows.Forms.Padding(8);
        this.categoriesTile = new WarehouseApp.Controls.CardTileControl();
        this.categoriesTile.Title = "Categories";
        this.categoriesTile.Subtitle = "Product hierarchy";
        this.categoriesTile.Glyph = '▤';
        this.categoriesTile.AccentColor = System.Drawing.Color.DarkGoldenrod;
        this.categoriesTile.Margin = new System.Windows.Forms.Padding(8);
        this.suppliersTile = new WarehouseApp.Controls.CardTileControl();
        this.suppliersTile.Title = "Suppliers";
        this.suppliersTile.Subtitle = "Supplier directory";
        this.suppliersTile.Glyph = '➜';
        this.suppliersTile.AccentColor = System.Drawing.Color.IndianRed;
        this.suppliersTile.Margin = new System.Windows.Forms.Padding(8);
        this.customersTile = new WarehouseApp.Controls.CardTileControl();
        this.customersTile.Title = "Customers";
        this.customersTile.Subtitle = "Customer directory";
        this.customersTile.Glyph = '☺';
        this.customersTile.AccentColor = System.Drawing.Color.MediumPurple;
        this.customersTile.Margin = new System.Windows.Forms.Padding(8);
        this.warehousesTile = new WarehouseApp.Controls.CardTileControl();
        this.warehousesTile.Title = "Warehouses";
        this.warehousesTile.Subtitle = "Locations & zones";
        this.warehousesTile.Glyph = '⌂';
        this.warehousesTile.AccentColor = System.Drawing.Color.SlateGray;
        this.warehousesTile.Margin = new System.Windows.Forms.Padding(8);
        this.stockOverviewTile = new WarehouseApp.Controls.CardTileControl();
        this.stockOverviewTile.Title = "Stock Overview";
        this.stockOverviewTile.Subtitle = "Current stock levels";
        this.stockOverviewTile.Glyph = '▦';
        this.stockOverviewTile.AccentColor = System.Drawing.Color.SteelBlue;
        this.stockOverviewTile.Margin = new System.Windows.Forms.Padding(8);
        this.stockInTile = new WarehouseApp.Controls.CardTileControl();
        this.stockInTile.Title = "Stock In";
        this.stockInTile.Subtitle = "Goods receipt";
        this.stockInTile.Glyph = '⬇';
        this.stockInTile.AccentColor = System.Drawing.Color.SeaGreen;
        this.stockInTile.Margin = new System.Windows.Forms.Padding(8);
        this.stockOutTile = new WarehouseApp.Controls.CardTileControl();
        this.stockOutTile.Title = "Stock Out";
        this.stockOutTile.Subtitle = "Goods issue";
        this.stockOutTile.Glyph = '⬆';
        this.stockOutTile.AccentColor = System.Drawing.Color.Firebrick;
        this.stockOutTile.Margin = new System.Windows.Forms.Padding(8);
        this.stockTransferTile = new WarehouseApp.Controls.CardTileControl();
        this.stockTransferTile.Title = "Stock Transfer";
        this.stockTransferTile.Subtitle = "Move between warehouses";
        this.stockTransferTile.Glyph = '↔';
        this.stockTransferTile.AccentColor = System.Drawing.Color.DarkCyan;
        this.stockTransferTile.Margin = new System.Windows.Forms.Padding(8);
        this.stockAdjustmentTile = new WarehouseApp.Controls.CardTileControl();
        this.stockAdjustmentTile.Title = "Stock Adjustment";
        this.stockAdjustmentTile.Subtitle = "Physical count";
        this.stockAdjustmentTile.Glyph = '⚖';
        this.stockAdjustmentTile.AccentColor = System.Drawing.Color.DarkOrange;
        this.stockAdjustmentTile.Margin = new System.Windows.Forms.Padding(8);
        this.purchaseOrdersTile = new WarehouseApp.Controls.CardTileControl();
        this.purchaseOrdersTile.Title = "Purchase Orders";
        this.purchaseOrdersTile.Subtitle = "Orders to suppliers";
        this.purchaseOrdersTile.Glyph = '▧';
        this.purchaseOrdersTile.AccentColor = System.Drawing.Color.SteelBlue;
        this.purchaseOrdersTile.Margin = new System.Windows.Forms.Padding(8);
        this.purchaseOrderDetailTile = new WarehouseApp.Controls.CardTileControl();
        this.purchaseOrderDetailTile.Title = "New Purchase Order";
        this.purchaseOrderDetailTile.Subtitle = "Create a PO";
        this.purchaseOrderDetailTile.Glyph = '➕';
        this.purchaseOrderDetailTile.AccentColor = System.Drawing.Color.SeaGreen;
        this.purchaseOrderDetailTile.Margin = new System.Windows.Forms.Padding(8);
        this.salesOrdersTile = new WarehouseApp.Controls.CardTileControl();
        this.salesOrdersTile.Title = "Sales Orders";
        this.salesOrdersTile.Subtitle = "Orders from customers";
        this.salesOrdersTile.Glyph = '▧';
        this.salesOrdersTile.AccentColor = System.Drawing.Color.MediumPurple;
        this.salesOrdersTile.Margin = new System.Windows.Forms.Padding(8);
        this.salesOrderDetailTile = new WarehouseApp.Controls.CardTileControl();
        this.salesOrderDetailTile.Title = "New Sales Order";
        this.salesOrderDetailTile.Subtitle = "Create an SO";
        this.salesOrderDetailTile.Glyph = '➕';
        this.salesOrderDetailTile.AccentColor = System.Drawing.Color.SeaGreen;
        this.salesOrderDetailTile.Margin = new System.Windows.Forms.Padding(8);
        this.usersTile = new WarehouseApp.Controls.CardTileControl();
        this.usersTile.Title = "Users & Roles";
        this.usersTile.Subtitle = "Manage accounts";
        this.usersTile.Glyph = '☻';
        this.usersTile.AccentColor = System.Drawing.Color.SlateGray;
        this.usersTile.Margin = new System.Windows.Forms.Padding(8);
        this.reportsTile = new WarehouseApp.Controls.CardTileControl();
        this.reportsTile.Title = "Reports";
        this.reportsTile.Subtitle = "Charts & audit log";
        this.reportsTile.Glyph = '❖';
        this.reportsTile.AccentColor = System.Drawing.Color.DarkGoldenrod;
        this.reportsTile.Margin = new System.Windows.Forms.Padding(8);
        this.settingsTile = new WarehouseApp.Controls.CardTileControl();
        this.settingsTile.Title = "Settings";
        this.settingsTile.Subtitle = "App preferences";
        this.settingsTile.Glyph = '⚙';
        this.settingsTile.AccentColor = System.Drawing.Color.Gray;
        this.settingsTile.Margin = new System.Windows.Forms.Padding(8);

        this.tileToolTip = new System.Windows.Forms.ToolTip(this.components);
        this.tileToolTip.AutomaticDelay = 400;
        this.tileToolTip.SetToolTip(this.productsTile, "Browse and manage the product catalog");
        this.tileToolTip.SetToolTip(this.productDetailTile, "Quickly add a new product");
        this.tileToolTip.SetToolTip(this.categoriesTile, "Manage the category hierarchy");
        this.tileToolTip.SetToolTip(this.suppliersTile, "Manage suppliers and their ratings");
        this.tileToolTip.SetToolTip(this.customersTile, "Manage customers and their orders");
        this.tileToolTip.SetToolTip(this.warehousesTile, "Browse warehouses, zones and shelves");
        this.tileToolTip.SetToolTip(this.stockOverviewTile, "View current stock levels across warehouses");
        this.tileToolTip.SetToolTip(this.stockInTile, "Record a goods receipt");
        this.tileToolTip.SetToolTip(this.stockOutTile, "Record a goods issue");
        this.tileToolTip.SetToolTip(this.stockTransferTile, "Move stock between warehouses");
        this.tileToolTip.SetToolTip(this.stockAdjustmentTile, "Reconcile stock via a physical count");
        this.tileToolTip.SetToolTip(this.purchaseOrdersTile, "Browse purchase orders");
        this.tileToolTip.SetToolTip(this.purchaseOrderDetailTile, "Quickly create a new purchase order");
        this.tileToolTip.SetToolTip(this.salesOrdersTile, "Browse sales orders");
        this.tileToolTip.SetToolTip(this.salesOrderDetailTile, "Quickly create a new sales order");
        this.tileToolTip.SetToolTip(this.usersTile, "Manage user accounts and roles");
        this.tileToolTip.SetToolTip(this.reportsTile, "View charts and the audit log");
        this.tileToolTip.SetToolTip(this.settingsTile, "Configure application preferences");
        this.refreshToolStripButton.ToolTipText = "Refresh dashboard statistics";
        this.logoutToolStripButton.ToolTipText = "Log out of WarehouseApp";

        this.productsTile.TileClicked += (_, _) => this.OpenForm(new ProductsListForm());
        this.productDetailTile.TileClicked += (_, _) => this.OpenForm(new ProductDetailForm());
        this.categoriesTile.TileClicked += (_, _) => this.OpenForm(new CategoriesForm());
        this.suppliersTile.TileClicked += (_, _) => this.OpenForm(new SuppliersForm());
        this.customersTile.TileClicked += (_, _) => this.OpenForm(new CustomersForm());
        this.warehousesTile.TileClicked += (_, _) => this.OpenForm(new WarehousesForm());
        this.stockOverviewTile.TileClicked += (_, _) => this.OpenForm(new StockOverviewForm());
        this.stockInTile.TileClicked += (_, _) => this.OpenForm(new StockInForm());
        this.stockOutTile.TileClicked += (_, _) => this.OpenForm(new StockOutForm());
        this.stockTransferTile.TileClicked += (_, _) => this.OpenForm(new StockTransferForm());
        this.stockAdjustmentTile.TileClicked += (_, _) => this.OpenForm(new StockAdjustmentForm());
        this.purchaseOrdersTile.TileClicked += (_, _) => this.OpenForm(new PurchaseOrdersListForm());
        this.purchaseOrderDetailTile.TileClicked += (_, _) => this.OpenForm(new PurchaseOrderDetailForm());
        this.salesOrdersTile.TileClicked += (_, _) => this.OpenForm(new SalesOrdersListForm());
        this.salesOrderDetailTile.TileClicked += (_, _) => this.OpenForm(new SalesOrderDetailForm());
        this.usersTile.TileClicked += (_, _) => this.OpenForm(new UsersForm());
        this.reportsTile.TileClicked += (_, _) => this.OpenForm(new ReportsForm());
        this.settingsTile.TileClicked += (_, _) => this.OpenForm(new SettingsForm());

        this.tilesFlowPanel.Controls.Add(this.productsTile);
        this.tilesFlowPanel.Controls.Add(this.productDetailTile);
        this.tilesFlowPanel.Controls.Add(this.categoriesTile);
        this.tilesFlowPanel.Controls.Add(this.suppliersTile);
        this.tilesFlowPanel.Controls.Add(this.customersTile);
        this.tilesFlowPanel.Controls.Add(this.warehousesTile);
        this.tilesFlowPanel.Controls.Add(this.stockOverviewTile);
        this.tilesFlowPanel.Controls.Add(this.stockInTile);
        this.tilesFlowPanel.Controls.Add(this.stockOutTile);
        this.tilesFlowPanel.Controls.Add(this.stockTransferTile);
        this.tilesFlowPanel.Controls.Add(this.stockAdjustmentTile);
        this.tilesFlowPanel.Controls.Add(this.purchaseOrdersTile);
        this.tilesFlowPanel.Controls.Add(this.purchaseOrderDetailTile);
        this.tilesFlowPanel.Controls.Add(this.salesOrdersTile);
        this.tilesFlowPanel.Controls.Add(this.salesOrderDetailTile);
        this.tilesFlowPanel.Controls.Add(this.usersTile);
        this.tilesFlowPanel.Controls.Add(this.reportsTile);
        this.tilesFlowPanel.Controls.Add(this.settingsTile);

        this.sidePanel = new System.Windows.Forms.Panel();
        this.sidePanel.Dock = System.Windows.Forms.DockStyle.Right;
        this.sidePanel.Width = 200;
        this.sidePanel.Padding = new System.Windows.Forms.Padding(12);
        this.capacityLabel = new System.Windows.Forms.Label();
        this.capacityLabel.Text = "Overall Capacity";
        this.capacityLabel.AutoSize = true;
        this.capacityLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this.capacityLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        this.capacityLabel.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        this.capacityGauge = new WarehouseApp.Controls.GaugeControl();
        this.capacityGauge.Dock = System.Windows.Forms.DockStyle.Top;
        this.capacityGauge.Unit = "%";
        this.capacityGauge.Minimum = 0;
        this.capacityGauge.Maximum = 100;
        this.sidePanel.Controls.Add(this.capacityGauge);
        this.sidePanel.Controls.Add(this.capacityLabel);

        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.userStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.userStatusLabel.Spring = true;
        this.userStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.statusProgressBar = new System.Windows.Forms.ToolStripProgressBar();
        this.statusProgressBar.Visible = false;
        this.statusProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
        this.clockStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.statusStrip.Items.Add(this.userStatusLabel);
        this.statusStrip.Items.Add(this.statusProgressBar);
        this.statusStrip.Items.Add(this.clockStatusLabel);

        this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
        this.notifyIcon.Icon = System.Drawing.Icon.FromHandle(Common.AppIcons.CreateLogo(32).GetHicon());
        this.notifyIcon.Text = "WarehouseApp";
        this.notifyIcon.Visible = true;
        this.trayShowMenuItem = new System.Windows.Forms.ToolStripMenuItem("Show Dashboard");
        this.trayExitMenuItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        this.trayShowMenuItem.Click += (_, _) => { this.Show(); this.WindowState = System.Windows.Forms.FormWindowState.Normal; this.Activate(); };
        this.trayExitMenuItem.Click += this.exitMenuItem_Click;
        this.notifyIconContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
        this.notifyIconContextMenu.Items.Add(this.trayShowMenuItem);
        this.notifyIconContextMenu.Items.Add(this.trayExitMenuItem);
        this.notifyIcon.ContextMenuStrip = this.notifyIconContextMenu;
        this.notifyIcon.DoubleClick += (_, _) => { this.Show(); this.WindowState = System.Windows.Forms.FormWindowState.Normal; this.Activate(); };

        this.clockTimer = new System.Windows.Forms.Timer(this.components);
        this.clockTimer.Interval = 1000;
        this.clockTimer.Tick += this.clockTimer_Tick;

        this.SuspendLayout();
        this.ClientSize = new System.Drawing.Size(1100, 700);
        this.Controls.Add(this.tilesFlowPanel);
        this.Controls.Add(this.sidePanel);
        this.Controls.Add(this.mainToolStrip);
        this.Controls.Add(this.menuStrip);
        this.Controls.Add(this.statusStrip);
        this.MainMenuStrip = this.menuStrip;
        this.MinimumSize = new System.Drawing.Size(900, 600);
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Dashboard — WarehouseApp";
        this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
