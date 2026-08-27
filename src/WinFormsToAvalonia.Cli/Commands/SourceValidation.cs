using Spectre.Console;
using WinFormsToAvalonia.Core.Parsing;
using Spectre.Console.Cli;

namespace WinFormsToAvalonia.Cli.Commands;

internal static class SourceValidation
{
    public static ValidationResult ValidateSourceCsproj(string? source) =>
        Validate(source, allowSolution: false);

    /// <summary>`convert` takes a solution too; `analyze` reports on one project at a time.</summary>
    public static ValidationResult ValidateSourceProjectOrSolution(string? source) =>
        Validate(source, allowSolution: true);

    private static ValidationResult Validate(string? source, bool allowSolution)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return ValidationResult.Error("--source is required.");
        }

        var isProject = source.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
        var isSolution = allowSolution && SolutionReader.IsSolutionPath(source);

        if (!isProject && !isSolution)
        {
            var expected = allowSolution ? ".csproj, .sln or .slnx" : ".csproj";
            return ValidationResult.Error($"--source must point to a {expected} file, got '{source}'.");
        }

        if (!File.Exists(source))
        {
            return ValidationResult.Error($"Source file not found: '{source}'.");
        }

        return ValidationResult.Success();
    }
}
