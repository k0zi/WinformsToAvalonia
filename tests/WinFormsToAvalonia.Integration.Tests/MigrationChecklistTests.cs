using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// The generated <c>MIGRATION.md</c>, and the one property that makes it worth having.
/// </summary>
public class MigrationChecklistTests
{
    /// <summary>
    /// Every method the checklist lists must really carry a <c>MigrationTodo</c> marker, and every
    /// marker must be listed. A checklist that drifts from the code it describes is worse than no
    /// checklist - it sends someone to a method that is already done, or lets one go unnoticed.
    /// </summary>
    /// <remarks>
    /// Both sides come from <c>CodeBehindHandlerPlan.IsUnfinished</c>, so this asserts a property
    /// that is true by construction rather than one maintained by hand. It is here to catch the
    /// day someone reintroduces a second opinion about what "finished" means.
    /// </remarks>
    [Theory]
    [InlineData("HandlerMigrationApp")]
    [InlineData("ComplexApp")]
    [InlineData("ComponentFieldApp")]
    public void GeneratedChecklist_ListsExactlyTheMethodsThatCarryAMarker(string sampleAppName)
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", sampleAppName, $"{sampleAppName}.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-checklist-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir) { DryRun = true });

            Assert.True(result.Vfs.TryGetText("MIGRATION.md", out var checklist));

            var listed = Regex.Matches(checklist, @"^- \[ \] `(\w+)`", RegexOptions.Multiline)
                .Select(m => m.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal);

            var marked = result.Vfs.RelativePaths
                .Where(p => (p.StartsWith("Views/", StringComparison.Ordinal) || p.StartsWith("ViewModels/", StringComparison.Ordinal))
                    && p.EndsWith(".cs", StringComparison.Ordinal))
                .Select(p => result.Vfs.TryGetText(p, out var text) ? text : "")
                // The preserved original code-behind is a comment; only the generated members count.
                .Select(text => text.Split("ORIGINAL WINFORMS CODE-BEHIND")[0])
                .SelectMany(text => Regex.Matches(text, @"MigrationTodo\.NotMigrated\(nameof\((\w+)\)").Select(m => m.Groups[1].Value))
                .ToHashSet(StringComparer.Ordinal);

            Assert.Equal(marked.OrderBy(n => n, StringComparer.Ordinal), listed.OrderBy(n => n, StringComparer.Ordinal));
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// It goes through the VirtualFileSystem like every other generated file, which is what makes
    /// `--dry-run` and the preserve-existing re-run behave on it without a special case.
    /// </summary>
    [Fact]
    public void GeneratedChecklist_IsNotWrittenOnADryRun()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "HandlerMigrationApp", "HandlerMigrationApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-checklist-dry-" + Guid.NewGuid());

        var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir) { DryRun = true });

        Assert.Contains("MIGRATION.md", result.Vfs.RelativePaths);
        Assert.False(Directory.Exists(outputDir));
    }
}
