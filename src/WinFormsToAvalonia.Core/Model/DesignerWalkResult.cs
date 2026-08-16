namespace WinFormsToAvalonia.Core.Model;

/// <summary>The raw facts DesignerSyntaxWalker extracts from one InitializeComponent() body, before ControlGraphBuilder assembles them into a tree.</summary>
public sealed record DesignerWalkResult(FormModel Form, IReadOnlyList<ParentChildEdge> Edges);
