using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class SettingsForm : Window
{
    public SettingsForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.SettingsFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.SettingsFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.SettingsFormViewModel)DataContext!;
}
