using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class EmptyScaffoldBuildTests
{
    [Fact]
    public void Run_ThrowsNoConvertibleArtifacts_WhenProjectHasNoFormsUserControlsOrComponents()
    {
        // EmptyApp has zero Form/UserControl/Component classes - converting it would only
        // ever produce the fixed placeholder skeleton, so the pipeline should refuse instead.
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "EmptyApp", "EmptyApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-integration-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(
                SourceProjectPath: sourceProject,
                OutputDirectory: outputDir);

            var ex = Assert.Throws<NoConvertibleArtifactsException>(() => pipeline.Run(options));
            Assert.Equal(Path.GetFullPath(sourceProject), ex.ProjectFilePath);
            Assert.False(Directory.Exists(outputDir));
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