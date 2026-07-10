using Spectre.Console;

namespace Converter.Cli.UI;

/// <summary>
/// Helper class for interactive prompts
/// </summary>
public static class InteractivePrompts
{
    public static string PromptForInputPath(ConverterTheme theme)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>($"[{theme.Primary}]Enter WinForms project path:[/]")
                .PromptStyle(theme.Primary)
                .ValidationErrorMessage($"[{theme.Error}]Path must exist and contain .cs or .csproj files[/]")
                .Validate(path =>
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return ValidationResult.Error("Path cannot be empty");
                    }

                    if (File.Exists(path))
                    {
                        if (path.EndsWith(".cs") || path.EndsWith(".Designer.cs"))
                        {
                            return ValidationResult.Success();
                        }
                        return ValidationResult.Error("File must be a .cs or .Designer.cs file");
                    }

                    if (Directory.Exists(path))
                    {
                        if (Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0)
                        {
                            return ValidationResult.Success();
                        }
                        return ValidationResult.Error("Directory must contain a .csproj file");
                    }

                    return ValidationResult.Error("Path does not exist");
                })
        );
    }

    public static string PromptForOutputPath(string inputPath, ConverterTheme theme)
    {
        var defaultPath = Path.Combine(
            Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory,
            Path.GetFileNameWithoutExtension(inputPath) + "-Avalonia"
        );

        var outputPath = AnsiConsole.Prompt(
            new TextPrompt<string>($"[{theme.Primary}]Enter output directory path:[/]")
                .PromptStyle(theme.Primary)
                .DefaultValue(defaultPath)
                .AllowEmpty()
        );

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = defaultPath;
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(outputPath))
        {
            var create = AnsiConsole.Confirm(
                $"Directory [yellow]{outputPath}[/] does not exist. Create it?",
                true
            );

            if (create)
            {
                Directory.CreateDirectory(outputPath);
                AnsiConsole.MarkupLine($"[{theme.Info}]📁 Created directory: {outputPath}[/]");
            }
            else
            {
                throw new OperationCanceledException("Output directory creation cancelled by user");
            }
        }

        return outputPath;
    }

    public static string PromptForLayoutMode(ConverterTheme theme)
    {
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{theme.Primary}]Select layout detection mode:[/]")
                .AddChoices(
                    "auto - Automatic detection (recommended)",
                    "canvas - Preserve exact positions",
                    "smart - Optimize for Avalonia layouts"
                )
        );

        // Extract the mode (before the " - ")
        return selection.Split(" - ")[0];
    }

    public static (bool GitEnabled, bool CreateBranch, string? BranchName) PromptForGitOptions(ConverterTheme theme)
    {
        var gitEnabled = AnsiConsole.Confirm(
            $"[{theme.Primary}]Enable git integration?[/]",
            defaultValue: true
        );

        if (!gitEnabled)
        {
            return (false, false, null);
        }

        var createBranch = AnsiConsole.Confirm(
            $"[{theme.Primary}]Create feature branch for conversion?[/]",
            defaultValue: true
        );

        string? branchName = null;
        if (createBranch)
        {
            branchName = AnsiConsole.Prompt(
                new TextPrompt<string>($"[{theme.Primary}]Enter branch name:[/]")
                    .DefaultValue($"feature/avalonia-migration-{DateTime.Now:yyyyMMdd-HHmmss}")
                    .AllowEmpty()
            );

            if (string.IsNullOrWhiteSpace(branchName))
            {
                branchName = $"feature/avalonia-migration-{DateTime.Now:yyyyMMdd-HHmmss}";
            }
        }

        return (gitEnabled, createBranch, branchName);
    }

    public static GenerationOptions PromptForGenerationOptions(ConverterTheme theme)
    {
        var migrationGuide = AnsiConsole.Confirm(
            $"[{theme.Primary}]Generate migration guide?[/]",
            defaultValue: true
        );

        var viewModels = AnsiConsole.Confirm(
            $"[{theme.Primary}]Generate ViewModels (MVVM pattern)?[/]",
            defaultValue: true
        );

        var extractStyles = AnsiConsole.Confirm(
            $"[{theme.Primary}]Extract common styles?[/]",
            defaultValue: true
        );

        return new GenerationOptions(migrationGuide, viewModels, extractStyles);
    }
}

/// <summary>
/// Generation options selected by user
/// </summary>
public record GenerationOptions(
    bool MigrationGuide,
    bool ViewModels,
    bool ExtractStyles
);
