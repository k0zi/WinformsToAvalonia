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
}
