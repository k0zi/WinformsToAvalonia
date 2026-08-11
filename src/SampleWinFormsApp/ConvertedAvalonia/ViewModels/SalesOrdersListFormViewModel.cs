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
/// ViewModel for SalesOrdersListForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class SalesOrdersListFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal async Task<List<SalesOrder>> LoadDataAsync(string? searchText)
        {
            var statusFilter = statusFilterComboBox.SelectedIndex > 0 ? statusFilterComboBox.SelectedItem?.ToString() : null;
    
            return await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                var query = ctx.SalesOrders.Include(o => o.Customer).AsQueryable();
    
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(o => o.OrderNumber.Contains(searchText) || o.Customer.Name.Contains(searchText));
                }
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    var status = Enum.Parse<SalesOrderStatus>(statusFilter);
                    query = query.Where(o => o.Status == status);
                }
    
                return query.OrderByDescending(o => o.OrderDate).ToList();
            });
        }

    internal void AddNew()
        {
            using var form = new SalesOrderDetailForm();
            if (form.ShowDialog(this) == ConvertedAvalonia.Common.DialogResult.OK)
            {
                _ = ReloadAsync();
            }
        }

    internal void EditEntity(SalesOrder entity)
        {
            using var form = new SalesOrderDetailForm(entity);
            if (form.ShowDialog(this) == ConvertedAvalonia.Common.DialogResult.OK)
            {
                _ = ReloadAsync();
            }
        }

    internal async Task DeleteEntityAsync(SalesOrder entity)
        {
            using var ctx = Db.CreateContext();
            var tracked = await ctx.SalesOrders.FindAsync(entity.Id);
            if (tracked is not null)
            {
                ctx.SalesOrders.Remove(tracked);
                await ctx.SaveChangesAsync();
            }
        }

    internal void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (Grid.Rows[e.RowIndex].DataBoundItem is not SalesOrder order)
            {
                return;
            }
    
            if (Grid.Columns[e.ColumnIndex].Name == "Customer")
            {
                e.Value = order.Customer?.Name;
                e.FormattingApplied = true;
            }
            else if (Grid.Columns[e.ColumnIndex].Name == "Status")
            {
                Grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = order.Status switch
                {
                    SalesOrderStatus.Delivered => Color.FromArgb(220, 245, 225),
                    SalesOrderStatus.Cancelled => Color.FromArgb(252, 224, 224),
                    SalesOrderStatus.Confirmed => Color.FromArgb(255, 244, 214),
                    _ => Color.White
                };
            }
        }

}
