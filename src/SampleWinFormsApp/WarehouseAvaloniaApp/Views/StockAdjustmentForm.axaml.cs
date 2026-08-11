using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class StockAdjustmentForm : Window
{
    public StockAdjustmentForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.StockAdjustmentFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.StockAdjustmentFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.StockAdjustmentFormViewModel)DataContext!;
}
