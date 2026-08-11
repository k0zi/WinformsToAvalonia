using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class SalesOrderDetailForm : Window
{
    public SalesOrderDetailForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.SalesOrderDetailFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.SalesOrderDetailFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.SalesOrderDetailFormViewModel)DataContext!;
}
