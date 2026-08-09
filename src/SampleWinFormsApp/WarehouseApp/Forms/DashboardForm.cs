using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;

namespace WarehouseApp.Forms;

public partial class DashboardForm : Form
{
    public DashboardForm()
    {
        InitializeComponent();
        Load += DashboardForm_Load;
        FormClosing += DashboardForm_FormClosing;
        Resize += DashboardForm_Resize;
    }

    private async void DashboardForm_Load(object? sender, EventArgs e)
    {
        userStatusLabel.Text = Session.CurrentUser is { } user
            ? $"Logged in as {user.DisplayName} ({user.Role?.Name ?? "—"})"
            : "Not logged in";
        clockTimer.Start();
        clockStatusLabel.Text = DateTime.Now.ToString("f");
        await RefreshCapacityAsync();
    }

    private void DashboardForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
            notifyIcon.ShowBalloonTip(1000, "WarehouseApp", "Minimized to tray. Double-click the icon to restore.", ToolTipIcon.Info);
        }
    }

    private void DashboardForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        notifyIcon.Visible = false;
    }

    private void clockTimer_Tick(object? sender, EventArgs e)
    {
        clockStatusLabel.Text = DateTime.Now.ToString("f");
    }

    private async void refreshToolStripButton_Click(object? sender, EventArgs e)
    {
        await RefreshCapacityAsync();
    }

    private async Task RefreshCapacityAsync()
    {
        statusProgressBar.Visible = true;
        try
        {
            var (onHand, capacity) = await Task.Run(async () =>
            {
                using var ctx = Db.CreateContext();
                var totalOnHand = await ctx.StockLevels.SumAsync(s => (int?)s.QuantityOnHand) ?? 0;
                var totalCapacity = await ctx.Warehouses.SumAsync(w => (int?)w.CapacityUnits) ?? 1;
                return (totalOnHand, totalCapacity);
            });

            var percent = capacity == 0 ? 0 : Math.Min(100.0, onHand * 100.0 / capacity);
            capacityGauge.Value = percent;
        }
        finally
        {
            statusProgressBar.Visible = false;
        }
    }

    private void OpenForm(Form form)
    {
        form.StartPosition = FormStartPosition.CenterParent;
        form.ShowDialog(this);
        form.Dispose();
    }

    private void logoutMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void exitMenuItem_Click(object? sender, EventArgs e)
    {
        notifyIcon.Visible = false;
        Environment.Exit(0);
    }

    private void aboutMenuItem_Click(object? sender, EventArgs e)
    {
        using var settings = new SettingsForm();
        settings.ShowDialog(this);
    }
}
