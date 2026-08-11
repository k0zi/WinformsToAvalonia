using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class PurchaseOrdersListForm : Window
{
    public PurchaseOrdersListForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.PurchaseOrdersListFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.PurchaseOrdersListFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.PurchaseOrdersListFormViewModel)DataContext!;
}
