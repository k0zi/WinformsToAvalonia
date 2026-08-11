using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Services;

/// <summary>
/// End-to-end proof of the declarative DialogResult-button + ShowDialog() caller rewrite
/// through the real ConversionOrchestrator.ExecuteAsync() pipeline. WarehouseApp (the other
/// real-sample end-to-end coverage in this project) doesn't exercise this idiom - its own
/// Save/Cancel logic is hand-written in a shared base class instead of the Designer's
/// declarative DialogResult property - so this synthetic three-form fixture is the actual
/// verification vehicle for the feature: a "PickerForm" with a Designer-declared OK button, a
/// "MainForm" that shows it modally and checks the result, and an "EditForm" call with a
/// constructor argument that must be left untouched (the "edit" flow is deliberately out of
/// scope - see ChildDialogTranspiler's doc comment).
/// </summary>
public class ChildDialogEndToEndTests
{
    private const string PickerFormDesignerContent = """
        namespace SampleApp
        {
            partial class PickerForm
            {
                private System.Windows.Forms.Button okButton;

                private void InitializeComponent()
                {
                    this.okButton = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.okButton.Name = "okButton";
                    this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                    this.Controls.Add(this.okButton);
                    this.Name = "PickerForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string MainFormDesignerContent = """
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

    private const string MainFormCodeBehindContent = """
        namespace SampleApp
        {
            partial class MainForm
            {
                private void AddNew()
                {
                    using var form = new PickerForm();
                    if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        Reload();
                    }
                }

                private void EditExisting(object existingEntity)
                {
                    using var form = new PickerForm(existingEntity);
                    if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        Reload();
                    }
                }

                private void Reload()
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
    public async Task ExecuteAsync_DeclarativeDialogResultButtonAndCallerShowDialog_AreWiredAndRewritten()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "PickerForm.Designer.cs"), PickerFormDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "MainForm.Designer.cs"), MainFormDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "MainForm.cs"), MainFormCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            // Callee side: PickerForm's okButton gets a synthetic Click wire-up that closes
            // with OK - no code needed in the WinForms source at all.
            var pickerAxaml = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "PickerForm.axaml"));
            Assert.Contains("Click=\"okButton_DialogResultClick\"", pickerAxaml);

            var pickerCodeBehind = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "PickerForm.axaml.cs"));
            Assert.Contains("okButton_DialogResultClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>", pickerCodeBehind);
            Assert.Contains("Close(SampleApp.Common.DialogResult.OK);", pickerCodeBehind);

            // Caller side: MainForm.AddNew's parameterless-constructor ShowDialog() pattern is
            // rewritten to the generated ShowChildAsync helper.
            var mainFormViewModel = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "MainFormViewModel.cs"));
            Assert.Contains(
                "await SampleApp.Common.Dialogs.ShowChildAsync<SampleApp.Views.PickerForm, SampleApp.ViewModels.PickerFormViewModel>()",
                mainFormViewModel);
            Assert.Contains("SampleApp.Common.DialogResult.OK", mainFormViewModel);

            // EditExisting's constructor-argument call is deliberately left untouched.
            Assert.Contains("new PickerForm(existingEntity)", mainFormViewModel);
            Assert.Contains("form.ShowDialog(this)", mainFormViewModel);

            // Common/Dialogs.cs is generated (needed for ShowChildAsync) even though this
            // fixture never calls MessageBox.Show at all.
            Assert.True(File.Exists(Path.Combine(outputDir, "Common", "Dialogs.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Common", "MessageBoxTypes.cs")));
            Assert.False(File.Exists(Path.Combine(outputDir, "Views", "MessageBoxWindow.axaml")));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}

/// <summary>
/// End-to-end proof of the modeless child-dialog rewrite: a handler that does
/// "var child = new ChildForm(); child.ShowDialog(this);" (fire-and-forget, no result
/// handling) gets rewritten to the generated Dialogs.ShowChildAsync helper the same way the
/// modal "ShowDialog(this) == DialogResult.OK" pattern does - and the rewrite gate requires
/// the child form to be part of the very conversion producing the helper (convertedFormClassNames).
/// </summary>
public class ModelessChildDialogEndToEndTests
{
    private const string ChildFormDesignerContent = """
        namespace SampleApp
        {
            partial class ChildForm
            {
                private System.Windows.Forms.Button closeButton;

