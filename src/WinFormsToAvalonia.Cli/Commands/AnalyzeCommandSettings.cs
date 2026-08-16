using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WinFormsToAvalonia.Cli.Commands;

public sealed class AnalyzeCommandSettings : CommandSettings
{
    [CommandOption("-s|--source <PATH>")]
    [Description("Path to the WinForms project file (.csproj) to analyze.")]
    public required string Source { get; init; }

    [CommandOption("--verbose")]
    [Description("Include additional project detail (style, target frameworks) in the output.")]
    public bool Verbose { get; init; }

    public override ValidationResult Validate() => SourceValidation.ValidateSourceCsproj(Source);
}
