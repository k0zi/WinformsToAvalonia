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
}

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
