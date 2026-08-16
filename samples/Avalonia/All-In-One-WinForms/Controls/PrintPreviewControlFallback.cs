using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms PrintPreviewControl: Avalonia has no printing API at all, so there
/// is nothing to render a preview from. This draws the page-shaped placeholder the original
/// control occupied, so the converted View's layout stays intact while printing is migrated
/// by hand.
/// </summary>
public class PrintPreviewControlFallback : UserControl
{
    public static readonly StyledProperty<object?> DocumentProperty =
        AvaloniaProperty.Register<PrintPreviewControlFallback, object?>(nameof(Document));

    public PrintPreviewControlFallback()
    {
        Content = new Border
        {
            Background = Brushes.DimGray,
            Padding = new Thickness(12),
            Child = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = "Print preview unavailable - Avalonia has no printing API.",
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };
    }

    /// <summary>Stands in for PrintPreviewControl.Document - kept so existing assignments still compile.</summary>
    public object? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }
}
