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
