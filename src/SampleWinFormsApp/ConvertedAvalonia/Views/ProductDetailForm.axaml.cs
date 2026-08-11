using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class ProductDetailForm : Window
{
    public ProductDetailForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.ProductDetailFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.ProductDetailFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.ProductDetailFormViewModel)DataContext!;
}
