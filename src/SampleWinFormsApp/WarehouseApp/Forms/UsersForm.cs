using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Data;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Forms;

public partial class UsersForm : Form
{
    private List<User> _users = [];
    private List<Role> _roles = [];
    private User? _current;

    public UsersForm()
    {
        InitializeComponent();
        Load += async (_, _) => await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
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

    private void UsersGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
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

    private void UsersGrid_SelectionChanged(object? sender, EventArgs e)
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

    private void RoleComboBox_SelectedIndexChanged(object? sender, EventArgs e) => UpdateAssignedPermissions();

    private void UpdateAssignedPermissions()
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

    private void NewUser()
    {
        _current = new User { IsActive = true };
        usersGrid.ClearSelection();
        ClearDetails(keepCurrent: true);
        usernameTextBox.Enabled = true;
        passwordTextBox.PlaceholderText = "Required for new users";
        usernameTextBox.Focus();
    }

    private void ClearDetails(bool keepCurrent = false)
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

    private async Task SaveUserAsync()
    {
        _current ??= new User();
        var isNew = _current.Id == 0;

        if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
        {
            MessageBox.Show(this, "Username is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (isNew && string.IsNullOrWhiteSpace(passwordTextBox.Text))
        {
            MessageBox.Show(this, "Password is required for a new user.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (roleComboBox.SelectedItem is not string roleName)
        {
            MessageBox.Show(this, "Choose a role.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    private async Task DeleteUserAsync()
    {
        if (_current is null || _current.Id == 0)
        {
            return;
        }

        var confirm = MessageBox.Show(this, $"Delete user '{_current.Username}'?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes)
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
            MessageBox.Show(this, $"Could not delete user: {ex.Message}", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
