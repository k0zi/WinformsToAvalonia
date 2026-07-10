using Converter.Cli.Services;
using Converter.Core.Configuration;
using Microsoft.CodeAnalysis.CSharp;

namespace Converter.Tests.Services;

public class EventHandlerMigrationEndToEndTests
{
    private const string DesignerContent = """
        namespace SampleApp
        {
            partial class HandlerBodyForm
            {
                private System.Windows.Forms.Button button1;

                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.button1.Name = "button1";
                    this.button1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.button1_MouseDown);
                    this.Controls.Add(this.button1);
                    this.Name = "HandlerBodyForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string CodeBehindContent = """
        namespace SampleApp
        {
            partial class HandlerBodyForm
            {
                private void button1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
                {
                    System.Windows.Forms.MessageBox.Show("Button was pressed");
                }
            }
        }
        """;

    private static ConverterConfig BaselineConfig(bool eventHandlerMigrationEnabled) => new()
    {
        GitIntegration = new GitIntegrationConfig { Enabled = false },
        Documentation = new DocumentationConfig { Enabled = false },
        EventHandlerMigration = new EventHandlerMigrationConfig { Enabled = eventHandlerMigrationEnabled },
        // Pinned explicitly: the fallback namespace (Path.GetFileName(_outputPath)) isn't
        // sanitized for C# identifier validity, and CreateTempSubdirectory names contain
        // hyphens - fine for every other test here since none of them Roslyn-parse the
        // generated code-behind, but this test does and would otherwise fail on an unrelated,
        // pre-existing gap.
        NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
    };

    [Fact]
    public async Task ExecuteAsync_WithSiblingCodeBehind_EmbedsOriginalBodyAndCompiles()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "HandlerBodyForm.Designer.cs"), DesignerContent);
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "HandlerBodyForm.cs"), CodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var codeBehindContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "HandlerBodyForm.axaml.cs"));

            Assert.Contains("private void button1_MouseDown(object? sender, Avalonia.Input.PointerPressedEventArgs e)", codeBehindContent);
            Assert.Contains("MessageBox.Show(\"Button was pressed\");", codeBehindContent);
            Assert.Matches(@"//\s*System\.Windows\.Forms\.MessageBox\.Show\(""Button was pressed""\);", codeBehindContent);

            var tree = CSharpSyntaxTree.ParseText(codeBehindContent);
            var errors = tree.GetDiagnostics()
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .ToList();
            Assert.Empty(errors);

            var axamlContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "HandlerBodyForm.axaml"));
            Assert.Contains("PointerPressed=\"button1_MouseDown\"", axamlContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NoSiblingCodeBehind_EmitsPortManuallyTodoInstead()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "HandlerBodyForm.Designer.cs"), DesignerContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var codeBehindContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "HandlerBodyForm.axaml.cs"));

            Assert.Contains("TODO: original \"button1_MouseDown\" handler body not found - port manually", codeBehindContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_EventHandlerMigrationDisabled_PreservesPriorBehavior()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "HandlerBodyForm.Designer.cs"), DesignerContent);
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "HandlerBodyForm.cs"), CodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(false)).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var codeBehindContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "HandlerBodyForm.axaml.cs"));

            // Even with EventHandlerMigration disabled (which only gates *body extraction*),
            // CodeBehindGenerator still emits a correctly-signed stub with a "not found"
            // placeholder - EventHandlerBodies is simply never populated.
            Assert.Contains("TODO: original \"button1_MouseDown\" handler body not found - port manually", codeBehindContent);
            Assert.DoesNotContain("MessageBox.Show", codeBehindContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
