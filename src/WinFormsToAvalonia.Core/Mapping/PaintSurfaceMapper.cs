using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// A per-instance decision layered over another mapper: a control whose designer wired a
/// <c>Paint</c> handler becomes the bundled <c>PaintSurfaceFallback</c>, so that handler has
/// somewhere to run.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has no <c>Paint</c> event - drawing means overriding <c>Render(DrawingContext)</c>,
/// which is a subclass. So the target element has to change, and it can only change where nothing
/// is lost by changing it. The fallback derives from <c>Canvas</c>, which is what a
/// <c>Panel</c> already became, so a Panel loses nothing at all; a <c>PictureBox</c> loses nothing
/// either as long as it carries no <c>Image</c>, since an <c>Image</c> with no <c>Source</c> draws
/// exactly as much as an empty Canvas does.
/// </para>
/// <para>
/// A PictureBox that has both an image and a Paint handler keeps its <c>Image</c>: WinForms drew
/// the handler's output *over* the picture, and this converter has no honest way to do both. The
/// inner mapper answers, and the unmapped Paint is reported the way it always was.
/// </para>
/// <para>
/// A control with children is left alone for a harder reason: <c>Panel.Render</c> is <c>sealed</c>
/// in Avalonia, so the fallback derives from <c>Control</c> and hosts nothing. A Panel that both
/// draws and contains controls has no base class offering both, and dropping its children to gain
/// its drawing would be the worse trade.
/// </para>
/// </remarks>
public sealed class PaintSurfaceMapper(IControlMapper inner) : IControlMapper
{
    /// <summary>The template key this mapper switches to, shared with the planner and the tests.</summary>
    public const string TemplateKey = "PaintSurfaceFallback";

    public string WinFormsTypeName => inner.WinFormsTypeName;

    /// <summary>The mapper that answers when the control is not a paint surface.</summary>
    public IControlMapper Inner => inner;

    /// <summary>
    /// Whether this control both wants to be drawn on and can be, which is the same question the
    /// planner asks before it subscribes the Paint handler - so the two cannot disagree.
    /// </summary>
    public static bool IsPaintSurface(ControlModel control) =>
        control.Events.Any(e => string.Equals(e.EventName, "Paint", StringComparison.Ordinal))
        && !control.Properties.ContainsKey("Image")
        && control.Children.Count == 0;

    public MappedControl Map(ControlModel control)
    {
        if (!IsPaintSurface(control))
        {
            return inner.Map(control);
        }

        return new MappedControl(
            control.ClrTypeName,
            MappingStatus.Fallback,
            TemplateKey,
            new Dictionary<string, string>(StringComparer.Ordinal),
            TemplateKey,
            [
                $"'{control.FieldName}' ({control.ClrTypeName}) has a Paint handler, which Avalonia expresses as a "
                + $"Render(DrawingContext) override rather than an event - so it becomes the bundled '{TemplateKey}', "
                + "a Canvas that turns that override back into the event the original code was written against.",
            ]);
    }
}
