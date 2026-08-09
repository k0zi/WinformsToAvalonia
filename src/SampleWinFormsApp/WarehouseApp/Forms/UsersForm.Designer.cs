using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class UsersForm
{
    private System.ComponentModel.IContainer components = null!;
    private ToolStrip mainToolStrip = null!;
    private ToolStripButton newButton = null!;
    private ToolStripButton saveButton = null!;
    private ToolStripButton deleteButton = null!;
    private ToolStripButton refreshButton = null!;

    private SplitContainer mainSplitContainer = null!;
    private DataGridView usersGrid = null!;
    private BindingSource bindingSourceControl = null!;
    private DataGridViewTextBoxColumn usernameColumn = null!;
    private DataGridViewTextBoxColumn displayNameColumn = null!;
    private DataGridViewTextBoxColumn roleColumn = null!;
    private DataGridViewCheckBoxColumn activeColumn = null!;

    private GroupBox detailsGroupBox = null!;
    private Label usernameLabel = null!;
    private TextBox usernameTextBox = null!;
    private Label displayNameLabel = null!;
    private TextBox displayNameTextBox = null!;
    private Label passwordLabel = null!;
    private TextBox passwordTextBox = null!;
    private Label roleLabel = null!;
    private ComboBox roleComboBox = null!;
    private Label activeLabel = null!;
    private ToggleSwitchControl activeToggle = null!;

    private Label availablePermissionsLabel = null!;
    private ListBox availablePermissionsListBox = null!;
    private Label assignedPermissionsLabel = null!;
    private CheckedListBox assignedPermissionsCheckedListBox = null!;

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
        this.newButton = new System.Windows.Forms.ToolStripButton("New", null, (_, _) => this.NewUser());
        this.newButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.saveButton = new System.Windows.Forms.ToolStripButton("Save", null, async (_, _) => await this.SaveUserAsync());
        this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.deleteButton = new System.Windows.Forms.ToolStripButton("Delete", null, async (_, _) => await this.DeleteUserAsync());
        this.deleteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.refreshButton = new System.Windows.Forms.ToolStripButton("Refresh", null, async (_, _) => await this.LoadUsersAsync());
        this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.mainToolStrip.Items.Add(this.newButton);
        this.mainToolStrip.Items.Add(this.saveButton);
        this.mainToolStrip.Items.Add(this.deleteButton);
        this.mainToolStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        this.mainToolStrip.Items.Add(this.refreshButton);
        this.mainToolStrip.Dock = System.Windows.Forms.DockStyle.Top;

        this.bindingSourceControl = new System.Windows.Forms.BindingSource();
        this.usernameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.usernameColumn.Name = "Username";
        this.usernameColumn.HeaderText = "Username";
        this.usernameColumn.DataPropertyName = "Username";
        this.usernameColumn.Width = 120;
        this.displayNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.displayNameColumn.Name = "DisplayName";
        this.displayNameColumn.HeaderText = "Display Name";
        this.displayNameColumn.DataPropertyName = "DisplayName";
        this.displayNameColumn.Width = 160;
        this.roleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.roleColumn.Name = "Role";
        this.roleColumn.HeaderText = "Role";
        this.roleColumn.Width = 100;
        this.activeColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this.activeColumn.Name = "Active";
        this.activeColumn.HeaderText = "Active";
        this.activeColumn.DataPropertyName = "IsActive";
        this.activeColumn.Width = 55;
        this.usersGrid = new System.Windows.Forms.DataGridView();
        this.usersGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this.usersGrid.AutoGenerateColumns = false;
        this.usersGrid.AllowUserToAddRows = false;
        this.usersGrid.AllowUserToDeleteRows = false;
        this.usersGrid.ReadOnly = true;
        this.usersGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.usersGrid.MultiSelect = false;
        this.usersGrid.RowHeadersVisible = false;
        this.usersGrid.DataSource = this.bindingSourceControl;
        this.usersGrid.Columns.AddRange(this.usernameColumn, this.displayNameColumn, this.roleColumn, this.activeColumn);
        this.usersGrid.CellFormatting += this.UsersGrid_CellFormatting;
        this.usersGrid.SelectionChanged += this.UsersGrid_SelectionChanged;

        this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
        this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainSplitContainer.SplitterDistance = 380;
        this.mainSplitContainer.Panel1.Controls.Add(this.usersGrid);

        this.detailsGroupBox = new System.Windows.Forms.GroupBox();
        this.detailsGroupBox.Text = "User Details";
        this.detailsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.detailsGroupBox.Padding = new System.Windows.Forms.Padding(10);

        this.usernameLabel = new System.Windows.Forms.Label();
        this.usernameLabel.Text = "Username:";
        this.usernameLabel.Location = new System.Drawing.Point(12, 30);
        this.usernameLabel.AutoSize = true;
        this.usernameTextBox = new System.Windows.Forms.TextBox();
        this.usernameTextBox.Location = new System.Drawing.Point(120, 27);
        this.usernameTextBox.Width = 200;
        this.displayNameLabel = new System.Windows.Forms.Label();
        this.displayNameLabel.Text = "Display Name:";
        this.displayNameLabel.Location = new System.Drawing.Point(12, 65);
        this.displayNameLabel.AutoSize = true;
        this.displayNameTextBox = new System.Windows.Forms.TextBox();
        this.displayNameTextBox.Location = new System.Drawing.Point(120, 62);
        this.displayNameTextBox.Width = 200;
        this.passwordLabel = new System.Windows.Forms.Label();
        this.passwordLabel.Text = "Password:";
        this.passwordLabel.Location = new System.Drawing.Point(12, 100);
        this.passwordLabel.AutoSize = true;
        this.passwordTextBox = new System.Windows.Forms.TextBox();
        this.passwordTextBox.Location = new System.Drawing.Point(120, 97);
        this.passwordTextBox.Width = 200;
        this.passwordTextBox.UseSystemPasswordChar = true;
        this.roleLabel = new System.Windows.Forms.Label();
        this.roleLabel.Text = "Role:";
        this.roleLabel.Location = new System.Drawing.Point(12, 135);
        this.roleLabel.AutoSize = true;
        this.roleComboBox = new System.Windows.Forms.ComboBox();
        this.roleComboBox.Location = new System.Drawing.Point(120, 132);
        this.roleComboBox.Width = 200;
        this.roleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.roleComboBox.SelectedIndexChanged += this.RoleComboBox_SelectedIndexChanged;
        this.activeLabel = new System.Windows.Forms.Label();
        this.activeLabel.Text = "Active:";
        this.activeLabel.Location = new System.Drawing.Point(12, 172);
        this.activeLabel.AutoSize = true;
        this.activeToggle = new WarehouseApp.Controls.ToggleSwitchControl();
        this.activeToggle.Location = new System.Drawing.Point(120, 168);

        this.availablePermissionsLabel = new System.Windows.Forms.Label();
        this.availablePermissionsLabel.Text = "All Permissions";
        this.availablePermissionsLabel.Location = new System.Drawing.Point(12, 210);
        this.availablePermissionsLabel.AutoSize = true;
        this.availablePermissionsListBox = new System.Windows.Forms.ListBox();
        this.availablePermissionsListBox.Location = new System.Drawing.Point(12, 228);
        this.availablePermissionsListBox.Size = new System.Drawing.Size(150, 110);
        this.assignedPermissionsLabel = new System.Windows.Forms.Label();
        this.assignedPermissionsLabel.Text = "Assigned to Role";
        this.assignedPermissionsLabel.Location = new System.Drawing.Point(180, 210);
        this.assignedPermissionsLabel.AutoSize = true;
        this.assignedPermissionsCheckedListBox = new System.Windows.Forms.CheckedListBox();
        this.assignedPermissionsCheckedListBox.Location = new System.Drawing.Point(180, 228);
        this.assignedPermissionsCheckedListBox.Size = new System.Drawing.Size(150, 110);
        this.assignedPermissionsCheckedListBox.Enabled = false;

        foreach (var permission in new[] { "Manage Inventory", "Manage Orders", "Manage Users", "View Reports" })
        {
            this.availablePermissionsListBox.Items.Add(permission);
            this.assignedPermissionsCheckedListBox.Items.Add(permission);
        }

        this.detailsGroupBox.Controls.Add(this.usernameLabel);
        this.detailsGroupBox.Controls.Add(this.usernameTextBox);
        this.detailsGroupBox.Controls.Add(this.displayNameLabel);
        this.detailsGroupBox.Controls.Add(this.displayNameTextBox);
        this.detailsGroupBox.Controls.Add(this.passwordLabel);
        this.detailsGroupBox.Controls.Add(this.passwordTextBox);
        this.detailsGroupBox.Controls.Add(this.roleLabel);
        this.detailsGroupBox.Controls.Add(this.roleComboBox);
        this.detailsGroupBox.Controls.Add(this.activeLabel);
        this.detailsGroupBox.Controls.Add(this.activeToggle);
        this.detailsGroupBox.Controls.Add(this.availablePermissionsLabel);
        this.detailsGroupBox.Controls.Add(this.availablePermissionsListBox);
        this.detailsGroupBox.Controls.Add(this.assignedPermissionsLabel);
        this.detailsGroupBox.Controls.Add(this.assignedPermissionsCheckedListBox);
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
        this.Text = "Users & Roles — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
