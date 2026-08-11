using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Data;
using WarehouseApp.Data.Models;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for UsersForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class UsersFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<User> _users = [];

    internal List<Role> _roles = [];

    internal User? _current;

    internal async Task LoadUsersAsync()
        {
            (_users, _roles) = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return (ctx.Users.Include(u => u.Role).OrderBy(u => u.Username).ToList(), ctx.Roles.OrderBy(r => r.Name).ToList());
            });
    
            roleComboBox.DataSource = _roles.Select(r => r.Name).ToList();
    
            bindingSourceControl.DataSource = _users;
            recordCountLabel.Text = $"{_users.Count} user(s)";
            ClearDetails();
        }

    internal void UsersGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (usersGrid.Rows[e.RowIndex].DataBoundItem is not User user)
            {
                return;
            }
            if (usersGrid.Columns[e.ColumnIndex].Name == "Role")
            {
                e.Value = user.Role?.Name;
                e.FormattingApplied = true;
            }
        }

    internal void UsersGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (bindingSourceControl.Current is not User user)
            {
                return;
            }
    
            _current = user;
            usernameTextBox.Text = user.Username;
            usernameTextBox.Enabled = false;
            displayNameTextBox.Text = user.DisplayName;
            passwordTextBox.Text = string.Empty;
            passwordTextBox.PlaceholderText = "(leave blank to keep current password)";
            roleComboBox.SelectedItem = user.Role?.Name;
            activeToggle.Checked = user.IsActive;
            UpdateAssignedPermissions();
        }

    internal void RoleComboBox_SelectedIndexChanged(object? sender, EventArgs e) => UpdateAssignedPermissions();

    internal void UpdateAssignedPermissions()
        {
            var role = _roles.FirstOrDefault(r => r.Name == roleComboBox.SelectedItem as string);
            var flags = role is null
                ? new[] { false, false, false, false }
                : new[] { role.CanManageInventory, role.CanManageOrders, role.CanManageUsers, role.CanViewReports };
    
            for (var i = 0; i < assignedPermissionsCheckedListBox.Items.Count && i < flags.Length; i++)
            {
                assignedPermissionsCheckedListBox.SetItemChecked(i, flags[i]);
            }
        }

    internal void NewUser()
        {
            _current = new User { IsActive = true };
            usersGrid.ClearSelection();
            ClearDetails(keepCurrent: true);
            usernameTextBox.Enabled = true;
            passwordTextBox.PlaceholderText = "Required for new users";
            usernameTextBox.Focus();
        }

    internal void ClearDetails(bool keepCurrent = false)
        {
            if (!keepCurrent)
            {
                _current = null;
            }
            usernameTextBox.Clear();
            usernameTextBox.Enabled = true;
            displayNameTextBox.Clear();
            passwordTextBox.Clear();
            passwordTextBox.PlaceholderText = string.Empty;
            if (roleComboBox.Items.Count > 0)
            {
                roleComboBox.SelectedIndex = 0;
            }
            activeToggle.Checked = true;
            UpdateAssignedPermissions();
        }

    internal async Task SaveUserAsync()
        {
            _current ??= new User();
            var isNew = _current.Id == 0;
    
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Username is required.","Validation",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Warning);
                return;
            }
            if (isNew && string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Password is required for a new user.","Validation",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Warning);
                return;
            }
            if (roleComboBox.SelectedItem is not string roleName)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Choose a role.","Validation",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Warning);
                return;
            }
    
            var role = _roles.First(r => r.Name == roleName);
    
            using var ctx = Db.CreateContext();
            if (isNew)
            {
                ctx.Users.Add(new User
                {
                    Username = usernameTextBox.Text.Trim(),
                    DisplayName = displayNameTextBox.Text.Trim(),
                    PasswordHash = PasswordHasher.Hash(passwordTextBox.Text),
                    RoleId = role.Id,
                    IsActive = activeToggle.Checked
                });
            }
            else
            {
                var tracked = await ctx.Users.FindAsync(_current.Id);
                if (tracked is not null)
                {
                    tracked.DisplayName = displayNameTextBox.Text.Trim();
                    tracked.RoleId = role.Id;
                    tracked.IsActive = activeToggle.Checked;
                    if (!string.IsNullOrWhiteSpace(passwordTextBox.Text))
                    {
                        tracked.PasswordHash = PasswordHasher.Hash(passwordTextBox.Text);
                    }
                }
            }
            await ctx.SaveChangesAsync();
    
            await LoadUsersAsync();
        }

    internal async Task DeleteUserAsync()
        {
            if (_current is null || _current.Id == 0)
            {
                return;
            }
    
            var confirm = MessageBox.Show(this, $"Delete user '{_current.Username}'?", "Confirm Delete",
                WarehouseAvaloniaApp.Common.MessageBoxButtons.YesNo, WarehouseAvaloniaApp.Common.MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (confirm != WarehouseAvaloniaApp.Common.DialogResult.Yes)
            {
                return;
            }
    
            try
            {
                using var ctx = Db.CreateContext();
                var tracked = await ctx.Users.FindAsync(_current.Id);
                if (tracked is not null)
                {
                    ctx.Users.Remove(tracked);
                    await ctx.SaveChangesAsync();
                }
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync($"Could not delete user: {ex.Message}","Delete Failed",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Error);
            }
        }

}
