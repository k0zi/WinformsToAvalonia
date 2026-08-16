using Avalonia.Controls;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms RichTextBox: Avalonia has no built-in RTF-capable text control.
/// Wraps a plain multi-line TextBox - RTF formatting is not preserved, only plain text.
/// </summary>
public class RichTextBoxFallback : TextBox
{
    public RichTextBoxFallback()
    {
        AcceptsReturn = true;
        TextWrapping = TextWrapping.Wrap;
    }
}
