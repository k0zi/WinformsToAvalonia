namespace Converter.Plugin.Abstractions;

/// <summary>
/// Defines a custom layout analyzer for detecting layout patterns.
/// </summary>
public interface ILayoutAnalyzer
{
    /// <summary>
    /// Priority for this analyzer (higher values execute first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Analyze the control hierarchy to detect layout patterns.
    /// </summary>
    Task<LayoutAnalysisResult> AnalyzeAsync(ControlNode root, LayoutAnalysisContext context);
}

/// <summary>
/// Result of layout analysis.
/// </summary>
public class LayoutAnalysisResult
{
    /// <summary>
    /// Detected layout type.
    /// </summary>
    public required LayoutType LayoutType { get; init; }

    /// <summary>
    /// Confidence score (0-100).
    /// </summary>
    public required int ConfidenceScore { get; init; }

    /// <summary>
    /// Layout-specific metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = [];

    /// <summary>
    /// Reason for the layout choice.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Child layout analyses for nested containers.
    /// </summary>
    public Dictionary<string, LayoutAnalysisResult> ChildLayouts { get; init; } = [];

    /// <summary>
    /// Per-child (row, column) cell assignment for LayoutType.Grid results produced by
    /// heuristic detection (LayoutAnalyzer.AnalyzeGridPattern), keyed by child control Name.
    /// Empty for TableLayoutPanel-derived Grid results, which carry exact designer intent via
    /// ControlNode.Properties["TableLayoutPanel.Column/Row"] instead - the two sources are
    /// mutually exclusive by construction (AnalyzeGridPattern never runs for a TableLayoutPanel
    /// root), so there's no double-source-of-truth risk.
    /// </summary>
    public Dictionary<string, GridCellAssignment> GridCellAssignments { get; init; } = [];

    /// <summary>
    /// Visual order (top-to-bottom or left-to-right, matching the winning orientation) of
    /// child control Names, as computed by LayoutAnalyzer.AnalyzeStackPattern. Only populated
    /// for LayoutType.StackPanel results. Controls without a Location are absent from this
    /// list; consumers should fall back to declaration order for them.
    /// </summary>
    public List<string> ChildOrder { get; init; } = [];
}

/// <summary>
/// A control's assigned cell within a detected Grid layout.
/// </summary>
public readonly record struct GridCellAssignment(int Row, int Column);

/// <summary>
/// Layout types supported by the converter.
/// </summary>
public enum LayoutType
{
    /// <summary>
    /// Absolute positioning using Canvas.
    /// </summary>
    Canvas,

    /// <summary>
    /// Grid layout with rows and columns.
    /// </summary>
    Grid,

    /// <summary>
    /// Vertical or horizontal stack.
    /// </summary>
    StackPanel,

    /// <summary>
    /// Dock-based layout.
    /// </summary>
    DockPanel,

    /// <summary>
    /// Wrap panel for flowing content.
    /// </summary>
    WrapPanel,

    /// <summary>
    /// Custom layout defined by plugin.
    /// </summary>
    Custom
}

/// <summary>
/// Context for layout analysis operations.
/// </summary>
public class LayoutAnalysisContext
{
    /// <summary>
    /// Alignment tolerance in pixels for grid detection.
    /// </summary>
    public int AlignmentTolerance { get; init; } = 5;

    /// <summary>
    /// Minimum confidence threshold for layout selection.
    /// </summary>
    public int ConfidenceThreshold { get; init; } = 70;

    /// <summary>
    /// Preferred layout mode.
    /// </summary>
    public LayoutMode Mode { get; init; } = LayoutMode.Auto;

    /// <summary>
    /// Weight applied to Grid pattern confidence when selecting the best-scoring layout.
    /// </summary>
    public double GridWeight { get; init; } = 1.0;

    /// <summary>
    /// Weight applied to StackPanel pattern confidence when selecting the best-scoring layout.
    /// </summary>
    public double StackWeight { get; init; } = 1.0;

    /// <summary>
    /// Weight applied to DockPanel pattern confidence when selecting the best-scoring layout.
    /// </summary>
    public double DockWeight { get; init; } = 1.0;

    /// <summary>
    /// Additional options.
    /// </summary>
    public Dictionary<string, object> Options { get; init; } = [];

    /// <summary>
    /// Service provider for DI.
    /// </summary>
    public IServiceProvider? Services { get; init; }
}

/// <summary>
/// Layout detection mode.
/// </summary>
public enum LayoutMode
{
    /// <summary>
    /// Automatically detect best layout.
    /// </summary>
    Auto,

    /// <summary>
    /// Force Canvas (pixel-perfect) layout.
    /// </summary>
    Canvas,

    /// <summary>
    /// Prefer smart layouts (Grid, StackPanel, etc.).
    /// </summary>
    Smart
}
