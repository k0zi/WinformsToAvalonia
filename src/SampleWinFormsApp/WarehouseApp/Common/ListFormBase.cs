namespace WarehouseApp.Common;

/// <summary>
/// Shared grid + toolbar + navigator + search scaffolding for master list screens.
/// Derived forms supply data loading, add/edit/delete behavior, and column setup.
/// </summary>
public abstract class ListFormBase<TEntity> : Form where TEntity : class
{
    protected ToolStrip ToolbarStrip { get; private set; } = null!;
    protected ToolStripButton AddButton { get; private set; } = null!;
    protected ToolStripButton EditButton { get; private set; } = null!;
    protected ToolStripButton DeleteButton { get; private set; } = null!;
    protected ToolStripButton RefreshButton { get; private set; } = null!;
    protected ToolStripComboBox? FilterComboBox { get; set; }
    protected ToolStripTextBox SearchTextBox { get; private set; } = null!;
    protected DataGridView Grid { get; private set; } = null!;
    protected BindingSource BindingSourceControl { get; private set; } = null!;
    protected BindingNavigator Navigator { get; private set; } = null!;
    protected StatusStrip StatusBar { get; private set; } = null!;
    protected ToolStripStatusLabel RecordCountLabel { get; private set; } = null!;
    protected ContextMenuStrip GridContextMenu { get; private set; } = null!;

    private bool _columnsConfigured;

    protected ListFormBase()
    {
        InitializeBaseComponents();
    }

    private void InitializeBaseComponents()
    {
        SuspendLayout();

        Grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None
        };
        Grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };

        BindingSourceControl = new BindingSource();
        Grid.DataSource = BindingSourceControl;

        Navigator = new BindingNavigator(BindingSourceControl) { Dock = DockStyle.Top };
        if (Navigator.AddNewItem is not null)
        {
            Navigator.Items.Remove(Navigator.AddNewItem);
            Navigator.AddNewItem = null;
        }
        if (Navigator.DeleteItem is not null)
        {
            Navigator.Items.Remove(Navigator.DeleteItem);
            Navigator.DeleteItem = null;
        }

        ToolbarStrip = new ToolStrip { Dock = DockStyle.Top };
        AddButton = new ToolStripButton("Add", null, (_, _) => AddNew()) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        EditButton = new ToolStripButton("Edit", null, (_, _) => EditSelected()) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        DeleteButton = new ToolStripButton("Delete", null, async (_, _) => await DeleteSelectedAsync()) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        RefreshButton = new ToolStripButton("Refresh", null, async (_, _) => await RefreshAsync()) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        SearchTextBox = new ToolStripTextBox { ToolTipText = "Search", Width = 180 };
        SearchTextBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await RefreshAsync();
            }
        };

        ToolbarStrip.Items.Add(AddButton);
        ToolbarStrip.Items.Add(EditButton);
        ToolbarStrip.Items.Add(DeleteButton);
        ToolbarStrip.Items.Add(new ToolStripSeparator());
        ToolbarStrip.Items.Add(RefreshButton);
        ToolbarStrip.Items.Add(new ToolStripSeparator());
        ToolbarStrip.Items.Add(new ToolStripLabel("Search:"));
        ToolbarStrip.Items.Add(SearchTextBox);

        GridContextMenu = new ContextMenuStrip();
        GridContextMenu.Items.Add(new ToolStripMenuItem("Edit", null, (_, _) => EditSelected()));
        GridContextMenu.Items.Add(new ToolStripMenuItem("Delete", null, async (_, _) => await DeleteSelectedAsync()));
        Grid.ContextMenuStrip = GridContextMenu;

        StatusBar = new StatusStrip();
        RecordCountLabel = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        StatusBar.Items.Add(RecordCountLabel);

        Controls.Add(Grid);
        Controls.Add(Navigator);
        Controls.Add(ToolbarStrip);
        Controls.Add(StatusBar);

        ClientSize = new Size(920, 580);
        StartPosition = FormStartPosition.CenterParent;

        Load += async (_, _) => await RefreshAsync();

        ResumeLayout(false);
        PerformLayout();
    }

    protected void InsertFilterComboBox(string labelText, ToolStripComboBox comboBox)
    {
        FilterComboBox = comboBox;
        comboBox.SelectedIndexChanged += async (_, _) => await RefreshAsync();
        var insertIndex = ToolbarStrip.Items.IndexOf(SearchTextBox);
        ToolbarStrip.Items.Insert(insertIndex, new ToolStripLabel(labelText));
        ToolbarStrip.Items.Insert(insertIndex + 1, comboBox);
        ToolbarStrip.Items.Insert(insertIndex + 2, new ToolStripSeparator());
    }

    protected abstract Task<List<TEntity>> LoadDataAsync(string? searchText);
    protected abstract void AddNew();
    protected abstract void EditEntity(TEntity entity);
    protected abstract Task DeleteEntityAsync(TEntity entity);
    protected virtual void ConfigureColumns()
    {
    }

    protected async Task RefreshAsync()
    {
        RefreshButton.Enabled = false;
        try
        {
            var data = await LoadDataAsync(SearchTextBox.Text);
            BindingSourceControl.DataSource = data;
            if (!_columnsConfigured)
            {
                ConfigureColumns();
                _columnsConfigured = true;
            }
            RecordCountLabel.Text = $"{data.Count} record(s)";
        }
        finally
        {
            RefreshButton.Enabled = true;
        }
    }

    protected async Task ReloadAsync() => await RefreshAsync();

    private void EditSelected()
    {
        if (BindingSourceControl.Current is TEntity entity)
        {
            EditEntity(entity);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (BindingSourceControl.Current is not TEntity entity)
        {
            return;
        }

        var confirm = MessageBox.Show(this, "Delete the selected record? This cannot be undone.", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await DeleteEntityAsync(entity);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not delete record: {ex.Message}", "Delete Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
