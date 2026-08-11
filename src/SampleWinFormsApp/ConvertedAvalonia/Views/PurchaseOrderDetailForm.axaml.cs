using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class PurchaseOrderDetailForm : Window
{
    public PurchaseOrderDetailForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.PurchaseOrderDetailFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.PurchaseOrderDetailFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.PurchaseOrderDetailFormViewModel)DataContext!;
}
