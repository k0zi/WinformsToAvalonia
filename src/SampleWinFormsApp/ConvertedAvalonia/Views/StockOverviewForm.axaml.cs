using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class StockOverviewForm : Window
{
    public StockOverviewForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.StockOverviewFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.StockOverviewFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.StockOverviewFormViewModel)DataContext!;
}
