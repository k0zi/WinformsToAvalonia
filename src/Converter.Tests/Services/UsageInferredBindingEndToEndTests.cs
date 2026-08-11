using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Services;

/// <summary>
/// End-to-end proof that a control property read/written from 2+ distinct migrated methods -
/// with no ".DataBindings.Add(...)" call anywhere - still gets a real [ObservableProperty]
/// binding via the real ConversionOrchestrator.ExecuteAsync() pipeline. Mirrors the shape found
/// in the real WarehouseApp sample (LoadFromEntity/SaveToEntity pairs), which never calls
/// DataBindings.Add at all.
/// </summary>
public class UsageInferredBindingEndToEndTests
{
    private const string DesignerContent = """
        namespace SampleApp
        {
            partial class ProductForm
            {
                private System.Windows.Forms.TextBox skuTextBox;
                private System.Windows.Forms.TextBox notesTextBox;

                private void InitializeComponent()
                {
                    this.skuTextBox = new System.Windows.Forms.TextBox();
                    this.notesTextBox = new System.Windows.Forms.TextBox();
                    this.SuspendLayout();
                    this.skuTextBox.Name = "skuTextBox";
                    this.notesTextBox.Name = "notesTextBox";
                    this.Controls.Add(this.skuTextBox);
                    this.Controls.Add(this.notesTextBox);
                    this.Name = "ProductForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string CodeBehindContent = """
        namespace SampleApp
        {
            partial class ProductForm
            {
                private void LoadFromEntity()
                {
                    skuTextBox.Text = entity.Sku;
                }

                private void SaveToEntity()
                {
                    entity.Sku = skuTextBox.Text;
                }

                private void ClearNotes()
                {
                    // Referenced from only one member - must not be promoted.
                    notesTextBox.Text = string.Empty;
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
    public async Task ExecuteAsync_ControlReferencedFromTwoMembersWithNoDataBindings_GetsInferredObservableProperty()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ProductForm.Designer.cs"), DesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ProductForm.cs"), CodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var vmGenPath = Path.Combine(outputDir, "ViewModels", "ProductFormViewModel.g.cs");
            Assert.True(File.Exists(vmGenPath));
            var vmGenContent = await File.ReadAllTextAsync(vmGenPath);
            Assert.Contains("[ObservableProperty]", vmGenContent);
            Assert.Contains("string sku", vmGenContent);
            // notesTextBox.Text is touched from only one member - not promoted.
            Assert.DoesNotContain("notes", vmGenContent, StringComparison.OrdinalIgnoreCase);

            var axamlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "ProductForm.axaml"));
            Assert.Contains("Name=\"skuTextBox\"", axamlContent);
            Assert.Contains("{Binding Sku}", axamlContent);

            var editableVmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "ProductFormViewModel.cs"));
            // The migrated method bodies must be rewritten to use the new ViewModel property
            // instead of a dangling "skuTextBox" reference the ViewModel has no view of.
            Assert.Contains("Sku = entity.Sku", editableVmContent);
            Assert.Contains("entity.Sku = Sku", editableVmContent);
            Assert.DoesNotContain("skuTextBox", editableVmContent);

            // notesTextBox stayed unbound (single-member usage) - still flagged as a manual step
            // instead of silently left as a dangling reference.
            Assert.Contains(result.Report!.ManualSteps, s =>
                s.Category == "Command Logic References View-Only Control" ||
                (s.Title.Contains("ClearNotes") && s.Title.Contains("notesTextBox")));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
