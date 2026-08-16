using Spectre.Console;
using Spectre.Console.Cli;

namespace WinFormsToAvalonia.Cli.Commands;

internal static class SourceValidation
{
    public static ValidationResult ValidateSourceCsproj(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return ValidationResult.Error("--source is required.");
        }

        if (!source.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Error($"--source must point to a .csproj file, got '{source}'.");
        }

        if (!File.Exists(source))
        {
            return ValidationResult.Error($"Source project file not found: '{source}'.");
        }

        return ValidationResult.Success();
    }
}
