using Spectre.Console;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Scaffolding;

namespace WinFormsToAvalonia.Cli.Rendering;

public static class SummaryRenderer
{
    public static void RenderReport(IAnsiConsole console, ConversionReport report, bool verbose)
    {
        var panel = new Panel(BuildSummaryText(report))
        {
            Header = new PanelHeader("Conversion summary"),
            Border = BoxBorder.Rounded,
        };
        console.Write(panel);

        if (report.Warnings.Count > 0)
        {
            var warningsToShow = verbose ? report.Warnings : report.Warnings.Take(5).ToList();
            console.MarkupLine($"[yellow]{report.Warnings.Count} warning(s):[/]");
            foreach (var warning in warningsToShow)
            {
                console.MarkupLine($"  [grey]-[/] {Markup.Escape(warning)}");
            }

            if (!verbose && report.Warnings.Count > warningsToShow.Count)
            {
                console.MarkupLine($"  [grey]... {report.Warnings.Count - warningsToShow.Count} more (use --verbose to see all)[/]");
            }
        }

        // Where the unfinished handlers stopped, bucketed: which missing translation would carry the
        // most code next. Top three by default, every bucket with --verbose.
        if (report.MigrationBlockers.Count > 0)
        {
            var blockersToShow = verbose ? report.MigrationBlockers : report.MigrationBlockers.Take(3).ToList();
            console.MarkupLine(
                $"[grey]Unfinished handlers stopped at ({report.MigrationBlockers.Sum(b => b.Value)} in total):[/]");
            foreach (var (bucket, count) in blockersToShow)
            {
                console.MarkupLine($"  [grey]blocker[/] {count,3} x {Markup.Escape(bucket)}");
            }

            if (!verbose && report.MigrationBlockers.Count > blockersToShow.Count)
            {
                console.MarkupLine($"  [grey]... {report.MigrationBlockers.Count - blockersToShow.Count} more kind(s) (use --verbose to see all)[/]");
            }
        }

        // Not warnings - code-behind is the default - so not counted among them, but still
        // reachable: --verbose lists the reasons, MIGRATION.md always does.
        if (report.CodeBehindNotes.Count > 0)
        {
            console.MarkupLine(
                $"[grey]{report.CodeBehindNotes.Count} command-candidate handler(s) stayed in code-behind "
                + "(reasons in MIGRATION.md).[/]");
            if (verbose)
            {
                foreach (var note in report.CodeBehindNotes)
                {
                    console.MarkupLine($"  [grey]- {Markup.Escape(note)}[/]");
                }
            }
        }
    }

    public static void RenderFileTree(IAnsiConsole console, string title, VirtualFileSystem vfs)
    {
        var tree = new Tree(title);
        var nodesByFolder = new Dictionary<string, TreeNode>(StringComparer.Ordinal);

        foreach (var path in vfs.RelativePaths)
        {
            var segments = path.Split('/');
            var currentPath = "";
            var parentNode = default(TreeNode);

            for (var i = 0; i < segments.Length - 1; i++)
            {
                currentPath = currentPath.Length == 0 ? segments[i] : $"{currentPath}/{segments[i]}";
                if (!nodesByFolder.TryGetValue(currentPath, out var node))
                {
                    node = parentNode is null ? tree.AddNode($"[blue]{segments[i]}/[/]") : parentNode.AddNode($"[blue]{segments[i]}/[/]");
                    nodesByFolder[currentPath] = node;
                }

                parentNode = node;
            }

            var fileName = segments[^1];
            if (parentNode is null)
            {
                tree.AddNode(fileName);
            }
            else
            {
                parentNode.AddNode(fileName);
            }
        }

        console.Write(tree);
    }

    private static string BuildSummaryText(ConversionReport report)
    {
        var style = report.IsLegacyStyle ? "legacy (.NET Framework)" : "SDK-style";
        var lines = new List<string>
        {
            $"Project style: [bold]{style}[/], target framework(s): [bold]{string.Join(", ", report.TargetFrameworks)}[/]",
            $"Forms converted: [bold]{report.FormCount}[/]{(report.UserControlCount > 0 ? $", user controls: [bold]{report.UserControlCount}[/]" : "")}",
            // "Converted elsewhere" is deliberately not red and deliberately not next to
            // "unsupported": a Timer that became a DispatcherTimer is a success, and counting it
            // as a failure is what made a working conversion look alarming.
            $"Controls: [green]{report.DirectControlCount} direct[/], [yellow]{report.FallbackControlCount} fallback[/], "
                + $"[green]{report.ConvertedElsewhereCount} converted without an element[/], "
                + $"[red]{report.UnsupportedControlCount} unsupported[/]",
        };

        if (report.UsedFallbackKeys.Count > 0)
        {
            lines.Add($"Fallback controls used: [bold]{string.Join(", ", report.UsedFallbackKeys)}[/]");
        }

        if (report.RequiredNuGetPackages.Count > 0)
        {
            lines.Add($"Extra NuGet packages added: [bold]{string.Join(", ", report.RequiredNuGetPackages)}[/]");
        }

        if (report.HandlerStatementCount > 0)
        {
            var percent = 100.0 * report.MigratedStatementCount / report.HandlerStatementCount;
            lines.Add(
                $"Handler statements migrated: [bold]{report.MigratedStatementCount}/{report.HandlerStatementCount}[/] " +
                $"({percent:F0}%) - the rest stay as commented TODOs.");
        }

        if (report.PreservedFiles.Count > 0)
        {
            lines.Add(
                $"[yellow]{report.PreservedFiles.Count} existing file(s) preserved[/] - your version was kept and the " +
                "regenerated one written alongside as [grey]*.w2a-new[/]. Pass [grey]--overwrite-all[/] to replace them instead.");
        }

        if (report.PrunedFiles.Count > 0)
        {
            lines.Add($"[green]{report.PrunedFiles.Count} stale generated file(s) removed[/] (--prune).");
        }

        if (report.OrphanedFiles.Count > 0)
        {
            lines.Add(
                $"[yellow]{report.OrphanedFiles.Count} file(s) from an earlier conversion are no longer generated[/]: " +
                string.Join(", ", report.OrphanedFiles.Take(5).Select(f => $"[grey]{Markup.Escape(f)}[/]")) +
                (report.OrphanedFiles.Count > 5 ? ", ..." : "") +
                " - left in place. [grey]--prune[/] deletes the ones you have not edited; an edited one is never deleted.");
        }

        lines.Add($"Elapsed: [bold]{report.Elapsed.TotalMilliseconds:F0} ms[/]");

        return string.Join("\n", lines);
    }
}
