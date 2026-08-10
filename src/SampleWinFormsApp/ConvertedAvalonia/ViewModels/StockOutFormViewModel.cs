using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WarehouseApp.Common;
using WarehouseApp.Data.Data;
using WarehouseApp.Data.Models;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for StockOutForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class StockOutFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Product> _products = [];

    internal List<Warehouse> _warehouses = [];

    internal async Task LoadLookupsAsync()
        {
            (_products, _warehouses) = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return (ctx.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList(), ctx.Warehouses.OrderBy(w => w.Name).ToList());
            });
    
            productComboBox.DataSource = _products;
            productComboBox.DisplayMember = nameof(Product.Name);
            productComboBox.ValueMember = nameof(Product.Id);
    
            warehouseComboBox.DataSource = _warehouses;
            warehouseComboBox.DisplayMember = nameof(Warehouse.Name);
            warehouseComboBox.ValueMember = nameof(Warehouse.Id);
        }

    internal void RemoveSelectedLine()
        {
            if (linesGrid.CurrentRow is { } row)
            {
                linesGrid.Rows.Remove(row);
            }
        }

    internal async Task PostIssueAsync()
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
                    await service.PostGoodsIssueAsync(line.ProductId, line.WarehouseId, line.Quantity, userId, notes: "Manual goods issue");
                }
    
                statusLabel.Text = $"Posted {linesGrid.Rows.Count} line(s) successfully.";
                linesGrid.Rows.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not post issue: {ex.Message}", "Post Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                postButton.Enabled = true;
            }
        }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void addLineButtonClick()
    {
            if (productComboBox.SelectedItem is not Product product || warehouseComboBox.SelectedItem is not Warehouse warehouse)
            {
                return;
            }
    
            var quantity = (int)quantityStepper.Value;
            if (quantity <= 0)
            {
                MessageBox.Show(this, "Quantity must be greater than zero.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
    
            var line = new PendingLine(product.Id, product.Name, warehouse.Id, warehouse.Name, quantity);
            var rowIndex = linesGrid.Rows.Add(line.ProductName, line.WarehouseName, line.Quantity);
            linesGrid.Rows[rowIndex].Tag = line;
            statusLabel.Text = string.Empty;
        }

}
