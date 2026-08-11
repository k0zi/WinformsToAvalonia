using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for SettingsForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class SettingsFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal AppSettings _settings = new();

    internal async Task LoadSettingsAsync()
        {
            _settings = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return ctx.AppSettings.FirstOrDefault(s => s.Id == 1) ?? new AppSettings { Id = 1, CompanyName = "WarehouseApp" };
            });
    
            companyNameTextBox.Text = _settings.CompanyName;
            darkModeToggle.Checked = _settings.ThemeDarkMode;
            backupFolderTextBox.Text = _settings.BackupFolderPath ?? string.Empty;
    
            var color = ColorTranslator.FromHtml(_settings.AccentColorHex);
            colorPreviewPanel.BackColor = color;
            customColorRadioButton.Checked = _settings.AccentColorHex != "#2D6CDF";
            presetColorRadioButton.Checked = !customColorRadioButton.Checked;
    
            advancedPropertyGrid.SelectedObject = _settings;
        }

    internal void chooseColorButton_Click(object? sender, EventArgs e)
        {
            colorDialog.Color = colorPreviewPanel.BackColor;
            if (colorDialog.ShowDialog(this) == ConvertedAvalonia.Common.DialogResult.OK)
            {
                colorPreviewPanel.BackColor = colorDialog.Color;
                customColorRadioButton.Checked = true;
            }
        }

    internal void browseFolderButton_Click(object? sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog(this) == ConvertedAvalonia.Common.DialogResult.OK)
            {
                backupFolderTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

    internal async Task SaveGeneralAsync()
        {
            _settings.CompanyName = companyNameTextBox.Text.Trim();
            _settings.ThemeDarkMode = darkModeToggle.Checked;
            _settings.AccentColorHex = customColorRadioButton.Checked
                ? ColorTranslator.ToHtml(colorPreviewPanel.BackColor)
                : "#2D6CDF";
            _settings.BackupFolderPath = string.IsNullOrWhiteSpace(backupFolderTextBox.Text) ? null : backupFolderTextBox.Text;
    
            await PersistSettingsAsync();
            await ConvertedAvalonia.Common.Dialogs.ShowAsync("Settings saved.","Settings",ConvertedAvalonia.Common.MessageBoxButtons.OK,ConvertedAvalonia.Common.MessageBoxIcon.Information);
        }

    internal async Task SaveAdvancedAsync()
        {
            await PersistSettingsAsync();
            await ConvertedAvalonia.Common.Dialogs.ShowAsync("Advanced settings saved.","Settings",ConvertedAvalonia.Common.MessageBoxButtons.OK,ConvertedAvalonia.Common.MessageBoxIcon.Information);
        }

    internal async Task PersistSettingsAsync()
        {
            using var ctx = Db.CreateContext();
            var tracked = await ctx.AppSettings.FirstOrDefaultAsync(s => s.Id == 1);
            if (tracked is null)
            {
                _settings.Id = 1;
                ctx.AppSettings.Add(_settings);
            }
            else
            {
                ctx.Entry(tracked).CurrentValues.SetValues(_settings);
            }
            await ctx.SaveChangesAsync();
        }

    internal void LoadAboutPage()
        {
            const string html = """
                <html>
                <head><style>
                    body { font-family: Segoe UI, sans-serif; margin: 16px; color: #222; }
                    h1 { color: #2D6CDF; font-size: 18px; }
                    h2 { font-size: 13px; margin-top: 16px; }
                    ul { margin: 4px 0; padding-left: 20px; }
                </style></head>
                <body>
                    <h1>WarehouseApp</h1>
                    <p>A sample warehouse inventory management showcase built with .NET 8 WinForms.</p>
                    <h2>Changelog</h2>
                    <ul>
                        <li>1.0 — Initial release: products, stock movements, purchase &amp; sales orders, reporting.</li>
                    </ul>
                    <h2>Credits</h2>
                    <p>Built to demonstrate a broad range of WinForms built-in and custom controls.</p>
                </body>
                </html>
                """;
            aboutWebBrowser.DocumentText = html;
        }

}
