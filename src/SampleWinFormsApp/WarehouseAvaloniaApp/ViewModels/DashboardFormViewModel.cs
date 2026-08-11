using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for DashboardForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class DashboardFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
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

    internal async void aboutMenuItem_Click(object? sender, EventArgs e)
        {
            await WarehouseAvaloniaApp.Common.Dialogs.ShowChildAsync<WarehouseAvaloniaApp.Views.SettingsForm, WarehouseAvaloniaApp.ViewModels.SettingsFormViewModel>();
        }

}
