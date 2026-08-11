using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class StockOutForm : Window
{
    public StockOutForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.StockOutFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.StockOutFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.StockOutFormViewModel)DataContext!;
}
