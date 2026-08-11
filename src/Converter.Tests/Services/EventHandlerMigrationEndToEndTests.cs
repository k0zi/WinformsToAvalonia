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
            Assert.DoesNotContain("// System.Windows.Forms.MessageBox.Show", codeBehindContent);

            var tree = CSharpSyntaxTree.ParseText(codeBehindContent);
            var errors = tree.GetDiagnostics()
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .ToList();
            Assert.Empty(errors);

            var axamlContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "HandlerBodyForm.axaml"));
            Assert.Contains("PointerPressed=\"button1_MouseDown\"", axamlContent);

            // HandlerBodyForm has no data bindings, so the auto-regenerated, properties-only
            // .g.cs is skipped entirely; the hand-editable file is always created.
            Assert.False(File.Exists(Path.Combine(outputDir, "ViewModels", "HandlerBodyFormViewModel.g.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "ViewModels", "HandlerBodyFormViewModel.cs")));
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

    private const string ClickDesignerContent = """
        namespace SampleApp
        {
            partial class ClickForm
            {
                private System.Windows.Forms.Button button1;

                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.button1.Name = "button1";
                    this.button1.Click += new System.EventHandler(this.button1_Click);
                    this.Controls.Add(this.button1);
                    this.Name = "ClickForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string ClickCodeBehindContent = """
        namespace SampleApp
        {
            partial class ClickForm
            {
                private void button1_Click(object sender, System.EventArgs e)
                {
                    DoWork();
                }

                private void DoWork()
                {
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_HandEditableViewModelFile_WriteOnce_UserEditsSurviveReconversion()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ClickForm.Designer.cs"), ClickDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ClickForm.cs"), ClickCodeBehindContent);

            var firstRun = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();
            Assert.True(firstRun.Success, firstRun.ErrorMessage);

            var vmPath = Path.Combine(outputDir, "ViewModels", "ClickFormViewModel.cs");
            Assert.True(File.Exists(vmPath));

            var seeded = await File.ReadAllTextAsync(vmPath);
            Assert.Contains("RelayCommand", seeded);
            Assert.Contains("DoWork();", seeded);

            const string handEdit = "\n// hand-edited marker - must survive reconversion\n";
            await File.WriteAllTextAsync(vmPath, seeded + handEdit);

            var secondRun = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();
            Assert.True(secondRun.Success, secondRun.ErrorMessage);

            var afterSecondRun = await File.ReadAllTextAsync(vmPath);
            Assert.Equal(seeded + handEdit, afterSecondRun);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string BoundDesignerContent = """
        namespace SampleApp
        {
            partial class BoundForm
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
                    this.Controls.Add(this.textBox1);
                    this.Name = "BoundForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string UnboundDesignerContent = """
        namespace SampleApp
        {
            partial class BoundForm
            {
                private System.Windows.Forms.TextBox textBox1;

                private void InitializeComponent()
                {
                    this.textBox1 = new System.Windows.Forms.TextBox();
                    this.SuspendLayout();
                    this.textBox1.Name = "textBox1";
                    this.Controls.Add(this.textBox1);
                    this.Name = "BoundForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_StaleGeneratedViewModel_DeletedWhenBindingRemoved()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;
        var designerPath = Path.Combine(sourceDir, "BoundForm.Designer.cs");
        var vmGenPath = Path.Combine(outputDir, "ViewModels", "BoundFormViewModel.g.cs");

        try
        {
            await File.WriteAllTextAsync(designerPath, BoundDesignerContent);

            var firstRun = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();
            Assert.True(firstRun.Success, firstRun.ErrorMessage);
            Assert.True(File.Exists(vmGenPath));
            Assert.Contains("[ObservableProperty]", await File.ReadAllTextAsync(vmGenPath));

            await File.WriteAllTextAsync(designerPath, UnboundDesignerContent);

            var secondRun = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();
            Assert.True(secondRun.Success, secondRun.ErrorMessage);
            Assert.False(File.Exists(vmGenPath));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string FieldRewriteDesignerContent = """
        namespace SampleApp
        {
            partial class FieldRewriteForm
            {
                private System.Windows.Forms.Button button1;

                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.button1.Name = "button1";
                    this.button1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.button1_MouseDown);
                    this.Controls.Add(this.button1);
                    this.Name = "FieldRewriteForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string FieldRewriteCodeBehindContent = """
        namespace SampleApp
        {
            partial class FieldRewriteForm
            {
                private int _clickCount;

                private void button1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
                {
                    _clickCount++;
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_PreservedHandlerReferencesMigratedField_RewritesToViewModelAccessor()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "FieldRewriteForm.Designer.cs"), FieldRewriteDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "FieldRewriteForm.cs"), FieldRewriteCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var codeBehindContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "FieldRewriteForm.axaml.cs"));
            Assert.Contains("ViewModel._clickCount++;", codeBehindContent);

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "FieldRewriteFormViewModel.cs"));
            Assert.Contains("internal int _clickCount;", vmContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string OverrideDesignerContent = """
        namespace SampleApp
        {
            partial class OverrideForm
            {
                private System.Windows.Forms.Button button1;

                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.button1.Name = "button1";
                    this.Controls.Add(this.button1);
                    this.Name = "OverrideForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string OverrideCodeBehindContent = """
        namespace SampleApp
        {
            partial class OverrideForm
            {
                protected override void OnLoad(System.EventArgs e)
                {
                    base.OnLoad(e);
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_OverrideMethodInCodeBehind_SkippedAndSurfacedAsManualStep()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "OverrideForm.Designer.cs"), OverrideDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "OverrideForm.cs"), OverrideCodeBehindContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true },
                NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "OverrideFormViewModel.cs"));
            Assert.DoesNotContain("OnLoad", vmContent);

            var migrationGuide = await File.ReadAllTextAsync(Path.Combine(outputDir, "MIGRATION_GUIDE.md"));
            Assert.Contains("Skipped Override Methods", migrationGuide);
            Assert.Contains("OnLoad", migrationGuide);

            Assert.Contains(result.Report!.ManualSteps, s => s.Category == "Skipped Override Methods");
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string BusinessLogicOverrideDesignerContent = """
        namespace SampleApp
        {
            partial class DetailForm
            {
                private System.Windows.Forms.TextBox nameTextBox;

                private void InitializeComponent()
                {
                    this.nameTextBox = new System.Windows.Forms.TextBox();
                    this.SuspendLayout();
                    this.nameTextBox.Name = "nameTextBox";
                    this.Controls.Add(this.nameTextBox);
                    this.Name = "DetailForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string BusinessLogicOverrideCodeBehindContent = """
        namespace SampleApp
        {
            partial class DetailForm
            {
                protected override void SaveToEntity()
                {
                    Entity.Name = nameTextBox.Text;
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_OverrideOfProjectLocalBaseClassMember_MigratedAsLiveViewModelMethod()
    {
        // Distinguishes a genuine WinForms lifecycle override (OnLoad, above - stays skipped)
        // from an override of the project's own base class member (SaveToEntity here - real
        // business logic that happens to use the "override" keyword) - the real-world case that
        // motivated this: DetailFormBase<T>-derived forms in the WarehouseApp sample had their
        // entire LoadFromEntity/SaveToEntity/ValidateInput/PersistAsync logic silently dropped.
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "DetailForm.Designer.cs"), BusinessLogicOverrideDesignerContent);
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "DetailForm.cs"), BusinessLogicOverrideCodeBehindContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true },
                NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "DetailFormViewModel.cs"));
            Assert.Contains("internal void SaveToEntity()", vmContent);
            Assert.Contains("Entity.Name = nameTextBox.Text;", vmContent);
            Assert.DoesNotContain("override", vmContent);

            Assert.DoesNotContain(result.Report!.ManualSteps, s => s.Category == "Skipped Override Methods");
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string TreeNodeOverrideDesignerContent = """
        namespace SampleApp
        {
            partial class WarehousesForm
            {
                private System.Windows.Forms.TreeView locationsTreeView;

                private void InitializeComponent()
                {
                    this.locationsTreeView = new System.Windows.Forms.TreeView();
                    this.SuspendLayout();
                    this.locationsTreeView.Name = "locationsTreeView";
                    this.Controls.Add(this.locationsTreeView);
                    this.Name = "WarehousesForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string TreeNodeOverrideCodeBehindContent = """
        namespace SampleApp
        {
            partial class WarehousesForm
            {
                protected override void LoadTree()
                {
                    var node = new TreeNode("root");
                    locationsTreeView.Nodes.Add(node);
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_MigratedOverrideBodyReferencesTreeNode_StillMigratedButFlagged()
    {
        // The real WarehousesFormViewModel.LoadTreeAsync case: a business-logic override (not
        // a WinForms framework override, so it's migrated as live code per the test above) that
        // happens to build raw TreeNode objects - no Avalonia equivalent, will not compile, but
        // previously gave zero signal that this had happened.
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "WarehousesForm.Designer.cs"), TreeNodeOverrideDesignerContent);
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "WarehousesForm.cs"), TreeNodeOverrideCodeBehindContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true },
                NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "WarehousesFormViewModel.cs"));
            Assert.Contains("internal void LoadTree()", vmContent);
            Assert.Contains("new TreeNode(\"root\")", vmContent);

            var step = Assert.Single(result.Report!.ManualSteps, s => s.Category == "Migrated Logic May Not Compile");
            Assert.Contains("LoadTree", step.Title);
            Assert.Contains("TreeNode", step.Description);

            var migrationGuide = await File.ReadAllTextAsync(Path.Combine(outputDir, "MIGRATION_GUIDE.md"));
            Assert.Contains("Migrated Logic May Not Compile", migrationGuide);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string DriftDesignerContentV2 = """
        namespace SampleApp
        {
            partial class ClickForm
            {
                private System.Windows.Forms.Button button1;
                private System.Windows.Forms.Button button2;

                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.button2 = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.button1.Name = "button1";
                    this.button1.Click += new System.EventHandler(this.button1_Click);
                    this.button2.Name = "button2";
                    this.button2.Click += new System.EventHandler(this.button2_Click);
                    this.Controls.Add(this.button1);
                    this.Controls.Add(this.button2);
                    this.Name = "ClickForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string DriftCodeBehindContentV2 = """
        namespace SampleApp
        {
            partial class ClickForm
            {
                private void button1_Click(object sender, System.EventArgs e)
                {
                    DoWork();
                }

                private void button2_Click(object sender, System.EventArgs e)
                {
                    DoWork();
                }

                private void DoWork()
                {
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_ExistingHandEditedViewModel_NewHandlerSurfacedAsDriftManualStep()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;
        var designerPath = Path.Combine(sourceDir, "ClickForm.Designer.cs");
        var codeBehindPath = Path.Combine(sourceDir, "ClickForm.cs");
        var vmPath = Path.Combine(outputDir, "ViewModels", "ClickFormViewModel.cs");

        try
        {
            await File.WriteAllTextAsync(designerPath, ClickDesignerContent);
            await File.WriteAllTextAsync(codeBehindPath, ClickCodeBehindContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true },
                NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
            };

            var firstRun = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();
            Assert.True(firstRun.Success, firstRun.ErrorMessage);
            var afterFirstRun = await File.ReadAllTextAsync(vmPath);

            await File.WriteAllTextAsync(designerPath, DriftDesignerContentV2);
            await File.WriteAllTextAsync(codeBehindPath, DriftCodeBehindContentV2);

            var secondRun = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();
            Assert.True(secondRun.Success, secondRun.ErrorMessage);

            // The hand-edited file already existed, so it is never rewritten - the newly
            // discovered button2Click command is surfaced as a manual step instead.
            Assert.Equal(afterFirstRun, await File.ReadAllTextAsync(vmPath));

            var migrationGuide = await File.ReadAllTextAsync(Path.Combine(outputDir, "MIGRATION_GUIDE.md"));
            Assert.Contains("ViewModel File Drift", migrationGuide);
            Assert.Contains("button2Click", migrationGuide);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string BoundControlRefDesignerContent = """
        namespace SampleApp
        {
            partial class BoundRefForm
            {
                private System.Windows.Forms.Button button1;
                private System.Windows.Forms.TextBox textBox1;
                private System.Windows.Forms.BindingSource customerBindingSource;

                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.textBox1 = new System.Windows.Forms.TextBox();
                    this.SuspendLayout();
                    this.button1.Name = "button1";
                    this.button1.Click += new System.EventHandler(this.button1_Click);
                    this.textBox1.Name = "textBox1";
                    this.textBox1.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.customerBindingSource, "CustomerName", true));
                    this.Controls.Add(this.button1);
                    this.Controls.Add(this.textBox1);
                    this.Name = "BoundRefForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string BoundControlRefCodeBehindContent = """
        namespace SampleApp
        {
            partial class BoundRefForm
            {
                private void button1_Click(object sender, System.EventArgs e)
                {
                    System.Console.WriteLine(textBox1.Text);
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_CommandBodyReferencesBoundControlProperty_RewritesToObservableProperty()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "BoundRefForm.Designer.cs"), BoundControlRefDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "BoundRefForm.cs"), BoundControlRefCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "BoundRefFormViewModel.cs"));

            // textBox1.Text is bound (DataBindings.Add -> CustomerName), so the migrated
            // [RelayCommand] body must reference the ViewModel's own ObservableProperty instead
            // of the View-only "textBox1" - the ViewModel cannot reach the View.
            Assert.Contains("Console.WriteLine(CustomerName);", vmContent);
            Assert.DoesNotContain("textBox1", vmContent);

            Assert.DoesNotContain(
                result.Report!.ManualSteps, s => s.Category == "Command Logic References View-Only Control");
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string UnboundControlRefDesignerContent = """
        namespace SampleApp
        {
            partial class UnboundRefForm
            {
                private System.Windows.Forms.Button button1;
                private System.Windows.Forms.Label label1;

                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.label1 = new System.Windows.Forms.Label();
                    this.SuspendLayout();
                    this.button1.Name = "button1";
                    this.button1.Click += new System.EventHandler(this.button1_Click);
                    this.label1.Name = "label1";
                    this.Controls.Add(this.button1);
                    this.Controls.Add(this.label1);
                    this.Name = "UnboundRefForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string UnboundControlRefCodeBehindContent = """
        namespace SampleApp
        {
            partial class UnboundRefForm
            {
                private void button1_Click(object sender, System.EventArgs e)
                {
                    System.Console.WriteLine(label1.Text);
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_CommandBodyReferencesUnboundControlProperty_SurfacedAsManualStep()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "UnboundRefForm.Designer.cs"), UnboundControlRefDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "UnboundRefForm.cs"), UnboundControlRefCodeBehindContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true },
                EventHandlerMigration = new EventHandlerMigrationConfig { Enabled = true },
                NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            // label1 has no DataBindings entry, so there is nothing to rewrite this reference
            // into - left as-is it wouldn't compile (the ViewModel has no "label1"), so this
            // must be surfaced as an explicit manual step instead of silently failing to build.
            var step = Assert.Single(
                result.Report!.ManualSteps, s => s.Category == "Command Logic References View-Only Control");
            Assert.Contains("label1", step.Title);

            var migrationGuide = await File.ReadAllTextAsync(Path.Combine(outputDir, "MIGRATION_GUIDE.md"));
            Assert.Contains("Command Logic References View-Only Control", migrationGuide);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string HelperMethodBoundDesignerContent = """
        namespace SampleApp
        {
            partial class HelperMethodBoundForm
            {
                private System.Windows.Forms.TextBox skuTextBox;
                private System.Windows.Forms.BindingSource productBindingSource;

                private void InitializeComponent()
                {
                    this.skuTextBox = new System.Windows.Forms.TextBox();
                    this.SuspendLayout();
                    this.skuTextBox.Name = "skuTextBox";
                    this.skuTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.productBindingSource, "Sku", true));
                    this.Controls.Add(this.skuTextBox);
                    this.Name = "HelperMethodBoundForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string HelperMethodBoundCodeBehindContent = """
        namespace SampleApp
        {
            partial class HelperMethodBoundForm
            {
                private void LoadFromEntity()
                {
                    skuTextBox.Text = "ABC";
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_HelperMethodReferencesBoundControlProperty_RewritesToObservableProperty()
    {
        // A migrated helper method (LoadFromEntity/ValidateInput/SaveToEntity-style code) is
        // just as likely as a RelayCommand body to reference another control directly - the
        // same rewrite RelayCommand bodies already get must apply here too, or the ViewModel
        // references a View-only control and doesn't compile.
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "HelperMethodBoundForm.Designer.cs"), HelperMethodBoundDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "HelperMethodBoundForm.cs"), HelperMethodBoundCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "HelperMethodBoundFormViewModel.cs"));

            Assert.Contains("Sku = \"ABC\";", vmContent);
            Assert.DoesNotContain("skuTextBox", vmContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private const string HelperMethodUnboundDesignerContent = """
        namespace SampleApp
        {
            partial class HelperMethodUnboundForm
            {
                private System.Windows.Forms.TextBox skuTextBox;

                private void InitializeComponent()
                {
                    this.skuTextBox = new System.Windows.Forms.TextBox();
                    this.SuspendLayout();
                    this.skuTextBox.Name = "skuTextBox";
                    this.Controls.Add(this.skuTextBox);
                    this.Name = "HelperMethodUnboundForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string HelperMethodUnboundCodeBehindContent = """
        namespace SampleApp
        {
            partial class HelperMethodUnboundForm
            {
                private void LoadFromEntity()
                {
                    skuTextBox.Text = "ABC";
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_HelperMethodReferencesUnboundControlProperty_SurfacedAsManualStep()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "HelperMethodUnboundForm.Designer.cs"), HelperMethodUnboundDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "HelperMethodUnboundForm.cs"), HelperMethodUnboundCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig(true)).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            var step = Assert.Single(
                result.Report!.ManualSteps, s => s.Category == "Command Logic References View-Only Control");
            Assert.Contains("LoadFromEntity", step.Title);
            Assert.Contains("skuTextBox", step.Title);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
