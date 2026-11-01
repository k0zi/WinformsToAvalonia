using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class InlineHandlerLifterTests
{
    private static (FormModel Model, IReadOnlyList<string> Methods) LiftOne(string lambda, params string[] taken)
    {
        var model = new FormModel { ClassName = "Form1" };
        var button = new ControlModel { FieldName = "goButton", ClrTypeName = "Button" };
        button.Events.Add(new EventHandlerBinding("Click", null, lambda));
        model.Controls[button.FieldName] = button;

        return (model, InlineHandlerLifter.Lift(model, new HashSet<string>(taken, StringComparer.Ordinal)));
    }

    [Theory]
    [InlineData("(s, e) => { Run(); }", "private void goButton_Click(object? s, EventArgs e)")]
    [InlineData("(_, _) => Run()", "private void goButton_Click(object? sender, EventArgs e)")]
    [InlineData("delegate { Run(); }", "private void goButton_Click(object? sender, EventArgs e)")]
    [InlineData("(object o, MouseEventArgs m) => Run()", "private void goButton_Click(object o, MouseEventArgs m)")]
    [InlineData("async (s, e) => await Task.Delay(1)", "private async void goButton_Click(object? s, EventArgs e)")]
    public void Lift_TurnsEachShapeIntoAHandlerMethod(string lambda, string expectedSignature)
    {
        var (model, methods) = LiftOne(lambda);

        var method = Assert.Single(methods);
        Assert.StartsWith(expectedSignature, method);
        Assert.Equal("goButton_Click", model.Controls["goButton"].Events[0].HandlerMethodName);
    }

    [Fact]
    public void Lift_TakenName_MarksTheOwnerHalfSoTheCommandNameStaysClean()
    {
        var (model, _) = LiftOne("(s, e) => Run()", "goButton_Click", "goButtonInline_Click");

        Assert.Equal("goButtonInline2_Click", model.Controls["goButton"].Events[0].HandlerMethodName);
    }

    [Fact]
    public void InjectInto_AddsToTheClassOrCreatesAPartialOne()
    {
        const string method = "private void a_Click(object? sender, EventArgs e) { }";

        var injected = InlineHandlerLifter.InjectInto("namespace Demo { public partial class Form1 : Form { } }", "Form1", "Demo", [method]);
        Assert.Contains("a_Click", injected);
        Assert.Contains("public partial class Form1 : Form", injected);

        var created = InlineHandlerLifter.InjectInto(null, "Form1", "Demo", [method]);
        Assert.Contains("partial class Form1", created);
        Assert.Contains("a_Click", created);
    }
}
