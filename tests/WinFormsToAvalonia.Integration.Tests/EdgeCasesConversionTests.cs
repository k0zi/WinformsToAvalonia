using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// What <c>SampleApps/EdgeCasesApp</c> is made of, asserted on the generated text. The build and
/// startup rows prove it compiles and runs; these say it is right for the right reason.
/// </summary>
public class EdgeCasesConversionTests
{
    private static ConversionRunResult Convert()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "SampleApps", "EdgeCasesApp", "EdgeCasesApp.csproj");
        return new ConversionPipeline().Run(
            // The generated root namespace comes from the output folder's name.
            new ConversionOptions(source, Path.Combine(Path.GetTempPath(), "w2a-edge-" + Guid.NewGuid(), "EdgeCasesApp")) { DryRun = true });
    }

    private static string Text(ConversionRunResult result, string path)
    {
        Assert.True(result.Vfs.TryGetText(path, out var text), $"'{path}' was not generated");
        return text;
    }

    [Fact]
    public void DesignerTextThatLooksLikeSyntax_IsCarriedAsData()
    {
        var result = Convert();
        var axaml = Text(result, "Views/MainView.axaml");

        Assert.Contains("Title=\"{}{Edge} cases\"", axaml);
        Assert.Contains("Text=\"Line one&#13;&#10;Line two&#9;end\"", axaml);
        Assert.DoesNotContain("----", axaml);

        var viewModel = Text(result, "ViewModels/MainViewModel.cs");
        Assert.Contains("new() { Text = \"C:\\\\Temp\" },", viewModel);
        Assert.Contains("new() { Text = \"Say \\\"hi\\\"\" },", viewModel);

        var app = Text(result, "App.axaml");
        Assert.Contains("ToolTipText=\"{}{R&amp;D} &lt;Monitor&gt; &quot;live&quot;\"", app);
        Assert.Contains("ToolTipText=\"Backup - - idle\"", app);
    }

    [Fact]
    public void FormInAFolderThatIsNotAnIdentifier_GetsALegalNamespaceAndKeepsItsFolder()
    {
        var result = Convert();

        Assert.Contains("namespace EdgeCasesApp.Views._2024_Dialogs;", Text(result, "Views/2024 Dialogs/SettingsView.axaml.cs"));
        Assert.Contains("x:Class=\"EdgeCasesApp.Views._2024_Dialogs.SettingsView\"", Text(result, "Views/2024 Dialogs/SettingsView.axaml"));
        Assert.Contains("namespace EdgeCasesApp.ViewModels._2024_Dialogs;", Text(result, "ViewModels/2024 Dialogs/SettingsViewModel.cs"));
    }

    /// <summary>
    /// <c>_game</c> is assigned only after the statement Load cannot translate. Carried over, it
    /// would be declared, never set, and read by the translated score handler.
    /// </summary>
    [Fact]
    public void FieldAssignedOnlyInTheUnmigratedRestOfLoad_IsNotCarried()
    {
        var view = Text(Convert(), "Views/MainView.axaml.cs").Split("ORIGINAL WINFORMS MEMBERS")[0];

        Assert.DoesNotContain("_game", view.Split("ORIGINAL WINFORMS BODY of 'MainForm_Load'")[0]);
        Assert.Contains("MigrationTodo.NotMigrated(nameof(scoreButton_Click)", view);
        Assert.DoesNotContain("Title = \"Score \"", view);
    }

    [Fact]
    public void OverloadedHelpersAndSameNamedNestedTypes_AreReportedNotFatal()
    {
        var result = Convert();

        Assert.Contains("MigrationTodo.NotMigrated(nameof(statusButton_Click)", Text(result, "Views/MainView.axaml.cs"));
        Assert.DoesNotContain("Models/Row.cs", result.Vfs.RelativePaths);
        Assert.Contains(result.Report.Warnings, w => w.Contains("'Row' is declared inside 'MainForm' and 'SettingsForm'", StringComparison.Ordinal));
    }
}
