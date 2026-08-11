using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class CategoriesForm : Window
{
    public CategoriesForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.CategoriesFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.CategoriesFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.CategoriesFormViewModel)DataContext!;
}
