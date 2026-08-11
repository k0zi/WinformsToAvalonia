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

    private static ControlNode BuildFormWithRootProperties(params (string Name, string Value)[] formProperties)
    {
        var root = new ControlNode
        {
            ControlType = "Form",
            FullTypeName = "System.Windows.Forms.Form",
            Name = "SampleForm"
        };

        foreach (var (name, value) in formProperties)
        {
            root.Properties[name] = new PropertyValue { Name = name, Value = value, Type = "object" };
        }

        return root;
    }

    [Fact]
    public void Generate_UserControlRoot_EmitsUserControlElement()
    {
        // ControlMappingRegistry maps "UserControl" -> "UserControl" (unlike "Form" ->
        // "Window") - the root element must mirror the source's real base type instead of
        // always hardcoding Window, or a custom control's own Designer.cs comes out broken.
        var root = new ControlNode { ControlType = "UserControl", FullTypeName = "System.Windows.Forms.UserControl", Name = "CustomerCard" };

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "CustomerCard");

        Assert.Contains("<UserControl xmlns=", axaml);
        Assert.Contains("</UserControl>", axaml);
        Assert.DoesNotContain("<Window", axaml);
    }

    [Fact]
    public void Generate_FormRoot_StillEmitsWindowElement()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "SampleForm" };

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("<Window xmlns=", axaml);
        Assert.Contains("</Window>", axaml);
    }

    [Fact]
    public void Generate_ChildIsConvertedCustomControl_ReferencesItInsteadOfTodoPlaceholder()
    {
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "SampleForm" };
        var card = new ControlNode { ControlType = "CustomerCard", FullTypeName = "SampleApp.Widgets.CustomerCard", Name = "customerCard1", Parent = root };
        root.Children.Add(card);

        var axaml = new AxamlGenerator().Generate(
            root, CanvasLayout(), "SampleApp", "SampleForm",
            convertedCustomControlClassNames: new HashSet<string> { "CustomerCard" });

        Assert.Contains("<views:CustomerCard", axaml);
        Assert.Contains("xmlns:views=\"using:SampleApp.Views\"", axaml);
        Assert.DoesNotContain("TODO: Unmapped control", axaml);
    }

    [Fact]
    public void Generate_ChildIsUnresolvedCustomControl_StillEmitsTodoPlaceholder()
    {
        // Nothing this run also converted matches "CustomerCard" - keeps today's exact
        // fallback behavior (a third-party control, or one outside the input path).
        var root = new ControlNode { ControlType = "Form", FullTypeName = "System.Windows.Forms.Form", Name = "SampleForm" };
        var card = new ControlNode { ControlType = "CustomerCard", FullTypeName = "SampleApp.Widgets.CustomerCard", Name = "customerCard1", Parent = root };
        root.Children.Add(card);

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("<!-- TODO: Unmapped control: CustomerCard (customerCard1) -->", axaml);
        Assert.DoesNotContain("xmlns:views=", axaml);
    }

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

    [Fact]
    public void Generate_UnquotedFormText_EmitsTitle_WithoutEscapedQuotes()
    {
        // Simulates post-fix WinFormsParser output (the raw C# literal's surrounding quote
        // characters already stripped) - regression coverage for the Title="&quot;...&quot;"
        // bug distinct from Generate_ButtonText_MapsToContent_NotText above, which
        // deliberately still feeds pre-quoted input to pin the Text->Content mapping itself.
        var root = BuildFormWithRootProperties(("Text", "Sign In"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("Title=\"Sign In\"", axaml);
        Assert.DoesNotContain("&quot;", axaml);
    }

    [Fact]
    public void Generate_FormClientSize_EmitsWidthAndHeight_OnWindow()
    {
        var root = BuildFormWithRootProperties(("ClientSize", "new System.Drawing.Size(400, 340)"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("Width=\"400\"", axaml);
        Assert.Contains("Height=\"340\"", axaml);
    }

    [Theory]
    [InlineData("System.Windows.Forms.FormBorderStyle.FixedSingle", "False")]
    [InlineData("System.Windows.Forms.FormBorderStyle.Sizable", "True")]
    public void Generate_FormBorderStyle_EmitsCanResize(string rawValue, string expectedCanResize)
    {
        var root = BuildFormWithRootProperties(("FormBorderStyle", rawValue));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains($"CanResize=\"{expectedCanResize}\"", axaml);
    }

    [Fact]
    public void Generate_FormBorderStyleNone_EmitsCanResizeFalse_AndNoSystemDecorations()
    {
        var root = BuildFormWithRootProperties(("FormBorderStyle", "System.Windows.Forms.FormBorderStyle.None"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("CanResize=\"False\"", axaml);
        Assert.Contains("SystemDecorations=\"None\"", axaml);
    }

    [Theory]
    [InlineData("System.Windows.Forms.FormStartPosition.CenterScreen", "CenterScreen")]
    [InlineData("System.Windows.Forms.FormStartPosition.CenterParent", "CenterOwner")]
    public void Generate_FormStartPosition_EmitsWindowStartupLocation(string rawValue, string expectedLocation)
    {
        var root = BuildFormWithRootProperties(("StartPosition", rawValue));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains($"WindowStartupLocation=\"{expectedLocation}\"", axaml);
    }

    [Fact]
    public void Generate_FormStartPositionManual_EmitsNoWindowStartupLocation()
    {
        var root = BuildFormWithRootProperties(("StartPosition", "System.Windows.Forms.FormStartPosition.Manual"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.DoesNotContain("WindowStartupLocation", axaml);
    }

    [Fact]
    public void Generate_FormWindowState_EmitsWindowState()
    {
        var root = BuildFormWithRootProperties(("WindowState", "System.Windows.Forms.FormWindowState.Maximized"));

        var axaml = new AxamlGenerator().Generate(root, CanvasLayout(), "SampleApp", "SampleForm");

        Assert.Contains("WindowState=\"Maximized\"", axaml);
    }
}
