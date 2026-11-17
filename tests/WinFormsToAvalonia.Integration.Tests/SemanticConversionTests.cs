using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// The semantic half of the conversion: the project compiled against the base class library
/// without WinForms, so that code which provably touches no UI comes across verbatim - the
/// project's plain source files, a referenced class library, fields of the project's own types,
/// the constructor, and handler statements the syntactic rules could not see into.
/// </summary>
public class SemanticConversionTests
{
    private static string SourceProject =>
        Path.Combine(AppContext.BaseDirectory, "SampleApps", "SemanticApp", "App", "App.csproj");

    [Fact]
    public async Task ConvertedSemanticApp_CarriesUiNeutralCodeAndBuildsWarningFree()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-semantic-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(SourceProject, outputDir));

            // The project's own plain file, minus the WinForms using a template left in it.
            Assert.True(result.Vfs.TryGetText("Services/Calculator.cs", out var calculator));
            Assert.StartsWith("#nullable disable", calculator);
            Assert.DoesNotContain("System.Windows.Forms", calculator);
            Assert.Contains("public int Add(int a, int b) => a + b;", calculator);

            // A referenced .NET Framework class library, under a folder named after it - but not
            // its AssemblyInfo, which would duplicate the generated project's attributes.
            Assert.True(result.Vfs.TryGetText("Lib/Scores.cs", out _));
            Assert.False(result.Vfs.TryGetText("Lib/Properties/AssemblyInfo.cs", out _));
            Assert.False(result.Vfs.TryGetText("Program.cs", out var program) && program.Contains("Application.Run"));

            // System.Drawing means something else in Avalonia: left behind, and said so.
            Assert.False(result.Vfs.TryGetText("Services/Painter.cs", out _));
            Assert.Contains(result.Report.Warnings, w => w.Contains("Services/Painter.cs") && w.Contains("System.Drawing"));

            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var view));
            var generated = view.Split("ORIGINAL WINFORMS CODE-BEHIND")[0];

            Assert.StartsWith("#nullable disable warnings", generated);

            // Fields of the project's own types, written namespace-qualified so nothing can bind
            // to an Avalonia type of the same name.
            Assert.Contains("private readonly SemanticApp.Services.Calculator calculator = new SemanticApp.Services.Calculator();", generated);
            Assert.Contains("private SemanticLib.Scores scores = default!;", generated);
            Assert.Contains("private string[] formats = { \"Short\", \"Long\" };", generated);

            // The constructor's own statements: the one that is plain .NET translates; `this` is
            // the generated View there, a different object, so `this.GetType()` stays a comment.
            Assert.Contains("scores = new SemanticLib.Scores();", generated);
            Assert.Contains("ORIGINAL WINFORMS CONSTRUCTOR STATEMENTS", generated);
            Assert.DoesNotContain("ownerTag = this.GetType();\n", generated.Split("ORIGINAL WINFORMS CONSTRUCTOR STATEMENTS")[0]);

            // A list filled from code, and AddRange as the loop it stands for.
            Assert.Contains("formatBox.Items.Clear();", generated);
            Assert.Contains("foreach (var w2aItem in formats)", generated);
            Assert.Contains("formatBox.Items.Add(w2aItem);", generated);

            // Handler statements the syntactic rules refused for the names they use.
            Assert.Contains("int total = calculator.Add(2, 3);", generated);
            Assert.Contains("scores.Record(total);", generated);
            Assert.Contains("resultLabel.Text = (resultLabel.Text ?? string.Empty) + (\" \" + calculator.Describe(total));", generated);
            Assert.Contains("var format = (string)formatBox.Items[formatBox.SelectedIndex];", generated);

            // A NuGet package the code uses comes across - its file, its call, its reference - and a
            // polyfill .NET ships itself does not.
            Assert.True(result.Vfs.TryGetText("Services/Snapshot.cs", out _));
            Assert.Contains(
                "string json = Newtonsoft.Json.JsonConvert.SerializeObject(new SemanticApp.Services.Snapshot { Total = total });",
                generated);
            var csprojPath = Assert.Single(result.Vfs.RelativePaths, p => p.EndsWith(".csproj", StringComparison.Ordinal));
            Assert.True(result.Vfs.TryGetText(csprojPath, out var csproj));
            Assert.Contains("<PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.3\" />", csproj);
            Assert.DoesNotContain("Microsoft.CSharp", csproj);
            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("NuGet package"));

            // A handler shared by two controls of one type, naming the one that raised it.
            Assert.Contains("if (sender is not Button side)", generated);
            Assert.Contains("side.Content = \"chosen\";", generated);
            Assert.Contains("if (sender is TextBlock card)", generated);
            Assert.Contains("card.Text = \"open\";", generated);

            // A helper whose signature names a library type.
            Assert.Contains("private string Report(SemanticLib.Scores source)", generated);

            // The busy cursor and the cross-thread idiom, onto the Dispatcher.
            Assert.Contains("Cursor = new Cursor(StandardCursorType.Wait);", generated);
            Assert.Contains("if ((!Dispatcher.UIThread.CheckAccess()))", generated);
            Assert.Contains("Dispatcher.UIThread.Invoke(() =>", generated);
            Assert.Contains("Dispatcher.UIThread.Post(() =>", generated);
            Assert.Contains("Cursor = Cursor.Default;", generated);
            Assert.Contains("using Avalonia.Threading;", generated);

            // An inherited member of `this` is asked of a different object in the generated View.
            Assert.Contains("this.Text = this.GetType().Name;", view.Split("REMAINING WINFORMS BODY")[1]);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);
            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
            Assert.DoesNotContain(": warning ", buildResult.StdOut);
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
