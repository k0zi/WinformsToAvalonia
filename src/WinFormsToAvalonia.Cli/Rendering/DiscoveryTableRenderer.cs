using Spectre.Console;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Cli.Rendering;

public static class DiscoveryTableRenderer
{
    public static void Render(IAnsiConsole console, WinFormsProjectModel project, IReadOnlyList<DesignerFilePairing> pairings)
    {
        var relevant = pairings.Where(p => p.Kind != WinFormsArtifactKind.Other).ToList();

        var table = new Table().Title("Discovered WinForms artifacts");
        table.AddColumn("Kind");
        table.AddColumn("Name");
        table.AddColumn("Namespace");
        table.AddColumn("Primary file");
        table.AddColumn("Designer");
        table.AddColumn("Resx");

        foreach (var pairing in relevant.OrderBy(p => p.Kind).ThenBy(p => p.ClassName, StringComparer.Ordinal))
        {
            table.AddRow(
                FormatKind(pairing.Kind),
                pairing.ClassName,
                pairing.Namespace ?? "-",
                pairing.PrimaryFilePath is null ? "-" : Path.GetRelativePath(project.ProjectDirectory, pairing.PrimaryFilePath),
                FormatPresence(pairing.DesignerFilePath is not null),
                FormatPresence(pairing.ResxFilePath is not null));
        }

        console.Write(table);

        var formCount = relevant.Count(p => p.Kind == WinFormsArtifactKind.Form);
        var userControlCount = relevant.Count(p => p.Kind == WinFormsArtifactKind.UserControl);
        var componentCount = relevant.Count(p => p.Kind == WinFormsArtifactKind.Component);
        var otherCount = pairings.Count - relevant.Count;

        console.MarkupLine(
            $"[bold]{formCount}[/] form(s), [bold]{userControlCount}[/] user control(s), " +
            $"[bold]{componentCount}[/] component(s) discovered ({otherCount} other class(es) skipped).");
    }

    private static string FormatKind(WinFormsArtifactKind kind) => kind switch
    {
        WinFormsArtifactKind.Form => "[cyan]Form[/]",
        WinFormsArtifactKind.UserControl => "[magenta]UserControl[/]",
        WinFormsArtifactKind.Component => "[yellow]Component[/]",
        _ => Markup.Escape(kind.ToString()),
    };

    private static string FormatPresence(bool present) => present ? "[green]yes[/]" : "[grey]no[/]";
}
