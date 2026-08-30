using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// End-to-end coverage of the code-behind migration split: one Form whose five handlers cover
/// every branch of the rule (promotable, Form-driving, EventArgs-using, no Avalonia equivalent,
/// Form lifecycle), converted and then actually `dotnet build`-ed. The build is the point - the
/// Avalonia XAML compiler rejects a Command binding to a missing ViewModel property or an event
/// attribute a control does not have, so only a green build proves the wiring is real.
/// </summary>
public class CodeBehindMigrationTests
{
    [Fact]
    public async Task ConvertedApp_SplitsHandlersBetweenViewModelAndCodeBehindAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "CodeBehindMigrationApp", "CodeBehindMigrationApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-codebehind-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            var vfs = result.Vfs;

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind);
            vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel);

            // greetButton_Click reads/writes only bindable value properties -> a RelayCommand,
            // bound from the AXAML, with an ObservableProperty per property it touches.
            Assert.Contains("Command=\"{Binding GreetButtonCommand}\"", axaml);
            Assert.Contains("[RelayCommand]", viewModel);
            Assert.Contains("private void GreetButton()", viewModel);
            Assert.Contains("public partial string NameTextBoxText { get; set; } = \"world\";", viewModel);
            Assert.Contains("public partial string GreetingLabelText { get; set; } = \"greeting\";", viewModel);
            Assert.Contains("Text=\"{Binding NameTextBoxText, Mode=TwoWay}\"", axaml);
            Assert.Contains("Text=\"{Binding GreetingLabelText, Mode=TwoWay}\"", axaml);

            // The designer literal moved to the ViewModel, so it is not also a plain attribute.
            Assert.DoesNotContain("Text=\"world\"", axaml);

            // clearButton_Click calls Close() -> stays event-driven.
            Assert.Contains("Click=\"clearButton_Click\"", axaml);
            Assert.Contains("private void clearButton_Click(object? sender, RoutedEventArgs e)", codeBehind);
            Assert.DoesNotContain("ClearButtonCommand", viewModel);

            // canvasPanel_MouseDown needs the pointer position -> stays event-driven.
            Assert.Contains("PointerPressed=\"canvasPanel_MouseDown\"", axaml);
            Assert.Contains("private void canvasPanel_MouseDown(object? sender, PointerPressedEventArgs e)", codeBehind);

            // Avalonia has no Paint event - drawing is a Render(DrawingContext) override, which is
            // a subclass - so a childless Panel with a Paint handler becomes the bundled surface
            // that turns that override back into the event, and the body really translates.
            Assert.Contains("private void canvasPanel_Paint(object? sender, PaintSurfaceEventArgs e)", codeBehind);
            Assert.Contains(
                "e.Context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse(\"#FF000000\"))), "
                + "new Rect(0, 0, 10, 10));",
                codeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(canvasPanel_Paint)", codeBehind);

            // Still never an AXAML attribute: it is a CLR event on a template, so the constructor
            // subscribes it.
            Assert.DoesNotContain("Paint=", axaml);
            Assert.Contains("canvasPanel.Paint += canvasPanel_Paint;", codeBehind);
            Assert.Contains("<controls:PaintSurfaceFallback x:Name=\"canvasPanel\"", axaml);

            // And the *other* handler on that same control survives the retarget - a bundled
            // template is a real Avalonia control, so the events it inherits from Control can
            // carry an attribute like any element's. They used to be dropped with a warning.
            Assert.Contains("PointerPressed=\"canvasPanel_MouseDown\"", axaml);

            // Form.Load becomes the Window's Opened event - the one raised as the window opens,
            // where Loaded is raised only after layout and render, with the window already up.
            Assert.Contains("Opened=\"MainForm_Load\"", axaml);

            // A promoted body is translated against the ViewModel's own properties: promotion
            // already proved every member it touches is bindable, so nothing is left to comment.
            Assert.Contains("GreetingLabelText = \"Hello, \" + NameTextBoxText;", viewModel);
            Assert.DoesNotContain("ORIGINAL WINFORMS BODY", viewModel);

            // The same happens in code-behind wherever the statements are provably equivalent -
            // here a bindable property write plus the Window's own Close().
            Assert.Contains("nameTextBox.Text = string.Empty;", codeBehind);
            Assert.Contains("Close();", codeBehind);

            // A code-behind read touches the Avalonia member directly, with no binding in between
            // to convert - so it has to come out as the type the WinForms expression had. Both of
            // these compiled to a CS0266 in the generated project until the catalog said what the
            // Avalonia side really is: `IsChecked` is a bool?, `Content` an object?.
            Assert.Contains("if ((agreeCheckBox.IsChecked ?? false))", codeBehind);
            // A value that changes shape on the way across: a WinForms bool, an Avalonia enum.
            Assert.Contains(
                "nameTextBox.TextWrapping = ((agreeCheckBox.IsChecked ?? false)) ? TextWrapping.Wrap : TextWrapping.NoWrap;",
                codeBehind);
            Assert.Contains("greetingLabel.Text = (readBackButton.Content as string ?? string.Empty);", codeBehind);

            // A body that needs 'sender'/EventArgs still cannot be translated, and survives
            // verbatim as the comment inside the method that replaced it.
            Assert.Contains("panel.Text = e.X + \",\" + e.Y;", codeBehind);
            Assert.Contains("MigrationTodo.NotMigrated(nameof(canvasPanel_MouseDown)", codeBehind);

            // The report quantifies exactly how much of the hand-written code came across.
            Assert.True(
                result.Report.MigratedStatementCount > 0
                && result.Report.MigratedStatementCount < result.Report.HandlerStatementCount,
                $"expected a partial migration, got {result.Report.MigratedStatementCount}/{result.Report.HandlerStatementCount}");

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);
            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

}
