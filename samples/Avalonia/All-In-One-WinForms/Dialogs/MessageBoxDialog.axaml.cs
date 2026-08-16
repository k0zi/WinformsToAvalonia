using System.Threading.Tasks;
using Avalonia.Controls;

namespace All_In_One_WinForms.Dialogs;

/// <summary>
/// Replacement for WinForms <c>MessageBox.Show</c>: Avalonia deliberately ships no message box,
/// so a converted project needs one small owned window of its own. Unlike WinForms' blocking
/// call, every entry point here is awaitable - which is what makes the callers async.
/// </summary>
public partial class MessageBoxDialog : Window
{
    public MessageBoxDialog()
    {
        InitializeComponent();
    }

    /// <summary>The <c>MessageBox.Show(owner, text, caption)</c> equivalent.</summary>
    public static Task ShowAsync(Window owner, string message, string title) =>
        ShowCore(owner, message, title, ("OK", true));

    /// <summary>
    /// The <c>MessageBox.Show(text, caption, MessageBoxButtons.YesNo)</c> equivalent;
    /// <see langword="true"/> stands for <c>DialogResult.Yes</c>.
    /// </summary>
    public static Task<bool> ShowYesNoAsync(Window owner, string message, string title) =>
        ShowCore(owner, message, title, ("Yes", true), ("No", false));

    private static Task<bool> ShowCore(
        Window owner,
        string message,
        string title,
        params (string Label, bool Result)[] buttons)
    {
        var dialog = new MessageBoxDialog { Title = title };
        dialog.messageText.Text = message;

        foreach (var (label, result) in buttons)
        {
            var button = new Button { Content = label, MinWidth = 80 };
            button.Click += (_, _) => dialog.Close(result);
            dialog.buttonsPanel.Children.Add(button);
        }

        return dialog.ShowDialog<bool>(owner);
    }
}
