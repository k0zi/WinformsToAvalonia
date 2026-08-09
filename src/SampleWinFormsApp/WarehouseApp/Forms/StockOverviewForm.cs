using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Controls;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Forms;

public partial class StockOverviewForm : Form
{
    private sealed class StockRow
    {
        public required string ProductName { get; init; }
        public required string WarehouseName { get; init; }
        public int OnHand { get; init; }
        public int Reserved { get; init; }
        public int ReorderLevel { get; init; }
    }

    private List<StockRow> _rows = [];
    private List<Warehouse> _warehouses = [];
    private List<StockLevel> _allStockLevels = [];

    public StockOverviewForm()
    {
        InitializeComponent();
        Load += async (_, _) => await LoadStockAsync();
    }

    private async Task LoadStockAsync()
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

    private void ApplyFilter()
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

    private void RefreshRowStatuses()
    {
        stockGrid.Refresh();
        StockGrid_SelectionChanged(this, EventArgs.Empty);
    }

    private (BadgeStyle Style, string Text) ClassifyRow(StockRow row)
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

    private void StockGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
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

    private void StockGrid_SelectionChanged(object? sender, EventArgs e)
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
