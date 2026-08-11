using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class SuppliersForm : Window
{
    public SuppliersForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.SuppliersFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.SuppliersFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.SuppliersFormViewModel)DataContext!;
}
