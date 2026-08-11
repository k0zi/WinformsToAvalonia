using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class PurchaseOrdersListForm : Window
{
    public PurchaseOrdersListForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.PurchaseOrdersListFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.PurchaseOrdersListFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.PurchaseOrdersListFormViewModel)DataContext!;
}
