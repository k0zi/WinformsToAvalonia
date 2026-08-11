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
}
