using System.ComponentModel;
using Spectre.Console.Cli;

namespace WinFormsToAvalonia.Cli.Commands;

public sealed class ListMappingsCommandSettings : CommandSettings
{
    [CommandOption("-f|--filter <SUBSTRING>")]
    [Description("Only list WinForms control types whose name contains this substring (case-insensitive).")]
    public string? Filter { get; init; }
}
