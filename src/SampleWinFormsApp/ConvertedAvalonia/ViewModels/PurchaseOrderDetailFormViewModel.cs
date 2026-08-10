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
/// ViewModel for PurchaseOrderDetailForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class PurchaseOrderDetailFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Supplier> _suppliers = [];

    internal List<Product> _products = [];

    internal void UpdateStatusBadge()
        {
            if (statusComboBox.SelectedItem is not PurchaseOrderStatus status)
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
            var rowIndex = linesGrid.Rows.Add(productName, quantity, unitPrice, total);
            linesGrid.Rows[rowIndex].Tag = (object?)existingLine ?? newLine;
        }

    internal void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
        {
            var g = e.Graphics!;
            using var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold);
            using var bodyFont = new Font("Segoe UI", 10f);
            var y = 40f;
    
            g.DrawString($"Purchase Order {orderNumberValueLabel.Text}", titleFont, Brushes.Black, 40, y);
            y += 30;
            g.DrawString($"Supplier: {supplierComboBox.Text}", bodyFont, Brushes.Black, 40, y);
            y += 20;
            g.DrawString($"Order Date: {orderDatePicker.Value:d}    Expected: {expectedDatePicker.Value:d}", bodyFont, Brushes.Black, 40, y);
            y += 30;
    
            foreach (DataGridViewRow row in linesGrid.Rows)
            {
                var line = $"{row.Cells["Product"].Value}   x{row.Cells["Qty"].Value}   @ {row.Cells["Price"].Value:C2}   = {row.Cells["Total"].Value:C2}";
                g.DrawString(line, bodyFont, Brushes.Black, 40, y);
                y += 20;
            }
        }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void addLineButtonClick()
    {
            if (productSearchBox.SelectedItem is not Product product)
            {
                MessageBox.Show(this, "Search and select a product first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
    
            AddLineRow(product.Name, (int)qtyNumericUpDown.Value, unitPriceNumericUpDown.Value, newLine: new NewLine(product, (int)qtyNumericUpDown.Value, unitPriceNumericUpDown.Value));
        }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void printButtonClick()
    {
            if (printDialog.ShowDialog(this) == DialogResult.OK)
            {
                printDocument.Print();
            }
        }

}
