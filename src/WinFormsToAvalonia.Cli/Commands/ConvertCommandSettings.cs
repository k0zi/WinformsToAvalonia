using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WinFormsToAvalonia.Cli.Commands;

public sealed class ConvertCommandSettings : CommandSettings
{
    [CommandOption("-s|--source <PATH>")]
    [Description("Path to the WinForms project (.csproj) or solution (.sln/.slnx) to convert.")]
    public required string Source { get; init; }

    [CommandOption("-o|--output <DIR>")]
    [Description("Output directory for the generated Avalonia project.")]
    public required string Output { get; init; }

    [CommandOption("--force")]
    [Description("Allow writing into an output directory that already exists and is not empty.")]
    public bool Force { get; init; }

    [CommandOption("--overwrite-all")]
    [Description("Replace existing output files instead of preserving them and writing the regenerated version as *.w2a-new.")]
    public bool OverwriteAll { get; init; }

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

    [CommandOption("--with-web")]
    [Description("Also generate a browser (WebAssembly) head: emits a shared library plus a desktop and a browser project instead of one desktop project. Needs the wasm-tools workload to build.")]
    public bool WithWeb { get; init; }

    [CommandOption("--log-file <PATH>")]
    [Description("Write a full structured (JSON) conversion report alongside the console output.")]
    public string? LogFile { get; init; }

    public override ValidationResult Validate()
    {
        var sourceValidation = SourceValidation.ValidateSourceProjectOrSolution(Source);
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
                $"Output directory '{Output}' already exists and is not empty. Pass --force to write into it " +
                "(files you have already edited are preserved; the regenerated version lands beside them as *.w2a-new).");
        }

        return ValidationResult.Success();
    }
}
