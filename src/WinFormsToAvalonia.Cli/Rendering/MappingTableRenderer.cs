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
        var elsewhereCount = 0;
        var unsupportedCount = 0;

        foreach (var mapper in entries)
        {
            var probe = new ControlModel { FieldName = "_probe", ClrTypeName = mapper.WinFormsTypeName };
            var mapped = mapper.Map(probe);

            switch (mapped.Status)
            {
                case MappingStatus.Direct: directCount++; break;
                case MappingStatus.Fallback: fallbackCount++; break;
                case MappingStatus.Unsupported
                    when mapped.Disposition == UnsupportedDisposition.FeatureElsewhere:
                    elsewhereCount++;
                    break;
                case MappingStatus.Unsupported: unsupportedCount++; break;
            }

            table.AddRow(
                Markup.Escape(mapper.WinFormsTypeName),
                FormatStatus(mapped.Status, mapped.Disposition),
                Markup.Escape(mapped.AvaloniaElementName ?? "-"),
                mapped.Warnings.Count > 0 ? Markup.Escape(string.Join(" ", mapped.Warnings)) : "-");
        }

        console.Write(table);
        console.MarkupLine(
            $"[green]{directCount}[/] direct, [yellow]{fallbackCount}[/] fallback, "
            + $"[green]{elsewhereCount}[/] converted without an element, "
            + $"[red]{unsupportedCount}[/] unsupported.");
    }

    /// <summary>
    /// The status a reader needs, which is not always the one the emitter uses. "Unsupported"
    /// means "produces no element" - true of a Timer and of a PrintDialog alike, though only one
    /// of them wants anything from the reader.
    /// </summary>
    private static string FormatStatus(MappingStatus status, UnsupportedDisposition? disposition) => status switch
    {
        MappingStatus.Direct => "[green]Direct[/]",
        MappingStatus.Fallback => "[yellow]Fallback[/]",
        MappingStatus.Unsupported => disposition switch
        {
            UnsupportedDisposition.FeatureElsewhere => "[green]Elsewhere[/]",
            UnsupportedDisposition.Unreachable => "[grey]Unreachable[/]",
            _ => "[red]Unsupported[/]",
        },
        _ => Markup.Escape(status.ToString()),
    };
}
