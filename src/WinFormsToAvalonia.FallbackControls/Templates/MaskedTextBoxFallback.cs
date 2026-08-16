using Avalonia;
using Avalonia.Controls;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms MaskedTextBox: Avalonia has no built-in masked input. Wraps
/// TextBox and exposes the original WinForms Mask string for reference/manual wiring - no
/// automatic input masking behavior is applied yet.
/// </summary>
public class MaskedTextBoxFallback : TextBox
{
    public static readonly StyledProperty<string?> MaskProperty =
        AvaloniaProperty.Register<MaskedTextBoxFallback, string?>(nameof(Mask));

    public string? Mask
    {
        get => GetValue(MaskProperty);
        set => SetValue(MaskProperty, value);
    }
}
