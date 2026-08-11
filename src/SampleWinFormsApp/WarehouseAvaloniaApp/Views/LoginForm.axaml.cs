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

    private async void loginButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
            errorProvider.Clear();
            statusLabel.Text = string.Empty;
    
            var username = usernameTextBox.Text.Trim();
            var password = passwordTextBox.Text;
            var valid = true;
    
            if (string.IsNullOrWhiteSpace(username))
            {
                errorProvider.SetError(usernameTextBox, "Username is required.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                errorProvider.SetError(passwordTextBox, "Password is required.");
                valid = false;
            }
            if (!valid)
            {
                return;
            }
    
            ViewModel.SetBusy(true);
            try
            {
                var user = await Task.Run(async () =>
                {
                    await Task.Delay(300);
                    using var ctx = Db.CreateContext();
                    var candidate = await ctx.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
                    if (candidate is null || !PasswordHasher.Verify(password, candidate.PasswordHash))
                    {
                        return null;
                    }
                    candidate.LastLoginAt = DateTime.UtcNow;
                    await ctx.SaveChangesAsync();
                    return candidate;
                });
    
                if (user is null)
                {
                    statusLabel.Text = "Invalid username or password.";
                    return;
                }
    
                Session.CurrentUser = user;
                Hide();
                await WarehouseAvaloniaApp.Common.Dialogs.ShowChildAsync<WarehouseAvaloniaApp.Views.DashboardForm, WarehouseAvaloniaApp.ViewModels.DashboardFormViewModel>();
                Session.CurrentUser = null;
                Close();
            }
            finally
            {
                ViewModel.SetBusy(false);
            }
        }
}
