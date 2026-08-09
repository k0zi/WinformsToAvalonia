using Converter.Core.Parsing;
using Converter.Generator.Axaml;
using Converter.Plugin.Abstractions;

namespace Converter.Tests.Generator;

public class AxamlGeneratorTests
{
    private static ControlNode BuildFormWithButton(params (string Name, string Value)[] buttonProperties)
    {
        var root = new ControlNode
        {
            ControlType = "Form",
            FullTypeName = "System.Windows.Forms.Form",
            Name = "SampleForm"
        };

        var button = new ControlNode
        {
            ControlType = "Button",
            FullTypeName = "System.Windows.Forms.Button",
            Name = "button1",
            Parent = root
        };

        foreach (var (name, value) in buttonProperties)
        {
            button.Properties[name] = new PropertyValue { Name = name, Value = value, Type = "object" };
        }

        root.Children.Add(button);
        return root;
    }

    private static LayoutAnalysisResult CanvasLayout() => new()
    {
        LayoutType = LayoutType.Canvas,
        ConfidenceScore = 100
    };

    [Fact]
    public void Generate_EmitsBackground_FromBackColor()
    {
        var root = BuildFormWithButton(("BackColor", "System.Drawing.Color.FromArgb(0, 120, 215)"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("Background=\"#0078D7\"", axaml);
    }

    [Fact]
    public void Generate_EmitsFontFamilySizeWeight_FromFont()
    {
        var root = BuildFormWithButton(("Font", "new System.Drawing.Font(\"Segoe UI\", 9F, System.Drawing.FontStyle.Bold)"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("FontFamily=\"Segoe UI\"", axaml);
        Assert.Contains("FontSize=\"9\"", axaml);
        Assert.Contains("FontWeight=\"Bold\"", axaml);
    }

    [Fact]
    public void Generate_EmitsDockPanelDock_FromDock()
    {
        var root = BuildFormWithButton(("Dock", "System.Windows.Forms.DockStyle.Top"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("DockPanel.Dock=\"Top\"", axaml);
    }

    [Fact]
    public void Generate_EmitsCanvasLeftTop_FromLocation()
    {
        var root = BuildFormWithButton(("Location", "new System.Drawing.Point(10, 25)"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("Canvas.Left=\"10\"", axaml);
        Assert.Contains("Canvas.Top=\"25\"", axaml);
    }

    [Fact]
    public void Generate_UnmappedContainerWithMappedChildren_StillEmitsChildren()
    {
        var root = new ControlNode
        {
            ControlType = "Form",
            FullTypeName = "System.Windows.Forms.Form",
            Name = "SampleForm"
        };

        var unmappedContainer = new ControlNode
        {
            ControlType = "AcmeVendor.WidgetPanel",
            FullTypeName = "AcmeVendor.WidgetPanel",
            Name = "widgetPanel1",
            Parent = root
        };

        var button1 = new ControlNode
        {
            ControlType = "Button",
            FullTypeName = "System.Windows.Forms.Button",
            Name = "button1",
            Parent = unmappedContainer
        };

        var button2 = new ControlNode
        {
            ControlType = "Button",
            FullTypeName = "System.Windows.Forms.Button",
            Name = "button2",
            Parent = unmappedContainer
        };

        unmappedContainer.Children.Add(button1);
        unmappedContainer.Children.Add(button2);
        root.Children.Add(unmappedContainer);

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("TODO: Unmapped control: AcmeVendor.WidgetPanel", axaml);
        Assert.Contains("Name=\"button1\"", axaml);
        Assert.Contains("Name=\"button2\"", axaml);
    }

    [Fact]
    public void Generate_PreserveEventHandlerEvent_EmitsAxamlEventAttribute()
    {
        var root = new ControlNode
        {
            ControlType = "Form",
            FullTypeName = "System.Windows.Forms.Form",
            Name = "SampleForm"
        };

        var button = new ControlNode
        {
            ControlType = "Button",
            FullTypeName = "System.Windows.Forms.Button",
            Name = "button1",
            Parent = root
        };
        button.EventHandlers["MouseDown"] = "button1_MouseDown";
        root.Children.Add(button);

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("PointerPressed=\"button1_MouseDown\"", axaml);
    }

    [Fact]
    public void Generate_ConvertToCommandEvent_DoesNotEmitAxamlEventAttribute()
    {
        var root = BuildFormWithButton();
        root.Children[0].EventHandlers["Click"] = "button1_Click";

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.DoesNotContain("Click=\"button1_Click\"", axaml);
    }

    [Fact]
    public void Generate_PreserveEventHandlerViaInlineLambda_DoesNotEmitAxamlEventAttribute()
    {
        // The sentinel marker has no corresponding code-behind method (CodeBehindGenerator
        // skips it too), so referencing it from AXAML would point at a non-existent handler.
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "SampleForm" };
        var button = new ControlNode
        {
            ControlType = "Button", FullTypeName = "System.Windows.Forms.Button", Name = "button1", Parent = root
        };
        button.EventHandlers["MouseDown"] = WinFormsParser.InlineLambdaHandlerMarker;
        root.Children.Add(button);

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.DoesNotContain("PointerPressed", axaml);
        Assert.DoesNotContain("inline lambda", axaml);
    }

    [Fact]
    public void Generate_ButtonText_MapsToContent_NotText()
    {
        // Avalonia's Button has no Text property (it's a ContentControl) - this used to emit
        // Text="..." verbatim, which fails Avalonia's XAML compiler (found via a real
        // WarehouseApp sample conversion, where every button in the app broke the build).
        var root = BuildFormWithButton(("Text", "\"Log In\""));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("Content=", axaml);
        Assert.DoesNotContain("Text=", axaml);
    }

    [Fact]
    public void Generate_PanelPadding_IsDroppedRatherThanEmittedAsBrokenAttribute()
    {
        // Avalonia's base Panel (Panel's WinForms->Avalonia target) has no Padding property at
        // all, unlike Border/ContentControl - emitting it always failed to compile.
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "SampleForm" };
        var panel = new ControlNode
        {
            ControlType = "Panel", FullTypeName = "System.Windows.Forms.Panel", Name = "panel1", Parent = root
        };
        panel.Properties["Padding"] = new PropertyValue
        {
            Name = "Padding", Value = "new System.Windows.Forms.Padding(8)", Type = "object"
        };
        root.Children.Add(panel);

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.DoesNotContain("Padding=", axaml);
    }

    [Fact]
    public void Generate_MarginWithFourArgPadding_ConvertsToAvaloniaThicknessSyntax()
    {
        // WinForms `new Padding(left, top, right, bottom)` was previously emitted as the raw
        // C# expression text, which Avalonia's Thickness parser can't read at all.
        var root = BuildFormWithButton(("Margin", "new System.Windows.Forms.Padding(3, 4, 3, 4)"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("Margin=\"3,4,3,4\"", axaml);
    }

    [Fact]
    public void Generate_Size_EmitsWidthAndHeight()
    {
        var root = BuildFormWithButton(("Size", "new System.Drawing.Size(75, 23)"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("Width=\"75\"", axaml);
        Assert.Contains("Height=\"23\"", axaml);
    }

    private static LayoutAnalysisResult GridLayoutWithPlacement(
        IReadOnlyDictionary<string, GridCellAssignment> placements) => new()
    {
        LayoutType = LayoutType.Grid,
        ConfidenceScore = 100,
        Metadata = new Dictionary<string, object> { ["Rows"] = 2, ["Columns"] = 2 },
        GridCellAssignments = new Dictionary<string, GridCellAssignment>(placements)
    };

    [Fact]
    public void Generate_GridLayoutWithCellAssignment_EmitsGridRowAndColumn()
    {
        var root = BuildFormWithButton(("Location", "new System.Drawing.Point(10, 25)"));
        var layoutInfo = GridLayoutWithPlacement(
            new Dictionary<string, GridCellAssignment> { ["button1"] = new(1, 0) });

        var axaml = new AxamlGenerator().Generate(root, layoutInfo, "SampleApp", "SampleForm");

        Assert.Contains("Grid.Row=\"1\"", axaml);
        Assert.Contains("Grid.Column=\"0\"", axaml);
    }

    [Fact]
    public void Generate_GridLayout_DoesNotEmitDeadCanvasLeftTop()
    {
        // Canvas.Left/Canvas.Top is meaningless inside a Grid container (the parent isn't a
        // Canvas) - it used to be emitted unconditionally anyway, cluttering the AXAML.
        var root = BuildFormWithButton(("Location", "new System.Drawing.Point(10, 25)"));
        var layoutInfo = GridLayoutWithPlacement(
            new Dictionary<string, GridCellAssignment> { ["button1"] = new(0, 0) });

        var axaml = new AxamlGenerator().Generate(root, layoutInfo, "SampleApp", "SampleForm");

        Assert.DoesNotContain("Canvas.Left", axaml);
        Assert.DoesNotContain("Canvas.Top", axaml);
    }

    [Fact]
    public void Generate_TableLayoutPanelChildren_EmitGridRowAndColumnFromCapturedCells()
    {
        var root = new ControlNode
        {
            ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "SampleForm"
        };

        var panel = new ControlNode
        {
            ControlType = "TableLayoutPanel", FullTypeName = "System.Windows.Forms.TableLayoutPanel",
            Name = "tableLayoutPanel1", Parent = root
        };
        panel.Properties["ColumnCount"] = new PropertyValue { Name = "ColumnCount", Value = "2", Type = "int" };
        panel.Properties["RowCount"] = new PropertyValue { Name = "RowCount", Value = "2", Type = "int" };

        var textBox2 = new ControlNode
        {
            ControlType = "TextBox", FullTypeName = "System.Windows.Forms.TextBox", Name = "textBox2", Parent = panel
        };
        textBox2.Properties["TableLayoutPanel.Column"] = new PropertyValue { Name = "TableLayoutPanel.Column", Value = "1", Type = "int" };
        textBox2.Properties["TableLayoutPanel.Row"] = new PropertyValue { Name = "TableLayoutPanel.Row", Value = "1", Type = "int" };
        panel.Children.Add(textBox2);
        root.Children.Add(panel);

        var layoutInfo = new LayoutAnalysisResult
        {
            LayoutType = LayoutType.Grid,
            ConfidenceScore = 100,
            ChildLayouts = new Dictionary<string, LayoutAnalysisResult>
            {
                ["tableLayoutPanel1"] = new() { LayoutType = LayoutType.Grid, ConfidenceScore = 100 }
            }
        };

        var axaml = new AxamlGenerator().Generate(root, layoutInfo, "SampleApp", "SampleForm");

        Assert.Contains("Grid.Row=\"1\"", axaml);
        Assert.Contains("Grid.Column=\"1\"", axaml);
        Assert.Contains("<RowDefinition", axaml);
        Assert.Contains("<ColumnDefinition", axaml);
    }
}
