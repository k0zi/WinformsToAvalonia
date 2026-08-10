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
/// ViewModel for PurchaseOrdersListForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class PurchaseOrdersListFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
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
