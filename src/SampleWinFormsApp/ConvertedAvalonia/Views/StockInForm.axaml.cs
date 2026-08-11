using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class StockInForm : Window
{
    public StockInForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.StockInFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.StockInFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.StockInFormViewModel)DataContext!;
}
