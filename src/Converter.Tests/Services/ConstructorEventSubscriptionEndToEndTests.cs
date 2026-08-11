using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Services;

/// <summary>
/// End-to-end proof that a form's own lifecycle events, wired manually in the regular
/// constructor (not via the Designer) - a very common WinForms idiom - get correctly
/// discovered and wired through the real ConversionOrchestrator.ExecuteAsync() pipeline: the
/// generated AXAML gets a "Closing=" attribute, and the code-behind stub uses the
/// Avalonia-correct WindowClosingEventArgs signature instead of the original
/// FormClosingEventArgs (which has no Avalonia equivalent and would otherwise be a build
/// error).
/// </summary>
public class ConstructorEventSubscriptionEndToEndTests
{
    private const string DesignerContent = """
        namespace SampleApp
        {
            partial class MainForm
            {
                private void InitializeComponent()
                {
                    this.SuspendLayout();
                    this.Name = "MainForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string CodeBehindContent = """
        namespace SampleApp
        {
            partial class MainForm
            {
                public MainForm()
                {
                    InitializeComponent();
                    Load += MainForm_Load;
                    FormClosing += MainForm_FormClosing;
                }

                private void MainForm_Load(object? sender, System.EventArgs e)
                {
                }

                private void MainForm_FormClosing(object? sender, System.Windows.Forms.FormClosingEventArgs e)
                {
                }
            }
        }
        """;

    private static ConverterConfig BaselineConfig() => new()
    {
        GitIntegration = new GitIntegrationConfig { Enabled = false },
        Documentation = new DocumentationConfig { Enabled = true },
        NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
    };

    [Fact]
    public async Task ExecuteAsync_ConstructorWiredLifecycleEvents_AreWiredIntoAxamlAndCodeBehind()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "MainForm.Designer.cs"), DesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "MainForm.cs"), CodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var axamlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "MainForm.axaml"));
            Assert.Contains("Loaded=\"MainForm_Load\"", axamlContent);
            Assert.Contains("Closing=\"MainForm_FormClosing\"", axamlContent);

            var codeBehindContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "MainForm.axaml.cs"));
            Assert.Contains("MainForm_FormClosing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)", codeBehindContent);
            Assert.DoesNotContain("FormClosingEventArgs", codeBehindContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
