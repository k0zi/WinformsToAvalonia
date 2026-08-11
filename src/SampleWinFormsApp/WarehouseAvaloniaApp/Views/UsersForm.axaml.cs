using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class UsersForm : Window
{
    public UsersForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.UsersFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.UsersFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.UsersFormViewModel)DataContext!;
}
