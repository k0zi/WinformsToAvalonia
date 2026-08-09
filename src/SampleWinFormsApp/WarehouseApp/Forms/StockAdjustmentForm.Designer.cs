namespace WarehouseApp.Forms;

partial class StockAdjustmentForm
{
    private System.ComponentModel.IContainer components = null!;
    private SplitContainer mainSplitContainer = null!;

    private Panel selectionPanel = null!;
    private Label warehouseLabel = null!;
    private ComboBox warehouseComboBox = null!;
    private Button loadItemsButton = null!;
    private CheckedListBox itemsCheckedListBox = null!;
    private Button startCountButton = null!;

    private Panel countPanel = null!;
    private DataGridView countGrid = null!;
    private DataGridViewTextBoxColumn productColumn = null!;
    private DataGridViewTextBoxColumn currentQtyColumn = null!;
    private DataGridViewTextBoxColumn countedQtyColumn = null!;
    private Label reasonLabel = null!;
    private RichTextBox reasonRichTextBox = null!;
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
        this.warehouseLabel.Location = new System.Drawing.Point(10, 12);
        this.warehouseLabel.AutoSize = true;
        this.warehouseComboBox = new System.Windows.Forms.ComboBox();
        this.warehouseComboBox.Location = new System.Drawing.Point(10, 30);
        this.warehouseComboBox.Width = 220;
        this.warehouseComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.loadItemsButton = new System.Windows.Forms.Button();
        this.loadItemsButton.Text = "Load Items";
        this.loadItemsButton.Location = new System.Drawing.Point(10, 60);
        this.loadItemsButton.Size = new System.Drawing.Size(220, 28);
        this.loadItemsButton.Click += async (_, _) => await this.LoadItemsAsync();

        this.itemsCheckedListBox = new System.Windows.Forms.CheckedListBox();
        this.itemsCheckedListBox.Location = new System.Drawing.Point(10, 96);
        this.itemsCheckedListBox.Size = new System.Drawing.Size(220, 280);
        this.itemsCheckedListBox.CheckOnClick = true;

        this.startCountButton = new System.Windows.Forms.Button();
        this.startCountButton.Text = "Start Count Session →";
        this.startCountButton.Location = new System.Drawing.Point(10, 384);
        this.startCountButton.Size = new System.Drawing.Size(220, 30);
        this.startCountButton.Click += (_, _) => this.StartCountSession();

        this.selectionPanel = new System.Windows.Forms.Panel();
        this.selectionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.selectionPanel.Controls.Add(this.warehouseLabel);
        this.selectionPanel.Controls.Add(this.warehouseComboBox);
        this.selectionPanel.Controls.Add(this.loadItemsButton);
        this.selectionPanel.Controls.Add(this.itemsCheckedListBox);
        this.selectionPanel.Controls.Add(this.startCountButton);

        this.productColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.productColumn.Name = "Product";
        this.productColumn.HeaderText = "Product";
        this.productColumn.Width = 220;
        this.productColumn.ReadOnly = true;
        this.currentQtyColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.currentQtyColumn.Name = "CurrentQty";
        this.currentQtyColumn.HeaderText = "System Qty";
        this.currentQtyColumn.Width = 90;
        this.currentQtyColumn.ReadOnly = true;
        this.currentQtyColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.countedQtyColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.countedQtyColumn.Name = "CountedQty";
        this.countedQtyColumn.HeaderText = "Counted Qty";
        this.countedQtyColumn.Width = 100;
        this.countedQtyColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.countedQtyColumn.DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow;
        this.countGrid = new System.Windows.Forms.DataGridView();
        this.countGrid.Dock = System.Windows.Forms.DockStyle.Top;
        this.countGrid.Height = 260;
        this.countGrid.AutoGenerateColumns = false;
        this.countGrid.AllowUserToAddRows = false;
        this.countGrid.RowHeadersVisible = false;
        this.countGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
        this.countGrid.Columns.AddRange(this.productColumn, this.currentQtyColumn, this.countedQtyColumn);

        this.reasonLabel = new System.Windows.Forms.Label();
        this.reasonLabel.Text = "Reason / Notes:";
        this.reasonLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this.reasonLabel.Height = 20;
        this.reasonLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
        this.reasonRichTextBox = new System.Windows.Forms.RichTextBox();
        this.reasonRichTextBox.Dock = System.Windows.Forms.DockStyle.Top;
        this.reasonRichTextBox.Height = 90;
        this.postButton = new System.Windows.Forms.Button();
        this.postButton.Text = "Post Adjustment";
        this.postButton.Dock = System.Windows.Forms.DockStyle.Top;
        this.postButton.Height = 32;
        this.postButton.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        this.postButton.Click += async (_, _) => await this.PostAdjustmentAsync();
        this.statusLabel = new System.Windows.Forms.Label();
        this.statusLabel.Dock = System.Windows.Forms.DockStyle.Top;
        this.statusLabel.Height = 24;
        this.statusLabel.ForeColor = System.Drawing.Color.SeaGreen;

        this.countPanel = new System.Windows.Forms.Panel();
        this.countPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.countPanel.Padding = new System.Windows.Forms.Padding(10);
        this.countPanel.Controls.Add(this.statusLabel);
        this.countPanel.Controls.Add(this.postButton);
        this.countPanel.Controls.Add(this.reasonRichTextBox);
        this.countPanel.Controls.Add(this.reasonLabel);
        this.countPanel.Controls.Add(this.countGrid);

        this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
        this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainSplitContainer.SplitterDistance = 250;
        this.mainSplitContainer.Panel1.Controls.Add(this.selectionPanel);
        this.mainSplitContainer.Panel2.Controls.Add(this.countPanel);

        this.ClientSize = new System.Drawing.Size(760, 500);
        this.Controls.Add(this.mainSplitContainer);
        this.Text = "Stock Adjustment — Physical Count — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}