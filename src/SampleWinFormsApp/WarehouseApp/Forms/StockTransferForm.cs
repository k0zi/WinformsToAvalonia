using WarehouseApp.Common;
using WarehouseApp.Data.Data;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Forms;

public partial class StockTransferForm : Form
{
    private sealed record PendingLine(int ProductId, string ProductName, int FromWarehouseId, string FromWarehouseName, int ToWarehouseId, string ToWarehouseName, int Quantity);

    private List<Product> _products = [];
    private List<Warehouse> _warehouses = [];

    public StockTransferForm()
    {
        InitializeComponent();
        Load += async (_, _) => await LoadLookupsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        (_products, _warehouses) = await Task.Run(() =>
        {
            using var ctx = Db.CreateContext();
            return (ctx.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList(), ctx.Warehouses.OrderBy(w => w.Name).ToList());
        });

        productComboBox.DataSource = _products;
        productComboBox.DisplayMember = nameof(Product.Name);
        productComboBox.ValueMember = nameof(Product.Id);

        fromWarehouseComboBox.DataSource = _warehouses;
        fromWarehouseComboBox.DisplayMember = nameof(Warehouse.Name);
        fromWarehouseComboBox.ValueMember = nameof(Warehouse.Id);

        toWarehouseComboBox.DataSource = _warehouses.ToList();
        toWarehouseComboBox.DisplayMember = nameof(Warehouse.Name);
        toWarehouseComboBox.ValueMember = nameof(Warehouse.Id);
        if (_warehouses.Count > 1)
        {
            toWarehouseComboBox.SelectedIndex = 1;
        }
    }

    private void SwapWarehouses()
    {
        (fromWarehouseComboBox.SelectedValue, toWarehouseComboBox.SelectedValue) =
            (toWarehouseComboBox.SelectedValue, fromWarehouseComboBox.SelectedValue);
    }

    private void addLineButton_Click(object? sender, EventArgs e)
    {
        if (productComboBox.SelectedItem is not Product product
            || fromWarehouseComboBox.SelectedItem is not Warehouse fromWarehouse
            || toWarehouseComboBox.SelectedItem is not Warehouse toWarehouse)
        {
            return;
        }

        if (fromWarehouse.Id == toWarehouse.Id)
        {
            MessageBox.Show(this, "Source and destination warehouses must differ.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var quantity = (int)quantityNumericUpDown.Value;
        var line = new PendingLine(product.Id, product.Name, fromWarehouse.Id, fromWarehouse.Name, toWarehouse.Id, toWarehouse.Name, quantity);
        var rowIndex = linesGrid.Rows.Add(line.ProductName, line.FromWarehouseName, line.ToWarehouseName, line.Quantity);
        linesGrid.Rows[rowIndex].Tag = line;
        statusLabel.Text = string.Empty;
    }

    private void RemoveSelectedLine()
    {
        if (linesGrid.CurrentRow is { } row)
        {
            linesGrid.Rows.Remove(row);
        }
    }

    private async Task PostTransferAsync()
    {
        if (linesGrid.Rows.Count == 0)
        {
            MessageBox.Show(this, "Add at least one line before posting.", "Nothing to Post", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var userId = Session.CurrentUser?.Id ?? 0;
        postButton.Enabled = false;
        try
        {
            using var ctx = Db.CreateContext();
            var service = new StockMovementService(ctx);
            foreach (DataGridViewRow row in linesGrid.Rows)
            {
                if (row.Tag is not PendingLine line)
                {
                    continue;
                }
                await service.PostTransferAsync(line.ProductId, line.FromWarehouseId, line.ToWarehouseId, line.Quantity, userId, "Manual warehouse transfer");
            }

            statusLabel.Text = $"Posted {linesGrid.Rows.Count} transfer(s) successfully.";
            linesGrid.Rows.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not post transfer: {ex.Message}", "Post Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            postButton.Enabled = true;
        }
    }
}
