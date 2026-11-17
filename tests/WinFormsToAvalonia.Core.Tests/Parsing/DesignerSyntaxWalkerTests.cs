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

    // ---- Control or property: the field-declaration rule ------------------------------------

    private const string RealDesignerShape = """
        namespace Demo
        {
            partial class TestForm
            {
                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                    this.ClientSize = new System.Drawing.Size(284, 261);
                    this.Controls.Add(this.button1);
                }

                private System.Windows.Forms.Button button1;
            }
        }
        """;

    /// <summary>
    /// The line every real Visual Studio designer emits, which used to register a phantom control
    /// called "AutoScaleDimensions" of type "SizeF" - and then reported it as an unsupported
    /// control, four times over in a four-form project.
    /// </summary>
    /// <remarks>
    /// It never surfaced because this repo's designer fixtures are hand-written and none of them
    /// contains the line. The rule is now a fact about the source rather than a list of type
    /// names: a designer declares a field for every control it creates, and never for an
    /// inherited property.
    /// </remarks>
    [Fact]
    public void Walk_FormPropertyWhoseValueIsConstructed_IsNotAControl()
    {
        var result = new DesignerSyntaxWalker().Walk(RealDesignerShape, "TestForm.Designer.cs", "TestForm", "Demo");

        Assert.Equal(["button1"], result.Form.Controls.Keys);
        Assert.True(result.Form.FormProperties.ContainsKey("AutoScaleDimensions"));
        Assert.Equal(new PropertyValue.SizeValue(284, 261), result.Form.FormProperties["ClientSize"]);

        // And silently: AutoScaleDimensions is in every Form a real designer ever wrote, so
        // warning about it would trade a wrong line for a noisy one.
        Assert.Empty(result.Warnings);
    }

    /// <summary>
    /// <c>Controls.Add(child, column, row)</c> records the cell; a later span or move makes it
    /// something else, and the cell is no longer known - a plain <c>Add</c> never had one.
    /// </summary>
    [Fact]
    public void Walk_TableLayoutPanelAdd_RecordsTheCellUnlessLaterChanged()
    {
        const string source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.table = new System.Windows.Forms.TableLayoutPanel();
                        this.a = new System.Windows.Forms.Label();
                        this.b = new System.Windows.Forms.Label();
                        this.c = new System.Windows.Forms.Label();
                        this.table.Controls.Add(this.a, 2, 1);
                        this.table.Controls.Add(this.b, 0, 0);
                        this.table.SetColumnSpan(this.b, 2);
                        this.table.Controls.Add(this.c);
                    }

                    private System.Windows.Forms.TableLayoutPanel table;
                    private System.Windows.Forms.Label a;
                    private System.Windows.Forms.Label b;
                    private System.Windows.Forms.Label c;
                }
            }
            """;

        var form = new DesignerSyntaxWalker().Walk(source, "TestForm.Designer.cs", "TestForm", "Demo").Form;

        Assert.Equal(new TableCellPosition(2, 1), form.Controls["a"].TableCell);
        Assert.Null(form.Controls["b"].TableCell);
        Assert.Null(form.Controls["c"].TableCell);
    }

    /// <summary>A declared field is still a control, whatever it is constructed from.</summary>
    [Fact]
    public void Walk_DeclaredFieldAssignedAConstruction_IsStillAControl()
    {
        const string source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.refreshTimer = new System.Windows.Forms.Timer(this.components);
                    }

                    private System.Windows.Forms.Timer refreshTimer;
                }
            }
            """;

        var result = new DesignerSyntaxWalker().Walk(source, "TestForm.Designer.cs", "TestForm", "Demo");

        Assert.Equal("Timer", result.Form.Controls["refreshTimer"].ClrTypeName);
    }

    /// <summary>
    /// Neither a declared field nor a known value type: reported by name rather than guessed at.
    /// </summary>
    /// <remarks>
    /// This is the honest replacement for what the old rule did silently - it made such a shape a
    /// control, which is how a `SizeF` ended up in the unsupported count.
    /// </remarks>
    [Fact]
    public void Walk_ConstructedValueThatIsNeitherFieldNorKnownType_IsReported()
    {
        const string source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.button1 = new System.Windows.Forms.Button();
                        this.SomeUnknownProperty = new Whatever(1);
                    }

                    private System.Windows.Forms.Button button1;
                }
            }
            """;

        var result = new DesignerSyntaxWalker().Walk(source, "TestForm.Designer.cs", "TestForm", "Demo");

        Assert.Equal(["button1"], result.Form.Controls.Keys);
        Assert.Contains(
            result.Warnings,
            w => w.Contains("SomeUnknownProperty", StringComparison.Ordinal)
                && w.Contains("declares no such field", StringComparison.Ordinal));
    }

    /// <summary>
    /// A class that declares no fields at all gets the old type-name rule, because there is no
    /// evidence to apply the new one to.
    /// </summary>
    /// <remarks>
    /// Not a fudge: a generated designer always declares its controls, but a hand-written
    /// <c>InitializeComponent</c> can keep them in the other half of the partial class, which this
    /// walker never sees. Losing every control there would be far worse than the bug being fixed.
    /// </remarks>
    [Fact]
    public void Walk_ClassThatDeclaresNoFields_FallsBackToTheTypeNameRule()
    {
        const string source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.button1 = new System.Windows.Forms.Button();
                        this.ClientSize = new System.Drawing.Size(284, 261);
                    }
                }
            }
            """;

        var result = new DesignerSyntaxWalker().Walk(source, "TestForm.Designer.cs", "TestForm", "Demo");

        Assert.Equal(["button1"], result.Form.Controls.Keys);
        Assert.True(result.Form.FormProperties.ContainsKey("ClientSize"));
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
    public void Walk_ImageListSetKeyName_RecordsEachKeysIndex_AndDataBindingsAreReported()
    {
        const string source = """
            namespace Demo
            {
                partial class TestForm
                {
                    private void InitializeComponent()
                    {
                        this.components = new System.ComponentModel.Container();
                        this.imageList1 = new System.Windows.Forms.ImageList(this.components);
                        this.textBox1 = new System.Windows.Forms.TextBox();
                        this.imageList1.Images.SetKeyName(0, "open.png");
                        this.imageList1.Images.SetKeyName(1, "save.png");
                        this.imageList1.Images.SetKeyName(2, "open.png");
                        this.textBox1.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource1, "Name", true));
                    }

                    private System.ComponentModel.IContainer components;
                    private System.Windows.Forms.ImageList imageList1;
                    private System.Windows.Forms.TextBox textBox1;
                }
            }
            """;

        var result = new DesignerSyntaxWalker().Walk(source, "test.designer.cs", "TestForm", "Demo");

        // A repeated key keeps its first index, as Images[key] finds the first match.
        Assert.Equal(
            new Dictionary<string, int> { ["open.png"] = 0, ["save.png"] = 1 },
            result.Form.Controls["imageList1"].ImageKeys);
        Assert.Contains(result.Warnings, w => w.Contains("'textBox1.Text' is data-bound to 'this.bindingSource1.Name'"));
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
    /// <summary>
    /// A derived Form's walk continues from its base's, as WinForms runs the two
    /// InitializeComponents back to back on one object: the derived designer reaches a field
    /// it never declared, its value wins, and its own controls join the base's.
    /// </summary>
    [Fact]
    public void Walk_InheritedFrom_ContinuesOnTheBasesControlsAndOverridesThem()
    {
        const string baseDesigner = """
            namespace Demo
            {
                partial class BaseForm
                {
                    protected System.Windows.Forms.Button okButton;

                    private void InitializeComponent()
                    {
                        this.okButton = new System.Windows.Forms.Button();
                        this.okButton.Text = "OK";
                        this.okButton.Click += new System.EventHandler(this.okButton_Click);
                        this.ClientSize = new System.Drawing.Size(300, 200);
                        this.Text = "Base";
                        this.Controls.Add(this.okButton);
                    }
                }
            }
            """;
        const string derivedDesigner = """
            namespace Demo
            {
                partial class MainForm
                {
                    private System.Windows.Forms.Label label1;

                    private void InitializeComponent()
                    {
                        this.label1 = new System.Windows.Forms.Label();
                        this.okButton.Text = "Go";
                        this.Text = "Main";
                        this.Controls.Add(this.label1);
                    }
                }
            }
            """;

        var walker = new DesignerSyntaxWalker();
        var inherited = walker.Walk(baseDesigner, "BaseForm.Designer.cs", "BaseForm", "Demo");
        var result = walker.Walk(derivedDesigner, "MainForm.Designer.cs", "MainForm", "Demo", inheritedFrom: inherited);

        Assert.Equal("MainForm", result.Form.ClassName);
        Assert.Equal(["label1", "okButton"], result.Form.Controls.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(new PropertyValue.Literal("Go"), result.Form.Controls["okButton"].Properties["Text"]);
        Assert.Single(result.Form.Controls["okButton"].Events);
        Assert.Equal(new PropertyValue.Literal("Main"), result.Form.FormProperties["Text"]);
        Assert.Equal(new PropertyValue.SizeValue(300, 200), result.Form.FormProperties["ClientSize"]);
        Assert.Equal(2, result.Edges.Count);
    }
}
