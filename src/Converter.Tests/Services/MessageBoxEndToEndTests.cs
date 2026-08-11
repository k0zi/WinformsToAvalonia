using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Services;

/// <summary>
/// End-to-end proof that a migrated helper method calling MessageBox.Show(...) gets rewritten
/// against the generated Dialogs helper via the real ConversionOrchestrator.ExecuteAsync()
/// pipeline - not just the MessageBoxTranspiler unit itself, but the full wiring: the
/// conditional Common/Dialogs.cs + friends generation, and the "Migrated Logic May Not Compile"
/// manual step no longer wrongly flagging what actually got rewritten.
/// </summary>
public class MessageBoxEndToEndTests
{
    private const string DesignerContent = """
        namespace SampleApp
        {
            partial class ConfirmForm
            {
                private System.Windows.Forms.TextBox nameTextBox;

                private void InitializeComponent()
                {
                    this.nameTextBox = new System.Windows.Forms.TextBox();
                    this.SuspendLayout();
                    this.nameTextBox.Name = "nameTextBox";
                    this.Controls.Add(this.nameTextBox);
                    this.Name = "ConfirmForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string CodeBehindContent = """
        namespace SampleApp
        {
            partial class ConfirmForm
            {
                private void ValidateAndWarn()
                {
                    if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        MessageBox.Show(this, "Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
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
    public async Task ExecuteAsync_HelperMethodCallsMessageBoxShow_RewritesAndGeneratesDialogsInfrastructure()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ConfirmForm.Designer.cs"), DesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ConfirmForm.cs"), CodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "ConfirmFormViewModel.cs"));

            Assert.Contains("internal async void ValidateAndWarn()", vmContent);
            Assert.Contains("await SampleApp.Common.Dialogs.ShowAsync(", vmContent);
            Assert.Contains("SampleApp.Common.MessageBoxButtons.OK", vmContent);
            Assert.Contains("SampleApp.Common.MessageBoxIcon.Warning", vmContent);
            Assert.DoesNotContain("MessageBox.Show", vmContent);

            // The generated dialog infrastructure must exist, and only because this run
            // actually used it.
            Assert.True(File.Exists(Path.Combine(outputDir, "Common", "Dialogs.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Common", "MessageBoxTypes.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Views", "MessageBoxWindow.axaml")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Views", "MessageBoxWindow.axaml.cs")));

            // The rewrite means this is no longer "Migrated Logic May Not Compile" - the stale
            // false-positive this exact scenario used to produce.
            Assert.DoesNotContain(result.Report!.ManualSteps, s =>
                s.Category == "Migrated Logic May Not Compile" && s.Title.Contains("ValidateAndWarn"));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NoMessageBoxUsage_DoesNotGenerateDialogsInfrastructure()
    {
        const string plainDesigner = """
            namespace SampleApp
            {
                partial class PlainForm
                {
                    private void InitializeComponent()
                    {
                        this.SuspendLayout();
                        this.Name = "PlainForm";
                        this.ResumeLayout(false);
                    }
                }
            }
            """;

        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "PlainForm.Designer.cs"), plainDesigner);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            Assert.False(File.Exists(Path.Combine(outputDir, "Common", "Dialogs.cs")));
            Assert.False(File.Exists(Path.Combine(outputDir, "Views", "MessageBoxWindow.axaml")));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
