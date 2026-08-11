using Converter.Cli.Services;
using Converter.Core.Configuration;
using Converter.Tests.TestSupport;

namespace Converter.Tests.Services;

/// <summary>
/// End-to-end conversion of the checked-in WarehouseApp sample (src/SampleWinFormsApp/WarehouseApp) -
/// a realistic, multi-form WinForms app with menus, grids, custom controls and data bindings, as
/// opposed to the minimal single-button fixtures used elsewhere in this test class. Runs against the
/// real on-disk project rather than a synthetic temp-directory fixture, so a regression that breaks
/// parsing/generation for real-world designer shapes (as opposed to the minimal fixtures used
/// elsewhere) shows up here.
/// </summary>
public class WarehouseAppConversionTests
{
    private static readonly string[] ExpectedForms =
    [
        "SalesOrdersListForm", "StockOverviewForm", "DashboardForm", "StockAdjustmentForm",
        "ReportsForm", "CategoriesForm", "CustomersForm", "SalesOrderDetailForm",
        "ProductDetailForm", "SettingsForm", "PurchaseOrdersListForm", "UsersForm",
        "StockOutForm", "WarehousesForm", "PurchaseOrderDetailForm", "StockTransferForm",
        "StockInForm", "SuppliersForm", "LoginForm", "ProductsListForm",
        // WarehouseApp.Controls has two composite custom controls (their own
        // InitializeComponent + child Controls.Add, no separate Designer.cs split) that
        // SingleFileCustomControlDiscovery now finds and converts as their own UserControl -
        // the other seven controls there are pure owner-drawn (OnPaint, no InitializeComponent)
        // and correctly stay out of this list (SupportFileScanner's manual-step path instead).
        "AutocompleteSearchBox", "NumericStepperControl"
    ];

    [Fact]
    public async Task ExecuteAsync_WarehouseApp_ConvertsAllFormsWithFullControlTrees()
    {
        var sourceDir = FixturePath.WarehouseAppDirectory();
        var outputDir = Directory.CreateTempSubdirectory("wf2av-warehouse-out-").FullName;

        try
        {
            var config = new ConverterConfig
            {
                // The source directory is inside this repo's own git working tree - git
                // integration defaults to creating a feature branch, which must never fire
                // against the real dev repo from a test run.
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.Report);

            var formNames = result.Report.Forms.Select(f => f.Name).ToList();
            Assert.Equal(ExpectedForms.Length, formNames.Count);
            foreach (var expected in ExpectedForms)
            {
                Assert.Contains(expected, formNames);
            }

            Assert.All(result.Report.Forms, f => Assert.Equal("Converted", f.Status));

            // Regression guard: before the WarehouseApp designer fixtures were rewritten into
            // authentic (this.-prefixed, no object-initializer) VS-designer shape, the parser
            // silently produced only the empty form root per file - exactly 1 control/form, 20
            // total. A real control-tree walk should be an order of magnitude richer than that.
            Assert.True(result.Report.Statistics.TotalControls > 150,
                $"Expected a rich control tree (>150 controls across {ExpectedForms.Length} forms), " +
                $"got {result.Report.Statistics.TotalControls} - did designer parsing regress to root-only?");

            var viewsDir = Path.Combine(outputDir, "Views");
            var viewModelsDir = Path.Combine(outputDir, "ViewModels");

            // None of the WarehouseApp fixtures use DataBindings.Add, so the auto-regenerated,
            // properties-only .g.cs is normally never written - except for these forms, whose
            // LoadFromEntity/SaveToEntity/ValidateInput-style methods read/write the same
            // controls across multiple members, which UsageInferredBindingDetector now promotes
            // to real [ObservableProperty] bindings even without an explicit DataBindings call.
            var formsWithInferredBindings = new HashSet<string>
            {
                "AutocompleteSearchBox", "ProductDetailForm", "PurchaseOrderDetailForm",
                "SalesOrderDetailForm", "StockInForm", "StockOutForm", "StockTransferForm",
                "WarehousesForm"
            };

            foreach (var form in ExpectedForms)
            {
                Assert.True(File.Exists(Path.Combine(viewsDir, $"{form}.axaml")), $"Missing AXAML for {form}");
                Assert.True(File.Exists(Path.Combine(viewsDir, $"{form}.axaml.cs")), $"Missing code-behind for {form}");
                var expectsGeneratedViewModel = formsWithInferredBindings.Contains(form);
                Assert.Equal(expectsGeneratedViewModel, File.Exists(Path.Combine(viewModelsDir, $"{form}ViewModel.g.cs")));
                Assert.True(File.Exists(Path.Combine(viewModelsDir, $"{form}ViewModel.cs")), $"Missing ViewModel for {form}");
            }

            // Project skeleton (ProjectFileGenerator output), generated once per run alongside the forms.
            var projectName = Path.GetFileName(outputDir);
            Assert.True(File.Exists(Path.Combine(outputDir, $"{projectName}.csproj")));
            Assert.True(File.Exists(Path.Combine(outputDir, "App.axaml")));
            Assert.True(File.Exists(Path.Combine(outputDir, "App.axaml.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Program.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "app.manifest")));
            Assert.True(File.Exists(Path.Combine(outputDir, "MIGRATION_GUIDE.md")));

            // Spot-check one form's actual generated content, rather than only its existence -
            // this guards against a tree that "exists" but is empty/degenerate.
            var loginAxaml = await File.ReadAllTextAsync(Path.Combine(viewsDir, "LoginForm.axaml"));
            Assert.Contains("usernameTextBox", loginAxaml);
            Assert.Contains("passwordTextBox", loginAxaml);
            Assert.Contains("loginButton", loginAxaml);

            var loginViewModel = await File.ReadAllTextAsync(
                Path.Combine(viewModelsDir, "LoginFormViewModel.cs"));
            Assert.Contains("RelayCommand", loginViewModel);
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
