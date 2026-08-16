using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class NonVisualComponentWarningsTests
{
    [Fact]
    public void ConvertedNonVisualComponentsApp_SurfacesTimerGuidanceAsWarning()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "NonVisualComponentsApp", "NonVisualComponentsApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-nonvisual-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir, DryRun: true);

            var result = pipeline.Run(options);

            Assert.Contains(result.Report.Warnings, w => w.Contains("DispatcherTimer", StringComparison.Ordinal));
            Assert.Equal(1, result.Report.UnsupportedControlCount);
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
