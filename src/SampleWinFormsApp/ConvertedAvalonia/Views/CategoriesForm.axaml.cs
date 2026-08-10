using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class CategoriesForm : Window
{
    public CategoriesForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.CategoriesFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.CategoriesFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.CategoriesFormViewModel)DataContext!;
}
