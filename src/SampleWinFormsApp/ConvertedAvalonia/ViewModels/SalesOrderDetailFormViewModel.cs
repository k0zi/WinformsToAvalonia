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
/// ViewModel for SalesOrderDetailForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class SalesOrderDetailFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Customer> _customers = [];

    internal List<Warehouse> _warehouses = [];

    internal List<Product> _products = [];

    internal sealed record NewLine(Product Product, int Quantity, decimal UnitPrice);

    internal void LoadFromEntity()
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
    
            OrderDatePicker = IsNew ? DateTime.Today : Entity.OrderDate;
            RequiredDatePicker = Entity.RequiredDate ?? DateTime.Today.AddDays(5);
            Notes = Entity.Notes;
            SatisfactionRatingControl = Entity.SatisfactionRating ?? 0;
    
            if (IsNew)
            {
                orderNumberValueLabel.Text = "(assigned on save)";
                Status = SalesOrderStatus.New;
            }
            else
            {
                orderNumberValueLabel.Text = Entity.OrderNumber;
                Customer = Entity.CustomerId;
                Warehouse = Entity.WarehouseId;
                Status = Entity.Status;
    
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

    internal void UpdateStatusBadge()
        {
            if (Status is not SalesOrderStatus status)
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

    internal void AddLineRow(string productName, int quantity, decimal unitPrice, SalesOrderLine? existingLine = null, NewLine? newLine = null)
        {
            var total = quantity * unitPrice;
            var rowIndex = LinesGrid.Add(productName, quantity, unitPrice, total);
            LinesGrid[rowIndex].Tag = (object?)existingLine ?? newLine;
        }

    internal bool ValidateInput()
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
            if (IsNew && LinesGrid.Count == 0)
            {
                MessageBox.Show(this, "Add at least one line item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

    internal void SaveToEntity()
        {
            Entity.CustomerId = (int)Customer!;
            Entity.WarehouseId = (int)Warehouse!;
            Entity.OrderDate = OrderDatePicker;
            Entity.RequiredDate = RequiredDatePicker;
            Entity.Status = (SalesOrderStatus)Status!;
            Entity.SatisfactionRating = SatisfactionRatingControl > 0 ? SatisfactionRatingControl : null;
            Entity.Notes = Notes.Trim();
            if (IsNew)
            {
                Entity.OrderNumber = $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}";
                Entity.CreatedByUserId = Session.CurrentUser?.Id ?? 0;
            }
        }

    internal async Task PersistAsync()
        {
            using var ctx = Db.CreateContext();
    
            if (IsNew)
            {
                foreach (DataGridViewRow row in LinesGrid)
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
    
                foreach (DataGridViewRow row in LinesGrid)
                {
                    if (row.Tag is NewLine newLine)
                    {
                        tracked.Lines.Add(new SalesOrderLine { ProductId = newLine.Product.Id, QuantityOrdered = newLine.Quantity, UnitPrice = newLine.UnitPrice });
                    }
                }
            }
    
            await ctx.SaveChangesAsync();
        }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async void addLineButtonClick()
    {
            if (ProductSearchBox is not Product product)
            {
                await ConvertedAvalonia.Common.Dialogs.ShowAsync("Search and select a product first.","Validation",ConvertedAvalonia.Common.MessageBoxButtons.OK,ConvertedAvalonia.Common.MessageBoxIcon.Warning);
                return;
            }
    
            AddLineRow(product.Name, (int)qtyNumericUpDown.Value, UnitPrice, newLine: new NewLine(product, (int)qtyNumericUpDown.Value, UnitPrice));
        }

}
