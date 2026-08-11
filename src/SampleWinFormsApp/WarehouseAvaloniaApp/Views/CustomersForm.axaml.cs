using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class CustomersForm : Window
{
    public CustomersForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.CustomersFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.CustomersFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.CustomersFormViewModel)DataContext!;
}
