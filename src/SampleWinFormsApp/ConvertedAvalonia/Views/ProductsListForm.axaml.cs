using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class ProductsListForm : Window
{
    public ProductsListForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.ProductsListFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.ProductsListFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.ProductsListFormViewModel)DataContext!;
}
