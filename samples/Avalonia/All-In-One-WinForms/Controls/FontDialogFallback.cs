using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// What a converted <c>FontDialog</c> hands back. A record rather than a single value because
/// WinForms' <c>Font</c> is one object where Avalonia has four separate properties - so this is
/// the shape that lets <c>control.Font = fontDialog1.Font</c> translate at all.
/// </summary>
public sealed record FontChoice(FontFamily Family, double Size, FontWeight Weight, FontStyle Style);

/// <summary>
/// Fallback for WinForms' <c>FontDialog</c>, which Avalonia has no equivalent of. The font list
/// comes from <c>FontManager.Current.SystemFonts</c>, so it is whatever the running platform
/// actually has rather than a list baked in here.
/// </summary>
/// <remarks>
/// Deliberately family, size, bold and italic only. WinForms' dialog also offers underline,
/// strikeout and a script/charset picker; underline and strikeout are <c>TextDecorations</c> in
/// Avalonia rather than part of the font, and inventing a mapping for the rest would put choices
/// in front of the user that the converted code cannot act on.
/// </remarks>
public static class FontDialogFallback
{
    public static async Task<FontChoice?> ShowAsync(Visual owner)
    {
        var families = FontManager.Current.SystemFonts.OrderBy(f => f.Name).ToList();

        var familyBox = new ComboBox
        {
            ItemsSource = families,
            SelectedItem = families.FirstOrDefault(f => f == FontManager.Current.DefaultFontFamily) ?? families.FirstOrDefault(),
            MinWidth = 220,
        };

        var sizeBox = new NumericUpDown { Value = 12, Minimum = 4, Maximum = 128, Increment = 1 };
        var boldBox = new CheckBox { Content = "Bold" };
        var italicBox = new CheckBox { Content = "Italic" };

        var okButton = new Button { Content = "OK", IsDefault = true, MinWidth = 88 };
        var cancelButton = new Button { Content = "Cancel", IsCancel = true, MinWidth = 88 };

        var dialog = new Window
        {
            Title = "Font",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    familyBox,
                    sizeBox,
                    boldBox,
                    italicBox,
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

        okButton.Click += (_, _) => dialog.Close(new FontChoice(
            familyBox.SelectedItem as FontFamily ?? FontManager.Current.DefaultFontFamily,
            (double)(sizeBox.Value ?? 12),
            boldBox.IsChecked == true ? FontWeight.Bold : FontWeight.Normal,
            italicBox.IsChecked == true ? FontStyle.Italic : FontStyle.Normal));

        cancelButton.Click += (_, _) => dialog.Close(null);

        if (TopLevel.GetTopLevel(owner) is not Window ownerWindow)
        {
            return null;
        }

        return await dialog.ShowDialog<FontChoice?>(ownerWindow);
    }
}
