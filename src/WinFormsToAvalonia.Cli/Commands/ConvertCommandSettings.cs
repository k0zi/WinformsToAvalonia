using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WinFormsToAvalonia.Cli.Commands;

public sealed class ConvertCommandSettings : CommandSettings
{
    [CommandOption("-s|--source <PATH>")]
    [Description("Path to the WinForms project file (.csproj) to convert.")]
    public required string Source { get; init; }

    [CommandOption("-o|--output <DIR>")]
    [Description("Output directory for the generated Avalonia project.")]
    public required string Output { get; init; }

    [CommandOption("--force")]
    [Description("Overwrite the contents of the output directory if it already exists.")]
    public bool Force { get; init; }

    [CommandOption("--dry-run")]
    [Description("Run the full pipeline and render the report UI, but write nothing to disk.")]
    public bool DryRun { get; init; }

    [CommandOption("--verbose")]
    [Description("Include per-property mapping trace output, not just the summary.")]
    public bool Verbose { get; init; }

    [CommandOption("--no-fallback-controls")]
    [Description("Fail/warn instead of emitting fallback controls (strict mode).")]
    public bool NoFallbackControls { get; init; }

    [CommandOption("--skip-code-behind-comments")]
    [Description("Omit the large commented-out code-behind block from generated views.")]
    public bool SkipCodeBehindComments { get; init; }

    [CommandOption("--log-file <PATH>")]
    [Description("Write a full structured (JSON) conversion report alongside the console output.")]
    public string? LogFile { get; init; }

    public override ValidationResult Validate()
    {
        var sourceValidation = SourceValidation.ValidateSourceCsproj(Source);
        if (!sourceValidation.Successful)
        {
            return sourceValidation;
        }

        if (string.IsNullOrWhiteSpace(Output))
        {
            return ValidationResult.Error("--output is required.");
        }

        if (!DryRun && Directory.Exists(Output) && Directory.EnumerateFileSystemEntries(Output).Any() && !Force)
        {
            return ValidationResult.Error(
                $"Output directory '{Output}' already exists and is not empty. Pass --force to overwrite it.");
        }

        return ValidationResult.Success();
    }
}
