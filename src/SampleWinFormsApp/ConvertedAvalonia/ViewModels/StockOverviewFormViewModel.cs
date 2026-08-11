using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Controls;
using WarehouseApp.Data.Models;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for StockOverviewForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class StockOverviewFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<StockRow> _rows = [];

    internal List<Warehouse> _warehouses = [];

    internal List<StockLevel> _allStockLevels = [];

    internal sealed class StockRow
        {
            public required string ProductName { get; init; }
            public required string WarehouseName { get; init; }
            public int OnHand { get; init; }
            public int Reserved { get; init; }
            public int ReorderLevel { get; init; }
        }

    internal async Task LoadStockAsync()
        {
            var (stockLevels, warehouses, totalCapacity) = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                var levels = ctx.StockLevels.Include(s => s.Product).Include(s => s.Warehouse).ToList();
                var whs = ctx.Warehouses.OrderBy(w => w.Name).ToList();
                var capacity = whs.Sum(w => (long)w.CapacityUnits);
                return (levels, whs, capacity);
            });
    
            _warehouses = warehouses;
            _allStockLevels = stockLevels;
            if (warehouseFilterComboBox.Items.Count == 0)
            {
                warehouseFilterComboBox.Items.Add("All Warehouses");
                foreach (var wh in warehouses)
                {
                    warehouseFilterComboBox.Items.Add(wh.Name);
                }
                warehouseFilterComboBox.SelectedIndex = 0;
                warehouseFilterComboBox.SelectedIndexChanged += (_, _) => ApplyFilter();
            }
    
            var totalOnHand = stockLevels.Sum(s => (long)s.QuantityOnHand);
            overallGauge.Value = totalCapacity == 0 ? 0 : Math.Min(100.0, totalOnHand * 100.0 / totalCapacity);
    
            ApplyFilter();
        }

    internal void ApplyFilter()
        {
            var filterName = warehouseFilterComboBox.SelectedIndex > 0 ? warehouseFilterComboBox.SelectedItem?.ToString() : null;
    
            var filtered = filterName is null
                ? _allStockLevels
                : _allStockLevels.Where(s => s.Warehouse.Name == filterName).ToList();
    
            _rows = filtered
                .OrderBy(s => s.Product.Name)
                .Select(s => new StockRow
                {
                    ProductName = s.Product.Name,
                    WarehouseName = s.Warehouse.Name,
                    OnHand = s.QuantityOnHand,
                    Reserved = s.QuantityReserved,
                    ReorderLevel = s.Product.ReorderLevel
                })
                .ToList();
    
            bindingSourceControl.DataSource = _rows;
            recordCountLabel.Text = $"{_rows.Count} stock record(s)";
        }

    internal void RefreshRowStatuses()
        {
            stockGrid.Refresh();
            StockGrid_SelectionChanged(this, EventArgs.Empty);
        }

    internal (BadgeStyle Style, string Text) ClassifyRow(StockRow row)
        {
            var thresholdQty = row.ReorderLevel * (lowStockTrackBar.Value / 100.0);
            if (row.OnHand <= thresholdQty)
            {
                return (BadgeStyle.Danger, "Low Stock");
            }
            if (row.ReorderLevel > 0 && row.OnHand > row.ReorderLevel * 4)
            {
                return (BadgeStyle.Info, "Overstock");
            }
            return (BadgeStyle.Success, "OK");
        }

    internal void StockGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (stockGrid.Rows[e.RowIndex].DataBoundItem is not StockRow row)
            {
                return;
            }
    
            var (style, _) = ClassifyRow(row);
            stockGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = style switch
            {
                BadgeStyle.Danger => Color.FromArgb(252, 224, 224),
                BadgeStyle.Info => Color.FromArgb(220, 235, 252),
                _ => Color.White
            };
        }

    internal void StockGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (bindingSourceControl.Current is not StockRow row)
            {
                selectedStatusBadge.Text = "Select a row";
                selectedStatusBadge.BadgeStyle = BadgeStyle.Neutral;
                return;
            }
    
            var (style, text) = ClassifyRow(row);
            selectedStatusBadge.Text = $"{row.ProductName}: {text}";
            selectedStatusBadge.BadgeStyle = style;
        }

}
