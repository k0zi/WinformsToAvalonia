using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class SalesOrderDetailForm : Window
{
    public SalesOrderDetailForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.SalesOrderDetailFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.SalesOrderDetailFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.SalesOrderDetailFormViewModel)DataContext!;
}
