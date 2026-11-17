using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class OverrideHandlerLifterTests
{
    private static readonly EventMappingRegistry Events = new();

    private static (FormModel Model, OverrideHandlerLifter.Result? Result) Lift(string members, params EventHandlerBinding[] designerEvents)
    {
        var model = new FormModel { ClassName = "MainForm" };
        model.FormEvents.AddRange(designerEvents);
        var source = $$"""
            using System;
            using System.Windows.Forms;

            public partial class MainForm : Form
            {
            {{members}}
            }
            """;

        return (model, OverrideHandlerLifter.Lift(source, "MainForm", model, new HashSet<string>(StringComparer.Ordinal), Events));
    }

    [Fact]
    public void Lift_OnLoadCallingBaseFirst_BecomesALoadHandlerWithoutTheBaseCall()
    {
        var (model, result) = Lift("""
                protected override void OnLoad(EventArgs e)
                {
                    base.OnLoad(e);
                    Text = "ready";
                }
            """);

        Assert.NotNull(result);
        var binding = Assert.Single(model.FormEvents);
        Assert.Equal(new EventHandlerBinding("Load", "MainForm_Load", null), binding);
        Assert.Contains("private void MainForm_Load(object? sender, EventArgs e)", result.Source);
        Assert.Contains("Text = \"ready\";", result.Source);
        Assert.DoesNotContain("base.OnLoad", result.Source);
        Assert.DoesNotContain("override", result.Source);
        Assert.Equal("OnLoad", result.LiftedFrom["MainForm_Load"]);
    }

    /// <summary>
    /// With nobody else subscribed, where the base call sits is unobservable - and so is its absence.
    /// </summary>
    [Theory]
    [InlineData("Text = \"x\"; base.OnShown(e);")]
    [InlineData("Text = \"x\";")]
    public void Lift_NoOtherSubscriber_BaseCallPositionDoesNotMatter(string body)
    {
        var (model, result) = Lift($"protected override void OnShown(EventArgs e) {{ {body} }}");

        Assert.NotNull(result);
        Assert.Equal("Shown", Assert.Single(model.FormEvents).EventName);
    }

    /// <summary>
    /// With a designer handler on the same event, only "raise it, then do my part" is reproducible:
    /// the lifted handler is subscribed after the designer's, so it runs after it.
    /// </summary>
    [Theory]
    [InlineData("base.OnLoad(e); Text = \"x\";", true)]
    [InlineData("Text = \"x\"; base.OnLoad(e);", false)]
    [InlineData("Text = \"x\";", false)]
    public void Lift_AnotherLoadHandlerExists_OnlyABaseCallFirstLifts(string body, bool lifts)
    {
        var (model, result) = Lift(
            $"protected override void OnLoad(EventArgs e) {{ {body} }}",
            new EventHandlerBinding("Load", "MainForm_Load", null));

        Assert.Equal(lifts, result is not null);
        Assert.Equal(lifts ? 2 : 1, model.FormEvents.Count);
        if (lifts)
        {
            // The designer's own name was taken - the override's gets the marker.
            Assert.Equal("MainFormOverride_Load", model.FormEvents[1].HandlerMethodName);
        }
    }

    [Theory]
    // Not an event the generated root raises.
    [InlineData("protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); }")]
    // Another base member, which the lifted handler has no base for.
    [InlineData("protected override void OnLoad(EventArgs e) { base.OnLoad(e); base.Refresh(); }")]
    // The lifted method gains a `sender` parameter, which would capture this one.
    [InlineData("protected override void OnLoad(EventArgs e) { base.OnLoad(e); var sender = 1; }")]
    // The base call twice raises the event twice.
    [InlineData("protected override void OnLoad(EventArgs e) { base.OnLoad(e); base.OnLoad(e); }")]
    // The base call in the middle, with something after it.
    [InlineData("protected override void OnLoad(EventArgs e) { Text = \"a\"; base.OnLoad(e); Text = \"b\"; }")]
    // Not void.
    [InlineData("protected override bool ProcessDialogKey(Keys keyData) { return true; }")]
    public void Lift_ShapesWithoutAnEquivalentHandler_StayAsTheyAre(string member)
    {
        var (model, result) = Lift(member);

        Assert.Null(result);
        Assert.Empty(model.FormEvents);
    }

    /// <summary>An OnPaint override is a Paint binding, which the paint layer then gives a surface.</summary>
    [Fact]
    public void Lift_OnPaint_BecomesAPaintBindingKeepingItsArgsType()
    {
        var (model, result) = Lift("""
                protected override void OnPaint(PaintEventArgs pe)
                {
                    base.OnPaint(pe);
                    pe.Graphics.DrawLine(Pens.Black, 0, 0, 10, 10);
                }
            """);

        Assert.NotNull(result);
        Assert.Equal("Paint", Assert.Single(model.FormEvents).EventName);
        Assert.Contains("private void MainForm_Paint(object? sender, PaintEventArgs pe)", result.Source);
    }
}
