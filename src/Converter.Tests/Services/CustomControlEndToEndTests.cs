using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Services;

/// <summary>
/// End-to-end proof that a custom WinForms UserControl converts through the same real
/// ConversionOrchestrator.ExecuteAsync() pipeline a Form already uses (its own View +
/// ViewModel, with its own simple public property re-exposed as a bindable Avalonia property),
/// and that a Form embedding an instance of it references the converted View correctly instead
/// of the dead "&lt;!-- TODO: Unmapped control --&gt;" placeholder.
/// </summary>
public class CustomControlEndToEndTests
{
    private const string CustomerCardDesignerContent = """
        namespace SampleApp
        {
            partial class CustomerCard
            {
                private void InitializeComponent()
                {
                    this.SuspendLayout();
                    this.Name = "CustomerCard";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string CustomerCardCodeBehindContent = """
        namespace SampleApp
        {
            public partial class CustomerCard : System.Windows.Forms.UserControl
            {
                public int CustomerId { get; set; }

                public CustomerCard()
                {
                    InitializeComponent();
                }
            }
        }
        """;

    private const string MainFormDesignerContent = """
        namespace SampleApp
        {
            partial class MainForm
            {
                private SampleApp.CustomerCard customerCard1;

                private void InitializeComponent()
                {
                    this.customerCard1 = new SampleApp.CustomerCard();
                    this.SuspendLayout();
                    this.customerCard1.Name = "customerCard1";
                    this.customerCard1.CustomerId = 5;
                    this.Controls.Add(this.customerCard1);
                    this.Name = "MainForm";
                    this.ResumeLayout(false);
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
    public async Task ExecuteAsync_CustomUserControl_ConvertsAsItsOwnViewAndViewModel()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "CustomerCard.Designer.cs"), CustomerCardDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "CustomerCard.cs"), CustomerCardCodeBehindContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "MainForm.Designer.cs"), MainFormDesignerContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var cardAxaml = await File.ReadAllTextAsync(Path.Combine(outputDir, "Controls", "CustomerCard.axaml"));
            Assert.StartsWith("<UserControl", cardAxaml);
            Assert.Contains("</UserControl>", cardAxaml);
            Assert.Contains("x:Class=\"SampleApp.Controls.CustomerCard\"", cardAxaml);

            var cardCodeBehind = await File.ReadAllTextAsync(Path.Combine(outputDir, "Controls", "CustomerCard.axaml.cs"));
            Assert.Contains("public partial class CustomerCard : UserControl", cardCodeBehind);
            Assert.Contains("namespace SampleApp.Controls;", cardCodeBehind);
            Assert.Contains("public static readonly Avalonia.StyledProperty<int> CustomerIdProperty =", cardCodeBehind);
            Assert.Contains("public int CustomerId", cardCodeBehind);

            Assert.True(File.Exists(Path.Combine(outputDir, "ViewModels", "CustomerCardViewModel.cs")));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_FormEmbeddingCustomControl_ReferencesConvertedViewWithLiteralProperty()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "CustomerCard.Designer.cs"), CustomerCardDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "CustomerCard.cs"), CustomerCardCodeBehindContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "MainForm.Designer.cs"), MainFormDesignerContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var mainFormAxaml = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "MainForm.axaml"));
            Assert.StartsWith("<Window", mainFormAxaml);
            Assert.Contains("xmlns:controls=\"using:SampleApp.Controls\"", mainFormAxaml);
            Assert.Contains("<controls:CustomerCard", mainFormAxaml);
            Assert.Contains("Name=\"customerCard1\"", mainFormAxaml);
            Assert.Contains("CustomerId=\"5\"", mainFormAxaml);
            Assert.DoesNotContain("TODO: Unmapped control", mainFormAxaml);

            // The literal CustomerId=5 assignment was auto-bound, so no manual step is needed
            // for it - but the category itself should still be present and correctly framed
            // (not the generic "Unmapped Controls", since this control isn't actually unmapped).
            Assert.DoesNotContain(result.Report!.ManualSteps, s => s.Category == "Unmapped Controls");
            Assert.Contains(result.Report!.ManualSteps, s => s.Category == "Custom Control Instance");

            // MainForm (Window-rooted) must still be picked as MainWindow, not the UserControl.
            var appCodeBehind = await File.ReadAllTextAsync(Path.Combine(outputDir, "App.axaml.cs"));
            Assert.Contains("desktop.MainWindow = new MainForm();", appCodeBehind);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string OwnerDrawnControlContent = """
        namespace SampleApp
        {
            public class GaugeControl : System.Windows.Forms.Control
            {
                protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
                {
                    base.OnPaint(e);
                }
            }
        }
        """;

    private const string OwnerDrawnFormDesignerContent = """
        namespace SampleApp
        {
            partial class GaugeForm
            {
                private SampleApp.GaugeControl capacityGauge;

                private void InitializeComponent()
                {
                    this.capacityGauge = new SampleApp.GaugeControl();
                    this.SuspendLayout();
                    this.capacityGauge.Name = "capacityGauge";
                    this.Controls.Add(this.capacityGauge);
                    this.Name = "GaugeForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_FormEmbeddingOwnerDrawnControl_GetsSpecificManualStepMessage()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "GaugeControl.cs"), OwnerDrawnControlContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "GaugeForm.Designer.cs"), OwnerDrawnFormDesignerContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            // GaugeControl has no InitializeComponent (owner-drawn, OnPaint-only) so it's never
            // independently converted - the embedded instance stays "Unmapped Controls", but
            // with the same specific "Custom-drawn control..." message the file-level skip gets,
            // instead of the generic "has no Avalonia mapping".
            var step = Assert.Single(result.Report!.ManualSteps, s => s.Category == "Unmapped Controls");
            Assert.Contains("Custom-drawn control", step.Description);
            Assert.Contains("no control tree to convert into AXAML", step.Description);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
