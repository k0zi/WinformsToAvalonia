using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for StockInForm (auto-generated).
/// </summary>
public partial class StockInFormViewModel : ObservableObject
{
    [RelayCommand]
    private void addLineButtonClick()
    {
        // Original WinForms handler "addLineButton_Click", preserved for reference - review and adapt:
        // private void addLineButton_Click(object? sender, EventArgs e)
        //     {
        //         if (productComboBox.SelectedItem is not Product product || warehouseComboBox.SelectedItem is not Warehouse warehouse)
        //         {
        //             return;
        //         }
        //
        //         var quantity = (int)quantityStepper.Value;
        //         if (quantity <= 0)
        //         {
        //             MessageBox.Show(this, "Quantity must be greater than zero.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //             return;
        //         }
        //
        //         var line = new PendingLine(product.Id, product.Name, warehouse.Id, warehouse.Name, quantity);
        //         var rowIndex = linesGrid.Rows.Add(line.ProductName, line.WarehouseName, line.Quantity);
        //         linesGrid.Rows[rowIndex].Tag = line;
        //         statusLabel.Text = string.Empty;
        //     }
    }

}
