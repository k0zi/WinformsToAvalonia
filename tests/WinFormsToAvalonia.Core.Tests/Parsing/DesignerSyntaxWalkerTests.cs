using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class DesignerSyntaxWalkerTests
{
    private static readonly string FixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DesignerCs");

    [Fact]
    public void Walk_FlatControlsFixture_ExtractsAllControlsWithProperties()
    {
        var path = Path.Combine(FixturesRoot, "FlatControls.designer.cs");
        var content = File.ReadAllText(path);

        var formModel = new DesignerSyntaxWalker().Walk(content, path, "FlatControlsForm", "Demo").Form;

        Assert.Equal("FlatControlsForm", formModel.ClassName);
        Assert.Equal("Demo", formModel.Namespace);
        Assert.Equal(5, formModel.Controls.Count);
        Assert.Equal(["button1", "label1", "textBox1", "checkBox1", "panel1"], formModel.Controls.Keys);

        var button1 = formModel.Controls["button1"];
        Assert.Equal("Button", button1.ClrTypeName);
        Assert.Equal(new PropertyValue.PointValue(12, 12), button1.Properties["Location"]);
        Assert.Equal(new PropertyValue.SizeValue(75, 23), button1.Properties["Size"]);
        Assert.Equal(new PropertyValue.Literal("button1"), button1.Properties["Name"]);
        Assert.Equal(new PropertyValue.Literal(0), button1.Properties["TabIndex"]);
        Assert.Equal(new PropertyValue.Literal("Click me"), button1.Properties["Text"]);
        Assert.Equal(new PropertyValue.Literal(true), button1.Properties["UseVisualStyleBackColor"]);
        // Click += new EventHandler(...) is an add-assignment, not a plain '=' assignment -
        // it must not show up as a property, but as an EventHandlerBinding instead.
        Assert.False(button1.Properties.ContainsKey("Click"));
        var clickBinding = Assert.Single(button1.Events);
        Assert.Equal(new EventHandlerBinding("Click", "button1_Click", null), clickBinding);

        var label1 = formModel.Controls["label1"];
        Assert.Equal("Label", label1.ClrTypeName);
        Assert.Equal(new PropertyValue.Literal("Name:"), label1.Properties["Text"]);

        var textBox1 = formModel.Controls["textBox1"];
        Assert.Equal("TextBox", textBox1.ClrTypeName);
        Assert.Equal(new PropertyValue.SizeValue(150, 23), textBox1.Properties["Size"]);

        var checkBox1 = formModel.Controls["checkBox1"];
        Assert.Equal("CheckBox", checkBox1.ClrTypeName);
        Assert.Equal(new PropertyValue.Literal(true), checkBox1.Properties["Checked"]);

        var panel1 = formModel.Controls["panel1"];
        Assert.Equal("Panel", panel1.ClrTypeName);
        Assert.Equal(new PropertyValue.SizeValue(260, 120), panel1.Properties["Size"]);

        // Form-level properties (`this.X = ...` where X is not a known control field).
        Assert.Equal(new PropertyValue.SizeValue(284, 261), formModel.FormProperties["ClientSize"]);
        Assert.Equal(new PropertyValue.Literal("FlatControlsForm"), formModel.FormProperties["Name"]);
        Assert.Equal(new PropertyValue.Literal("Flat Controls Demo"), formModel.FormProperties["Text"]);

        // `this.components` must not leak in as a fake control.
        Assert.DoesNotContain("components", formModel.Controls.Keys);
    }

    [Fact]
    public void Walk_SetToolTipInvocation_StoresToolTipTextOnTargetControl()
    {
        var path = Path.Combine(FixturesRoot, "NestedControls.designer.cs");
        var content = File.ReadAllText(path);

        var formModel = new DesignerSyntaxWalker().Walk(content, path, "NestedControlsForm", "Demo").Form;

        var innerButton = formModel.Controls["innerButton"];
        Assert.Equal(new PropertyValue.Literal("Inner button tooltip"), innerButton.Properties["ToolTipText"]);

        // The ToolTip component itself never gets a "ToolTipText" of its own - only the target.
        var toolTip1 = formModel.Controls["toolTip1"];
        Assert.False(toolTip1.Properties.ContainsKey("ToolTipText"));
    }

    [Fact]
    public void Walk_ContextMenuStripAssignment_CapturesControlReference()
    {
        const string source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.treeView1 = new System.Windows.Forms.TreeView();
                        this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip();
                        this.treeView1.ContextMenuStrip = this.contextMenuStrip1;
                    }

                    private System.Windows.Forms.TreeView treeView1;
                    private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
                }
            }
            """;

        var formModel = new DesignerSyntaxWalker().Walk(source, "test.designer.cs", "TestForm", "Demo").Form;

        var treeView = formModel.Controls["treeView1"];
        Assert.Equal(new PropertyValue.ControlReference("contextMenuStrip1"), treeView.Properties["ContextMenuStrip"]);
    }

    [Fact]
    public void Walk_TimerWithTickHandler_CapturesIntervalAndTickBinding()
    {
        var path = Path.Combine(FixturesRoot, "NestedControls.designer.cs");
        var content = File.ReadAllText(path);

        var formModel = new DesignerSyntaxWalker().Walk(content, path, "NestedControlsForm", "Demo").Form;

        var refreshTimer = formModel.Controls["refreshTimer"];
        Assert.Equal(new PropertyValue.Literal(1000), refreshTimer.Properties["Interval"]);
        Assert.Contains(refreshTimer.Events, e => e.EventName == "Tick" && e.HandlerMethodName == "refreshTimer_Tick");
    }

    [Fact]
    public void Walk_UnknownClassName_ReturnsEmptyModel()
    {
        var path = Path.Combine(FixturesRoot, "FlatControls.designer.cs");
        var content = File.ReadAllText(path);

        var formModel = new DesignerSyntaxWalker().Walk(content, path, "SomeOtherClass", "Demo").Form;

        Assert.Empty(formModel.Controls);
        Assert.Empty(formModel.FormProperties);
    }
}
