using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Data;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for LoginForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class LoginFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal void SetBusy(bool busy)
        {
            loginProgressBar.Visible = busy;
            loadingSpinner.Spinning = busy;
            loginButton.Enabled = !busy;
            usernameTextBox.Enabled = !busy;
            passwordTextBox.Enabled = !busy;
        }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async void loginButtonClick()
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
    
            SetBusy(true);
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
                using var dashboard = new DashboardForm();
                dashboard.ShowDialog();
                Session.CurrentUser = null;
                Close();
            }
            finally
            {
                SetBusy(false);
            }
        }

}
