using Avalonia.Controls;
using WarehouseAvaloniaApp.Common;

namespace WarehouseAvaloniaApp.Views;

public partial class MessageBoxWindow : Window
{
    public MessageBoxWindow(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        InitializeComponent();
        Title = caption;
        MessageText.Text = text;

        foreach (var (label, result) in GetButtons(buttons))
        {
            var button = new Button { Content = label, MinWidth = 75 };
            button.Click += (_, _) => Close(result);
            ButtonPanel.Children.Add(button);
        }
    }

    private static IEnumerable<(string Label, DialogResult Result)> GetButtons(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.OKCancel => [("OK", DialogResult.OK), ("Cancel", DialogResult.Cancel)],
        MessageBoxButtons.YesNo => [("Yes", DialogResult.Yes), ("No", DialogResult.No)],
        MessageBoxButtons.YesNoCancel => [("Yes", DialogResult.Yes), ("No", DialogResult.No), ("Cancel", DialogResult.Cancel)],
        MessageBoxButtons.RetryCancel => [("Retry", DialogResult.Retry), ("Cancel", DialogResult.Cancel)],
        MessageBoxButtons.AbortRetryIgnore => [("Abort", DialogResult.Abort), ("Retry", DialogResult.Retry), ("Ignore", DialogResult.Ignore)],
        _ => [("OK", DialogResult.OK)]
    };
}
