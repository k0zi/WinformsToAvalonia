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

    /// <remarks>
    /// Settable for one reason: a control whose type is the project's own subclass of a WinForms
    /// control (<c>MyButton : Button</c>) is re-typed to that base right after the designer walk, so
    /// every table downstream answers for the control it really is. The written name is kept in
    /// <see cref="DeclaredClrTypeName"/>.
    /// </remarks>
    public required string ClrTypeName { get; set; }

    /// <summary>
    /// The type the designer actually wrote, when <see cref="ClrTypeName"/> is the WinForms control
    /// it derives from rather than itself; null otherwise.
    /// </summary>
    public string? DeclaredClrTypeName { get; set; }

    /// <summary>
    /// The type exactly as the designer wrote it in <c>new T(...)</c> - namespace and all, when it
    /// was written with one (<c>DevExpress.XtraEditors.TextEdit</c>). What identifies a vendor
    /// control: <c>LayoutControlItem</c> is a DevExpress type and a Telerik one.
    /// </summary>
    public string? WrittenTypeName { get; init; }

    /// <summary>
    /// The full name of the built-in vendor entry this control was converted through, once it has
    /// been - null for every WinForms control and every user-mapped one.
    /// </summary>
    public string? VendorTypeName { get; set; }

    /// <summary>
    /// For a vendor control: the only members a handler body may name on it, each with the WinForms
    /// member it stands for. Proved to have the same type on both sides - a member the vendor shares
    /// a name with but not a type (Telerik's float <c>RadTrackBar.Value</c>) is absent, so it
    /// refuses rather than translating into a build error. Null for a WinForms control.
    /// </summary>
    public IReadOnlyDictionary<string, string>? VendorMembers { get; set; }

    /// <summary>
    /// A model with no field behind it: a vendor designer's local variable
    /// (<c>GridViewTextBoxColumn col1 = new ...;</c>), or one the conversion synthesized. Such a
    /// control gets no <c>x:Name</c> - nothing in code could name it - and its key is not an
    /// identifier, so no handler body can resolve it either.
    /// </summary>
    public bool IsDesignerLocal { get; init; }

    public PropertyBag Properties { get; } = new();

    /// <summary>
    /// For a value an extender provider set on this control (<c>toolTip1.SetToolTip(this, ...)</c>),
    /// the provider's field, keyed by the property key the value sits under - so the provider's own
    /// settings (a ToolTip's delays) can follow its value onto this control.
    /// </summary>
    public Dictionary<string, string> ExtenderProviders { get; } = new(StringComparer.Ordinal);

    public List<EventHandlerBinding> Events { get; } = [];

    /// <summary>
    /// ImageList-specific: the key of each image, by index - the designer's
    /// <c>this.imageList1.Images.SetKeyName(0, "folder.png")</c>. The keys are not in the
    /// <c>ImageStream</c> payload at all, so this is the only place an <c>ImageKey</c> can be resolved.
    /// </summary>
    public Dictionary<string, int> ImageKeys { get; } = new(StringComparer.Ordinal);

    public List<ControlModel> Children { get; } = [];

    /// <summary>
    /// The <c>TableLayoutPanel</c> cell the designer put this control in -
    /// <c>this.tableLayoutPanel1.Controls.Add(this.label1, 2, 0)</c> - when that is the only thing
    /// that placed it. Null when it was added without a cell, or when a later
    /// <c>SetColumn</c>/<c>SetRow</c>/<c>SetCellPosition</c>/<c>SetColumnSpan</c>/<c>SetRowSpan</c>
    /// made the cell, or the area it covers, something else (<see cref="TableCellOverridden"/>).
    /// </summary>
    public TableCellPosition? TableCell => TableCellOverridden ? null : DesignerTableCell;

    /// <summary>The cell from the three-argument <c>Controls.Add</c>, before any override is considered.</summary>
    public TableCellPosition? DesignerTableCell { get; set; }

    /// <summary>A designer call changed this control's cell or span after it was added.</summary>
    public bool TableCellOverridden { get; set; }

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
