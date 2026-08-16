using Spectre.Console;
using Spectre.Console.Cli;
using WinFormsToAvalonia.Cli.Rendering;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Cli.Commands;

/// <summary>
/// Runs the discovery stage only (project loading + Form/UserControl/Component
/// classification) and renders the report - no scaffolding/emission, nothing written to
/// disk. Useful to preview a project before committing to a real `convert` run.
/// </summary>
public sealed class AnalyzeCommand : Command<AnalyzeCommandSettings>
{
    protected override int Execute(CommandContext context, AnalyzeCommandSettings settings, CancellationToken cancellationToken)
    {
        var project = new WinFormsProjectLoader().Load(settings.Source);

        if (settings.Verbose)
        {
            AnsiConsole.MarkupLine(
                $"Project style: [bold]{(project.IsLegacyStyle ? "legacy (.NET Framework)" : "SDK-style")}[/], " +
                $"target framework(s): [bold]{string.Join(", ", project.TargetFrameworks)}[/], " +
                $"root namespace: [bold]{project.RootNamespace}[/]");
        }

        var pairings = new DesignerFileLocator().Locate(project);
        DiscoveryTableRenderer.Render(AnsiConsole.Console, project, pairings);

        return 0;
    }
}
