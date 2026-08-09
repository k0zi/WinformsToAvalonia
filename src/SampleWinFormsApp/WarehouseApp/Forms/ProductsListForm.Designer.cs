namespace WarehouseApp.Forms;

partial class ProductsListForm
{
    private ImageList statusImageList = null!;
    private ToolStripComboBox categoryFilterComboBox = null!;
    private ToolStripSplitButton quickAddSplitButton = null!;
    private DataGridViewImageColumn statusColumn = null!;
    private DataGridViewTextBoxColumn skuColumn = null!;
    private DataGridViewTextBoxColumn nameColumn = null!;
    private DataGridViewTextBoxColumn categoryColumn = null!;
    private DataGridViewTextBoxColumn supplierColumn = null!;
    private DataGridViewTextBoxColumn priceColumn = null!;
    private DataGridViewTextBoxColumn reorderColumn = null!;
    private DataGridViewCheckBoxColumn activeColumn = null!;

    private void InitializeComponent()
    {
        this.Text = "Products — WarehouseApp";

        this.statusImageList = new System.Windows.Forms.ImageList();
        this.statusImageList.ImageSize = new System.Drawing.Size(16, 16);
        this.statusImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
        this.statusImageList.Images.Add("Active", Common.AppIcons.CreateGlyph("●", System.Drawing.Color.SeaGreen, 16));
        this.statusImageList.Images.Add("Inactive", Common.AppIcons.CreateGlyph("●", System.Drawing.Color.Gray, 16));
        this.statusImageList.Images.Add("Add", Common.AppIcons.CreateGlyph("+", System.Drawing.Color.SeaGreen, 16));

        this.ToolbarStrip.ImageList = this.statusImageList;
        this.AddButton.ImageKey = "Add";
        this.AddButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;

        this.categoryFilterComboBox = new System.Windows.Forms.ToolStripComboBox();
        this.categoryFilterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.categoryFilterComboBox.Width = 150;
        this.InsertFilterComboBox("Category:", this.categoryFilterComboBox);

        this.quickAddSplitButton = new System.Windows.Forms.ToolStripSplitButton("Quick Add");
        this.quickAddSplitButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.quickAddSplitButton.ButtonClick += (_, _) => this.AddNew();
        this.quickAddSplitButton.DropDownItems.Add("Duplicate Selected Product", null, this.quickAddDuplicate_Click);
        this.ToolbarStrip.Items.Insert(this.ToolbarStrip.Items.IndexOf(this.AddButton) + 1, this.quickAddSplitButton);

        this.statusColumn = new System.Windows.Forms.DataGridViewImageColumn();
        this.statusColumn.Name = "Status";
        this.statusColumn.HeaderText = "";
        this.statusColumn.Width = 32;
        this.statusColumn.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
        this.skuColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.skuColumn.Name = "Sku";
        this.skuColumn.HeaderText = "SKU";
        this.skuColumn.DataPropertyName = "Sku";
        this.skuColumn.Width = 90;
        this.nameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.nameColumn.Name = "Name";
        this.nameColumn.HeaderText = "Name";
        this.nameColumn.DataPropertyName = "Name";
        this.nameColumn.Width = 220;
        this.categoryColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.categoryColumn.Name = "Category";
        this.categoryColumn.HeaderText = "Category";
        this.categoryColumn.Width = 130;
        this.supplierColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.supplierColumn.Name = "Supplier";
        this.supplierColumn.HeaderText = "Supplier";
        this.supplierColumn.Width = 150;
        this.priceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.priceColumn.Name = "Price";
        this.priceColumn.HeaderText = "Unit Price";
        this.priceColumn.DataPropertyName = "UnitPrice";
        this.priceColumn.Width = 90;
        this.priceColumn.DefaultCellStyle.Format = "C2";
        this.priceColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.reorderColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.reorderColumn.Name = "Reorder";
        this.reorderColumn.HeaderText = "Reorder Lvl";
        this.reorderColumn.DataPropertyName = "ReorderLevel";
        this.reorderColumn.Width = 90;
        this.reorderColumn.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
        this.activeColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this.activeColumn.Name = "Active";
        this.activeColumn.HeaderText = "Active";
        this.activeColumn.DataPropertyName = "IsActive";
        this.activeColumn.Width = 55;

        this.Grid.Columns.AddRange(this.statusColumn, this.skuColumn, this.nameColumn, this.categoryColumn, this.supplierColumn, this.priceColumn, this.reorderColumn, this.activeColumn);
        this.Grid.CellFormatting += this.Grid_CellFormatting;
    }
}