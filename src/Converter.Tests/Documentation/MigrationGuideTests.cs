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
                            this.button1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
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
            Assert.Contains("button1.Anchor", guideContent);
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

    [Fact]
    public async Task ExecuteAsync_RequiresCustomLogicPropertyThatConvertsSuccessfully_DoesNotFlagManualStep()
    {
        // Regression: "Custom Property Logic" used to fire for every RequiresCustomLogic
        // property regardless of whether PropertyValueConverter actually handled it - Font is
        // RequiresCustomLogic in PropertyMappingRegistry but PropertyValueConverter.TryConvertFont
        // successfully converts it, so this used to be a false positive.
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            const string designerContent = """
                namespace SampleApp
                {
                    partial class FontForm
                    {
                        private System.Windows.Forms.Button button1;

                        private void InitializeComponent()
                        {
                            this.button1 = new System.Windows.Forms.Button();
                            this.SuspendLayout();
                            this.button1.Font = new System.Drawing.Font("Segoe UI", 9F);
                            this.button1.Name = "button1";
                            this.Controls.Add(this.button1);
                            this.Name = "FontForm";
                            this.ResumeLayout(false);
                        }
                    }
                }
                """;

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "FontForm.Designer.cs"), designerContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);
            Assert.DoesNotContain(result.Report!.ManualSteps, s => s.Category == "Custom Property Logic");

            var guideContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MIGRATION_GUIDE.md"));
            Assert.DoesNotContain("Custom Property Logic", guideContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_PaintEventSubscribed_NowFlagsManualStep()
    {
        // Regression: RequiresCustomLogic *events* (Paint/TextChanged/ValueChanged/
        // CheckedChanged/Validating/Validated) used to get zero manual step at all -
        // CollectManualStepsRecursive's event loop only branched on InlineLambdaHandlerMarker
        // and PreserveEventHandler, silently dropping Paint with no signal whatsoever. Paint
        // has no automation path (different rendering model), so it must always be flagged now.
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            const string designerContent = """
                namespace SampleApp
                {
                    partial class PaintForm
                    {
                        private System.Windows.Forms.Panel panel1;

                        private void InitializeComponent()
                        {
                            this.panel1 = new System.Windows.Forms.Panel();
                            this.SuspendLayout();
                            this.panel1.Name = "panel1";
                            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
                            this.Controls.Add(this.panel1);
                            this.Name = "PaintForm";
                            this.ResumeLayout(false);
                        }
                    }
                }
                """;

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "PaintForm.Designer.cs"), designerContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Contains(result.Report!.ManualSteps, s => s.Category == "Custom Event Logic" && s.Title.Contains("Paint"));

            var guideContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MIGRATION_GUIDE.md"));
            Assert.Contains("Custom Event Logic", guideContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_TextChangedWithBoundProperty_AutoMigratesWithoutManualStep()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            const string designerContent = """
                namespace SampleApp
                {
                    partial class BoundTextChangedForm
                    {
                        private System.Windows.Forms.BindingSource customerBindingSource;
                        private System.Windows.Forms.TextBox textBox1;

                        private void InitializeComponent()
                        {
                            this.customerBindingSource = new System.Windows.Forms.BindingSource();
                            this.textBox1 = new System.Windows.Forms.TextBox();
                            this.SuspendLayout();
                            this.textBox1.Name = "textBox1";
                            this.textBox1.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.customerBindingSource, "CustomerName", true));
                            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
                            this.Controls.Add(this.textBox1);
                            this.Name = "BoundTextChangedForm";
                            this.ResumeLayout(false);
                        }
                    }
                }
                """;
            const string codeBehindContent = """
                namespace SampleApp
                {
                    partial class BoundTextChangedForm
                    {
                        private void textBox1_TextChanged(object sender, System.EventArgs e)
                        {
                            Validate();
                        }
                    }
                }
                """;

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "BoundTextChangedForm.Designer.cs"), designerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "BoundTextChangedForm.cs"), codeBehindContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);
            Assert.DoesNotContain(result.Report!.ManualSteps, s => s.Category == "Custom Event Logic");

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "BoundTextChangedFormViewModel.cs"));
            Assert.Contains("partial void OnCustomerNameChanged(string value)", vmContent);
            Assert.Contains("Validate();", vmContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
