using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class SalesOrdersListForm : Window
{
    public SalesOrdersListForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.SalesOrdersListFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.SalesOrdersListFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.SalesOrdersListFormViewModel)DataContext!;
}
