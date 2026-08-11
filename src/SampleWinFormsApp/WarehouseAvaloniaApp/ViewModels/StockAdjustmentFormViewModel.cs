using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Data;
using WarehouseApp.Data.Models;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for StockAdjustmentForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class StockAdjustmentFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Warehouse> _warehouses = [];

    internal List<StockLevel> _stockLevels = [];

    internal async Task LoadWarehousesAsync()
        {
            _warehouses = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return ctx.Warehouses.OrderBy(w => w.Name).ToList();
            });
    
            warehouseComboBox.DataSource = _warehouses;
            warehouseComboBox.DisplayMember = nameof(Warehouse.Name);
            warehouseComboBox.ValueMember = nameof(Warehouse.Id);
        }

    internal async Task LoadItemsAsync()
        {
            if (warehouseComboBox.SelectedItem is not Warehouse warehouse)
            {
                return;
            }
    
            _stockLevels = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return ctx.StockLevels.Include(s => s.Product).Where(s => s.WarehouseId == warehouse.Id).OrderBy(s => s.Product.Name).ToList();
            });
    
            itemsCheckedListBox.Items.Clear();
            foreach (var stock in _stockLevels)
            {
                itemsCheckedListBox.Items.Add($"{stock.Product.Name} (current: {stock.QuantityOnHand})", false);
            }
            countGrid.Rows.Clear();
            statusLabel.Text = string.Empty;
        }

    internal async void StartCountSession()
        {
            countGrid.Rows.Clear();
            for (var i = 0; i < itemsCheckedListBox.Items.Count; i++)
            {
                if (!itemsCheckedListBox.GetItemChecked(i))
                {
                    continue;
                }
    
                var stock = _stockLevels[i];
                var rowIndex = countGrid.Rows.Add(stock.Product.Name, stock.QuantityOnHand, stock.QuantityOnHand);
                countGrid.Rows[rowIndex].Tag = stock;
            }
    
            if (countGrid.Rows.Count == 0)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Check at least one item to start a count session.","Nothing Selected",                WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Information);
            }
        }

    internal async Task PostAdjustmentAsync()
        {
            if (countGrid.Rows.Count == 0)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Start a count session first.","Nothing to Post",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Information);
                return;
            }
    
            var userId = Session.CurrentUser?.Id ?? 0;
            var reason = reasonRichTextBox.Text.Trim();
            postButton.Enabled = false;
            var posted = 0;
            try
            {
                using var ctx = Db.CreateContext();
                var service = new StockMovementService(ctx);
                foreach (DataGridViewRow row in countGrid.Rows)
                {
                    if (row.Tag is not StockLevel stock)
                    {
                        continue;
                    }
                    if (!int.TryParse(row.Cells["CountedQty"].Value?.ToString(), out var countedQty))
                    {
                        continue;
                    }
                    if (countedQty == stock.QuantityOnHand)
                    {
                        continue;
                    }
    
                    await service.PostAdjustmentAsync(stock.ProductId, stock.WarehouseId, countedQty, userId,
                        string.IsNullOrWhiteSpace(reason) ? "Physical count adjustment" : reason);
                    posted++;
                }
    
                statusLabel.Text = posted == 0 ? "No quantities changed — nothing to post." : $"Posted {posted} adjustment(s) successfully.";
                countGrid.Rows.Clear();
                itemsCheckedListBox.Items.Clear();
            }
            catch (Exception ex)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync($"Could not post adjustment: {ex.Message}","Post Failed",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Error);
            }
            finally
            {
                postButton.Enabled = true;
            }
        }

}
