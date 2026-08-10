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

    internal void UpdateStatusBadge()
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

    internal void AddLineRow(string productName, int quantity, decimal unitPrice, SalesOrderLine? existingLine = null, NewLine? newLine = null)
        {
            var total = quantity * unitPrice;
            var rowIndex = linesGrid.Rows.Add(productName, quantity, unitPrice, total);
            linesGrid.Rows[rowIndex].Tag = (object?)existingLine ?? newLine;
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

}
