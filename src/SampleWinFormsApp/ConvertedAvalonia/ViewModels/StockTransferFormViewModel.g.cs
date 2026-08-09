using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for StockTransferForm (auto-generated).
/// </summary>
public partial class StockTransferFormViewModel : ObservableObject
{
    [RelayCommand]
    private void <inline lambda - manual review required>()
    {
        // TODO: Implement Click logic
    }

    [RelayCommand]
    private void <inline lambda - manual review required>()
    {
        // TODO: Implement Click logic
    }

    [RelayCommand]
    private void <inline lambda - manual review required>()
    {
        // TODO: Implement Click logic
    }

    [RelayCommand]
    private void addLineButtonClick()
    {
        // Original WinForms handler "addLineButton_Click", preserved for reference - review and adapt:
        // private void addLineButton_Click(object? sender, EventArgs e)
        //     {
        //         if (productComboBox.SelectedItem is not Product product
        //             || fromWarehouseComboBox.SelectedItem is not Warehouse fromWarehouse
        //             || toWarehouseComboBox.SelectedItem is not Warehouse toWarehouse)
        //         {
        //             return;
        //         }
        //
        //         if (fromWarehouse.Id == toWarehouse.Id)
        //         {
        //             MessageBox.Show(this, "Source and destination warehouses must differ.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //             return;
        //         }
        //
        //         var quantity = (int)quantityNumericUpDown.Value;
        //         var line = new PendingLine(product.Id, product.Name, fromWarehouse.Id, fromWarehouse.Name, toWarehouse.Id, toWarehouse.Name, quantity);
        //         var rowIndex = linesGrid.Rows.Add(line.ProductName, line.FromWarehouseName, line.ToWarehouseName, line.Quantity);
        //         linesGrid.Rows[rowIndex].Tag = line;
        //         statusLabel.Text = string.Empty;
        //     }
    }

}
