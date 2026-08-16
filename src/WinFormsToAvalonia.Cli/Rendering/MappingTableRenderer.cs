using Spectre.Console;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Cli.Rendering;

public static class MappingTableRenderer
{
    public static void Render(IAnsiConsole console, ControlMappingRegistry registry, string? filter)
    {
        var table = new Table().Title("WinForms -> Avalonia control mapping");
        table.AddColumn("WinForms control");
        table.AddColumn("Status");
        table.AddColumn("Avalonia target / fallback");
        table.AddColumn("Notes");

        var entries = registry.Mappers.Values
            .Where(m => filter is null || m.WinFormsTypeName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.WinFormsTypeName, StringComparer.Ordinal);

        var directCount = 0;
        var fallbackCount = 0;
        var unsupportedCount = 0;

        foreach (var mapper in entries)
        {
            var probe = new ControlModel { FieldName = "_probe", ClrTypeName = mapper.WinFormsTypeName };
            var mapped = mapper.Map(probe);

            switch (mapped.Status)
            {
                case MappingStatus.Direct: directCount++; break;
                case MappingStatus.Fallback: fallbackCount++; break;
                case MappingStatus.Unsupported: unsupportedCount++; break;
            }

            table.AddRow(
                Markup.Escape(mapper.WinFormsTypeName),
                FormatStatus(mapped.Status),
                Markup.Escape(mapped.AvaloniaElementName ?? "-"),
                mapped.Warnings.Count > 0 ? Markup.Escape(string.Join(" ", mapped.Warnings)) : "-");
        }

        console.Write(table);
        console.MarkupLine(
            $"[green]{directCount}[/] direct, [yellow]{fallbackCount}[/] fallback, [red]{unsupportedCount}[/] unsupported.");
    }

    private static string FormatStatus(MappingStatus status) => status switch
    {
        MappingStatus.Direct => "[green]Direct[/]",
        MappingStatus.Fallback => "[yellow]Fallback[/]",
        MappingStatus.Unsupported => "[red]Unsupported[/]",
        _ => Markup.Escape(status.ToString()),
    };
}
