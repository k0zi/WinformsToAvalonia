namespace WinFormsToAvalonia.Core.Model;

/// <summary>The raw facts DesignerSyntaxWalker extracts from one InitializeComponent() body, before ControlGraphBuilder assembles them into a tree.</summary>
/// <param name="Warnings">Things the walker could see but not resolve - e.g. a resource lookup with no .resx to read.</param>
public sealed record DesignerWalkResult(
    FormModel Form,
    IReadOnlyList<ParentChildEdge> Edges,
    IReadOnlyList<string>? Warnings = null)
{
    public IReadOnlyList<string> Warnings { get; } = Warnings ?? [];
}
