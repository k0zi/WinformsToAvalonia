using System.Globalization;
using System.Text;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>
/// Writes <c>MIGRATION.md</c> into the generated project: the list of methods that still need a
/// human, in the order someone would work through them.
/// </summary>
/// <remarks>
/// <para>
/// This converter's premise is that the unit of manual migration is one method, not one file - and
/// until now the map to that work existed only in the console output and the <c>--log-file</c>
/// JSON, neither of which survives into the thing the developer actually opens.
/// </para>
/// <para>
/// Built from the plans rather than by re-reading the emitted text or re-parsing warning strings:
/// <see cref="CodeBehindHandlerPlan.IsUnfinished"/> is the same predicate the code emitter uses to
/// decide whether to write a <c>MigrationTodo</c>, so the checklist and the code cannot disagree.
/// </para>
/// </remarks>
public sealed class MigrationChecklistEmitter
{
    /// <remarks>
    /// Takes the counts and warnings rather than a whole <see cref="ConversionReport"/> on
    /// purpose: the report is only complete after the files are on disk, and this file has to be
    /// one of them.
    /// </remarks>
    public string Emit(
        string projectName,
        string sourceProjectPath,
        int migratedStatements,
        int handlerStatements,
        IReadOnlyList<string> warnings,
        IReadOnlyList<ArtifactMigrationSummary> artifacts)
    {
        var sb = new StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        Line($"# {projectName} — migration checklist");
        Line();
        Line($"Generated from `{Path.GetFileName(sourceProjectPath)}` by WinFormsToAvalonia.");
        Line();

        var (migrated, total) = (migratedStatements, handlerStatements);
        if (total > 0)
        {
            var percent = (migrated * 100.0 / total).ToString("0", CultureInfo.InvariantCulture);
            Line($"**{migrated} of {total} handler statements ({percent}%)** came across as real Avalonia code.");
        }

        Line();
        Line("Everything below is preserved in the generated project as a comment, inside a method that");
        Line("calls `MigrationTodo.NotMigrated(...)`. The marker reports rather than throws, so the app");
        Line("runs while you work through this list.");
        Line();

        var unfinished = artifacts.SelectMany(a => a.Unfinished).ToList();
        if (unfinished.Count == 0)
        {
            Line("## Nothing left");
            Line();
            Line("Every handler translated completely.");
        }
        else
        {
            Line($"## Methods to migrate ({unfinished.Count})");
            Line();

            foreach (var file in unfinished.GroupBy(u => u.FilePath).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                Line($"### `{file.Key}`");
                Line();
                foreach (var member in file.OrderBy(m => m.MemberName, StringComparer.Ordinal))
                {
                    var origin = member.MemberName == member.OriginalMethodName
                        ? ""
                        : $" (was `{member.OriginalMethodName}`)";
                    var first = member.FirstRemainingLine.Length > 0 ? $" — `{member.FirstRemainingLine}`" : "";
                    Line($"- [ ] `{member.MemberName}`{origin}{first}");
                }

                Line();
            }
        }

        var preserved = artifacts.Where(a => a.PreservedMemberNames.Count > 0).ToList();
        if (preserved.Count > 0)
        {
            Line("## Preserved members");
            Line();
            Line("Not handlers, and not translatable as they stand, so they are kept as a comment block at");
            Line("the end of their View. Nothing in the generated code calls them yet.");
            Line();
            foreach (var artifact in preserved.OrderBy(a => a.SourceArtifactName, StringComparer.Ordinal))
            {
                Line($"- **{artifact.SourceArtifactName}**: {string.Join(", ", artifact.PreservedMemberNames.Select(n => $"`{n}`"))}");
            }

            Line();
        }

        if (warnings.Count > 0)
        {
            Line($"## Conversion notes ({warnings.Count})");
            Line();
            Line("Everything the conversion decided not to guess at, and why.");
            Line();
            foreach (var warning in warnings)
            {
                Line($"- {warning}");
            }

            Line();
        }

        return sb.ToString();
    }
}
