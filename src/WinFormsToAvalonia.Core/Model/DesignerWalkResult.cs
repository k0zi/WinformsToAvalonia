namespace WinFormsToAvalonia.Core.Model;

/// <summary>The raw facts DesignerSyntaxWalker extracts from one InitializeComponent() body, before ControlGraphBuilder assembles them into a tree.</summary>
/// <param name="Warnings">Things the walker could see but not resolve - e.g. a resource lookup with no .resx to read.</param>
/// <param name="HostedControlAliases">
/// Host field to the control it was constructed around (<c>new ToolStripControlHost(this.trackBar1)</c>).
/// Recorded rather than acted on: the host keeps taking property assignments for the rest of the
/// walk, and only ControlGraphBuilder - which owns tree assembly - collapses it away.
/// </param>
public sealed record DesignerWalkResult(
    FormModel Form,
    IReadOnlyList<ParentChildEdge> Edges,
    IReadOnlyList<string>? Warnings = null,
    IReadOnlyDictionary<string, string>? HostedControlAliases = null)
{
    public IReadOnlyList<string> Warnings { get; } = Warnings ?? [];

    public IReadOnlyDictionary<string, string> HostedControlAliases { get; } =
        HostedControlAliases ?? new Dictionary<string, string>(StringComparer.Ordinal);
}
