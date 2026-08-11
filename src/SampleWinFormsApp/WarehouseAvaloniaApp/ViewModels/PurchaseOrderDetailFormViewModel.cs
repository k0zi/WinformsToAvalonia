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

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for PurchaseOrderDetailForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class PurchaseOrderDetailFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Supplier> _suppliers = [];

    internal List<Product> _products = [];

    internal sealed record NewLine(Product Product, int Quantity, decimal UnitPrice);

    internal void LoadFromEntity()
        {
            using var ctx = Db.CreateContext();
            _suppliers = ctx.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
            _products = ctx.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
    
            supplierComboBox.DataSource = _suppliers;
            supplierComboBox.DisplayMember = nameof(Supplier.Name);
            supplierComboBox.ValueMember = nameof(Supplier.Id);
    
            productSearchBox.DataSource = _products;
    
            statusComboBox.DataSource = Enum.GetValues<PurchaseOrderStatus>();
    
            OrderDatePicker = IsNew ? DateTime.Today : Entity.OrderDate;
            ExpectedDatePicker = Entity.ExpectedDate ?? DateTime.Today.AddDays(7);
            Notes = Entity.Notes;
    
            if (IsNew)
            {
                OrderNumberValue = "(assigned on save)";
                Status = PurchaseOrderStatus.Draft;
            }
            else
            {
                OrderNumberValue = Entity.OrderNumber;
                Supplier = Entity.SupplierId;
                Status = Entity.Status;
    
                using var detailCtx = Db.CreateContext();
                var lines = detailCtx.PurchaseOrderLines.Include(l => l.Product).Where(l => l.PurchaseOrderId == Entity.Id).ToList();
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
            if (Status is not PurchaseOrderStatus status)
            {
                return;
            }
            statusBadge.Text = status.ToString();
            statusBadge.BadgeStyle = status switch
            {
                PurchaseOrderStatus.Received => BadgeStyle.Success,
                PurchaseOrderStatus.PartiallyReceived => BadgeStyle.Warning,
                PurchaseOrderStatus.Cancelled => BadgeStyle.Danger,
                PurchaseOrderStatus.Sent => BadgeStyle.Info,
                _ => BadgeStyle.Neutral
            };
        }

    internal void AddLineRow(string productName, int quantity, decimal unitPrice, PurchaseOrderLine? existingLine = null, NewLine? newLine = null)
        {
            var total = quantity * unitPrice;
            var rowIndex = LinesGrid.Add(productName, quantity, unitPrice, total);
            LinesGrid[rowIndex].Tag = (object?)existingLine ?? newLine;
        }

    internal bool ValidateInput()
        {
            if (supplierComboBox.SelectedItem is null)
            {
                Validation.SetError(supplierComboBox, "Choose a supplier.");
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
            Entity.SupplierId = (int)Supplier!;
            Entity.OrderDate = OrderDatePicker;
            Entity.ExpectedDate = ExpectedDatePicker;
            Entity.Status = (PurchaseOrderStatus)Status!;
            Entity.Notes = Notes.Trim();
            if (IsNew)
            {
                Entity.OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}";
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
                        Entity.Lines.Add(new PurchaseOrderLine { ProductId = newLine.Product.Id, QuantityOrdered = newLine.Quantity, UnitPrice = newLine.UnitPrice });
                    }
                }
                ctx.PurchaseOrders.Add(Entity);
            }
            else
            {
                var tracked = await ctx.PurchaseOrders.Include(p => p.Lines).FirstAsync(p => p.Id == Entity.Id);
                tracked.SupplierId = Entity.SupplierId;
                tracked.OrderDate = Entity.OrderDate;
                tracked.ExpectedDate = Entity.ExpectedDate;
                tracked.Status = Entity.Status;
                tracked.Notes = Entity.Notes;
    
                foreach (DataGridViewRow row in LinesGrid)
                {
                    if (row.Tag is NewLine newLine)
                    {
                        tracked.Lines.Add(new PurchaseOrderLine { ProductId = newLine.Product.Id, QuantityOrdered = newLine.Quantity, UnitPrice = newLine.UnitPrice });
                    }
                }
            }
    
            await ctx.SaveChangesAsync();
        }

    internal void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
        {
            var g = e.Graphics!;
            using var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold);
            using var bodyFont = new Font("Segoe UI", 10f);
            var y = 40f;
    
            g.DrawString($"Purchase Order {OrderNumberValue}", titleFont, Brushes.Black, 40, y);
            y += 30;
            g.DrawString($"Supplier: {supplierComboBox.Text}", bodyFont, Brushes.Black, 40, y);
            y += 20;
            g.DrawString($"Order Date: {OrderDatePicker:d}    Expected: {ExpectedDatePicker:d}", bodyFont, Brushes.Black, 40, y);
            y += 30;
    
            foreach (DataGridViewRow row in LinesGrid)
            {
                var line = $"{row.Cells["Product"].Value}   x{row.Cells["Qty"].Value}   @ {row.Cells["Price"].Value:C2}   = {row.Cells["Total"].Value:C2}";
                g.DrawString(line, bodyFont, Brushes.Black, 40, y);
                y += 20;
            }
        }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void printButtonClick()
    {
            if (printDialog.ShowDialog(this) == WarehouseAvaloniaApp.Common.DialogResult.OK)
            {
                printDocument.Print();
            }
        }

}
