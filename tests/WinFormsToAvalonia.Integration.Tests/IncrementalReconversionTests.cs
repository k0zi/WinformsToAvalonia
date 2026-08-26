using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Core.Scaffolding;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// This converter's premise is that a human migrates the generated output one method at a time,
/// so re-running it over that half-migrated output must not destroy the work. These tests drive
/// the full pipeline twice over the same directory, with a hand edit in between.
/// </summary>
public class IncrementalReconversionTests
{
    private const string HandMigratedMarker = "// hand-migrated: do not lose this";

    [Fact]
    public void SecondConversion_PreservesHandEditedFilesAndWritesTheRegeneratedVersionAlongside()
    {
        RunTwiceOverAHandEditedOutput(overwriteAll: false, (outputDir, editedFile, secondReport) =>
        {
            Assert.Contains(HandMigratedMarker, File.ReadAllText(editedFile));

            var regenerated = editedFile + VirtualFileSystem.GeneratedFileSuffix;
            Assert.True(File.Exists(regenerated), $"expected the regenerated file at '{regenerated}'");
            Assert.DoesNotContain(HandMigratedMarker, File.ReadAllText(regenerated));

            Assert.Contains("Views/MainView.axaml.cs", secondReport.PreservedFiles);

            // Everything the human did not touch is byte-identical, so nothing else is diverted.
            Assert.Equal(["Views/MainView.axaml.cs"], secondReport.PreservedFiles);
        });
    }

    [Fact]
    public void SecondConversionWithOverwriteAll_ReplacesTheHandEditedFile()
    {
        RunTwiceOverAHandEditedOutput(overwriteAll: true, (outputDir, editedFile, secondReport) =>
        {
            Assert.DoesNotContain(HandMigratedMarker, File.ReadAllText(editedFile));
            Assert.False(File.Exists(editedFile + VirtualFileSystem.GeneratedFileSuffix));
            Assert.Empty(secondReport.PreservedFiles);
        });
    }

    private static void RunTwiceOverAHandEditedOutput(
        bool overwriteAll,
        Action<string, string, Core.Model.ConversionReport> assert)
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "GroupBoxApp", "GroupBoxApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-reconvert-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();

            var first = pipeline.Run(new ConversionOptions(sourceProject, outputDir));
            Assert.Empty(first.Report.PreservedFiles);

            var editedFile = Path.Combine(outputDir, "Views", "MainView.axaml.cs");
            File.AppendAllText(editedFile, Environment.NewLine + HandMigratedMarker + Environment.NewLine);

            var second = pipeline.Run(new ConversionOptions(sourceProject, outputDir, OverwriteAll: overwriteAll));

            assert(outputDir, editedFile, second.Report);
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
