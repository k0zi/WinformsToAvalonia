using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Gives a <c>Paint</c> handler on a <em>container</em> - a Form, a UserControl, a GroupBox or a
/// Panel that holds controls - somewhere to draw: a synthetic, childless <c>Panel</c> laid under the
/// container's children, which the ordinary paint-surface path then turns into the bundled
/// <c>PaintSurfaceFallback</c>.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia draws by overriding <c>Render</c>, and <c>Panel.Render</c> is sealed - so a container
/// cannot draw and contain at once, and the paint surface was only ever used for leaf controls. But
/// a WinForms container's Paint draws <em>underneath</em> its children, and an element placed first
/// in the same Canvas, at its origin and its size, is exactly that: drawn before every child and in
/// the coordinate system the children are positioned in. Whatever offset the Avalonia container
/// puts on its content (a GroupBox's header), the drawing and the children share it.
/// </para>
/// <para>
/// This is a rewrite of the model, not of the emitters: once the layer is an ordinary control with
/// an ordinary Paint binding, the mapper, the planner's subscription and the <c>e.Graphics</c>
/// translation need nothing new. The container keeps every other event; only Paint moves. The
/// handler's <c>sender</c> becomes the layer, which is why a body casting <c>sender</c> to the
/// container type refuses rather than translating.
/// </para>
/// <para>
/// <see cref="FormModel.PaintLayers"/> records the move, because repainting has to follow it:
/// <c>groupBox1.Invalidate()</c> re-renders only the group box itself in Avalonia, not the layer
/// inside it.
/// </para>
/// </remarks>
public static class PaintLayerInserter
{
    /// <summary>The container types whose children the emitter positions in a Canvas the layer can join.</summary>
    private static readonly HashSet<string> LayerableContainers = new(StringComparer.Ordinal) { "Panel", "GroupBox" };

    /// <param name="takenNames">Members the class already declares; the layer's field name avoids them.</param>
    /// <param name="isUserControl">Whether the Form's own size is <c>Size</c> rather than <c>ClientSize</c>.</param>
    /// <returns>One note per layer inserted.</returns>
    public static IReadOnlyList<string> Insert(FormModel formModel, IReadOnlySet<string> takenNames, bool isUserControl)
    {
        var notes = new List<string>();
        var taken = new HashSet<string>(takenNames, StringComparer.Ordinal);
        taken.UnionWith(formModel.Controls.Keys);

        string NameFor(string owner)
        {
            var name = owner + "PaintLayer";
            for (var attempt = 2; !taken.Add(name); attempt++)
            {
                name = owner + "PaintLayer" + attempt.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return name;
        }

        if (formModel.FormEvents.Any(IsPaint)
            && formModel.FormProperties.TryGetValue(isUserControl ? "Size" : "ClientSize", out var formSizeValue)
            && formSizeValue is PropertyValue.SizeValue formSize)
        {
            var layer = CreateLayer(NameFor(char.ToLowerInvariant(formModel.ClassName[0]) + formModel.ClassName[1..]), formSize, formModel.FormEvents);
            formModel.Controls[layer.FieldName] = layer;
            formModel.RootControls.Insert(0, layer);
            formModel.PaintLayers[""] = layer.FieldName;
            notes.Add(
                $"'{formModel.ClassName}' draws in a Paint handler, which Avalonia has no event for; it now draws on "
                + $"'{layer.FieldName}', a paint surface laid under every control at the {(isUserControl ? "control's" : "window's")} "
                + "designed size - it does not follow a resize.");
        }

        foreach (var container in formModel.Controls.Values
                     .Where(c => LayerableContainers.Contains(c.ClrTypeName) && c.Children.Count > 0 && c.Events.Any(IsPaint))
                     .OrderBy(c => c.FieldName, StringComparer.Ordinal)
                     .ToList())
        {
            if (!container.Properties.TryGetValue("Size", out var sizeValue) || sizeValue is not PropertyValue.SizeValue size)
            {
                continue;
            }

            var layer = CreateLayer(NameFor(container.FieldName), size, container.Events);
            formModel.Controls[layer.FieldName] = layer;
            container.Children.Insert(0, layer);
            formModel.PaintLayers[container.FieldName] = layer.FieldName;
            notes.Add(
                $"'{container.FieldName}' ({container.ClrTypeName}) both contains controls and draws in a Paint handler, "
                + $"and an Avalonia control can do one or the other; the drawing moved onto '{layer.FieldName}', a paint "
                + "surface laid under its children in the same coordinates.");
        }

        return notes;
    }

    private static bool IsPaint(EventHandlerBinding binding) =>
        string.Equals(binding.EventName, "Paint", StringComparison.Ordinal) && binding.HandlerMethodName is not null;

    /// <summary>The layer, carrying the owner's Paint bindings, which leave the owner.</summary>
    private static ControlModel CreateLayer(string fieldName, PropertyValue.SizeValue size, List<EventHandlerBinding> ownerEvents)
    {
        var layer = new ControlModel { FieldName = fieldName, ClrTypeName = "Panel" };
        layer.Properties["Location"] = new PropertyValue.PointValue(0, 0);
        layer.Properties["Size"] = size;

        layer.Events.AddRange(ownerEvents.Where(IsPaint));
        ownerEvents.RemoveAll(IsPaint);
        return layer;
    }
}
