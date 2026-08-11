using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class StockInForm : Window
{
    public StockInForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.StockInFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.StockInFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.StockInFormViewModel)DataContext!;

    private async void addLineButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
            if (productComboBox.SelectedItem is not Product product || warehouseComboBox.SelectedItem is not Warehouse warehouse)
            {
                return;
            }
    
            var quantity = (int)quantityStepper.Value;
            if (quantity <= 0)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Quantity must be greater than zero.","Validation",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Warning);
                return;
            }
    
            var line = new PendingLine(product.Id, product.Name, warehouse.Id, warehouse.Name, quantity);
            var rowIndex = linesGrid.Rows.Add(line.ProductName, line.WarehouseName, line.Quantity);
            linesGrid.Rows[rowIndex].Tag = line;
            statusLabel.Text = string.Empty;
        }
}
