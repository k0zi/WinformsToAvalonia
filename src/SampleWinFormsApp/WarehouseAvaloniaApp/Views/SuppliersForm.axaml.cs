using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class SuppliersForm : Window
{
    public SuppliersForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.SuppliersFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.SuppliersFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.SuppliersFormViewModel)DataContext!;
}
