using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms' <c>MessageBox.Show(...)</c>: Avalonia ships no message box at all, so
/// this builds a small modal window out of ordinary controls.
/// </summary>
/// <remarks>
/// <para>
/// Not a control but a static helper, because that is the shape the call site needs - which is
/// also why it is the one catalog entry a converted <em>handler body</em> can pull in, rather
/// than the AXAML.
/// </para>
/// <para>
/// Deliberately only the "show some text, dismiss it" case. WinForms' button/icon overloads
/// return a <c>DialogResult</c> the caller branches on, and inventing an answer for that would
/// change what the original handler did - those calls are left un-migrated for a human instead.
/// </para>
/// <para>
/// The owner is a <c>Visual</c> rather than a <c>Window</c> so this works unchanged from a
/// converted UserControl's code-behind, which is not itself a window.
/// </para>
/// </remarks>
public static class MessageBoxFallback
{
    public static Task ShowAsync(Visual owner, string? text, string? caption = "")
    {
        var okButton = new Button
        {
            Content = "OK",
            IsDefault = true,
            MinWidth = 88,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var dialog = new Window
        {
            Title = caption ?? string.Empty,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            MinWidth = 260,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20,
                Children =
                {
                    new TextBlock
                    {
                        Text = text ?? string.Empty,
                        MaxWidth = 420,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    okButton,
                },
            },
        };

        okButton.Click += (_, _) => dialog.Close();

        // A converted app always has a window by the time a handler runs, but a non-modal
        // fallback is still better than throwing if one is somehow not there yet.
        return TopLevel.GetTopLevel(owner) is Window ownerWindow
            ? dialog.ShowDialog(ownerWindow)
            : ShowNonModal(dialog);
    }

    private static Task ShowNonModal(Window dialog)
    {
        dialog.Show();
        return Task.CompletedTask;
    }
}
