using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class StockAdjustmentForm : Window
{
    public StockAdjustmentForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.StockAdjustmentFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.StockAdjustmentFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.StockAdjustmentFormViewModel)DataContext!;
}
