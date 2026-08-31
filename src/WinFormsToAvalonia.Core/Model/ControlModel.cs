namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A single control instance extracted from InitializeComponent(), keyed by its designer
/// field name (e.g. "button1"). <see cref="Children"/> is populated by ControlGraphBuilder
/// from `Controls.Add`/`AddRange` edges - a freshly-walked ControlModel always has an empty
/// Children list.
/// </summary>
public sealed class ControlModel
{
    public required string FieldName { get; init; }

    public required string ClrTypeName { get; init; }

    public Dictionary<string, PropertyValue> Properties { get; } = new(StringComparer.Ordinal);

    public List<EventHandlerBinding> Events { get; } = [];

    public List<ControlModel> Children { get; } = [];

    /// <summary>
    /// Literal entries the designer added to this control's <c>Items</c> collection
    /// (`comboBox1.Items.AddRange(new object[] { "A", "B" })`). Only plain literals - anything
    /// else in an Items call is either a real child control (which becomes a
    /// <see cref="Children"/> entry) or something this converter cannot resolve statically.
    /// </summary>
    public List<string> LiteralItems { get; } = [];

    /// <summary>
    /// SplitContainer-specific: children added via `this.splitContainer1.Panel1.Controls.Add(...)`.
    /// Populated by ControlGraphBuilder only when <see cref="ClrTypeName"/> is "SplitContainer" -
    /// empty (unused) for every other control type.
    /// </summary>
    public List<ControlModel> Panel1Children { get; } = [];

    /// <summary>SplitContainer-specific counterpart to <see cref="Panel1Children"/> for `Panel2`.</summary>
    public List<ControlModel> Panel2Children { get; } = [];

    /// <summary>
    /// Children added through one of a container's named sub-regions, keyed by region name -
    /// `this.toolStripContainer1.ContentPanel.Controls.Add(...)`.
    /// </summary>
    /// <remarks>
    /// The general form of <see cref="Panel1Children"/>/<see cref="Panel2Children"/>, which stay
    /// separate because a SplitContainer's two halves are emitted as a Grid with a splitter
    /// between them - a shape nothing else shares. A ToolStripContainer's five regions are
    /// emitted as themselves, so one dictionary covers them.
    /// </remarks>
    public Dictionary<string, List<ControlModel>> RegionChildren { get; } = new(StringComparer.Ordinal);
}
