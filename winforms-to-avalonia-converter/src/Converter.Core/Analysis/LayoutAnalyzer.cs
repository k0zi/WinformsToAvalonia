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

        // Select best pattern based on confidence
        var results = new[] { dockResult, gridResult, stackResult }
            .OrderByDescending(r => r.ConfidenceScore)
            .ToList();

        var bestResult = results.First();

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

        return new LayoutAnalysisResult
        {
            LayoutType = LayoutType.Grid,
            ConfidenceScore = confidence,
            Reason = $"{alignedControls}/{positions.Count} controls aligned to {rows.Count}x{columns.Count} grid",
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

        return new LayoutAnalysisResult
        {
            LayoutType = LayoutType.StackPanel,
            ConfidenceScore = confidence,
            Reason = isVertical
                ? $"Vertical stack: {verticallyStacked}/{positions.Count - 1} controls aligned"
                : $"Horizontal stack: {horizontallyStacked}/{positions.Count - 1} controls aligned",
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

    private int ParseCoordinate(string? location, int index)
    {
        if (string.IsNullOrEmpty(location)) return 0;

        // Parse "new Point(x, y)" or "x, y"
        var cleaned = location.Replace("new Point(", "").Replace(")", "").Trim();
        var parts = cleaned.Split(',');

        if (parts.Length > index && int.TryParse(parts[index].Trim(), out var value))
        {
            return value;
        }

        return 0;
    }

    private bool IsVisibleControl(ControlNode control)
    {
        // Skip controls that are typically not visible layout containers
        return !control.ControlType.Contains("Component") &&
               !control.ControlType.Contains("ContextMenu") &&
               !control.ControlType.Contains("Timer");
    }
}
