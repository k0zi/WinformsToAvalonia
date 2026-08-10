using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class CustomersForm : Window
{
    public CustomersForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.CustomersFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.CustomersFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.CustomersFormViewModel)DataContext!;
}
