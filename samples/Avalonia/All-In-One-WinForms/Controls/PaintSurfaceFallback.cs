using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for a WinForms control whose <c>Paint</c> handler drew on it directly.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has no <c>Paint</c> event: a control draws by overriding
/// <c>Render(DrawingContext)</c>. That is a subclass, which a converted View cannot conjure for
/// an arbitrary element - so this template is the subclass, and it turns the override back into
/// the event the WinForms code was written against.
/// </para>
/// <para>
/// A bare <c>Control</c>, and not by preference: <c>Panel.Render</c> is <c>sealed</c> in Avalonia,
/// so a Canvas subclass cannot override it at all. That is why the mapper only retargets a control
/// with no children - a container that needed both its children and its own drawing has no base
/// class here that offers both.
/// </para>
/// <para>
/// <c>Background</c> is declared here rather than inherited, for the same reason: a
/// <c>Control</c> has none. Painting it first is what a WinForms control's <c>BackColor</c> did,
/// and it keeps the designer's colour from being dropped on a technicality.
/// </para>
/// <para>
/// No <c>StyleKeyOverride</c>: a <c>Control</c> is not a templated control, so there is no theme
/// to lose - the documented exemption from that rule.
/// </para>
/// </remarks>
public class PaintSurfaceFallback : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<PaintSurfaceFallback, IBrush?>(nameof(Background));

    /// <summary>The surface's own fill, drawn before the Paint handler runs.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Raised on every render pass, which is when Avalonia asks a control to draw itself.
    /// </summary>
    /// <remarks>
    /// WinForms raised Paint on invalidation and Avalonia calls Render on the render pass, so the
    /// timing is not identical - but the contract a Paint handler was written against holds: it is
    /// called when the surface needs its contents, and <c>InvalidateVisual()</c> asks for another.
    /// </remarks>
    public event EventHandler<PaintSurfaceEventArgs>? Paint;

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(Bounds.Size);

        if (Background is { } background)
        {
            context.FillRectangle(background, bounds);
        }

        Paint?.Invoke(this, new PaintSurfaceEventArgs(context, bounds));
    }
}

/// <summary>
/// The Avalonia counterpart of WinForms' <c>PaintEventArgs</c>.
/// </summary>
/// <remarks>
/// <c>Context</c> stands in for <c>PaintEventArgs.Graphics</c> and <c>ClipRectangle</c> for the
/// property of the same name - the two members a Paint handler actually uses. The context is only
/// valid for the duration of the call, exactly as a WinForms <c>Graphics</c> was.
/// </remarks>
public sealed class PaintSurfaceEventArgs : EventArgs
{
    public PaintSurfaceEventArgs(DrawingContext context, Rect clipRectangle)
    {
        Context = context;
        ClipRectangle = clipRectangle;
    }

    /// <summary>Where to draw - the WinForms PaintEventArgs.Graphics equivalent.</summary>
    public DrawingContext Context { get; }

    /// <summary>The area being redrawn, which for this surface is always the whole of it.</summary>
    public Rect ClipRectangle { get; }
}
