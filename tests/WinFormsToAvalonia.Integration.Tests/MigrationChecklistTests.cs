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

    /// <summary>
    /// A handler that stayed in code-behind is the conversion working as designed, so it is not a
    /// warning. On the all-in-one sample it was thirty of the forty-three "Needs your attention"
    /// lines, which buried the dozen that really were.
    /// </summary>
    [Fact]
    public void HandlersThatStayedInCodeBehind_AreListedApartFromTheWarnings()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "CodeBehindMigrationApp", "CodeBehindMigrationApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-checklist-notes-" + Guid.NewGuid());

        var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir) { DryRun = true });

        Assert.NotEmpty(result.Report.CodeBehindNotes);
        Assert.All(result.Report.CodeBehindNotes, n => Assert.Contains("stays in code-behind", n));
        Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("stays in code-behind", StringComparison.Ordinal));

        Assert.True(result.Vfs.TryGetText("MIGRATION.md", out var checklist));
        var notesSection = checklist.IndexOf($"## Stayed in code-behind ({result.Report.CodeBehindNotes.Count})", StringComparison.Ordinal);
        Assert.True(notesSection >= 0, checklist);

        // Every such line sits in that section, and nowhere above it.
        Assert.DoesNotContain("stays in code-behind", checklist[..notesSection], StringComparison.Ordinal);
    }
}
