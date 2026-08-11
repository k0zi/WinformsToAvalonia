using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Services;

/// <summary>
/// End-to-end regression coverage for two AXAML property-mapping bugs found via a real
/// WarehouseApp sample conversion review: a form deriving from a custom (non-"Form") base
/// class getting an invalid "Text=" attribute on its generated Window instead of "Title=", and
/// a usage-inferred binding for a property with no real PropertyMappingRegistry entry leaking
/// the raw WinForms property name straight into AXAML instead of being omitted.
/// </summary>
public class InvalidAxamlPropertyRegressionTests
{
    private static ConverterConfig BaselineConfig() => new()
    {
        GitIntegration = new GitIntegrationConfig { Enabled = false },
        Documentation = new DocumentationConfig { Enabled = true },
        NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
    };

    private const string CustomBaseFormDesignerContent = """
        namespace SampleApp
        {
            partial class DetailForm
            {
                private void InitializeComponent()
                {
                    this.SuspendLayout();
                    this.Text = "Detail — Sample App";
                    this.Name = "DetailForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    // No "CustomFormBase" definition needs to exist anywhere - SiblingFileResolver.
    // ResolveRootBaseTypeAsync just reads the declared base-list text off this partial class
    // declaration, mirroring how the real WarehouseApp sample's DetailFormBase<T>/
    // ListFormBase<T> forms are resolved.
    private const string CustomBaseFormCodeBehindContent = """
        namespace SampleApp
        {
            partial class DetailForm : CustomFormBase
            {
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_FormDerivingFromCustomBaseClass_GetsWindowTitleNotText()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "DetailForm.Designer.cs"), CustomBaseFormDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "DetailForm.cs"), CustomBaseFormCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var axamlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "DetailForm.axaml"));

            Assert.StartsWith("<Window ", axamlContent);
            Assert.Contains("Title=\"Detail — Sample App\"", axamlContent);
            Assert.DoesNotContain("Text=\"Detail", axamlContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string UnmappedInferredPropertyDesignerContent = """
        namespace SampleApp
        {
            partial class TreeForm
            {
                private System.Windows.Forms.TreeView categoryTreeView;

                private void InitializeComponent()
                {
                    this.categoryTreeView = new System.Windows.Forms.TreeView();
                    this.SuspendLayout();
                    this.categoryTreeView.Name = "categoryTreeView";
                    this.Controls.Add(this.categoryTreeView);
                    this.Name = "TreeForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    // "NotARealAvaloniaOrWinFormsProperty" stands in for any property name
    // PropertyMappingRegistry has (and will always have) no entry for - referenced from 2
    // distinct methods so UsageInferredBindingDetector promotes it to an inferred binding.
    private const string UnmappedInferredPropertyCodeBehindContent = """
        namespace SampleApp
        {
            partial class TreeForm
            {
                private void LoadTree()
                {
                    var x = categoryTreeView.NotARealAvaloniaOrWinFormsProperty;
                }

                private void SaveSelection()
                {
                    categoryTreeView.NotARealAvaloniaOrWinFormsProperty = null;
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_InferredBindingWithNoRegistryMapping_EmitsNoAttributeInstead()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "TreeForm.Designer.cs"), UnmappedInferredPropertyDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "TreeForm.cs"), UnmappedInferredPropertyCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var axamlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "TreeForm.axaml"));

            Assert.Contains("Name=\"categoryTreeView\"", axamlContent);
            Assert.DoesNotContain("NotARealAvaloniaOrWinFormsProperty", axamlContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
