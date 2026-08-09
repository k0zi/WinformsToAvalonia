using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Forms;

public partial class PurchaseOrdersListForm : ListFormBase<PurchaseOrder>
{
    public PurchaseOrdersListForm()
    {
        InitializeComponent();
        statusFilterComboBox.Items.Add("All Statuses");
        foreach (var status in Enum.GetNames<PurchaseOrderStatus>())
        {
            statusFilterComboBox.Items.Add(status);
        }
        statusFilterComboBox.SelectedIndex = 0;
    }

    protected override async Task<List<PurchaseOrder>> LoadDataAsync(string? searchText)
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

    protected override void AddNew()
    {
        using var form = new PurchaseOrderDetailForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _ = ReloadAsync();
        }
    }

    protected override void EditEntity(PurchaseOrder entity)
    {
        using var form = new PurchaseOrderDetailForm(entity);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _ = ReloadAsync();
        }
    }

    protected override async Task DeleteEntityAsync(PurchaseOrder entity)
    {
        using var ctx = Db.CreateContext();
        var tracked = await ctx.PurchaseOrders.FindAsync(entity.Id);
        if (tracked is not null)
        {
            ctx.PurchaseOrders.Remove(tracked);
            await ctx.SaveChangesAsync();
        }
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
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
