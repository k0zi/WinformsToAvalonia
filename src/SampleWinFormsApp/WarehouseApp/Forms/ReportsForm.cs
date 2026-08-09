using System.Drawing.Printing;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

public partial class ReportsForm : Form
{
    private List<(string Label, double Value)> _lastChartData = [];

    public ReportsForm()
    {
        InitializeComponent();
        referenceMonthCalendar.SetDate(DateTime.Today);
        Load += async (_, _) =>
        {
            await RefreshChartAsync();
            await LoadAuditLogAsync();
        };
    }

    private async Task RefreshChartAsync()
    {
        var endMonth = new DateTime(referenceMonthCalendar.SelectionStart.Year, referenceMonthCalendar.SelectionStart.Month, 1)
            .AddMonths(-panScrollBar.Value);
        var monthCount = zoomScrollBar.Value;
        var startMonth = endMonth.AddMonths(-(monthCount - 1));

        var counts = await Task.Run(() =>
        {
            using var ctx = Db.CreateContext();
            return ctx.SalesOrders
                .Where(o => o.OrderDate >= startMonth && o.OrderDate < endMonth.AddMonths(1))
                .AsEnumerable()
                .GroupBy(o => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1))
                .ToDictionary(g => g.Key, g => g.Count());
        });

        _lastChartData = [];
        for (var i = 0; i < monthCount; i++)
        {
            var month = startMonth.AddMonths(i);
            var count = counts.GetValueOrDefault(month, 0);
            _lastChartData.Add((month.ToString("MMM yy"), count));
        }

        salesChart.SetData(
        [
            new ChartSeries { Name = "Sales Orders", Color = Color.SteelBlue, Points = _lastChartData }
        ]);
    }

    private async Task LoadAuditLogAsync()
    {
        var entries = await Task.Run(() =>
        {
            using var ctx = Db.CreateContext();
            return ctx.AuditLogs.Include(a => a.User).OrderByDescending(a => a.Timestamp).ToList();
        });

        auditListView.Items.Clear();
        foreach (var entry in entries)
        {
            var item = new ListViewItem(entry.Timestamp.ToLocalTime().ToString("g"));
            item.SubItems.Add(entry.EntityName);
            item.SubItems.Add(entry.Action.ToString());
            item.SubItems.Add(entry.User?.DisplayName ?? "—");
            item.SubItems.Add(entry.Details ?? string.Empty);
            auditListView.Items.Add(item);
        }
    }

    private void exportCsvButton_Click(object? sender, EventArgs e)
    {
        if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Month,SalesOrders");
        foreach (var (label, value) in _lastChartData)
        {
            sb.AppendLine($"{label},{value}");
        }

        File.WriteAllText(saveFileDialog.FileName, sb.ToString());
        MessageBox.Show(this, $"Exported to {saveFileDialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void chooseFontButton_Click(object? sender, EventArgs e)
    {
        fontDialog.Font = salesChart.TitleFont;
        if (fontDialog.ShowDialog(this) == DialogResult.OK)
        {
            salesChart.TitleFont = fontDialog.Font;
            salesChart.Invalidate();
        }
    }

    private void printPreviewButton_Click(object? sender, EventArgs e)
    {
        printPreviewDialog.ShowDialog(this);
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        var g = e.Graphics!;
        using var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 10f);
        g.DrawString("Sales Orders by Month", titleFont, Brushes.Black, 40, 40);

        var y = 90f;
        foreach (var (label, value) in _lastChartData)
        {
            g.DrawString($"{label}: {value} order(s)", bodyFont, Brushes.Black, 40, y);
            y += 20;
        }
    }
}
