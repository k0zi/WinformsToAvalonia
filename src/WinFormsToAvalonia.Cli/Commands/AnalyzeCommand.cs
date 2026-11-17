using Spectre.Console;
using Spectre.Console.Cli;
using WinFormsToAvalonia.Cli.Rendering;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Mapping.Vendors;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Cli.Commands;

/// <summary>
/// Runs the discovery stage only (project loading + Form/UserControl/Component
/// classification) and renders the report - no scaffolding/emission, nothing written to
/// disk. Useful to preview a project before committing to a real `convert` run.
/// </summary>
/// <remarks>
/// A solution is analysed one project at a time, in the order it lists them - the same set
/// <c>convert</c> would work through, so a preview covers exactly what a real run would do.
/// </remarks>
public sealed class AnalyzeCommand : Command<AnalyzeCommandSettings>
{
    protected override int Execute(CommandContext context, AnalyzeCommandSettings settings, CancellationToken cancellationToken)
    {
        var projectPaths = SolutionReader.IsSolutionPath(settings.Source)
            ? SolutionReader.ReadProjectPaths(settings.Source)
            : [settings.Source];

        foreach (var projectPath in projectPaths)
        {
            if (!File.Exists(projectPath))
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]{Markup.Escape(Path.GetFileName(projectPath))}[/]: " +
                    "the solution lists it, but the file is not there.");
                continue;
            }

            if (projectPaths.Count > 1)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]{Markup.Escape(Path.GetFileName(projectPath))}[/]");
            }

            Analyze(projectPath, settings.Verbose);
        }

        return 0;
    }

    private static void Analyze(string projectPath, bool verbose)
    {
        var project = new WinFormsProjectLoader().Load(projectPath);

        if (verbose)
        {
            AnsiConsole.MarkupLine(
                $"Project style: [bold]{(project.IsLegacyStyle ? "legacy (.NET Framework)" : "SDK-style")}[/], " +
                $"target framework(s): [bold]{Markup.Escape(string.Join(", ", project.TargetFrameworks))}[/], " +
                $"root namespace: [bold]{Markup.Escape(project.RootNamespace)}[/]");
        }

        // A Form built on a DevExpress or Telerik base class is a Form here too, as it is to convert.
        var pairings = new DesignerFileLocator().Locate(
            project, VendorCatalog.BuiltIn.ExternalBaseTypes(new Dictionary<string, UserMapping>(StringComparer.Ordinal)));
        DiscoveryTableRenderer.Render(AnsiConsole.Console, project, pairings);
    }
}
