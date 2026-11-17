using Spectre.Console;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Mapping.Vendors;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Cli.Rendering;

/// <summary>
/// The built-in DevExpress and Telerik support, read from the same catalog the conversion uses -
/// what each vendor control is converted as, and the Avalonia element that comes out.
/// </summary>
public static class VendorTableRenderer
{
    public static void Render(IAnsiConsole console, VendorCatalog catalog, ControlMappingRegistry registry, string? filter)
    {
        var table = new Table().Title("Built-in vendor support (DevExpress, Telerik)");
        table.AddColumn("Vendor control");
        table.AddColumn("Converted as");
        table.AddColumn("Avalonia element");

        var rows = 0;
        foreach (var control in catalog.Controls
                     .Where(c => filter is null
                         || c.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                         || (c.As?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false))
                     .OrderBy(c => c.Vendor, StringComparer.Ordinal)
                     .ThenBy(c => c.FullName, StringComparer.Ordinal))
        {
            var targets = control.As is { } target ? [target] : control.PlacedAs.Values.Distinct().ToList();
            var asText = targets.Count == 0 ? "[grey](folded into its owner)[/]" : Markup.Escape(string.Join(" / ", targets));
            var elements = targets
                .Select(t => registry.Mappers.TryGetValue(t, out var mapper)
                    ? mapper.Map(new ControlModel { FieldName = "_probe", ClrTypeName = t }).AvaloniaElementName
                    : null)
                .OfType<string>()
                .Distinct()
                .ToList();

            table.AddRow(
                Markup.Escape(control.FullName),
                asText,
                elements.Count == 0 ? "-" : Markup.Escape(string.Join(" / ", elements)));
            rows++;
        }

        if (rows == 0)
        {
            return;
        }

        console.Write(table);
        var bases = string.Join(", ", catalog.BaseTypes.Select(b => $"{b.SimpleName} ({b.As})").Order(StringComparer.Ordinal));
        console.MarkupLine($"Vendor base classes converted as WinForms ones: [grey]{Markup.Escape(bases)}[/].");
    }
}
