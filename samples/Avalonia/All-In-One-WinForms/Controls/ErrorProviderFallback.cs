using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms' <c>ErrorProvider</c>: an attached property that puts a red error
/// indicator beside a control, with the message as its tooltip.
/// </summary>
/// <remarks>
/// <para>
/// An adorner rather than Avalonia's <c>DataValidationErrors</c>, which is the idiomatic answer
/// to a different question. <c>DataValidationErrors</c> is for binding-time validation - its
/// <c>SetError</c> takes an <c>Exception</c>, not a message - and it only draws anything where
/// the target's <c>ControlTheme</c> hosts a presenter for it. Under the Simple theme this
/// generated app uses, that is <c>TextBox</c>, <c>NumericUpDown</c>, the pickers and a couple
/// more; on a <c>Button</c>, a <c>Label</c> or a panel it would render nothing at all. WinForms'
/// ErrorProvider works on any control, so this does too: the adorner layer lives in the Window's
/// own template, not in the adorned control's.
/// </para>
/// <para>
/// Setting a null or empty message removes the indicator, which is exactly how WinForms clears
/// one - <c>errorProvider1.SetError(control, string.Empty)</c>.
/// </para>
/// <para>
/// What is not carried over: the blink (<c>BlinkStyle</c>/<c>BlinkRate</c>), a custom
/// <c>Icon</c>, and <c>SetIconAlignment</c>/<c>SetIconPadding</c> - the indicator always sits at
/// the control's right edge.
/// </para>
/// </remarks>
public sealed class ErrorProviderFallback
{
    private ErrorProviderFallback()
    {
    }

    public static readonly AttachedProperty<string?> ErrorProperty =
        AvaloniaProperty.RegisterAttached<ErrorProviderFallback, Control, string?>("Error");

    /// <summary>Marks the indicator this class created, so it only ever removes its own.</summary>
    private static readonly AttachedProperty<Control?> IndicatorProperty =
        AvaloniaProperty.RegisterAttached<ErrorProviderFallback, Control, Control?>("Indicator");

    static ErrorProviderFallback()
    {
        ErrorProperty.Changed.AddClassHandler<Control>((control, _) => Apply(control));
    }

    public static string? GetError(Control control) => control.GetValue(ErrorProperty);

    public static void SetError(Control control, string? value) => control.SetValue(ErrorProperty, value);

    private static void Apply(Control control)
    {
        var message = control.GetValue(ErrorProperty);

        if (string.IsNullOrEmpty(message))
        {
            Remove(control);
            return;
        }

        // The adorner layer belongs to the window, so it only exists once the control is in a
        // visual tree. A handler that runs before that - from a constructor, say - must not
        // throw: the generated app has to start.
        if (AdornerLayer.GetAdornerLayer(control) is null)
        {
            control.AttachedToVisualTree -= OnAttached;
            control.AttachedToVisualTree += OnAttached;
            return;
        }

        var indicator = control.GetValue(IndicatorProperty);
        if (indicator is null)
        {
            indicator = CreateIndicator();
            control.SetValue(IndicatorProperty, indicator);
            AdornerLayer.SetAdorner(control, indicator);
        }

        ToolTip.SetTip(indicator, message);
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        control.AttachedToVisualTree -= OnAttached;
        Apply(control);
    }

    private static void Remove(Control control)
    {
        control.AttachedToVisualTree -= OnAttached;

        if (control.GetValue(IndicatorProperty) is null)
        {
            return;
        }

        control.SetValue(IndicatorProperty, null);
        AdornerLayer.SetAdorner(control, null);
    }

    /// <summary>The red "!" WinForms draws, at the control's right edge.</summary>
    private static Control CreateIndicator() => new Border
    {
        Width = 14,
        Height = 14,
        CornerRadius = new CornerRadius(7),
        Background = Brushes.Red,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Center,
        IsHitTestVisible = true,
        Child = new TextBlock
        {
            Text = "!",
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };
}
