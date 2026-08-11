using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class LoginForm : Window
{
    public LoginForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.LoginFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.LoginFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.LoginFormViewModel)DataContext!;
}
