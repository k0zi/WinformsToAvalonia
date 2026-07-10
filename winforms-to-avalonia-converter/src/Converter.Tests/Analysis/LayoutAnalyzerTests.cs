using Converter.Core.Analysis;
using Converter.Plugin.Abstractions;

namespace Converter.Tests.Analysis;

public class LayoutAnalyzerTests
{
    private static ControlNode BuildControl(string name, string controlType, ControlNode? parent = null,
        string? location = null, string? size = null, string? dock = null, string? anchor = null)
    {
        var node = new ControlNode
        {
            ControlType = controlType,
            FullTypeName = $"System.Windows.Forms.{controlType}",
            Name = name,
            Parent = parent
        };

        if (location != null)
            node.Properties["Location"] = new PropertyValue { Name = "Location", Value = location, Type = "object" };
        if (size != null)
            node.Properties["Size"] = new PropertyValue { Name = "Size", Value = size, Type = "object" };
        if (dock != null)
            node.Properties["Dock"] = new PropertyValue { Name = "Dock", Value = dock, Type = "object" };
        if (anchor != null)
            node.Properties["Anchor"] = new PropertyValue { Name = "Anchor", Value = anchor, Type = "object" };

        parent?.Children.Add(node);
        return node;
    }

    [Fact]
    public async Task AnalyzeAsync_TableLayoutPanelRoot_AlwaysReturnsGrid_RegardlessOfChildPositions()
    {
        var root = BuildControl("tableLayoutPanel1", "TableLayoutPanel");

        // Deliberately scattered, non-grid-aligned child positions - must be irrelevant,
        // since a TableLayoutPanel already declares Grid layout by its type.
        BuildControl("label1", "Label", root, location: "new System.Drawing.Point(3, 137)");
        BuildControl("label2", "Label", root, location: "new System.Drawing.Point(211, 9)");

        var result = await new LayoutAnalyzer().AnalyzeAsync(root, new LayoutAnalysisContext());

        Assert.Equal(LayoutType.Grid, result.LayoutType);
        Assert.Equal(100, result.ConfidenceScore);
    }

    [Fact]
    public async Task AnalyzeAsync_FlowLayoutPanelRoot_AlwaysReturnsWrapPanel()
    {
        var root = BuildControl("flowLayoutPanel1", "FlowLayoutPanel");
        BuildControl("button1", "Button", root, location: "new System.Drawing.Point(0, 0)");

        var result = await new LayoutAnalyzer().AnalyzeAsync(root, new LayoutAnalysisContext());

        Assert.Equal(LayoutType.WrapPanel, result.LayoutType);
    }

    [Fact]
    public async Task AnalyzeAsync_SplitContainerRoot_AlwaysReturnsGrid()
    {
        var root = BuildControl("splitContainer1", "SplitContainer");
        BuildControl("panel1", "Panel", root, location: "new System.Drawing.Point(0, 0)");

        var result = await new LayoutAnalyzer().AnalyzeAsync(root, new LayoutAnalysisContext());

        Assert.Equal(LayoutType.Grid, result.LayoutType);
    }

    [Fact]
    public async Task AnalyzeAsync_OverlappingBoundingBoxes_SuppressesGridAndStackConfidence()
    {
        // Regression coverage for two things at once: (1) the overlap penalty itself, and
        // (2) that Location/Size are actually parsed from realistic, fully-qualified
        // WinForms designer syntax ("new System.Drawing.Point/Size(...)", not bare
        // "new Point(...)") - if parsing silently failed and returned (0,0) for both
        // Location and Size (the pre-fix behavior), width/height would be 0 and no overlap
        // would ever be detected, so this test would fail by staying at Grid/Stack instead
        // of falling back to Canvas.
        var root = BuildControl("panel1", "Panel");

        // Same Y (looks like a clean horizontal stack/grid by position alone) but wide
        // enough that the two controls' bounding boxes actually overlap.
        BuildControl("label1", "Label", root,
            location: "new System.Drawing.Point(0, 10)", size: "new System.Drawing.Size(200, 50)");
        BuildControl("label2", "Label", root,
            location: "new System.Drawing.Point(100, 10)", size: "new System.Drawing.Size(200, 50)");

        // Without the overlap penalty, Grid/Stack would both score 100 here (trivially
        // aligned) - well above threshold. With the penalty (-30), both drop to 70, which
        // falls below this test's 71 threshold and correctly falls back to Canvas.
        var context = new LayoutAnalysisContext { ConfidenceThreshold = 71 };

        var result = await new LayoutAnalyzer().AnalyzeAsync(root, context);

        Assert.Equal(LayoutType.Canvas, result.LayoutType);
    }

