using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for PurchaseOrdersListForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class PurchaseOrdersListFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal async Task<List<PurchaseOrder>> LoadDataAsync(string? searchText)
        {
            var statusFilter = statusFilterComboBox.SelectedIndex > 0 ? statusFilterComboBox.SelectedItem?.ToString() : null;
    
            return await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                var query = ctx.PurchaseOrders.Include(p => p.Supplier).AsQueryable();
    
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(p => p.OrderNumber.Contains(searchText) || p.Supplier.Name.Contains(searchText));
                }
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    var status = Enum.Parse<PurchaseOrderStatus>(statusFilter);
                    query = query.Where(p => p.Status == status);
                }
    
                return query.OrderByDescending(p => p.OrderDate).ToList();
            });
        }

    internal void AddNew()
        {
            using var form = new PurchaseOrderDetailForm();
            if (form.ShowDialog(this) == ConvertedAvalonia.Common.DialogResult.OK)
            {
                _ = ReloadAsync();
            }
        }

    internal void EditEntity(PurchaseOrder entity)
        {
            using var form = new PurchaseOrderDetailForm(entity);
            if (form.ShowDialog(this) == ConvertedAvalonia.Common.DialogResult.OK)
            {
                _ = ReloadAsync();
            }
        }

    internal async Task DeleteEntityAsync(PurchaseOrder entity)
        {
            using var ctx = Db.CreateContext();
            var tracked = await ctx.PurchaseOrders.FindAsync(entity.Id);
            if (tracked is not null)
            {
                ctx.PurchaseOrders.Remove(tracked);
                await ctx.SaveChangesAsync();
            }
        }

    internal void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (Grid.Rows[e.RowIndex].DataBoundItem is not PurchaseOrder order)
            {
                return;
            }
    
            if (Grid.Columns[e.ColumnIndex].Name == "Supplier")
            {
                e.Value = order.Supplier?.Name;
                e.FormattingApplied = true;
            }
        }

}
