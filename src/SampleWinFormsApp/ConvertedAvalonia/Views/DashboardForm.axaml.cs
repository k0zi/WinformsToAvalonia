using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class DashboardForm : Window
{
    public DashboardForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.DashboardFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.DashboardFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.DashboardFormViewModel)DataContext!;
}
