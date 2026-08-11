using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class LoginForm : Window
{
    public LoginForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.LoginFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.LoginFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.LoginFormViewModel)DataContext!;
}
