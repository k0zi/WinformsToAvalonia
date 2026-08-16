using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace All_In_One_WinForms.Dialogs;

/// <summary>
/// Replacement for the WinForms <c>FontDialog</c>: Avalonia has no common font dialog, so this
/// small owned window lists the installed families (<see cref="FontManager.SystemFonts"/>) and
/// a size, which is the part of <c>Font</c> an Avalonia control can actually apply.
/// </summary>
public partial class FontPickerDialog : Window
{
    public FontPickerDialog()
    {
        InitializeComponent();
        this.familyList.ItemsSource = FontManager.Current.SystemFonts.OrderBy(family => family.Name).ToList();
    }

    /// <summary>
    /// The <c>fontDialog1.ShowDialog(owner) == DialogResult.OK ? fontDialog1.Font : null</c>
    /// equivalent - <see langword="null"/> means the user cancelled.
    /// </summary>
    public static Task<FontChoice?> ShowAsync(Window owner, FontFamily current, double currentSize)
    {
        var dialog = new FontPickerDialog();
        dialog.familyList.SelectedItem = dialog.familyList.ItemsSource?
            .OfType<FontFamily>()
            .FirstOrDefault(family => family.Name == current.Name);
        dialog.sizeUpDown.Value = (decimal)currentSize;
        return dialog.ShowDialog<FontChoice?>(owner);
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e) =>
        Close(this.familyList.SelectedItem is FontFamily family
            ? new FontChoice(family, (double)(this.sizeUpDown.Value ?? 12m))
            : null);

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);
}

/// <summary>The <see cref="FontFamily"/>/size pair a <see cref="FontPickerDialog"/> returns.</summary>
public sealed record FontChoice(FontFamily Family, double Size);
