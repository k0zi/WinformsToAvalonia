using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms' <c>ColorDialog</c>: Avalonia has a real <c>ColorPicker</c> control, but
/// no ready-made modal dialog around it.
/// </summary>
/// <remarks>
/// <para>
/// A static helper rather than a control, because that is the shape the call site needs - the
/// WinForms original is a component you show and then ask for its <c>Color</c>, and the
/// translation collapses those two steps into this one call's return value.
/// </para>
/// <para>
/// The <c>initial</c> argument is the colour the dialog opens on, which is what WinForms'
/// <c>colorDialog1.Color = ...;</c> before the call meant. Omitted, it opens on white.
/// </para>
/// </remarks>
public static class ColorDialogFallback
{
    /// <summary>The chosen colour, or null when the dialog was cancelled or closed.</summary>
    public static async Task<Color?> ShowAsync(Visual owner, Color? initial = null)
    {
        var picker = new ColorView
        {
            Color = initial ?? Colors.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var okButton = new Button { Content = "OK", IsDefault = true, MinWidth = 88 };
        var cancelButton = new Button { Content = "Cancel", IsCancel = true, MinWidth = 88 };

        var dialog = new Window
        {
            Title = "Color",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    picker,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { okButton, cancelButton },
                    },
                },
            },
        };

        okButton.Click += (_, _) => dialog.Close(picker.Color);
        cancelButton.Click += (_, _) => dialog.Close(null);

        if (TopLevel.GetTopLevel(owner) is not Window ownerWindow)
        {
            return null;
        }

        return await dialog.ShowDialog<Color?>(ownerWindow);
    }
}
