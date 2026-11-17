using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// The <c>--verbose</c>/<c>--trace</c> log: it has to say what each stage did, and it must not
/// change a single thing the conversion produces.
/// </summary>
public class ConversionLogTests
{
    private static string SampleProject(string name) =>
        Path.Combine(AppContext.BaseDirectory, "SampleApps", name, $"{name}.csproj");

    [Fact]
    public void Listening_ChangesNeitherTheGeneratedFilesNorTheReport()
    {
        var options = new ConversionOptions(SampleProject("HandlerMigrationApp"), Path.Combine(Path.GetTempPath(), "w2a-log-" + Guid.NewGuid())) { DryRun = true };

        var silent = new ConversionPipeline().Run(options);
        var entries = new List<ConversionLogEntry>();
        var logged = new ConversionPipeline(new ConversionLog(entries.Add)).Run(options);

        Assert.NotEmpty(entries);
        Assert.Equal(silent.Vfs.RelativePaths, logged.Vfs.RelativePaths);
        foreach (var path in silent.Vfs.RelativePaths)
        {
            if (silent.Vfs.TryGetText(path, out var expected))
            {
                Assert.True(logged.Vfs.TryGetText(path, out var actual));
                Assert.Equal(expected, actual);
            }
        }

        Assert.Equal(silent.Report.Warnings, logged.Report.Warnings);
        Assert.Equal(silent.Report.MigratedStatementCount, logged.Report.MigratedStatementCount);
    }

    [Fact]
    public void Log_NamesEveryStageAndWhereEachHandlerLanded()
    {
        var entries = new List<ConversionLogEntry>();
        var options = new ConversionOptions(SampleProject("HandlerMigrationApp"), Path.Combine(Path.GetTempPath(), "w2a-log-" + Guid.NewGuid())) { DryRun = true };

        var result = new ConversionPipeline(new ConversionLog(entries.Add)).Run(options);

        var stages = entries.Where(e => e.Level == ConversionLogLevel.Stage).Select(e => e.Message).ToList();
        Assert.Contains(stages, s => s.StartsWith("Loading ", StringComparison.Ordinal));
        Assert.Contains(stages, s => s.StartsWith("Discovering ", StringComparison.Ordinal));
        Assert.Contains(stages, s => s.StartsWith("Parsing ", StringComparison.Ordinal));
        Assert.Contains(stages, s => s.StartsWith("Converting ", StringComparison.Ordinal));
        Assert.Contains(stages, s => s.StartsWith("Scaffolding ", StringComparison.Ordinal));

        // One trace line per handler, wherever the planner put it.
        var handlerLines = entries
            .Where(e => e.Level == ConversionLogLevel.Trace && e.Message.Contains(" -> ", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(handlerLines);
        Assert.Contains(handlerLines, e => e.Message.Contains("migrated", StringComparison.Ordinal));

        // A dry run says it wrote nothing, and names what it would have.
        Assert.Contains(entries, e => e.Message.StartsWith("Dry run:", StringComparison.Ordinal));
        Assert.Equal(
            result.Vfs.RelativePaths.Count(),
            entries.Count(e => e.Message.StartsWith("Would write: ", StringComparison.Ordinal)));

        Assert.Equal(entries.Select(e => e.Elapsed).Order(), entries.Select(e => e.Elapsed));
    }
}
