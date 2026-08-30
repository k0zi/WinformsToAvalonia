namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A fixed nested element the mapper itself prescribes, independent of the WinForms control's
/// own children - e.g. a DataGridTemplateColumn's
/// `&lt;DataGridTemplateColumn.CellTemplate&gt;&lt;DataTemplate&gt;&lt;Button /&gt;`. Emitted
/// recursively by AxamlEmitter after the element's own attributes and before its children.
/// <see cref="Comment"/>, when set, is written as an XML comment just inside the element -
/// used to tell the human what still needs hand-wiring (a cell template has no binding path,
/// since Designer.cs never records one).
/// </summary>
public sealed record AxamlElementSpec(
    string ElementName,
    IReadOnlyDictionary<string, string>? Attributes = null,
    IReadOnlyList<AxamlElementSpec>? Children = null,
    string? Comment = null)
{
    public IReadOnlyDictionary<string, string> Attributes { get; } =
        Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<AxamlElementSpec> Children { get; } = Children ?? [];
}

/// <summary>
/// The result of mapping one WinForms <see cref="ControlModel"/> to its Avalonia
/// equivalent. <see cref="AvaloniaElementName"/> is the target control's XAML element name
/// (null only for <see cref="MappingStatus.Unsupported"/>); <see cref="FallbackTemplateKey"/>
/// is set when <see cref="Status"/> is <see cref="MappingStatus.Fallback"/> and identifies
/// which bundled fallback control template to copy into the generated project's Controls/
/// folder (resolved against FallbackControlCatalog). <see cref="ChildWrapperElementNames"/>
/// is set for targets that can't host multiple direct children the way a Panel can (e.g.
/// Avalonia's TabItem has a single Content) - when set, AxamlEmitter nests this control's
/// children inside that chain of synthetic elements instead of nesting them directly. It is
/// a chain rather than a single name because some targets need two levels
/// (ToolStripDropDownButton -> `Button.Flyout` > `MenuFlyout` > items).
/// <see cref="NestedElements"/> is a fixed element subtree the mapper prescribes regardless
/// of the control's own children (see <see cref="AxamlElementSpec"/>).
/// <see cref="RequiredNuGetPackage"/> is set when the target element ships in a separate
/// official Avalonia package (e.g. DataGrid) that must be added to the generated project's
/// csproj only when actually used. <see cref="SupportsName"/> is false for targets that
/// aren't a Visual/StyledElement at all (e.g. DataGrid's column types, which live in
/// DataGrid.Columns as plain objects, not the visual tree) - Avalonia rejects x:Name on
/// those with a compile-time AVLN2000 error, so AxamlEmitter must skip it.
/// </summary>
public sealed record MappedControl(
    string SourceClrTypeName,
    MappingStatus Status,
    string? AvaloniaElementName,
    IReadOnlyDictionary<string, string> Attributes,
    string? FallbackTemplateKey,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string>? ChildWrapperElementNames = null,
    string? RequiredNuGetPackage = null,
    bool SupportsName = true,
    IReadOnlyList<AxamlElementSpec>? NestedElements = null,
    /// <summary>
    /// Members <see cref="Mapping.BindablePropertyCatalog"/> lists for the WinForms type that this
    /// particular target does <i>not</i> have.
    /// </summary>
    /// <remarks>
    /// The catalog is keyed on the WinForms type alone, which is fine while one type means one
    /// element - but a per-instance mapper can choose between two. A DateTimePicker's
    /// <c>Value</c> is a <c>CalendarDatePicker.SelectedDate</c> or a <c>TimePicker.SelectedTime</c>
    /// depending on its Format, and emitting the first against the second is a CS1061 in the
    /// *generated* project, which nothing here would otherwise catch. A mapper that narrows the
    /// element narrows this too.
    /// </remarks>
    IReadOnlyList<string>? UnreachableBindableMembers = null)
{
    public IReadOnlyList<string> ChildWrapperElementNames { get; } = ChildWrapperElementNames ?? [];

    public IReadOnlyList<AxamlElementSpec> NestedElements { get; } = NestedElements ?? [];

    public IReadOnlyList<string> UnreachableBindableMembers { get; } = UnreachableBindableMembers ?? [];
}
