using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class SuppliersForm
{
    private System.ComponentModel.IContainer components = null!;
    private ToolStrip mainToolStrip = null!;
    private ToolStripButton newButton = null!;
    private ToolStripButton saveButton = null!;
    private ToolStripButton deleteButton = null!;
    private ToolStripButton refreshButton = null!;
    private ToolStripTextBox searchTextBox = null!;
    private SplitContainer mainSplitContainer = null!;
    private ListView suppliersListView = null!;
    private ColumnHeader nameColumnHeader = null!;
    private ColumnHeader phoneColumnHeader = null!;
    private ColumnHeader ratingColumnHeader = null!;
    private ImageList supplierImageList = null!;
    private GroupBox detailsGroupBox = null!;
    private TableLayoutPanel detailsTableLayoutPanel = null!;
    private Label nameLabel = null!;
    private TextBox nameTextBox = null!;
    private Label contactLabel = null!;
    private TextBox contactTextBox = null!;
    private Label phoneLabel = null!;
    private MaskedTextBox phoneMaskedTextBox = null!;
    private Label emailLabel = null!;
    private TextBox emailTextBox = null!;
    private Label addressLabel = null!;
    private TextBox addressTextBox = null!;
    private Label ratingLabel = null!;
    private StarRatingControl ratingControl = null!;
    private CheckBox activeCheckBox = null!;
    private LinkLabel emailLinkLabel = null!;
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

        this.supplierImageList = new System.Windows.Forms.ImageList();
        this.supplierImageList.ImageSize = new System.Drawing.Size(16, 16);
        this.supplierImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
        this.supplierImageList.Images.Add("Supplier", Common.AppIcons.CreateGlyph("➜", System.Drawing.Color.IndianRed, 16));

        this.mainToolStrip = new System.Windows.Forms.ToolStrip();
        this.newButton = new System.Windows.Forms.ToolStripButton("New", null, (_, _) => this.NewSupplier());
        this.newButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.saveButton = new System.Windows.Forms.ToolStripButton("Save", null, async (_, _) => await this.SaveSupplierAsync());
        this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.deleteButton = new System.Windows.Forms.ToolStripButton("Delete", null, async (_, _) => await this.DeleteSupplierAsync());
        this.deleteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.refreshButton = new System.Windows.Forms.ToolStripButton("Refresh", null, async (_, _) => await this.LoadSuppliersAsync());
        this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.searchTextBox = new System.Windows.Forms.ToolStripTextBox();
        this.searchTextBox.ToolTipText = "Search suppliers";
        this.searchTextBox.KeyDown += async (_, e) => { if (e.KeyCode == System.Windows.Forms.Keys.Enter) { e.SuppressKeyPress = true; await this.LoadSuppliersAsync(); } };
        this.mainToolStrip.Items.Add(this.newButton);
        this.mainToolStrip.Items.Add(this.saveButton);
        this.mainToolStrip.Items.Add(this.deleteButton);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(this.refreshButton);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripLabel("Search:"));
        this.mainToolStrip.Items.Add(this.searchTextBox);
        this.mainToolStrip.Dock = System.Windows.Forms.DockStyle.Top;

        this.nameColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.nameColumnHeader.Text = "Name";
        this.nameColumnHeader.Width = 180;
        this.phoneColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.phoneColumnHeader.Text = "Phone";
        this.phoneColumnHeader.Width = 110;
        this.ratingColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.ratingColumnHeader.Text = "Rating";
        this.ratingColumnHeader.Width = 60;

        this.suppliersListView = new System.Windows.Forms.ListView();
        this.suppliersListView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.suppliersListView.View = System.Windows.Forms.View.Details;
        this.suppliersListView.FullRowSelect = true;
        this.suppliersListView.MultiSelect = false;
        this.suppliersListView.HideSelection = false;
        this.suppliersListView.SmallImageList = this.supplierImageList;
        this.suppliersListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.nameColumnHeader, this.phoneColumnHeader, this.ratingColumnHeader });
        this.suppliersListView.SelectedIndexChanged += this.SuppliersListView_SelectedIndexChanged;

        this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
        this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainSplitContainer.SplitterDistance = 380;
        this.mainSplitContainer.Panel1.Controls.Add(this.suppliersListView);

        this.detailsGroupBox = new System.Windows.Forms.GroupBox();
        this.detailsGroupBox.Text = "Supplier Details";
        this.detailsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.detailsGroupBox.Padding = new System.Windows.Forms.Padding(10);
        this.detailsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
        this.detailsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.detailsTableLayoutPanel.Height = 250;
        this.detailsTableLayoutPanel.ColumnCount = 2;
        this.detailsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90));
        this.detailsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
        {
            this.detailsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34));
        }

        this.nameLabel = new System.Windows.Forms.Label();
        this.nameLabel.Text = "Name:";
        this.nameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.nameTextBox = new System.Windows.Forms.TextBox();
        this.nameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.nameTextBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.contactLabel = new System.Windows.Forms.Label();
        this.contactLabel.Text = "Contact:";
        this.contactLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.contactLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.contactTextBox = new System.Windows.Forms.TextBox();
        this.contactTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.contactTextBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.phoneLabel = new System.Windows.Forms.Label();
        this.phoneLabel.Text = "Phone:";
        this.phoneLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.phoneLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.phoneMaskedTextBox = new System.Windows.Forms.MaskedTextBox();
        this.phoneMaskedTextBox.Mask = "000-0000";
        this.phoneMaskedTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.phoneMaskedTextBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.emailLabel = new System.Windows.Forms.Label();
        this.emailLabel.Text = "Email:";
        this.emailLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.emailLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.emailTextBox = new System.Windows.Forms.TextBox();
        this.emailTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.emailTextBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.addressLabel = new System.Windows.Forms.Label();
        this.addressLabel.Text = "Address:";
        this.addressLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.addressLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.addressTextBox = new System.Windows.Forms.TextBox();
        this.addressTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.addressTextBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.ratingLabel = new System.Windows.Forms.Label();
        this.ratingLabel.Text = "Rating:";
        this.ratingLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.ratingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.ratingControl = new WarehouseApp.Controls.StarRatingControl();
        this.ratingControl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.ratingControl.Margin = new System.Windows.Forms.Padding(3, 8, 3, 5);

        this.detailsTableLayoutPanel.Controls.Add(this.nameLabel, 0, 0);
        this.detailsTableLayoutPanel.Controls.Add(this.nameTextBox, 1, 0);
        this.detailsTableLayoutPanel.Controls.Add(this.contactLabel, 0, 1);
        this.detailsTableLayoutPanel.Controls.Add(this.contactTextBox, 1, 1);
        this.detailsTableLayoutPanel.Controls.Add(this.phoneLabel, 0, 2);
        this.detailsTableLayoutPanel.Controls.Add(this.phoneMaskedTextBox, 1, 2);
        this.detailsTableLayoutPanel.Controls.Add(this.emailLabel, 0, 3);
        this.detailsTableLayoutPanel.Controls.Add(this.emailTextBox, 1, 3);
        this.detailsTableLayoutPanel.Controls.Add(this.addressLabel, 0, 4);
        this.detailsTableLayoutPanel.Controls.Add(this.addressTextBox, 1, 4);
        this.detailsTableLayoutPanel.Controls.Add(this.ratingLabel, 0, 5);
        this.detailsTableLayoutPanel.Controls.Add(this.ratingControl, 1, 5);

        this.activeCheckBox = new System.Windows.Forms.CheckBox();
        this.activeCheckBox.Text = "Active";
        this.activeCheckBox.Location = new System.Drawing.Point(10, 258);
        this.activeCheckBox.AutoSize = true;
        this.activeCheckBox.Checked = true;
        this.emailLinkLabel = new System.Windows.Forms.LinkLabel();
        this.emailLinkLabel.Text = "Send email";
        this.emailLinkLabel.Location = new System.Drawing.Point(10, 284);
        this.emailLinkLabel.AutoSize = true;
        this.emailLinkLabel.LinkClicked += this.EmailLinkLabel_LinkClicked;

        this.detailsGroupBox.Controls.Add(this.detailsTableLayoutPanel);
        this.detailsGroupBox.Controls.Add(this.activeCheckBox);
        this.detailsGroupBox.Controls.Add(this.emailLinkLabel);
        this.mainSplitContainer.Panel2.Controls.Add(this.detailsGroupBox);

        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.recordCountLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.recordCountLabel.Spring = true;
        this.recordCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.statusStrip.Items.Add(this.recordCountLabel);

        this.ClientSize = new System.Drawing.Size(820, 520);
        this.Controls.Add(this.mainSplitContainer);
        this.Controls.Add(this.mainToolStrip);
        this.Controls.Add(this.statusStrip);
        this.Text = "Suppliers — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
