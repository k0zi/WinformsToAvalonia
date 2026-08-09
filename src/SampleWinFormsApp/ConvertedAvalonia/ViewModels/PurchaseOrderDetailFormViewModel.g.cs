using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for PurchaseOrderDetailForm (auto-generated).
/// </summary>
public partial class PurchaseOrderDetailFormViewModel : ObservableObject
{
    [RelayCommand]
    private void addLineButtonClick()
    {
        // Original WinForms handler "addLineButton_Click", preserved for reference - review and adapt:
        // private void addLineButton_Click(object? sender, EventArgs e)
        //     {
        //         if (productSearchBox.SelectedItem is not Product product)
        //         {
        //             MessageBox.Show(this, "Search and select a product first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //             return;
        //         }
        //
        //         AddLineRow(product.Name, (int)qtyNumericUpDown.Value, unitPriceNumericUpDown.Value, newLine: new NewLine(product, (int)qtyNumericUpDown.Value, unitPriceNumericUpDown.Value));
        //     }
    }

    [RelayCommand]
    private void printButtonClick()
    {
        // Original WinForms handler "printButton_Click", preserved for reference - review and adapt:
        // private void printButton_Click(object? sender, EventArgs e)
        //     {
        //         if (printDialog.ShowDialog(this) == DialogResult.OK)
        //         {
        //             printDocument.Print();
        //         }
        //     }
    }

}
