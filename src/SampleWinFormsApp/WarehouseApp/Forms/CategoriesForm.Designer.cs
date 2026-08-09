namespace WarehouseApp.Forms;

partial class CategoriesForm
{
    private System.ComponentModel.IContainer components = null!;
    private ToolStrip mainToolStrip = null!;
    private ToolStripButton newRootButton = null!;
    private ToolStripButton newChildButton = null!;
    private ToolStripButton saveButton = null!;
    private ToolStripButton deleteButton = null!;
    private ToolStripButton refreshButton = null!;

    private SplitContainer mainSplitContainer = null!;
    private TreeView categoriesTreeView = null!;
    private ImageList categoryImageList = null!;
    private ContextMenuStrip treeContextMenu = null!;
    private ToolStripMenuItem addChildMenuItem = null!;
    private ToolStripMenuItem deleteMenuItem = null!;

    private GroupBox detailsGroupBox = null!;
    private Label nameLabel = null!;
    private TextBox nameTextBox = null!;
    private Label descriptionLabel = null!;
    private TextBox descriptionTextBox = null!;
    private Label parentLabel = null!;
    private Label parentValueLabel = null!;

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

        this.categoryImageList = new System.Windows.Forms.ImageList();
        this.categoryImageList.ImageSize = new System.Drawing.Size(16, 16);
        this.categoryImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
        this.categoryImageList.Images.Add("Folder", Common.AppIcons.CreateGlyph("▤", System.Drawing.Color.DarkGoldenrod, 16));
        this.categoryImageList.Images.Add("Leaf", Common.AppIcons.CreateGlyph("▫", System.Drawing.Color.SlateGray, 16));

        this.mainToolStrip = new System.Windows.Forms.ToolStrip();
        this.newRootButton = new System.Windows.Forms.ToolStripButton("New Root", null, (_, _) => this.NewCategory(null));
        this.newRootButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.newChildButton = new System.Windows.Forms.ToolStripButton("New Subcategory", null, (_, _) => this.NewCategory(this.SelectedCategory));
        this.newChildButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.saveButton = new System.Windows.Forms.ToolStripButton("Save", null, async (_, _) => await this.SaveCategoryAsync());
        this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.deleteButton = new System.Windows.Forms.ToolStripButton("Delete", null, async (_, _) => await this.DeleteCategoryAsync());
        this.deleteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.refreshButton = new System.Windows.Forms.ToolStripButton("Refresh", null, async (_, _) => await this.LoadCategoriesAsync());
        this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.mainToolStrip.Items.Add(this.newRootButton);
        this.mainToolStrip.Items.Add(this.newChildButton);
        this.mainToolStrip.Items.Add(this.saveButton);
        this.mainToolStrip.Items.Add(this.deleteButton);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(this.refreshButton);
        this.mainToolStrip.Dock = System.Windows.Forms.DockStyle.Top;

        this.treeContextMenu = new System.Windows.Forms.ContextMenuStrip();
        this.addChildMenuItem = new System.Windows.Forms.ToolStripMenuItem("Add Subcategory", null, (_, _) => this.NewCategory(this.SelectedCategory));
        this.deleteMenuItem = new System.Windows.Forms.ToolStripMenuItem("Delete", null, async (_, _) => await this.DeleteCategoryAsync());
        this.treeContextMenu.Items.Add(this.addChildMenuItem);
        this.treeContextMenu.Items.Add(this.deleteMenuItem);

        this.categoriesTreeView = new System.Windows.Forms.TreeView();
        this.categoriesTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.categoriesTreeView.ImageList = this.categoryImageList;
        this.categoriesTreeView.ContextMenuStrip = this.treeContextMenu;
        this.categoriesTreeView.HideSelection = false;
        this.categoriesTreeView.AfterSelect += this.CategoriesTreeView_AfterSelect;

        this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
        this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainSplitContainer.SplitterDistance = 320;
        this.mainSplitContainer.Panel1.Controls.Add(this.categoriesTreeView);

        this.detailsGroupBox = new System.Windows.Forms.GroupBox();
        this.detailsGroupBox.Text = "Category Details";
        this.detailsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.detailsGroupBox.Padding = new System.Windows.Forms.Padding(10);
        this.nameLabel = new System.Windows.Forms.Label();
        this.nameLabel.Text = "Name:";
        this.nameLabel.Location = new System.Drawing.Point(10, 30);
        this.nameLabel.AutoSize = true;
        this.nameTextBox = new System.Windows.Forms.TextBox();
        this.nameTextBox.Location = new System.Drawing.Point(110, 27);
        this.nameTextBox.Width = 260;
        this.descriptionLabel = new System.Windows.Forms.Label();
        this.descriptionLabel.Text = "Description:";
        this.descriptionLabel.Location = new System.Drawing.Point(10, 65);
        this.descriptionLabel.AutoSize = true;
        this.descriptionTextBox = new System.Windows.Forms.TextBox();
        this.descriptionTextBox.Location = new System.Drawing.Point(110, 62);
        this.descriptionTextBox.Width = 260;
        this.descriptionTextBox.Height = 60;
        this.descriptionTextBox.Multiline = true;
        this.parentLabel = new System.Windows.Forms.Label();
        this.parentLabel.Text = "Parent:";
        this.parentLabel.Location = new System.Drawing.Point(10, 135);
        this.parentLabel.AutoSize = true;
        this.parentValueLabel = new System.Windows.Forms.Label();
        this.parentValueLabel.Text = "(none — root category)";
        this.parentValueLabel.Location = new System.Drawing.Point(110, 135);
        this.parentValueLabel.AutoSize = true;
        this.parentValueLabel.ForeColor = System.Drawing.Color.Gray;

        this.detailsGroupBox.Controls.Add(this.nameLabel);
        this.detailsGroupBox.Controls.Add(this.nameTextBox);
        this.detailsGroupBox.Controls.Add(this.descriptionLabel);
        this.detailsGroupBox.Controls.Add(this.descriptionTextBox);
        this.detailsGroupBox.Controls.Add(this.parentLabel);
        this.detailsGroupBox.Controls.Add(this.parentValueLabel);
        this.mainSplitContainer.Panel2.Controls.Add(this.detailsGroupBox);

        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.recordCountLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.recordCountLabel.Spring = true;
        this.recordCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.statusStrip.Items.Add(this.recordCountLabel);

        this.ClientSize = new System.Drawing.Size(760, 500);
        this.Controls.Add(this.mainSplitContainer);
        this.Controls.Add(this.mainToolStrip);
        this.Controls.Add(this.statusStrip);
        this.Text = "Categories — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}