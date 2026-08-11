using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class WarehousesForm : Window
{
    public WarehousesForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.WarehousesFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.WarehousesFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.WarehousesFormViewModel)DataContext!;
}
