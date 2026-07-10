using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Documentation;

public class MigrationGuideTests
{
    [Fact]
    public async Task ExecuteAsync_UnmappedControlAndCustomLogicProperty_PopulatesManualSteps()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            const string designerContent = """
                namespace SampleApp
                {
                    partial class MixedForm
                    {
                        private System.Windows.Forms.Button button1;
                        private Vendor.Widgets.Gauge gauge1;

                        private void InitializeComponent()
                        {
                            this.button1 = new System.Windows.Forms.Button();
                            this.gauge1 = new Vendor.Widgets.Gauge();
                            this.SuspendLayout();
                            this.button1.Font = new System.Drawing.Font("Segoe UI", 9F);
                            this.button1.Name = "button1";
                            this.gauge1.Name = "gauge1";
                            this.Controls.Add(this.button1);
                            this.Controls.Add(this.gauge1);
                            this.Name = "MixedForm";
                            this.ResumeLayout(false);
                        }
                    }
                }
                """;

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "MixedForm.Designer.cs"), designerContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var guidePath = Path.Combine(outputDir, "MIGRATION_GUIDE.md");
            Assert.True(File.Exists(guidePath));

            var guideContent = await File.ReadAllTextAsync(guidePath);

            Assert.DoesNotContain("No manual steps required", guideContent);
            Assert.Contains("Unmapped Controls", guideContent);
            Assert.Contains("gauge1", guideContent);
            Assert.Contains("Custom Property Logic", guideContent);
            Assert.Contains("button1.Font", guideContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_FullyMappedForm_ReportsNoManualStepsRequired()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            const string designerContent = """
                namespace SampleApp
                {
                    partial class SimpleForm
                    {
                        private System.Windows.Forms.Button button1;

                        private void InitializeComponent()
                        {
                            this.button1 = new System.Windows.Forms.Button();
                            this.SuspendLayout();
                            this.button1.Name = "button1";
                            this.Controls.Add(this.button1);
                            this.Name = "SimpleForm";
                            this.ResumeLayout(false);
                        }
                    }
                }
                """;

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "SimpleForm.Designer.cs"), designerContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var guideContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MIGRATION_GUIDE.md"));
            Assert.Contains("No manual steps required", guideContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