    [Fact]
    public async Task AnalyzeAsync_ResizingAnchor_RecoversConfidenceLostToOverlapPenalty()
    {
        // Baseline: same overlapping pair as above (Grid/Stack both penalized to 70),
        // which falls below this test's 80 threshold -> Canvas fallback.
        var baselineRoot = BuildControl("panel1", "Panel");
        BuildControl("textBox1", "TextBox", baselineRoot,
            location: "new System.Drawing.Point(0, 10)", size: "new System.Drawing.Size(200, 50)");
        BuildControl("textBox2", "TextBox", baselineRoot,
            location: "new System.Drawing.Point(100, 10)", size: "new System.Drawing.Size(200, 50)");

        var context = new LayoutAnalysisContext { ConfidenceThreshold = 80 };
        var baseline = await new LayoutAnalyzer().AnalyzeAsync(baselineRoot, context);
        Assert.Equal(LayoutType.Canvas, baseline.LayoutType);

        // Same overlap, but both controls now have a resizing (3+ edge) Anchor - Grid's
        // +20 boost (Min(100, 70 + 20) = 90) clears the same 80 threshold; Stack has no
        // such boost and stays at 70, so Grid wins outright.
        var anchoredRoot = BuildControl("panel1", "Panel");
        BuildControl("textBox1", "TextBox", anchoredRoot,
            location: "new System.Drawing.Point(0, 10)", size: "new System.Drawing.Size(200, 50)",
            anchor: "AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right");
        BuildControl("textBox2", "TextBox", anchoredRoot,
            location: "new System.Drawing.Point(100, 10)", size: "new System.Drawing.Size(200, 50)",
            anchor: "AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right");

        var anchored = await new LayoutAnalyzer().AnalyzeAsync(anchoredRoot, context);
        Assert.Equal(LayoutType.Grid, anchored.LayoutType);
    }

    [Fact]
    public async Task AnalyzeAsync_WeightConfig_FlipsWinnerBetweenGridAndStack()
    {
        var root = BuildControl("panel1", "Panel");
        // Vertically aligned pair (X=0 for both) plus a third point off both axes gives
        // StackPanel a partial (50%) confidence, while Grid scores its usual trivial 100%
        // (grid lines are derived directly from the same points, so every point is always
        // "aligned" to a grid built from itself), with no Dock property anywhere (0%).
        BuildControl("c1", "Label", root, location: "new System.Drawing.Point(0, 0)");
        BuildControl("c2", "Label", root, location: "new System.Drawing.Point(0, 50)");
        BuildControl("c3", "Label", root, location: "new System.Drawing.Point(80, 100)");

        var lowThreshold = new LayoutAnalysisContext { ConfidenceThreshold = 40 };

        var defaultWeights = await new LayoutAnalyzer().AnalyzeAsync(root, lowThreshold);
        Assert.Equal(LayoutType.Grid, defaultWeights.LayoutType);

        var stackFavored = new LayoutAnalysisContext
        {
            ConfidenceThreshold = 40,
            GridWeight = 0.3,
            StackWeight = 3.0
        };

        var weightedResult = await new LayoutAnalyzer().AnalyzeAsync(root, stackFavored);
        Assert.Equal(LayoutType.StackPanel, weightedResult.LayoutType);
    }
}
