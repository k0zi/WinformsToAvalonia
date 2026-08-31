using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms' <c>PrintPreviewDialog</c>: a window showing the page a
/// <see cref="PrintDocumentFallback"/> draws.
/// </summary>
/// <remarks>
/// <para>
/// This one only became possible once the document could be produced. Avalonia still has no
/// printing API, but a preview does not need one - it needs a rendered page, and
/// <c>RenderFirstPage</c> is a real page drawn by the handler the original wrote. Until that
/// existed, the <c>PrintPreviewControl</c> fallback could only be a page-shaped placeholder.
/// </para>
/// <para>
/// A static helper rather than a control, the same shape as the other dialog fallbacks: the
/// WinForms original is a component you show, so the translation is one call.
/// </para>
/// </remarks>
public static class PrintPreviewDialogFallback
{
    public static async Task ShowAsync(Visual owner, PrintDocumentFallback document, string? title = null)
    {
        using var page = document.RenderFirstPage();

        var preview = new Border
        {
            Background = Brushes.DimGray,
            Padding = new Thickness(12),
            Child = new Image
            {
                Source = page,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var closeButton = new Button { Content = "Close", IsDefault = true, IsCancel = true, MinWidth = 88 };

        var dialog = new Window
        {
            Title = title ?? "Print preview",
            Width = 640,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false,
            Content = new DockPanel
            {
                Margin = new Thickness(12),
                Children =
                {
                    new StackPanel
                    {
                        [DockPanel.DockProperty] = Dock.Bottom,
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 12, 0, 0),
                        Children = { closeButton },
                    },
                    preview,
                },
            },
        };

        closeButton.Click += (_, _) => dialog.Close();

        if (TopLevel.GetTopLevel(owner) is Window parent)
        {
            await dialog.ShowDialog(parent);
            return;
        }

        dialog.Show();
    }
}
