using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class PurchaseOrderDetailForm : Window
{
    public PurchaseOrderDetailForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.PurchaseOrderDetailFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.PurchaseOrderDetailFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.PurchaseOrderDetailFormViewModel)DataContext!;

    private async void addLineButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
            if (productSearchBox.SelectedItem is not Product product)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Search and select a product first.","Validation",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Warning);
                return;
            }
    
            ViewModel.AddLineRow(product.Name, (int)qtyNumericUpDown.Value, unitPriceNumericUpDown.Value, newLine: new NewLine(product, (int)qtyNumericUpDown.Value, unitPriceNumericUpDown.Value));
        }
}
