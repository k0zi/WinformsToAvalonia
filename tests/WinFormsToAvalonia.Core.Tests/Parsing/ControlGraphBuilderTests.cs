using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class ControlGraphBuilderTests
{
    private static readonly string FixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DesignerCs");

    [Fact]
    public void Build_NestedControlsFixture_ReconstructsTreeAndSeparatesComponents()
    {
        var path = Path.Combine(FixturesRoot, "NestedControls.designer.cs");
        var content = File.ReadAllText(path);

        var walkResult = new DesignerSyntaxWalker().Walk(content, path, "NestedControlsForm", "Demo");
        var formModel = new ControlGraphBuilder().Build(walkResult);

        Assert.Equal(7, formModel.Controls.Count);

        // Root controls: groupBox1 (single Add), then topButton1/topButton2 (AddRange), in
        // the order the designer code invoked Controls.Add/AddRange.
        Assert.Equal(["groupBox1", "topButton1", "topButton2"], formModel.RootControls.Select(c => c.FieldName));

        var groupBox1 = formModel.RootControls[0];
        Assert.Equal("GroupBox", groupBox1.ClrTypeName);
        Assert.Equal(["innerLabel", "innerButton"], groupBox1.Children.Select(c => c.FieldName));
        Assert.Equal(new PropertyValue.PointValue(12, 12), groupBox1.Properties["Location"]);

        // Non-visual components: never targeted by any Controls.Add call.
        Assert.Equal(["refreshTimer", "toolTip1"], formModel.Components.Select(c => c.FieldName));
        var refreshTimer = formModel.Components[0];
        Assert.Equal("Timer", refreshTimer.ClrTypeName);
        Assert.Equal(new PropertyValue.Literal(1000), refreshTimer.Properties["Interval"]);
        var tickBinding = Assert.Single(refreshTimer.Events);
        Assert.Equal(new EventHandlerBinding("Tick", "refreshTimer_Tick", null), tickBinding);

        // `this.components` itself must never appear anywhere in the model.
        Assert.DoesNotContain(formModel.Controls.Values, c => c.FieldName == "components");

        // Form-level lambda event subscription.
        var loadBinding = Assert.Single(formModel.FormEvents);
        Assert.Equal("Load", loadBinding.EventName);
        Assert.Null(loadBinding.HandlerMethodName);
        Assert.Contains("this.Text = \"Loaded\"", loadBinding.InlineHandlerBody);

        Assert.Equal(new PropertyValue.SizeValue(300, 200), formModel.FormProperties["ClientSize"]);
    }

    [Fact]
    public void Build_SplitContainerNestedFixture_RoutesPanel1AndPanel2ChildrenIntoTheirOwnSlots()
    {
        var path = Path.Combine(FixturesRoot, "SplitContainerNested.designer.cs");
        var content = File.ReadAllText(path);

        var walkResult = new DesignerSyntaxWalker().Walk(content, path, "SplitContainerNestedForm", "Demo");
        var formModel = new ControlGraphBuilder().Build(walkResult);

        var splitContainer1 = Assert.Single(formModel.RootControls);
        Assert.Equal("splitContainer1", splitContainer1.FieldName);
        Assert.Empty(splitContainer1.Children);

        var panel1Child = Assert.Single(splitContainer1.Panel1Children);
        Assert.Equal("leftButton", panel1Child.FieldName);

        var panel2Child = Assert.Single(splitContainer1.Panel2Children);
        Assert.Equal("rightLabel", panel2Child.FieldName);

        // Panel1/Panel2 children must not also leak into FormModel.Components (they were
        // used, just via the synthetic "field.PanelN" parent id, not a real ControlModel).
        Assert.DoesNotContain(formModel.Components, c => c.FieldName is "leftButton" or "rightLabel");
    }

    [Fact]
    public void Build_MenuAndGridNestedFixture_NestsMenuItemsAndGridColumnsWithoutLeakingIntoComponents()
    {
        var path = Path.Combine(FixturesRoot, "MenuAndGridNested.designer.cs");
        var content = File.ReadAllText(path);

        var walkResult = new DesignerSyntaxWalker().Walk(content, path, "MenuAndGridNestedForm", "Demo");
        var formModel = new ControlGraphBuilder().Build(walkResult);

        Assert.Equal(["dataGridView1", "menuStrip1"], formModel.RootControls.Select(c => c.FieldName));

        var menuStrip1 = formModel.RootControls.Single(c => c.FieldName == "menuStrip1");
        var fileMenuItem = Assert.Single(menuStrip1.Children);
        Assert.Equal("fileMenuItem", fileMenuItem.FieldName);
        Assert.Equal("ToolStripMenuItem", fileMenuItem.ClrTypeName);

        Assert.Equal(["exitMenuItem", "fileSeparator"], fileMenuItem.Children.Select(c => c.FieldName));
        Assert.Equal("ToolStripSeparator", fileMenuItem.Children[1].ClrTypeName);

        var dataGridView1 = formModel.RootControls.Single(c => c.FieldName == "dataGridView1");
        Assert.Equal(["nameColumn", "activeColumn"], dataGridView1.Children.Select(c => c.FieldName));
        Assert.Equal("DataGridViewTextBoxColumn", dataGridView1.Children[0].ClrTypeName);
        Assert.Equal("DataGridViewCheckBoxColumn", dataGridView1.Children[1].ClrTypeName);

        // The whole point: menu items and grid columns must nest under their real owner
        // instead of leaking into FormModel.Components as flat, parent-less entries.
        Assert.DoesNotContain(formModel.Components, c => c.FieldName is
            "fileMenuItem" or "exitMenuItem" or "fileSeparator" or "nameColumn" or "activeColumn");
    }
}
