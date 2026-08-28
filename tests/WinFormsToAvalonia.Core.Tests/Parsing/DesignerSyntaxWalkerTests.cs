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

    /// <summary>
    /// The same mechanism, for the provider whose contribution used to disappear entirely.
    /// </summary>
    [Fact]
    public void Walk_SetHelpStringInvocation_StoresTheHelpTextOnTheTargetControl()
    {
        const string source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.helpProvider1 = new HelpProvider();
                        this.notesBox = new TextBox();
                        this.helpProvider1.SetHelpString(this.notesBox, "Free-form notes.");
                    }
                }
            }
            """;

        var result = new DesignerSyntaxWalker().Walk(source, "TestForm.Designer.cs", "TestForm", "Demo");

        Assert.Equal(
            new PropertyValue.Literal("Free-form notes."),
            result.Form.Controls["notesBox"].Properties["HelpString"]);

        Assert.False(result.Form.Controls["helpProvider1"].Properties.ContainsKey("HelpString"));
        Assert.Empty(result.Warnings);
    }

    /// <summary>
    /// A setter on a recognised provider that has no Avalonia target is named rather than
    /// dropped - which is what `SetShowHelp` and `HelpNamespace` used to be: silently gone.
    /// </summary>
    [Fact]
    public void Walk_UntranslatableProviderSetter_IsReportedByName()
    {
        const string source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.helpProvider1 = new HelpProvider();
                        this.notesBox = new TextBox();
                        this.helpProvider1.SetShowHelp(this.notesBox, true);
                    }
                }
            }
            """;

        var result = new DesignerSyntaxWalker().Walk(source, "TestForm.Designer.cs", "TestForm", "Demo");

        Assert.Contains(
            result.Warnings,
            w => w.Contains("helpProvider1", StringComparison.Ordinal)
                && w.Contains("SetShowHelp", StringComparison.Ordinal));

        Assert.Empty(result.Form.Controls["notesBox"].Properties);
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

    private const string LocalizableFormSource = """
        namespace Demo
        {
            partial class TestForm
            {
                private void InitializeComponent()
                {
                    System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestForm));
                    this.button1 = new System.Windows.Forms.Button();
                    resources.ApplyResources(this.button1, "button1");
                    this.button1.Name = "button1";
                    resources.ApplyResources(this, "$this");
                    this.Controls.Add(this.button1);
                }

                private System.Windows.Forms.Button button1;
            }
        }
        """;

    [Fact]
    public void Walk_ApplyResources_ResolvesResxEntriesOntoTheTargetControl()
    {
        var resx = new ResxDocument("MainForm.resx", [
            new ResxEntry("button1.Text", null, null, "OK"),
            new ResxEntry("button1.Location", "System.Drawing.Point, System.Drawing", null, "12, 34"),
        ]);

        var formModel = new DesignerSyntaxWalker()
            .Walk(LocalizableFormSource, "test.designer.cs", "TestForm", "Demo", resx).Form;

        var button = formModel.Controls["button1"];
        Assert.Equal(new PropertyValue.Literal("OK"), button.Properties["Text"]);
        Assert.Equal(new PropertyValue.PointValue(12, 34), button.Properties["Location"]);
    }

    [Fact]
    public void Walk_ApplyResourcesForDollarThis_ResolvesOntoTheFormItself()
    {
        var resx = new ResxDocument("MainForm.resx", [
            new ResxEntry("$this.Text", null, null, "My Form"),
            new ResxEntry("$this.ClientSize", "System.Drawing.Size, System.Drawing", null, "284, 136"),
        ]);

        var formModel = new DesignerSyntaxWalker()
            .Walk(LocalizableFormSource, "test.designer.cs", "TestForm", "Demo", resx).Form;

        Assert.Equal(new PropertyValue.Literal("My Form"), formModel.FormProperties["Text"]);
        Assert.Equal(new PropertyValue.SizeValue(284, 136), formModel.FormProperties["ClientSize"]);
    }

    /// <summary>
    /// The designer emits ApplyResources first in a control's block, so a later explicit
    /// assignment must still win - exactly as it does at run time.
    /// </summary>
    [Fact]
    public void Walk_ExplicitAssignmentAfterApplyResources_OverridesTheResourceValue()
    {
        var source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestForm));
                        this.button1 = new System.Windows.Forms.Button();
                        resources.ApplyResources(this.button1, "button1");
                        this.button1.Text = "Explicit";
                    }

                    private System.Windows.Forms.Button button1;
                }
            }
            """;

        var resx = new ResxDocument("MainForm.resx", [new ResxEntry("button1.Text", null, null, "FromResx")]);

        var formModel = new DesignerSyntaxWalker().Walk(source, "test.designer.cs", "TestForm", "Demo", resx).Form;

        Assert.Equal(new PropertyValue.Literal("Explicit"), formModel.Controls["button1"].Properties["Text"]);
    }

    [Fact]
    public void Walk_ApplyResourcesWithNoResxFile_WarnsOncePerFormInsteadOfPerControl()
    {
        var result = new DesignerSyntaxWalker().Walk(LocalizableFormSource, "test.designer.cs", "TestForm", "Demo");

        // Two ApplyResources calls in the source, one warning out.
        var warning = Assert.Single(result.Warnings);
        Assert.Contains("TestForm", warning, StringComparison.Ordinal);
        Assert.Contains("no .resx file was found", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void Walk_NoResources_ProducesNoWarnings()
    {
        var path = Path.Combine(FixturesRoot, "FlatControls.designer.cs");
        var content = File.ReadAllText(path);

        var result = new DesignerSyntaxWalker().Walk(content, path, "FlatControlsForm", "Demo");

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Walk_ItemsAddRangeWithLiterals_CapturesThemOnTheOwningControl()
    {
        var source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.comboBox1 = new System.Windows.Forms.ComboBox();
                        this.listBox1 = new System.Windows.Forms.ListBox();
                        this.comboBox1.Items.AddRange(new object[] { "Alpha", "Beta" });
                        this.listBox1.Items.Add("Only");
                    }

                    private System.Windows.Forms.ComboBox comboBox1;
                    private System.Windows.Forms.ListBox listBox1;
                }
            }
            """;

        var formModel = new DesignerSyntaxWalker().Walk(source, "test.designer.cs", "TestForm", "Demo").Form;

        Assert.Equal(["Alpha", "Beta"], formModel.Controls["comboBox1"].LiteralItems);
        Assert.Equal(["Only"], formModel.Controls["listBox1"].LiteralItems);
    }

    /// <summary>
    /// A ToolStrip's Items hold real controls, not literals - those must keep becoming
    /// parent/child edges, never item strings.
    /// </summary>
    [Fact]
    public void Walk_ItemsAddWithControlReferences_StaysAParentChildEdge()
    {
        var source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.menuStrip1 = new System.Windows.Forms.MenuStrip();
                        this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                        this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.fileMenuItem });
                    }

                    private System.Windows.Forms.MenuStrip menuStrip1;
                    private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
                }
            }
            """;

        var result = new DesignerSyntaxWalker().Walk(source, "test.designer.cs", "TestForm", "Demo");

        Assert.Empty(result.Form.Controls["menuStrip1"].LiteralItems);
        Assert.Contains(result.Edges, e => e.ParentFieldName == "menuStrip1" && e.ChildFieldName == "fileMenuItem");
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
