using System.Text.RegularExpressions;
using Converter.Plugin.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Core.Analysis;

/// <summary>
/// Analyzes control layouts to detect patterns.
/// </summary>
public class LayoutAnalyzer
{
    private readonly ILogger<LayoutAnalyzer>? _logger;

    public LayoutAnalyzer(ILogger<LayoutAnalyzer>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyze a control tree to detect layout patterns.
    /// </summary>
    public async Task<LayoutAnalysisResult> AnalyzeAsync(ControlNode root, LayoutAnalysisContext context)
    {
        // If Canvas mode is forced, return immediately
        if (context.Mode == LayoutMode.Canvas)
        {
            return new LayoutAnalysisResult
            {
                LayoutType = LayoutType.Canvas,
                ConfidenceScore = 100,
                Reason = "Canvas mode forced by user"
            };
        }

        // WinForms layout-container types that already declare their own layout intent -
        // re-deriving it from child pixel positions would be strictly worse information
        // than what the container type itself tells us.
        switch (root.ControlType)
        {
            case "TableLayoutPanel":
                return new LayoutAnalysisResult
                {
                    LayoutType = LayoutType.Grid,
                    ConfidenceScore = 100,
                    Reason = "TableLayoutPanel declares Grid layout explicitly"
                };
            case "FlowLayoutPanel":
                return new LayoutAnalysisResult
                {
                    LayoutType = LayoutType.WrapPanel,
                    ConfidenceScore = 100,
                    Reason = "FlowLayoutPanel declares WrapPanel layout explicitly"
                };
            case "SplitContainer":
                return new LayoutAnalysisResult
                {
                    LayoutType = LayoutType.Grid,
                    ConfidenceScore = 100,
                    Reason = "SplitContainer maps to a Grid with a GridSplitter"
                };
        }

        // Analyze children to detect patterns
        var children = root.Children.Where(c => IsVisibleControl(c)).ToList();

        if (children.Count == 0)
        {
            return new LayoutAnalysisResult
            {
                LayoutType = LayoutType.Canvas,
                ConfidenceScore = 100,
                Reason = "No child controls"
            };
        }

        // Check for DockPanel pattern
        var dockResult = AnalyzeDockPattern(children, context);

        // Check for Grid pattern
        var gridResult = AnalyzeGridPattern(children, context);

        // Check for StackPanel pattern
        var stackResult = AnalyzeStackPattern(children, context);

        // Select best pattern based on confidence, weighted by the configured per-pattern
        // preference (ConverterConfig.LayoutDetection's *DetectionWeight settings).
        var weighted = new[]
        {
            (Result: dockResult, Score: dockResult.ConfidenceScore * context.DockWeight),
            (Result: gridResult, Score: gridResult.ConfidenceScore * context.GridWeight),
            (Result: stackResult, Score: stackResult.ConfidenceScore * context.StackWeight)
        };

        var bestResult = weighted.OrderByDescending(w => w.Score).First().Result;

        // If confidence is below threshold, fallback to Canvas
        if (bestResult.ConfidenceScore < context.ConfidenceThreshold)
        {
            _logger?.LogInformation(
                "Layout confidence {Score} below threshold {Threshold}, using Canvas fallback",
                bestResult.ConfidenceScore, context.ConfidenceThreshold);

            return new LayoutAnalysisResult
            {
                LayoutType = LayoutType.Canvas,
                ConfidenceScore = 100,
                Reason = $"Confidence below threshold. Best match was {bestResult.LayoutType} ({bestResult.ConfidenceScore}%)"
            };
        }

        _logger?.LogInformation(
            "Detected {LayoutType} with {Score}% confidence",
            bestResult.LayoutType, bestResult.ConfidenceScore);

        // Recursively analyze children
        foreach (var child in children.Where(c => c.Children.Count > 0))
        {
            var childResult = await AnalyzeAsync(child, context);
            bestResult.ChildLayouts[child.Name] = childResult;
        }

        return bestResult;
    }

    private LayoutAnalysisResult AnalyzeDockPattern(List<ControlNode> controls, LayoutAnalysisContext context)
    {
        var dockedControls = controls.Count(c => 
            c.Properties.ContainsKey("Dock") && 
            c.Properties["Dock"].Value?.ToString() != "None");

        var confidence = dockedControls > 0 
            ? (int)((dockedControls / (double)controls.Count) * 100)
            : 0;

        return new LayoutAnalysisResult
        {
            LayoutType = LayoutType.DockPanel,
            ConfidenceScore = confidence,
            Reason = $"{dockedControls}/{controls.Count} controls have Dock property set",
            Metadata = new Dictionary<string, object>
            {
                ["DockedControlsCount"] = dockedControls
            }
        };
    }

    private LayoutAnalysisResult AnalyzeGridPattern(List<ControlNode> controls, LayoutAnalysisContext context)
    {
        if (controls.Count < 2)
        {
            return new LayoutAnalysisResult
            {
                LayoutType = LayoutType.Grid,
                ConfidenceScore = 0,
                Reason = "Not enough controls for grid detection"
            };
        }

        // Extract positions
        var positions = controls
            .Where(c => c.Properties.ContainsKey("Location"))
            .Select(c => new
            {
                Control = c,
                X = ParseCoordinate(c.Properties["Location"].Value?.ToString(), 0),
                Y = ParseCoordinate(c.Properties["Location"].Value?.ToString(), 1)
            })
            .ToList();

        if (positions.Count < 2)
        {
            return new LayoutAnalysisResult
            {
                LayoutType = LayoutType.Grid,
                ConfidenceScore = 0,
                Reason = "Not enough position data"
            };
        }

        // Detect rows (unique Y coordinates with tolerance)
        var rows = DetectGridLines(positions.Select(p => p.Y).ToList(), context.AlignmentTolerance);
        
        // Detect columns (unique X coordinates with tolerance)
        var columns = DetectGridLines(positions.Select(p => p.X).ToList(), context.AlignmentTolerance);

        var totalCells = rows.Count * columns.Count;
        var alignedControls = positions.Count(p =>
            IsAlignedToGrid(p.X, p.Y, columns, rows, context.AlignmentTolerance));

        var confidence = totalCells > 0
            ? (int)((alignedControls / (double)positions.Count) * 100)
            : 0;

        var reasonSuffix = "";

        // Overlapping bounding boxes contradict Grid semantics (cells shouldn't overlap) -
        // more likely a Canvas with z-ordered/absolute-positioned controls. Applied before
        // the anchor boost below so the boost isn't silently clamped away by an already-
        // saturated (100%) base score before the penalty ever gets a chance to apply.
        if (HasOverlappingControls(controls))
        {
            confidence = Math.Max(0, confidence - 30);
            reasonSuffix += "; some controls overlap (penalized)";
        }

        // Controls with a multi-edge Anchor (e.g. "Top, Left, Right") signal an intent to
        // stretch/resize with the parent, which Grid expresses naturally and Canvas (fixed
        // pixel position) cannot - treat it as a supporting signal for Grid.
        var anchoredControls = controls.Count(HasResizingAnchor);
        if (anchoredControls > 0)
        {
            var anchorBoost = (int)((anchoredControls / (double)controls.Count) * 20);
            confidence = Math.Min(100, confidence + anchorBoost);
            reasonSuffix += $"; {anchoredControls}/{controls.Count} controls have a resizing Anchor";
        }

        return new LayoutAnalysisResult
        {
            LayoutType = LayoutType.Grid,
            ConfidenceScore = confidence,
            Reason = $"{alignedControls}/{positions.Count} controls aligned to {rows.Count}x{columns.Count} grid{reasonSuffix}",
            Metadata = new Dictionary<string, object>
            {
                ["Rows"] = rows.Count,
                ["Columns"] = columns.Count,
                ["AlignedControls"] = alignedControls
            }
        };
    }

    private LayoutAnalysisResult AnalyzeStackPattern(List<ControlNode> controls, LayoutAnalysisContext context)
    {
        if (controls.Count < 2)
        {
            return new LayoutAnalysisResult
            {
                LayoutType = LayoutType.StackPanel,
                ConfidenceScore = 0,
                Reason = "Not enough controls for stack detection"
            };
        }

        var positions = controls
            .Where(c => c.Properties.ContainsKey("Location"))
            .Select(c => new
            {
                Control = c,
                X = ParseCoordinate(c.Properties["Location"].Value?.ToString(), 0),
                Y = ParseCoordinate(c.Properties["Location"].Value?.ToString(), 1)
            })
            .OrderBy(p => p.Y)
            .ThenBy(p => p.X)
            .ToList();

        if (positions.Count < 2)
        {
            return new LayoutAnalysisResult
            {
                LayoutType = LayoutType.StackPanel,
                ConfidenceScore = 0,
                Reason = "Not enough position data"
            };
        }

        // Check for vertical stacking
        var verticallyStacked = positions
            .Zip(positions.Skip(1), (a, b) => Math.Abs(a.X - b.X) < context.AlignmentTolerance)
            .Count(aligned => aligned);

        var verticalConfidence = positions.Count > 1
            ? (int)((verticallyStacked / (double)(positions.Count - 1)) * 100)
            : 0;

        // Check for horizontal stacking
        var sortedByX = positions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        var horizontallyStacked = sortedByX
            .Zip(sortedByX.Skip(1), (a, b) => Math.Abs(a.Y - b.Y) < context.AlignmentTolerance)
            .Count(aligned => aligned);

        var horizontalConfidence = positions.Count > 1
            ? (int)((horizontallyStacked / (double)(positions.Count - 1)) * 100)
            : 0;

        var isVertical = verticalConfidence > horizontalConfidence;
        var confidence = Math.Max(verticalConfidence, horizontalConfidence);
        var reasonSuffix = "";

        // Overlapping bounding boxes contradict Stack semantics (items shouldn't overlap).
        if (HasOverlappingControls(controls))
        {
            confidence = Math.Max(0, confidence - 30);
            reasonSuffix = "; some controls overlap (penalized)";
        }

        return new LayoutAnalysisResult
        {
            LayoutType = LayoutType.StackPanel,
            ConfidenceScore = confidence,
            Reason = (isVertical
                ? $"Vertical stack: {verticallyStacked}/{positions.Count - 1} controls aligned"
                : $"Horizontal stack: {horizontallyStacked}/{positions.Count - 1} controls aligned") + reasonSuffix,
            Metadata = new Dictionary<string, object>
            {
                ["Orientation"] = isVertical ? "Vertical" : "Horizontal",
                ["AlignedCount"] = isVertical ? verticallyStacked : horizontallyStacked
            }
        };
    }

    private List<int> DetectGridLines(List<int> coordinates, int tolerance)
    {
        var lines = new List<int>();
        var sorted = coordinates.OrderBy(c => c).ToList();

        foreach (var coord in sorted)
        {
            if (!lines.Any(line => Math.Abs(line - coord) <= tolerance))
            {
                lines.Add(coord);
            }
        }

        return lines;
    }

    private bool IsAlignedToGrid(int x, int y, List<int> columns, List<int> rows, int tolerance)
    {
        var alignedX = columns.Any(col => Math.Abs(col - x) <= tolerance);
        var alignedY = rows.Any(row => Math.Abs(row - y) <= tolerance);
        return alignedX && alignedY;
    }

    private static readonly Regex TwoIntPairPattern = new(@"\(\s*(?<a>-?\d+)\s*,\s*(?<b>-?\d+)\s*\)", RegexOptions.Compiled);

    /// <summary>
    /// Extracts an (x, y) or (width, height) pair from a raw captured value such as
    /// "new Point(10, 10)" or "new System.Drawing.Size(75, 23)". Matches the "(int, int)"
    /// shape directly rather than string-replacing a specific "new Point(" prefix, since
    /// real WinForms designer code emits the fully-qualified System.Drawing.Point/Size
    /// type name, which a literal "new Point(" replace never matches.
    /// </summary>
    private int ParseCoordinate(string? value, int index)
    {
        if (string.IsNullOrEmpty(value)) return 0;

        var match = TwoIntPairPattern.Match(value);
        if (!match.Success) return 0;

        var group = index == 0 ? "a" : "b";
        return int.TryParse(match.Groups[group].Value, out var parsed) ? parsed : 0;
    }

    private readonly record struct BoundingBox(int X, int Y, int Width, int Height)
    {
        public bool Overlaps(BoundingBox other) =>
            Width > 0 && Height > 0 && other.Width > 0 && other.Height > 0 &&
            X < other.X + other.Width && X + Width > other.X &&
            Y < other.Y + other.Height && Y + Height > other.Y;
    }

    private BoundingBox GetBoundingBox(ControlNode control)
    {
        var location = control.Properties.GetValueOrDefault("Location")?.Value?.ToString();
        var size = control.Properties.GetValueOrDefault("Size")?.Value?.ToString();

        return new BoundingBox(
            X: ParseCoordinate(location, 0),
            Y: ParseCoordinate(location, 1),
            Width: ParseCoordinate(size, 0),
            Height: ParseCoordinate(size, 1));
    }

    /// <summary>
    /// True if any two controls' bounding boxes (Location + Size) intersect. Only
    /// meaningful for controls that have both properties captured, so controls missing a
    /// Size (Width/Height parse to 0) never register an overlap.
    /// </summary>
    private bool HasOverlappingControls(List<ControlNode> controls)
    {
        var boxes = controls
            .Where(c => c.Properties.ContainsKey("Location") && c.Properties.ContainsKey("Size"))
            .Select(GetBoundingBox)
            .ToList();

        for (var i = 0; i < boxes.Count; i++)
        {
            for (var j = i + 1; j < boxes.Count; j++)
            {
                if (boxes[i].Overlaps(boxes[j]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static readonly string[] AnchorEdges = ["Top", "Bottom", "Left", "Right"];

    /// <summary>
    /// True when a control's Anchor spans 3+ edges (e.g. "Top, Left, Right"), signaling an
    /// intent to stretch/resize with its parent rather than stay a fixed size - the
    /// WinForms default is Top+Left only (2 edges, no stretch), so 3+ is a meaningful
    /// non-default signal.
    /// </summary>
    private bool HasResizingAnchor(ControlNode control)
    {
        if (!control.Properties.TryGetValue("Anchor", out var anchorValue))
        {
            return false;
        }

        var raw = anchorValue.Value?.ToString() ?? string.Empty;
        var edgeCount = AnchorEdges.Count(edge => raw.Contains(edge, StringComparison.Ordinal));

        return edgeCount >= 3;
    }

    private bool IsVisibleControl(ControlNode control)
    {
        // Skip controls that are typically not visible layout containers
        return !control.ControlType.Contains("Component") &&
               !control.ControlType.Contains("ContextMenu") &&
               !control.ControlType.Contains("Timer");
    }
}
