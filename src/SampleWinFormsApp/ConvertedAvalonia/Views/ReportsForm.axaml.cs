using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class ReportsForm : Window
{
    public ReportsForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.ReportsFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.ReportsFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.ReportsFormViewModel)DataContext!;
}
