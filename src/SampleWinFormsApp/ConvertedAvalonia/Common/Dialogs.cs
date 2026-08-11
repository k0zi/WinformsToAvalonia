using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ConvertedAvalonia.Common;

public static class Dialogs
{
    public static async Task<DialogResult> ShowAsync(
        string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        var owner = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
        var window = new ConvertedAvalonia.Views.MessageBoxWindow(text, caption, buttons, icon);
        return await window.ShowDialog<DialogResult>(owner);
    }

    public static async Task<DialogResult?> ShowChildAsync<TView, TViewModel>()
        where TView : Window, new()
        where TViewModel : new()
    {
        var owner = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
        var view = new TView { DataContext = new TViewModel() };
        return await view.ShowDialog<DialogResult?>(owner);
    }
}
