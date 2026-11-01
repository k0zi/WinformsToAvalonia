using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// The project shapes the real-world corpus (samples/corpus) broke on, each in miniature, so a
/// regression shows up here rather than only when someone runs the corpus.
/// </summary>
/// <remarks>
/// Two Forms sharing a simple name in different folders (the whole conversion crashed); a wildcard
/// link to Forms outside the project (they were not found, then landed under a `..` namespace); a
/// Shared Project whose item list spells a file in the wrong case (not found on Linux); and a
/// carried-over field only part of whose users came across (the generated build warned).
/// </remarks>
public class CorpusShapesConversionTests
{
    [Fact]
    public async Task ConvertedCorpusShapes_FindsEveryFormAndBuildsWarningFree()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "CorpusShapes", "App", "App.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-corpusshapes-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));

            Assert.Contains("Views/A/MainView.axaml", result.Vfs.RelativePaths);
            Assert.Contains("Views/B/MainView.axaml", result.Vfs.RelativePaths);
            Assert.Contains("Views/LinkedView.axaml", result.Vfs.RelativePaths);
            Assert.Contains("Views/SharedView.axaml", result.Vfs.RelativePaths);

            Assert.True(result.Vfs.TryGetText("App.axaml.cs", out var app));
            Assert.Contains(".Views.A.MainView()", app);

            Assert.True(result.Vfs.TryGetText("Views/A/MainView.axaml.cs", out var view));
            Assert.Contains("    private string pattern = default!;", view);
            Assert.Contains("#pragma warning disable CS0169, CS0414, CS0649", view);

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
