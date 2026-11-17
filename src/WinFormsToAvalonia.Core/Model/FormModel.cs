namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// The result of walking one Form/UserControl's InitializeComponent(), then assembling the
/// parent/child tree (ControlGraphBuilder). <see cref="Controls"/> is the flat lookup by
/// designer field name (every control, regardless of position in the tree);
/// <see cref="RootControls"/> and <see cref="Components"/> are the two disjoint partitions
/// ControlGraphBuilder derives from it: controls actually added to the Form's/a container's
/// visual tree vs. non-visual designer-only components (Timer, ImageList, ErrorProvider,
/// ...) that are never targets of a `Controls.Add` call.
/// </summary>
public sealed class FormModel
{
    public required string ClassName { get; init; }

    public string? Namespace { get; init; }

    public PropertyBag FormProperties { get; } = new();

    public List<EventHandlerBinding> FormEvents { get; } = [];

    public Dictionary<string, ControlModel> Controls { get; } = new(StringComparer.Ordinal);

    public List<ControlModel> RootControls { get; } = [];

    public List<ControlModel> Components { get; } = [];

    /// <summary>
    /// The synthetic paint surface a container's <c>Paint</c> handler was moved onto
    /// (<c>PaintLayerInserter</c>), keyed by the container's field - or by the empty string for
    /// the Form itself. Repainting has to follow the drawing: invalidating the container no longer
    /// re-renders it.
    /// </summary>
    public Dictionary<string, string> PaintLayers { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Fields of the Form's own code that hold a control it creates at run time
    /// (<c>private ToolStripStatusLabel label;</c> + <c>label = new ToolStripStatusLabel();</c>), keyed
    /// by field name. Not designer controls: nothing emits an element for them.
    /// </summary>
    /// <remarks>
    /// Filled by <c>FormMigrationPlanner</c> - not by parsing - because whether such a field may exist
    /// on the View at all depends on whether every assignment to it was translated, which is only
    /// known after the bodies are. <c>HandlerBodyRewriter</c> reads it, so everything a designer
    /// control supports works through such a field too.
    /// </remarks>
    public Dictionary<string, ControlModel> RuntimeControls { get; } = new(StringComparer.Ordinal);
}
