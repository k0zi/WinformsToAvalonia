using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms RichTextBox: Avalonia has no built-in RTF-capable text control.
/// Wraps a plain multi-line TextBox - RTF formatting is not preserved, only plain text.
/// </summary>
public class RichTextBoxFallback : TextBox
{
    /// <remarks>
    /// Avalonia resolves a control's theme by its <em>concrete</em> type, so a subclass of a
    /// templated control finds no theme and gets no template - it renders as nothing at all,
    /// not as an unstyled box. Measured: without this the fallback was absent from the window
    /// while compiling, starting and passing every test.
    /// </remarks>
    protected override Type StyleKeyOverride => typeof(TextBox);

    public RichTextBoxFallback()
    {
        AcceptsReturn = true;
        TextWrapping = TextWrapping.Wrap;
    }
}
