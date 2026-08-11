using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class ProductDetailForm : Window
{
    public ProductDetailForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.ProductDetailFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.ProductDetailFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.ProductDetailFormViewModel)DataContext!;
}
