namespace WarehouseApp.Forms;

partial class CustomersForm
{
    private System.ComponentModel.IContainer components = null!;
    private ToolStrip mainToolStrip = null!;
    private ToolStripButton newButton = null!;
    private ToolStripButton saveButton = null!;
    private ToolStripButton deleteButton = null!;
    private ToolStripButton refreshButton = null!;
    private ToolStripTextBox searchTextBox = null!;
    private SplitContainer mainSplitContainer = null!;
    private DataGridView customersGrid = null!;
    private DataGridViewTextBoxColumn nameColumn = null!;
    private DataGridViewTextBoxColumn phoneColumn = null!;
    private DataGridViewCheckBoxColumn activeColumn = null!;
    private BindingSource bindingSourceControl = null!;

    private TabControl detailTabControl = null!;
    private TabPage infoTabPage = null!;
    private TabPage ordersTabPage = null!;
    private TabPage notesTabPage = null!;

    private TableLayoutPanel infoTableLayoutPanel = null!;
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
    private CheckBox activeCheckBox = null!;

    private ListView ordersListView = null!;
    private ColumnHeader orderNumberColumnHeader = null!;
    private ColumnHeader orderStatusColumnHeader = null!;
    private ColumnHeader orderDateColumnHeader = null!;

    private RichTextBox notesRichTextBox = null!;

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
        this.newButton = new System.Windows.Forms.ToolStripButton("New", null, (_, _) => this.NewCustomer());
        this.newButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.saveButton = new System.Windows.Forms.ToolStripButton("Save", null, async (_, _) => await this.SaveCustomerAsync());
        this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.deleteButton = new System.Windows.Forms.ToolStripButton("Delete", null, async (_, _) => await this.DeleteCustomerAsync());
        this.deleteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.refreshButton = new System.Windows.Forms.ToolStripButton("Refresh", null, async (_, _) => await this.LoadCustomersAsync());
        this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.searchTextBox = new System.Windows.Forms.ToolStripTextBox();
        this.searchTextBox.ToolTipText = "Search customers";
        this.searchTextBox.KeyDown += async (_, e) => { if (e.KeyCode == System.Windows.Forms.Keys.Enter) { e.SuppressKeyPress = true; await this.LoadCustomersAsync(); } };
        this.mainToolStrip.Items.Add(this.newButton);
        this.mainToolStrip.Items.Add(this.saveButton);
        this.mainToolStrip.Items.Add(this.deleteButton);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(this.refreshButton);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripLabel("Search:"));
        this.mainToolStrip.Items.Add(this.searchTextBox);
        this.mainToolStrip.Dock = System.Windows.Forms.DockStyle.Top;

        this.bindingSourceControl = new System.Windows.Forms.BindingSource();
        this.nameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.nameColumn.Name = "Name";
        this.nameColumn.HeaderText = "Name";
        this.nameColumn.DataPropertyName = "Name";
        this.nameColumn.Width = 180;
        this.phoneColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.phoneColumn.Name = "Phone";
        this.phoneColumn.HeaderText = "Phone";
        this.phoneColumn.DataPropertyName = "Phone";
        this.phoneColumn.Width = 110;
        this.activeColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this.activeColumn.Name = "Active";
        this.activeColumn.HeaderText = "Active";
        this.activeColumn.DataPropertyName = "IsActive";
        this.activeColumn.Width = 55;
        this.customersGrid = new System.Windows.Forms.DataGridView();
        this.customersGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this.customersGrid.AutoGenerateColumns = false;
        this.customersGrid.AllowUserToAddRows = false;
        this.customersGrid.AllowUserToDeleteRows = false;
        this.customersGrid.ReadOnly = true;
        this.customersGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.customersGrid.MultiSelect = false;
        this.customersGrid.RowHeadersVisible = false;
        this.customersGrid.DataSource = this.bindingSourceControl;
        this.customersGrid.Columns.AddRange(this.nameColumn, this.phoneColumn, this.activeColumn);
        this.customersGrid.SelectionChanged += this.CustomersGrid_SelectionChanged;

        this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
        this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainSplitContainer.SplitterDistance = 380;
        this.mainSplitContainer.Panel1.Controls.Add(this.customersGrid);

        this.infoTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
        this.infoTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.infoTableLayoutPanel.ColumnCount = 2;
        this.infoTableLayoutPanel.Padding = new System.Windows.Forms.Padding(10);
        this.infoTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90));
        this.infoTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
        {
            this.infoTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34));
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
        this.activeCheckBox = new System.Windows.Forms.CheckBox();
        this.activeCheckBox.Text = "Active";
        this.activeCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.activeCheckBox.Checked = true;

        this.infoTableLayoutPanel.Controls.Add(this.nameLabel, 0, 0);
        this.infoTableLayoutPanel.Controls.Add(this.nameTextBox, 1, 0);
        this.infoTableLayoutPanel.Controls.Add(this.contactLabel, 0, 1);
        this.infoTableLayoutPanel.Controls.Add(this.contactTextBox, 1, 1);
        this.infoTableLayoutPanel.Controls.Add(this.phoneLabel, 0, 2);
        this.infoTableLayoutPanel.Controls.Add(this.phoneMaskedTextBox, 1, 2);
        this.infoTableLayoutPanel.Controls.Add(this.emailLabel, 0, 3);
        this.infoTableLayoutPanel.Controls.Add(this.emailTextBox, 1, 3);
        this.infoTableLayoutPanel.Controls.Add(this.addressLabel, 0, 4);
        this.infoTableLayoutPanel.Controls.Add(this.addressTextBox, 1, 4);
        this.infoTableLayoutPanel.Controls.Add(this.activeCheckBox, 1, 5);

        this.orderNumberColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.orderNumberColumnHeader.Text = "Order #";
        this.orderNumberColumnHeader.Width = 100;
        this.orderStatusColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.orderStatusColumnHeader.Text = "Status";
        this.orderStatusColumnHeader.Width = 100;
        this.orderDateColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.orderDateColumnHeader.Text = "Order Date";
        this.orderDateColumnHeader.Width = 110;
        this.ordersListView = new System.Windows.Forms.ListView();
        this.ordersListView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.ordersListView.View = System.Windows.Forms.View.Details;
        this.ordersListView.FullRowSelect = true;
        this.ordersListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.orderNumberColumnHeader, this.orderStatusColumnHeader, this.orderDateColumnHeader });

        this.notesRichTextBox = new System.Windows.Forms.RichTextBox();
        this.notesRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;

        this.infoTabPage = new System.Windows.Forms.TabPage("Info");
        this.infoTabPage.Controls.Add(this.infoTableLayoutPanel);
        this.ordersTabPage = new System.Windows.Forms.TabPage("Orders");
        this.ordersTabPage.Controls.Add(this.ordersListView);
        this.notesTabPage = new System.Windows.Forms.TabPage("Notes");
        this.notesTabPage.Controls.Add(this.notesRichTextBox);

        this.detailTabControl = new System.Windows.Forms.TabControl();
        this.detailTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.detailTabControl.TabPages.AddRange(new System.Windows.Forms.TabPage[] { this.infoTabPage, this.ordersTabPage, this.notesTabPage });
        this.mainSplitContainer.Panel2.Controls.Add(this.detailTabControl);

        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.recordCountLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.recordCountLabel.Spring = true;
        this.recordCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.statusStrip.Items.Add(this.recordCountLabel);

        this.ClientSize = new System.Drawing.Size(860, 540);
        this.Controls.Add(this.mainSplitContainer);
        this.Controls.Add(this.mainToolStrip);
        this.Controls.Add(this.statusStrip);
        this.Text = "Customers — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
