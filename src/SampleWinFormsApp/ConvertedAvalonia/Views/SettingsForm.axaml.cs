using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class SettingsForm : Window
{
    public SettingsForm()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.SettingsFormViewModel();
    }

    private ConvertedAvalonia.ViewModels.SettingsFormViewModel ViewModel => (ConvertedAvalonia.ViewModels.SettingsFormViewModel)DataContext!;
}
