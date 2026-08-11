using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WarehouseApp.Common;
using WarehouseApp.Data.Data;
using WarehouseApp.Data.Models;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for StockInForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class StockInFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Product> _products = [];

    internal List<Warehouse> _warehouses = [];

    internal sealed record PendingLine(int ProductId, string ProductName, int WarehouseId, string WarehouseName, int Quantity);

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
                LinesGrid.Remove(row);
            }
        }

    internal async Task PostReceiptAsync()
        {
            if (LinesGrid.Count == 0)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Add at least one line before posting.","Nothing to Post",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Information);
                return;
            }
    
            var userId = Session.CurrentUser?.Id ?? 0;
            postButton.Enabled = false;
            try
            {
                using var ctx = Db.CreateContext();
                var service = new StockMovementService(ctx);
                foreach (DataGridViewRow row in LinesGrid)
                {
                    if (row.Tag is not PendingLine line)
                    {
                        continue;
                    }
                    await service.PostGoodsReceiptAsync(line.ProductId, line.WarehouseId, line.Quantity, userId,
                        notes: $"Manual receipt on {receiptDatePicker.Value:d}");
                }
    
                Status = $"Posted {LinesGrid.Count} line(s) successfully.";
                LinesGrid.Clear();
            }
            catch (Exception ex)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync($"Could not post receipt: {ex.Message}","Post Failed",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Error);
            }
            finally
            {
                postButton.Enabled = true;
            }
        }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void removeLineButtonClickInlineHandler()
    {
        this.RemoveSelectedLine();
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async void postButtonClickInlineHandler()
    {
        await this.PostReceiptAsync();
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async void addLineButtonClick()
    {
            if (productComboBox.SelectedItem is not Product product || warehouseComboBox.SelectedItem is not Warehouse warehouse)
            {
                return;
            }
    
            var quantity = (int)quantityStepper.Value;
            if (quantity <= 0)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Quantity must be greater than zero.","Validation",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Warning);
                return;
            }
    
            var line = new PendingLine(product.Id, product.Name, warehouse.Id, warehouse.Name, quantity);
            var rowIndex = LinesGrid.Add(line.ProductName, line.WarehouseName, line.Quantity);
            LinesGrid[rowIndex].Tag = line;
            Status = string.Empty;
        }

}