                private void InitializeComponent()
                {
                    this.closeButton = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.closeButton.Name = "closeButton";
                    this.Controls.Add(this.closeButton);
                    this.Name = "ChildForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string ParentDesignerContent = """
        namespace SampleApp
        {
            partial class ParentForm
            {
                private System.Windows.Forms.Button openButton;
                private System.Windows.Forms.Label statusLabel;

                private void InitializeComponent()
                {
                    this.openButton = new System.Windows.Forms.Button();
                    this.statusLabel = new System.Windows.Forms.Label();
                    this.SuspendLayout();
                    this.openButton.Name = "openButton";
                    this.openButton.Click += new System.EventHandler(this.openButton_Click);
                    this.statusLabel.Name = "statusLabel";
                    this.Controls.Add(this.openButton);
                    this.Controls.Add(this.statusLabel);
                    this.Name = "ParentForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private const string CleanCommandCodeBehindContent = """
        namespace SampleApp
        {
            partial class ParentForm
            {
                private void openButton_Click(object sender, System.EventArgs e)
                {
                    var child = new ChildForm();
                    child.ShowDialog(this);
                }
            }
        }
        """;

    private const string DirtyCommandCodeBehindContent = """
        namespace SampleApp
        {
            partial class ParentForm
            {
                private void openButton_Click(object sender, System.EventArgs e)
                {
                    var child = new ChildForm();
                    child.ShowDialog(this);
                    System.Console.WriteLine(statusLabel.Text);
                }
            }
        }
        """;

    private static ConverterConfig BaselineConfig() => new()
    {
        GitIntegration = new GitIntegrationConfig { Enabled = false },
        Documentation = new DocumentationConfig { Enabled = true },
        EventHandlerMigration = new EventHandlerMigrationConfig { Enabled = true },
        NamingConventions = new NamingConventionsConfig { RootNamespace = "SampleApp" }
    };

    [Fact]
    public async Task ExecuteAsync_ModelessShowDialogInCleanCommand_StaysRelayCommandWithAwaitShowChildAsync()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ChildForm.Designer.cs"), ChildFormDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ParentForm.Designer.cs"), ParentDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ParentForm.cs"), CleanCommandCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            // The body only opens a child modelessly - nothing view-only referenced - so it
            // stays a [RelayCommand] and the AXAML wires a Command binding.
            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "ParentFormViewModel.cs"));
            Assert.Contains("private async void openButtonClick()", vmContent);
            Assert.Contains(
                "await SampleApp.Common.Dialogs.ShowChildAsync<SampleApp.Views.ChildForm, SampleApp.ViewModels.ChildFormViewModel>()",
                vmContent);
            Assert.DoesNotContain("child.ShowDialog", vmContent);
            Assert.DoesNotContain("statusLabel", vmContent);

            var axamlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "ParentForm.axaml"));
            Assert.Contains("Command=\"{Binding OpenButtonClickCommand}\"", axamlContent);

            // The generated Dialogs helper must exist even though this fixture never calls
            // MessageBox.Show - the modeless rewrite needs ShowChildAsync (and DialogResult).
            Assert.True(File.Exists(Path.Combine(outputDir, "Common", "Dialogs.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Common", "MessageBoxTypes.cs")));

            Assert.Equal(1, result.Report!.Statistics.ConvertedToCommands);
            Assert.Equal(1, result.Report!.Statistics.TotalEvents);
            Assert.Equal(0, result.Report!.Statistics.PartialControls);
            Assert.True(result.Report!.Statistics.TotalControls > 2);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ModelessShowDialogPlusUnboundControlReference_DowngradedToCodeBehind()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ChildForm.Designer.cs"), ChildFormDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ParentForm.Designer.cs"), ParentDesignerContent);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "ParentForm.cs"), DirtyCommandCodeBehindContent);

            var result = await new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig()).ExecuteAsync();
            Assert.True(result.Success, result.ErrorMessage);

            // statusLabel.Text is unbound, so the command is downgraded to code-behind - but
            // the modeless ShowDialog rewrite still applies to the stub body there.
            var codeBehindContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "ParentForm.axaml.cs"));
            Assert.Contains("private async void openButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)", codeBehindContent);
            Assert.Contains(
                "await SampleApp.Common.Dialogs.ShowChildAsync<SampleApp.Views.ChildForm, SampleApp.ViewModels.ChildFormViewModel>()",
                codeBehindContent);
            Assert.Contains("Console.WriteLine(statusLabel.Text);", codeBehindContent);
            Assert.DoesNotContain("child.ShowDialog", codeBehindContent);

            var axamlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "ParentForm.axaml"));
            Assert.Contains("Click=\"openButton_Click\"", axamlContent);
            Assert.DoesNotContain("Command=\"{Binding", axamlContent);

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "ViewModels", "ParentFormViewModel.cs"));
            Assert.DoesNotContain("openButtonClick", vmContent);
            Assert.DoesNotContain("RelayCommand", vmContent);

            Assert.True(File.Exists(Path.Combine(outputDir, "Common", "Dialogs.cs")));
            Assert.Equal(0, result.Report!.Statistics.ConvertedToCommands);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
