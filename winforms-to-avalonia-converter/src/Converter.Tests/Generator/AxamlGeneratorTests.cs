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
}
