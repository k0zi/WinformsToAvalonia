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

    public Dictionary<string, PropertyValue> FormProperties { get; } = new(StringComparer.Ordinal);

    public List<EventHandlerBinding> FormEvents { get; } = [];

    public Dictionary<string, ControlModel> Controls { get; } = new(StringComparer.Ordinal);

    public List<ControlModel> RootControls { get; } = [];

    public List<ControlModel> Components { get; } = [];
}
