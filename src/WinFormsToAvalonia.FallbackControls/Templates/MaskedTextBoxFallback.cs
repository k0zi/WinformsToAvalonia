using System;
using Avalonia.Controls;
using Avalonia;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms MaskedTextBox: Avalonia has no built-in masked input. Wraps
/// TextBox and exposes the original WinForms Mask string for reference/manual wiring - no
/// automatic input masking behavior is applied yet.
/// </summary>
public class MaskedTextBoxFallback : TextBox
{
    /// <remarks>
    /// Avalonia resolves a control's theme by its <em>concrete</em> type, so a subclass of a
    /// templated control finds no theme and gets no template - it renders as nothing at all,
    /// not as an unstyled box. Measured: without this the fallback was absent from the window
    /// while compiling, starting and passing every test.
    /// </remarks>
    protected override Type StyleKeyOverride => typeof(TextBox);

    public static readonly StyledProperty<string?> MaskProperty =
        AvaloniaProperty.Register<MaskedTextBoxFallback, string?>(nameof(Mask));

    public string? Mask
    {
        get => GetValue(MaskProperty);
        set => SetValue(MaskProperty, value);
    }
}
