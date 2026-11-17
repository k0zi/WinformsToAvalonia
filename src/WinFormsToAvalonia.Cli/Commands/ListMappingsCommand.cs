using Spectre.Console;
using Spectre.Console.Cli;
using WinFormsToAvalonia.Cli.Rendering;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Mapping.Vendors;

namespace WinFormsToAvalonia.Cli.Commands;

/// <summary>Prints the full stock control mapping table, sourced directly from ControlMappingRegistry so it can never drift from actual convert-time behavior.</summary>
public sealed class ListMappingsCommand : Command<ListMappingsCommandSettings>
{
    protected override int Execute(CommandContext context, ListMappingsCommandSettings settings, CancellationToken cancellationToken)
    {
        var registry = new ControlMappingRegistry();
        MappingTableRenderer.Render(AnsiConsole.Console, registry, settings.Filter);
        AnsiConsole.WriteLine();
        VendorTableRenderer.Render(AnsiConsole.Console, VendorCatalog.BuiltIn, registry, settings.Filter);
        return 0;
    }
}
