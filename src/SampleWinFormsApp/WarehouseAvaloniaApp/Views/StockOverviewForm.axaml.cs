using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class StockOverviewForm : Window
{
    public StockOverviewForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.StockOverviewFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.StockOverviewFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.StockOverviewFormViewModel)DataContext!;
}
