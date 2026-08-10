using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class StockTransferForm : Window
{
    public StockTransferForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.StockTransferFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.StockTransferFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.StockTransferFormViewModel)DataContext!;
}
