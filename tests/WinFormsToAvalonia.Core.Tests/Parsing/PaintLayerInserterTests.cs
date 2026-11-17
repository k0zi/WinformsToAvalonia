using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class PaintLayerInserterTests
{
    private static readonly HashSet<string> NoMembers = new(StringComparer.Ordinal);

    private static ControlModel Control(string field, string type, int width = 200, int height = 100)
    {
        var control = new ControlModel { FieldName = field, ClrTypeName = type };
        control.Properties["Size"] = new PropertyValue.SizeValue(width, height);
        return control;
    }

    [Fact]
    public void Insert_ContainerWithChildrenAndPaint_DrawsOnALayerUnderItsChildren()
    {
        var model = new FormModel { ClassName = "MainForm" };
        var group = Control("groupBox1", "GroupBox", 700, 466);
        group.Events.Add(new EventHandlerBinding("Paint", "groupBox1_Paint", null));
        group.Events.Add(new EventHandlerBinding("MouseDown", "groupBox1_MouseDown", null));
        var label = Control("label1", "Label");
        group.Children.Add(label);
        model.Controls[group.FieldName] = group;
        model.Controls[label.FieldName] = label;

        var notes = PaintLayerInserter.Insert(model, NoMembers, isUserControl: false);

        var layer = group.Children[0];
        Assert.Equal("groupBox1PaintLayer", layer.FieldName);
        Assert.Equal("Panel", layer.ClrTypeName);
        Assert.Same(label, group.Children[1]);
        Assert.Equal(new PropertyValue.PointValue(0, 0), layer.Properties["Location"]);
        Assert.Equal(new PropertyValue.SizeValue(700, 466), layer.Properties["Size"]);
        Assert.Equal("groupBox1_Paint", Assert.Single(layer.Events).HandlerMethodName);
        // Only Paint moves - the container keeps its other events.
        Assert.Equal("MouseDown", Assert.Single(group.Events).EventName);
        Assert.Same(layer, model.Controls["groupBox1PaintLayer"]);
        Assert.Equal("groupBox1PaintLayer", model.PaintLayers["groupBox1"]);
        Assert.Contains("groupBox1PaintLayer", Assert.Single(notes));
    }

    /// <summary>A childless Panel already is a paint surface; it needs no layer.</summary>
    [Fact]
    public void Insert_ChildlessPanel_IsLeftToThePaintSurfaceMapper()
    {
        var model = new FormModel { ClassName = "MainForm" };
        var panel = Control("canvasPanel", "Panel");
        panel.Events.Add(new EventHandlerBinding("Paint", "canvasPanel_Paint", null));
        model.Controls[panel.FieldName] = panel;

        Assert.Empty(PaintLayerInserter.Insert(model, NoMembers, isUserControl: false));
        Assert.Single(panel.Events);
        Assert.Empty(model.PaintLayers);
    }

    [Theory]
    [InlineData(false, "ClientSize")]
    [InlineData(true, "Size")]
    public void Insert_FormPaint_LaysALayerUnderEveryRootControl(bool isUserControl, string sizeProperty)
    {
        var model = new FormModel { ClassName = "MainForm" };
        model.FormProperties[sizeProperty] = new PropertyValue.SizeValue(400, 300);
        model.FormEvents.Add(new EventHandlerBinding("Paint", "MainForm_Paint", null));
        var button = Control("mainFormPaintLayer", "Button");
        model.Controls[button.FieldName] = button;
        model.RootControls.Add(button);

        PaintLayerInserter.Insert(model, NoMembers, isUserControl);

        // The obvious name was a control's already.
        var layer = model.RootControls[0];
        Assert.Equal("mainFormPaintLayer2", layer.FieldName);
        Assert.Equal(new PropertyValue.SizeValue(400, 300), layer.Properties["Size"]);
        Assert.Empty(model.FormEvents);
        Assert.Equal("mainFormPaintLayer2", model.PaintLayers[""]);
    }
}
