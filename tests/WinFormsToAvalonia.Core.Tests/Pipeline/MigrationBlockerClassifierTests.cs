using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Pipeline;

public class MigrationBlockerClassifierTests
{
    private static readonly Dictionary<string, ControlModel> Controls = new(StringComparer.Ordinal)
    {
        ["comboBox1"] = new ControlModel { FieldName = "comboBox1", ClrTypeName = "ComboBox" },
        ["label1"] = new ControlModel { FieldName = "label1", ClrTypeName = "Label" },
    };

    private static readonly HashSet<string> ControlTypes = new(StringComparer.Ordinal) { "Button", "Label", "ComboBox" };

    [Theory]
    [InlineData("var b = new Button { Text = \"x\" };\nControls.Add(b);", "Runtime control creation: new Button")]
    [InlineData("this.Controls.Add(panel);", "Runtime control creation: Controls.Add")]
    [InlineData("MessageBox.Show(comboBox1.Text);", "Dialog: MessageBox.Show")]
    [InlineData("new Form2().ShowDialog();", "Dialog: ShowDialog")]
    [InlineData("e.Graphics.DrawCurve(pen, points);", "Graphics: DrawCurve")]
    [InlineData("using (var p = new Pen(Color.Black)) { }", "Graphics: Pen")]
    [InlineData("label1.Font = new Font(\"Arial\", 9);", "Graphics: Font")]
    [InlineData("comboBox1.Items.Insert(0, \"a\");", "Items collection: Items.Insert")]
    [InlineData("var l = (Label)sender;", "Cast: (Label)")]
    [InlineData("if (sender is Label l) { }", "Type test: is Label")]
    [InlineData("label1.Font.Bold.ToString();", "Control member: Label.Font")]
    [InlineData("StartNewGame();", "Call: StartNewGame()")]
    [InlineData("client.Close();", "Call: client.Close()")]
    [InlineData("this.CurrentPage = 3;", "Form member: CurrentPage")]
    [InlineData("if (formatInfo) { label.Text = \"\"; }", "Member: label.Text")]
    [InlineData("x = y;", "Statement: assignment")]
    public void Classify_FirstStatement_NamesItsBucket(string remainder, string expected) =>
        Assert.Equal(expected, MigrationBlockerClassifier.Classify(remainder, Controls, ControlTypes));

    [Fact]
    public void Classify_OnlyTheFirstStatementCounts()
    {
        var bucket = MigrationBlockerClassifier.Classify("StartNewGame();\nMessageBox.Show(\"x\");", Controls, ControlTypes);
        Assert.Equal("Call: StartNewGame()", bucket);
    }

    [Fact]
    public void Classify_CommentOnlyRemainder_IsNull() =>
        Assert.Null(MigrationBlockerClassifier.Classify("// nothing left\n", Controls, ControlTypes));

    [Fact]
    public void Histogram_CountsAndOrdersMostFrequentFirst()
    {
        var histogram = MigrationBlockerClassifier.Histogram(["b", "a", "b", "c", "a", "b"]);
        Assert.Equal(["b", "a", "c"], histogram.Select(p => p.Key));
        Assert.Equal([3, 2, 1], histogram.Select(p => p.Value));
    }
}
