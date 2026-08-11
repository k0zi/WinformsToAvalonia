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

    private async void DashboardForm_Load(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
            userStatusLabel.Text = Session.CurrentUser is { } user
                ? $"Logged in as {user.DisplayName} ({user.Role?.Name ?? "—"})"
                : "Not logged in";
            clockTimer.Start();
            clockStatusLabel.Text = DateTime.Now.ToString("f");
            await ViewModel.RefreshCapacityAsync();
        }

    private void DashboardForm_FormClosing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
            notifyIcon.Visible = false;
        }

    private void DashboardForm_Resize(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
    {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.ShowBalloonTip(1000, "WarehouseApp", "Minimized to tray. Double-click the icon to restore.", ToolTipIcon.Info);
            }
        }
}
