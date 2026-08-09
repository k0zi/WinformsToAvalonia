using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Controls;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Forms;

public partial class SalesOrderDetailForm : DetailFormBase<SalesOrder>
{
    private sealed record NewLine(Product Product, int Quantity, decimal UnitPrice);

    private List<Customer> _customers = [];
    private List<Warehouse> _warehouses = [];
    private List<Product> _products = [];

    public SalesOrderDetailForm(SalesOrder? order = null) : base(order)
    {
        InitializeComponent();
        Text = IsNew ? "New Sales Order — WarehouseApp" : $"Sales Order — {order!.OrderNumber}";
    }

    protected override void LoadFromEntity()
    {
        using var ctx = Db.CreateContext();
        _customers = ctx.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
        _warehouses = ctx.Warehouses.OrderBy(w => w.Name).ToList();
        _products = ctx.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();

        customerComboBox.DataSource = _customers;
        customerComboBox.DisplayMember = nameof(Customer.Name);
        customerComboBox.ValueMember = nameof(Customer.Id);

        warehouseComboBox.DataSource = _warehouses;
        warehouseComboBox.DisplayMember = nameof(Warehouse.Name);
        warehouseComboBox.ValueMember = nameof(Warehouse.Id);

        productSearchBox.DataSource = _products;

        statusComboBox.DataSource = Enum.GetValues<SalesOrderStatus>();

        orderDatePicker.Value = IsNew ? DateTime.Today : Entity.OrderDate;
        requiredDatePicker.Value = Entity.RequiredDate ?? DateTime.Today.AddDays(5);
        notesTextBox.Text = Entity.Notes;
        satisfactionRatingControl.Value = Entity.SatisfactionRating ?? 0;

        if (IsNew)
        {
            orderNumberValueLabel.Text = "(assigned on save)";
            statusComboBox.SelectedItem = SalesOrderStatus.New;
        }
        else
        {
            orderNumberValueLabel.Text = Entity.OrderNumber;
            customerComboBox.SelectedValue = Entity.CustomerId;
            warehouseComboBox.SelectedValue = Entity.WarehouseId;
            statusComboBox.SelectedItem = Entity.Status;

            using var detailCtx = Db.CreateContext();
            var lines = detailCtx.SalesOrderLines.Include(l => l.Product).Where(l => l.SalesOrderId == Entity.Id).ToList();
            foreach (var line in lines)
            {
                AddLineRow(line.Product.Name, line.QuantityOrdered, line.UnitPrice, existingLine: line);
            }
        }

        UpdateStatusBadge();
        statusComboBox.SelectedIndexChanged += (_, _) => UpdateStatusBadge();
    }

    private void UpdateStatusBadge()
    {
        if (statusComboBox.SelectedItem is not SalesOrderStatus status)
        {
            return;
        }
        statusBadge.Text = status.ToString();
        statusBadge.BadgeStyle = status switch
        {
            SalesOrderStatus.Delivered => BadgeStyle.Success,
            SalesOrderStatus.Shipped => BadgeStyle.Info,
            SalesOrderStatus.Cancelled => BadgeStyle.Danger,
            SalesOrderStatus.Confirmed => BadgeStyle.Warning,
            _ => BadgeStyle.Neutral
        };
    }

    private void ProductSearchBox_SelectedItemChanged(object? sender, EventArgs e)
    {
        if (productSearchBox.SelectedItem is Product product)
        {
            unitPriceNumericUpDown.Value = product.UnitPrice;
        }
    }

    private void addLineButton_Click(object? sender, EventArgs e)
    {
        if (productSearchBox.SelectedItem is not Product product)
        {
            MessageBox.Show(this, "Search and select a product first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        AddLineRow(product.Name, (int)qtyNumericUpDown.Value, unitPriceNumericUpDown.Value, newLine: new NewLine(product, (int)qtyNumericUpDown.Value, unitPriceNumericUpDown.Value));
    }

    private void AddLineRow(string productName, int quantity, decimal unitPrice, SalesOrderLine? existingLine = null, NewLine? newLine = null)
    {
        var total = quantity * unitPrice;
        var rowIndex = linesGrid.Rows.Add(productName, quantity, unitPrice, total);
        linesGrid.Rows[rowIndex].Tag = (object?)existingLine ?? newLine;
    }

    protected override bool ValidateInput()
    {
        if (customerComboBox.SelectedItem is null)
        {
            Validation.SetError(customerComboBox, "Choose a customer.");
            return false;
        }
        if (warehouseComboBox.SelectedItem is null)
        {
            Validation.SetError(warehouseComboBox, "Choose a warehouse.");
            return false;
        }
        if (IsNew && linesGrid.Rows.Count == 0)
        {
            MessageBox.Show(this, "Add at least one line item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    protected override void SaveToEntity()
    {
        Entity.CustomerId = (int)customerComboBox.SelectedValue!;
        Entity.WarehouseId = (int)warehouseComboBox.SelectedValue!;
        Entity.OrderDate = orderDatePicker.Value;
        Entity.RequiredDate = requiredDatePicker.Value;
        Entity.Status = (SalesOrderStatus)statusComboBox.SelectedItem!;
        Entity.SatisfactionRating = satisfactionRatingControl.Value > 0 ? satisfactionRatingControl.Value : null;
        Entity.Notes = notesTextBox.Text.Trim();
        if (IsNew)
        {
            Entity.OrderNumber = $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}";
            Entity.CreatedByUserId = Session.CurrentUser?.Id ?? 0;
        }
    }

    protected override async Task PersistAsync()
    {
        using var ctx = Db.CreateContext();

        if (IsNew)
        {
            foreach (DataGridViewRow row in linesGrid.Rows)
            {
                if (row.Tag is NewLine newLine)
                {
                    Entity.Lines.Add(new SalesOrderLine { ProductId = newLine.Product.Id, QuantityOrdered = newLine.Quantity, UnitPrice = newLine.UnitPrice });
                }
            }
            ctx.SalesOrders.Add(Entity);
        }
        else
        {
            var tracked = await ctx.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == Entity.Id);
            tracked.CustomerId = Entity.CustomerId;
            tracked.WarehouseId = Entity.WarehouseId;
            tracked.OrderDate = Entity.OrderDate;
            tracked.RequiredDate = Entity.RequiredDate;
            tracked.Status = Entity.Status;
            tracked.SatisfactionRating = Entity.SatisfactionRating;
            tracked.Notes = Entity.Notes;

            foreach (DataGridViewRow row in linesGrid.Rows)
            {
                if (row.Tag is NewLine newLine)
                {
                    tracked.Lines.Add(new SalesOrderLine { ProductId = newLine.Product.Id, QuantityOrdered = newLine.Quantity, UnitPrice = newLine.UnitPrice });
                }
            }
        }

        await ctx.SaveChangesAsync();
    }
}
