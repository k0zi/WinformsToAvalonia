using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms' <c>MessageBox.Show(...)</c>: Avalonia deliberately ships no message box,
/// so a converted project needs one small owned window of its own.
/// </summary>
/// <remarks>
/// <para>
/// Not a control but a static helper, because that is the shape the call site needs - which is
/// also why it is one of the few catalog entries a converted <em>handler body</em> can pull in,
/// rather than the AXAML.
/// </para>
/// <para>
/// Three entry points, matching the three WinForms shapes that have an unambiguous meaning here:
/// a plain acknowledgement, and the two-button questions whose answer is a yes/no. WinForms'
/// three-way overloads (<c>YesNoCancel</c>, <c>AbortRetryIgnore</c>) have no bool answer and are
/// deliberately absent - the translation refuses them rather than inventing a third state.
/// </para>
/// <para>
/// Every entry point is awaitable, unlike WinForms' blocking call - which is what makes the
/// converted handlers async.
/// </para>
/// </remarks>
public static class MessageBoxFallback
{
    /// <summary>The <c>MessageBox.Show(text[, caption])</c> equivalent.</summary>
    public static async Task ShowAsync(Visual owner, string? text, string? caption = "") =>
        await ShowCore(owner, text, caption, "OK", cancelContent: null);

    /// <summary>True for Yes. The <c>MessageBoxButtons.YesNo</c> equivalent.</summary>
    public static Task<bool> ShowYesNoAsync(Visual owner, string? text, string? caption = "") =>
        ShowCore(owner, text, caption, "Yes", "No");

    /// <summary>True for OK. The <c>MessageBoxButtons.OKCancel</c> equivalent.</summary>
    public static Task<bool> ShowOkCancelAsync(Visual owner, string? text, string? caption = "") =>
        ShowCore(owner, text, caption, "OK", "Cancel");

    /// <param name="cancelContent">Null for the one-button shape, which always answers true.</param>
    private static async Task<bool> ShowCore(
        Visual owner, string? text, string? caption, string acceptContent, string? cancelContent)
    {
        var acceptButton = new Button { Content = acceptContent, IsDefault = true, MinWidth = 88 };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { acceptButton },
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
                    buttons,
                },
            },
        };

        acceptButton.Click += (_, _) => dialog.Close(true);

        if (cancelContent is not null)
        {
            var cancelButton = new Button { Content = cancelContent, IsCancel = true, MinWidth = 88 };
            cancelButton.Click += (_, _) => dialog.Close(false);
            buttons.Children.Add(cancelButton);
        }

        // A converted app always has a window by the time a handler runs, but answering "not
        // accepted" is still better than throwing if one is somehow not there yet.
        if (TopLevel.GetTopLevel(owner) is not Window ownerWindow)
        {
            dialog.Show();
            return false;
        }

        return await dialog.ShowDialog<bool>(ownerWindow);
    }
}
