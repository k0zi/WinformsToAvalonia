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
/// ViewModel for StockTransferForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class StockTransferFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Product> _products = [];

    internal List<Warehouse> _warehouses = [];

    internal sealed record PendingLine(int ProductId, string ProductName, int FromWarehouseId, string FromWarehouseName, int ToWarehouseId, string ToWarehouseName, int Quantity);

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

    internal void SwapWarehouses()
        {
            (fromWarehouseComboBox.SelectedValue, toWarehouseComboBox.SelectedValue) =
                (toWarehouseComboBox.SelectedValue, fromWarehouseComboBox.SelectedValue);
        }

    internal void RemoveSelectedLine()
        {
            if (linesGrid.CurrentRow is { } row)
            {
                LinesGrid.Remove(row);
            }
        }

    internal async Task PostTransferAsync()
        {
            if (LinesGrid.Count == 0)
            {
                await ConvertedAvalonia.Common.Dialogs.ShowAsync("Add at least one line before posting.","Nothing to Post",ConvertedAvalonia.Common.MessageBoxButtons.OK,ConvertedAvalonia.Common.MessageBoxIcon.Information);
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
                    await service.PostTransferAsync(line.ProductId, line.FromWarehouseId, line.ToWarehouseId, line.Quantity, userId, "Manual warehouse transfer");
                }
    
                Status = $"Posted {LinesGrid.Count} transfer(s) successfully.";
                LinesGrid.Clear();
            }
            catch (Exception ex)
            {
                await ConvertedAvalonia.Common.Dialogs.ShowAsync($"Could not post transfer: {ex.Message}","Post Failed",ConvertedAvalonia.Common.MessageBoxButtons.OK,ConvertedAvalonia.Common.MessageBoxIcon.Error);
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
        await this.PostTransferAsync();
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void swapButtonClickInlineHandler()
    {
        this.SwapWarehouses();
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async void addLineButtonClick()
    {
            if (productComboBox.SelectedItem is not Product product
                || fromWarehouseComboBox.SelectedItem is not Warehouse fromWarehouse
                || toWarehouseComboBox.SelectedItem is not Warehouse toWarehouse)
            {
                return;
            }
    
            if (fromWarehouse.Id == toWarehouse.Id)
            {
                await ConvertedAvalonia.Common.Dialogs.ShowAsync("Source and destination warehouses must differ.","Validation",ConvertedAvalonia.Common.MessageBoxButtons.OK,ConvertedAvalonia.Common.MessageBoxIcon.Warning);
                return;
            }
    
            var quantity = (int)quantityNumericUpDown.Value;
            var line = new PendingLine(product.Id, product.Name, fromWarehouse.Id, fromWarehouse.Name, toWarehouse.Id, toWarehouse.Name, quantity);
            var rowIndex = LinesGrid.Add(line.ProductName, line.FromWarehouseName, line.ToWarehouseName, line.Quantity);
            LinesGrid[rowIndex].Tag = line;
            Status = string.Empty;
        }

}
