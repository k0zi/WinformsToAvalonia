using Avalonia;
using Avalonia.Controls;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms ErrorProvider: Avalonia already ships a declarative validation-
/// error adorner (Avalonia.Controls.DataValidationErrors) that is the idiomatic
/// replacement for real INotifyDataErrorInfo-based validation. This attached property only
/// covers the imperative `errorProvider1.SetError(control, message)` call style, which
/// doesn't map to that declarative pattern directly - it just stores the message as
/// metadata for now.
/// </summary>
public sealed class ErrorProviderFallback
{
    private ErrorProviderFallback()
    {
    }

    public static readonly AttachedProperty<string?> ErrorProperty =
        AvaloniaProperty.RegisterAttached<ErrorProviderFallback, Control, string?>("Error");

    public static string? GetError(Control control) => control.GetValue(ErrorProperty);

    public static void SetError(Control control, string? value) => control.SetValue(ErrorProperty, value);
}
