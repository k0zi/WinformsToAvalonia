using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class StockOutForm : Window
{
    public StockOutForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.StockOutFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.StockOutFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.StockOutFormViewModel)DataContext!;
}
