using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for LoginForm (auto-generated).
/// </summary>
public partial class LoginFormViewModel : ObservableObject
{
    [RelayCommand]
    private void loginButtonClick()
    {
        // Original WinForms handler "loginButton_Click", preserved for reference - review and adapt:
        // private async void loginButton_Click(object? sender, EventArgs e)
        //     {
        //         errorProvider.Clear();
        //         statusLabel.Text = string.Empty;
        //
        //         var username = usernameTextBox.Text.Trim();
        //         var password = passwordTextBox.Text;
        //         var valid = true;
        //
        //         if (string.IsNullOrWhiteSpace(username))
        //         {
        //             errorProvider.SetError(usernameTextBox, "Username is required.");
        //             valid = false;
        //         }
        //         if (string.IsNullOrWhiteSpace(password))
        //         {
        //             errorProvider.SetError(passwordTextBox, "Password is required.");
        //             valid = false;
        //         }
        //         if (!valid)
        //         {
        //             return;
        //         }
        //
        //         SetBusy(true);
        //         try
        //         {
        //             var user = await Task.Run(async () =>
        //             {
        //                 await Task.Delay(300);
        //                 using var ctx = Db.CreateContext();
        //                 var candidate = await ctx.Users
        //                     .Include(u => u.Role)
        //                     .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        //                 if (candidate is null || !PasswordHasher.Verify(password, candidate.PasswordHash))
        //                 {
        //                     return null;
        //                 }
        //                 candidate.LastLoginAt = DateTime.UtcNow;
        //                 await ctx.SaveChangesAsync();
        //                 return candidate;
        //             });
        //
        //             if (user is null)
        //             {
        //                 statusLabel.Text = "Invalid username or password.";
        //                 return;
        //             }
        //
        //             Session.CurrentUser = user;
        //             Hide();
        //             using var dashboard = new DashboardForm();
        //             dashboard.ShowDialog();
        //             Session.CurrentUser = null;
        //             Close();
        //         }
        //         finally
        //         {
        //             SetBusy(false);
        //         }
        //     }
    }

}
