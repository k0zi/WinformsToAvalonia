namespace WinFormsToAvalonia.Core.Model;

/// <summary>The raw facts DesignerSyntaxWalker extracts from one InitializeComponent() body, before ControlGraphBuilder assembles them into a tree.</summary>
/// <param name="Warnings">Things the walker could see but not resolve - e.g. a resource lookup with no .resx to read.</param>
/// <param name="HostedControlAliases">
/// Host field to the control it was constructed around (<c>new ToolStripControlHost(this.trackBar1)</c>).
/// Recorded rather than acted on: the host keeps taking property assignments for the rest of the
/// walk, and only ControlGraphBuilder - which owns tree assembly - collapses it away.
/// </param>
/// <param name="VendorLinks">
/// What a vendor control's collections hold that is not a plain child - a DevExpress bar's
/// <c>LinksPersistInfo</c>, a ribbon group's <c>ItemLinks</c>, a Telerik drop-down's item objects.
/// Left for <c>VendorDesignerNormalizer</c>, which knows what each one becomes.
/// </param>
public sealed record DesignerWalkResult(
    FormModel Form,
    IReadOnlyList<ParentChildEdge> Edges,
    IReadOnlyList<string>? Warnings = null,
    IReadOnlyDictionary<string, string>? HostedControlAliases = null,
    IReadOnlyList<VendorLink>? VendorLinks = null)
{
    public IReadOnlyList<string> Warnings { get; } = Warnings ?? [];

    public IReadOnlyList<VendorLink> VendorLinks { get; } = VendorLinks ?? [];

    public IReadOnlyDictionary<string, string> HostedControlAliases { get; } =
        HostedControlAliases ?? new Dictionary<string, string>(StringComparer.Ordinal);
}
