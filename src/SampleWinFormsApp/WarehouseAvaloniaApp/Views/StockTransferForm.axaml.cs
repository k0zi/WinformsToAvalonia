using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class StockTransferForm : Window
{
    public StockTransferForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.StockTransferFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.StockTransferFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.StockTransferFormViewModel)DataContext!;

    private async void addLineButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
            if (productComboBox.SelectedItem is not Product product
                || fromWarehouseComboBox.SelectedItem is not Warehouse fromWarehouse
                || toWarehouseComboBox.SelectedItem is not Warehouse toWarehouse)
            {
                return;
            }
    
            if (fromWarehouse.Id == toWarehouse.Id)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Source and destination warehouses must differ.","Validation",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Warning);
                return;
            }
    
            var quantity = (int)quantityNumericUpDown.Value;
            var line = new PendingLine(product.Id, product.Name, fromWarehouse.Id, fromWarehouse.Name, toWarehouse.Id, toWarehouse.Name, quantity);
            var rowIndex = linesGrid.Rows.Add(line.ProductName, line.FromWarehouseName, line.ToWarehouseName, line.Quantity);
            linesGrid.Rows[rowIndex].Tag = line;
            statusLabel.Text = string.Empty;
        }
}
