using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace All_In_One_WinForms.Dialogs;

/// <summary>
/// Replacement for the WinForms <c>ColorDialog</c>: Avalonia has no common color dialog, and
/// its full <c>ColorPicker</c> lives in a separate package this project deliberately does not
/// pull in (same reasoning as the bundled fallback controls), so this is a small owned window
/// offering a fixed palette.
/// </summary>
public partial class ColorPickerDialog : Window
{
    private static readonly IReadOnlyList<ColorChoice> Palette =
    [
        new("Black", Colors.Black),
        new("White", Colors.White),
        new("Gainsboro", Colors.Gainsboro),
        new("Silver", Colors.Silver),
        new("Gray", Colors.Gray),
        new("Steel blue", Colors.SteelBlue),
        new("Cornflower blue", Colors.CornflowerBlue),
        new("Sea green", Colors.SeaGreen),
        new("Olive", Colors.Olive),
        new("Gold", Colors.Gold),
        new("Orange", Colors.Orange),
        new("Tomato", Colors.Tomato),
        new("Firebrick", Colors.Firebrick),
        new("Orchid", Colors.Orchid),
        new("Purple", Colors.Purple),
        new("Teal", Colors.Teal),
    ];

    public ColorPickerDialog()
    {
        InitializeComponent();
        this.colorList.ItemsSource = Palette;
    }

    /// <summary>
    /// The <c>colorDialog1.ShowDialog(owner) == DialogResult.OK ? colorDialog1.Color : null</c>
    /// equivalent - <see langword="null"/> means the user cancelled.
    /// </summary>
    public static Task<Color?> ShowAsync(Window owner, Color? current)
    {
        var dialog = new ColorPickerDialog();
        dialog.colorList.SelectedItem = Palette.FirstOrDefault(choice => choice.Value == current) ?? Palette[0];
        return dialog.ShowDialog<Color?>(owner);
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e) =>
        Close((this.colorList.SelectedItem as ColorChoice)?.Value);

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);
}

/// <summary>One entry of the <see cref="ColorPickerDialog"/> palette.</summary>
public sealed record ColorChoice(string Name, Color Value)
{
    /// <summary>Brush of <see cref="Value"/>, for the swatch in the list.</summary>
    public IBrush Swatch { get; } = new SolidColorBrush(Value);
}
