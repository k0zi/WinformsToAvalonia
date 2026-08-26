using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using WinFormsToAvalonia.Cli.Rendering;
using WinFormsToAvalonia.Core.Pipeline;

namespace WinFormsToAvalonia.Cli.Commands;

public sealed class ConvertCommand : Command<ConvertCommandSettings>
{
    protected override int Execute(CommandContext context, ConvertCommandSettings settings, CancellationToken cancellationToken)
    {
        var options = new ConversionOptions(
            SourceProjectPath: settings.Source,
            OutputDirectory: settings.Output,
            Force: settings.Force,
            DryRun: settings.DryRun,
            Verbose: settings.Verbose,
            NoFallbackControls: settings.NoFallbackControls,
            SkipCodeBehindComments: settings.SkipCodeBehindComments,
            LogFile: settings.LogFile,
            OverwriteAll: settings.OverwriteAll);

        ConversionRunResult result;
        try
        {
            result = AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Converting...", _ => new ConversionPipeline().Run(options));
        }
        catch (NoConvertibleArtifactsException ex)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Nothing to convert:[/] [grey]{Markup.Escape(ex.ProjectFilePath)}[/] " +
                "contains no WinForms Form, UserControl, or Component.");
            return 1;
        }

        SummaryRenderer.RenderReport(AnsiConsole.Console, result.Report, settings.Verbose);

        var treeTitle = options.DryRun ? "Would generate (--dry-run)" : $"Generated at {settings.Output}";
        SummaryRenderer.RenderFileTree(AnsiConsole.Console, treeTitle, result.Vfs);

        if (settings.LogFile is not null)
        {
            WriteLogFile(settings.LogFile, result, options);
            AnsiConsole.MarkupLine($"Structured report written to [grey]{settings.LogFile}[/].");
        }

        if (options.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry run[/] - nothing was written to disk.");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Done.[/] Next: [grey]cd {settings.Output} && dotnet build && dotnet run[/]");
        }

        return 0;
    }

    private static void WriteLogFile(string logFilePath, ConversionRunResult result, ConversionOptions options)
    {
        var payload = new
        {
            source = options.SourceProjectPath,
            output = options.OutputDirectory,
            dryRun = options.DryRun,
            report = result.Report,
            generatedFiles = result.Vfs.RelativePaths,
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(logFilePath, json);
    }
}
