using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class SalesOrdersListForm : Window
{
    public SalesOrdersListForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.SalesOrdersListFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.SalesOrdersListFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.SalesOrdersListFormViewModel)DataContext!;
}
