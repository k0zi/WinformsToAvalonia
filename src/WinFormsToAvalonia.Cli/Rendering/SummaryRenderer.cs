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
            $"Controls: [green]{report.DirectControlCount} direct[/], [yellow]{report.FallbackControlCount} fallback[/], [red]{report.UnsupportedControlCount} unsupported[/]",
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

        lines.Add($"Elapsed: [bold]{report.Elapsed.TotalMilliseconds:F0} ms[/]");

        return string.Join("\n", lines);
    }
}
