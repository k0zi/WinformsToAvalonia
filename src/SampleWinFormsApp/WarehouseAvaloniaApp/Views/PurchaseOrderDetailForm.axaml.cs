using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class PurchaseOrderDetailForm : Window
{
    public PurchaseOrderDetailForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.PurchaseOrderDetailFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.PurchaseOrderDetailFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.PurchaseOrderDetailFormViewModel)DataContext!;
}
