using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for DashboardForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class DashboardFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal async void DashboardForm_Load(object? sender, EventArgs e)
        {
            userStatusLabel.Text = Session.CurrentUser is { } user
                ? $"Logged in as {user.DisplayName} ({user.Role?.Name ?? "—"})"
                : "Not logged in";
            clockTimer.Start();
            clockStatusLabel.Text = DateTime.Now.ToString("f");
            await RefreshCapacityAsync();
        }

    internal void DashboardForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.ShowBalloonTip(1000, "WarehouseApp", "Minimized to tray. Double-click the icon to restore.", ToolTipIcon.Info);
            }
        }

    internal void DashboardForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            notifyIcon.Visible = false;
        }

    internal void clockTimer_Tick(object? sender, EventArgs e)
        {
            clockStatusLabel.Text = DateTime.Now.ToString("f");
        }

    internal async void refreshToolStripButton_Click(object? sender, EventArgs e)
        {
            await RefreshCapacityAsync();
        }

    internal async Task RefreshCapacityAsync()
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

    internal void OpenForm(Form form)
        {
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog(this);
            form.Dispose();
        }

    internal void logoutMenuItem_Click(object? sender, EventArgs e)
        {
            Close();
        }

    internal void exitMenuItem_Click(object? sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Environment.Exit(0);
        }

    internal void aboutMenuItem_Click(object? sender, EventArgs e)
        {
            using var settings = new SettingsForm();
            settings.ShowDialog(this);
        }

}
