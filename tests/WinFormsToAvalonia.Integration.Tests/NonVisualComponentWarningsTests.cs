using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A <c>Timer</c> becomes a <c>DispatcherTimer</c> field on the View. It emits no AXAML element,
/// which is what <c>MappingStatus.Unsupported</c> means - and for a long time that made it a
/// *warning* and part of a red "unsupported" count, telling the user a converted feature had
/// failed.
/// </summary>
public class NonVisualComponentWarningsTests
{
    [Fact]
    public void ConvertedNonVisualComponentsApp_ReportsTheTimerAsConvertedRatherThanUnsupported()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "NonVisualComponentsApp", "NonVisualComponentsApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-nonvisual-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir, DryRun: true);

            var result = pipeline.Run(options);

            // Where it went is still reported - just not as something needing attention.
            Assert.Contains(
                result.Report.ConvertedElsewhereNotes,
                w => w.Contains("DispatcherTimer", StringComparison.Ordinal));
            Assert.Equal(1, result.Report.ConvertedElsewhereCount);

            // ...and nothing about this app is unsupported, which is the honest answer.
            Assert.Equal(0, result.Report.UnsupportedControlCount);
            Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("DispatcherTimer", StringComparison.Ordinal));
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
