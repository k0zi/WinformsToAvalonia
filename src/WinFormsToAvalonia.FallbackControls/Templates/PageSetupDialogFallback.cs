using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms' <c>PageSetupDialog</c>: paper size, orientation and margins, written
/// back onto the <see cref="PrintDocumentFallback"/> the dialog was pointed at.
/// </summary>
/// <remarks>
/// <para>
/// This was called "easy to build and useless" for as long as nothing consumed a page setup -
/// which was true while there was no page. Now there is one: these two values are exactly what
/// <c>RenderFirstPage</c> lays the page out with, so the dialog changes what the export looks
/// like.
/// </para>
/// <para>
/// Sizes are in device-independent pixels at 96 dpi, which is what the document renders at -
/// 816x1056 is US Letter, 794x1123 is A4. WinForms measured in hundredths of an inch; nothing is
/// carried over from the original dialog's units because a <c>PageSettings</c> object does not
/// survive the conversion to be read.
/// </para>
/// </remarks>
public static class PageSetupDialogFallback
{
    /// <summary>True when the settings were applied, false when the dialog was cancelled.</summary>
    public static async Task<bool> ShowAsync(Visual owner, PrintDocumentFallback document)
    {
        var sizes = new[]
        {
            new PaperSize("US Letter", 816, 1056),
            new PaperSize("A4", 794, 1123),
            new PaperSize("Legal", 816, 1344),
        };

        var paper = new ComboBox
        {
            ItemsSource = sizes,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var landscape = new CheckBox { Content = "Landscape" };
        var margin = new NumericUpDown { Minimum = 0, Maximum = 300, Value = (decimal)document.Margins.Left };

        var okButton = new Button { Content = "OK", IsDefault = true, MinWidth = 88 };
        var cancelButton = new Button { Content = "Cancel", IsCancel = true, MinWidth = 88 };

        var dialog = new Window
        {
            Title = "Page setup",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 8,
                MinWidth = 260,
                Children =
                {
                    new TextBlock { Text = "Paper" },
                    paper,
                    landscape,
                    new TextBlock { Text = "Margin (pixels at 96 dpi)" },
                    margin,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Margin = new Thickness(0, 8, 0, 0),
                        Children = { okButton, cancelButton },
                    },
                },
            },
        };

        var accepted = false;

        okButton.Click += (_, _) =>
        {
            accepted = true;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        if (TopLevel.GetTopLevel(owner) is Window parent)
        {
            await dialog.ShowDialog(parent);
        }
        else
        {
            dialog.Show();
        }

        if (!accepted)
        {
            return false;
        }

        var chosen = sizes[paper.SelectedIndex < 0 ? 0 : paper.SelectedIndex];
        document.PageSize = landscape.IsChecked == true
            ? new Size(chosen.Height, chosen.Width)
            : new Size(chosen.Width, chosen.Height);

        var inset = (double)(margin.Value ?? 0);
        document.Margins = new Thickness(inset);
        return true;
    }

    /// <summary>One of the paper sizes offered, in the units the document renders at.</summary>
    private sealed record PaperSize(string Name, double Width, double Height)
    {
        public override string ToString() => Name;
    }
}
