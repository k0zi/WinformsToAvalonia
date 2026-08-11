using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class ReportsForm : Window
{
    public ReportsForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.ReportsFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.ReportsFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.ReportsFormViewModel)DataContext!;
}
