using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A file dialog's designer settings reach the picker that replaces it - on both paths: inlined
/// into a handler, and through the generated helper method. The builder is unit-tested; this is
/// the half only a build can prove: the collection expressions, the escaped strings, and an
/// <c>await</c> inside an object initializer naming an extension method from the right namespace.
/// </summary>
public class FileDialogOptionsConversionTests
{
    [Fact]
    public async Task ConvertedFileDialogOptionsApp_OpensPickersWithTheDesignerSettingsAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "FileDialogOptionsApp", "FileDialogOptionsApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-filedialogs-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var view));

            // Inlined: the open dialog, with every designer setting it has.
            Assert.Contains("OpenFilePickerAsync(new FilePickerOpenOptions { Title = \"Open \\\"log\\\"\"", view);
            Assert.Contains("SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(\"C:\\\\Logs\")", view);
            Assert.Contains("new FilePickerFileType(\"Log files\") { Patterns = [\"*.log\", \"*.txt\"] }", view);
            Assert.Contains("AllowMultiple = true", view);

            // Inlined through the guard-clause shape: the save dialog.
            Assert.Contains("SaveFilePickerAsync(new FilePickerSaveOptions { Title = \"Export\"", view);
            Assert.Contains("DefaultExtension = \"csv\", ShowOverwritePrompt = false", view);

            // The helper path, with the same builder.
            Assert.Contains("OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = \"Pick the export folder\", "
                + "SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(\"/tmp\") })", view);

            Assert.Contains(result.Report.Warnings, w => w.Contains("'saveFileDialog1'") && w.Contains("preselects filter 2"));
            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("OpenFileDialog.Filter") || w.Contains("SaveFileDialog.Title"));

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);
            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
            Assert.DoesNotContain(": warning ", buildResult.StdOut);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }
}
