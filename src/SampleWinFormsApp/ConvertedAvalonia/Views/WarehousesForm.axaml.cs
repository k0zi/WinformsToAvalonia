using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class WarehousesForm : Window
{
    public WarehousesForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.WarehousesFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.WarehousesFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.WarehousesFormViewModel)DataContext!;
}
