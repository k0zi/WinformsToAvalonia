using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class UsersForm : Window
{
    public UsersForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.UsersFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.UsersFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.UsersFormViewModel)DataContext!;
}
