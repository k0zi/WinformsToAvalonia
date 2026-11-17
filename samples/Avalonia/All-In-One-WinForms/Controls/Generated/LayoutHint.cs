using Avalonia;
using Avalonia.Controls;

namespace All_In_One_WinForms.Controls.Generated;

/// <summary>
/// Pure metadata carriers preserving the original WinForms Anchor/Dock values for a
/// control - not wired to any runtime layout behavior. See the XML comment above
/// each control in the generated Views for the human-readable form of the same data.
/// </summary>
/// <remarks>Not a `static class`: AvaloniaProperty.RegisterAttached's owner-type
/// argument can't be a static type, even though every member here is static.</remarks>
public sealed class LayoutHint
{
    private LayoutHint()
    {
    }

    public static readonly AttachedProperty<string?> AnchorProperty =
        AvaloniaProperty.RegisterAttached<LayoutHint, Control, string?>("Anchor");

    public static readonly AttachedProperty<string?> DockProperty =
        AvaloniaProperty.RegisterAttached<LayoutHint, Control, string?>("Dock");

    public static string? GetAnchor(Control control) => control.GetValue(AnchorProperty);

    public static void SetAnchor(Control control, string? value) => control.SetValue(AnchorProperty, value);

    public static string? GetDock(Control control) => control.GetValue(DockProperty);

    public static void SetDock(Control control, string? value) => control.SetValue(DockProperty, value);
}