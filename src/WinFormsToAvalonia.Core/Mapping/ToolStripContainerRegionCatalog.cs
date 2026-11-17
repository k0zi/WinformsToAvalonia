namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The five fixed regions a WinForms <c>ToolStripContainer</c> is made of, and the bundled
/// template each one becomes.
/// </summary>
/// <remarks>
/// <para>
/// A ToolStripContainer has no children of its own - everything goes into one of these, through
/// <c>this.toolStripContainer1.ContentPanel.Controls.Add(...)</c>. That is a three-level member
/// access, which the designer walker encodes the same way it already encodes a SplitContainer's
/// <c>Panel1</c>/<c>Panel2</c>: a synthetic <c>"field.Region"</c> parent id.
/// </para>
/// <para>
/// One table, read from both ends: the walker asks whether a name is a region at all, and the
/// emitter asks which element to wrap the region's children in. The property name is the same on
/// both sides, because <c>ToolStripContainerFallback</c> deliberately mirrors the WinForms API.
/// </para>
/// </remarks>
public static class ToolStripContainerRegionCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Regions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TopToolStripPanel"] = "ToolStripPanelFallback",
            ["BottomToolStripPanel"] = "ToolStripPanelFallback",
            ["LeftToolStripPanel"] = "ToolStripPanelFallback",
            ["RightToolStripPanel"] = "ToolStripPanelFallback",
            ["ContentPanel"] = "ToolStripContentPanelFallback",
        };

    /// <summary>Whether this member name is one of the container's regions.</summary>
    public static bool IsRegion(string memberName) => Regions.ContainsKey(memberName);

    /// <summary>The bundled template a region's children are wrapped in.</summary>
    public static bool TryGetTemplateKey(string regionName, out string templateKey) =>
        Regions.TryGetValue(regionName, out templateKey!);

    /// <summary>
    /// Every region, in the order they are emitted - which matches the order
    /// <c>ToolStripContainerFallback</c> docks them, so the AXAML reads the way the layout works.
    /// </summary>
    public static IEnumerable<(string RegionName, string TemplateKey)> All =>
        Regions.Select(e => (e.Key, e.Value));
}
