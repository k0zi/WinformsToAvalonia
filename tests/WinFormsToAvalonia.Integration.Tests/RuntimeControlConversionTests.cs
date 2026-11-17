using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Controls a Form creates at run time - a status label held in a field and created in Load, a
/// label created by its field initializer, a button made per click - which used to stop every
/// handler at the <c>new</c>.
/// </summary>
public class RuntimeControlConversionTests
{
    private static readonly string SourceProject =
        Path.Combine(AppContext.BaseDirectory, "SampleApps", "RuntimeControlsApp", "RuntimeControlsApp.csproj");

    [Fact]
    public async Task ConvertedApp_CreatesPositionsAndParentsRuntimeControls()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-runtimecontrols-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(SourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

            // The fields are the Avalonia elements, not comments.
            Assert.Contains("private TextBlock statusText = default!;", codeBehind);
            Assert.Contains("private TextBlock counterLabel = new TextBlock();", codeBehind);

            Assert.Contains("statusText = new TextBlock();", codeBehind);
            Assert.Contains("statusStrip1.Children.AddRange(items);", codeBehind);
            Assert.Contains("Canvas.SetTop(button, 10 + 30 * count);", codeBehind);
            Assert.Contains("hostPanel.Children.Add(button);", codeBehind);

            Assert.Equal(result.Report.HandlerStatementCount, result.Report.MigratedStatementCount);
            Assert.Empty(result.Report.MigrationBlockers);

            var build = await DotnetRunner.RunAsync("build", outputDir);
            Assert.True(
                build.ExitCode == 0 && !build.StdOut.Contains(": warning ", StringComparison.Ordinal),
                $"dotnet build exit {build.ExitCode}.\n--- stdout ---\n{build.StdOut}\n--- stderr ---\n{build.StdErr}");
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
