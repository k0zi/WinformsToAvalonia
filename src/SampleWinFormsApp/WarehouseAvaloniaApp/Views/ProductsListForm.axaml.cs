using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class ProductsListForm : Window
{
    public ProductsListForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.ProductsListFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.ProductsListFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.ProductsListFormViewModel)DataContext!;
}
