using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using WinFormsToAvalonia.Cli.Rendering;
using WinFormsToAvalonia.Core.Parsing;
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

        if (SolutionReader.IsSolutionPath(settings.Source))
        {
            return ConvertSolution(settings, options);
        }

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

    /// <summary>
    /// Every WinForms project the solution lists, each through the ordinary single-project
    /// pipeline, into one output solution.
    /// </summary>
    private static int ConvertSolution(ConvertCommandSettings settings, ConversionOptions options)
    {
        var solution = AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Converting solution...", _ => new SolutionConversionPipeline().Run(options));

        if (solution.Converted.Count == 0)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Nothing to convert:[/] no project in [grey]{Markup.Escape(settings.Source)}[/] " +
                "contains a WinForms Form, UserControl, or Component.");
            return 1;
        }

        foreach (var project in solution.Converted)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]{Markup.Escape(Path.GetFileName(project.SourceProjectPath))}[/]");
            SummaryRenderer.RenderReport(AnsiConsole.Console, project.Result.Report, settings.Verbose);
        }

        foreach (var skipped in solution.Skipped)
        {
            AnsiConsole.MarkupLine(
                $"[grey]Skipped[/] {Markup.Escape(Path.GetFileName(skipped.SourceProjectPath))}: {Markup.Escape(skipped.Reason)}.");
        }

        AnsiConsole.WriteLine();
        if (options.DryRun)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Dry run[/] - {solution.Converted.Count} project(s) would be written to " +
                $"[grey]{settings.Output}[/], with [grey]{solution.SolutionFileName}[/] alongside them.");
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"[green]Done.[/] {solution.Converted.Count} project(s). " +
                $"Next: [grey]cd {settings.Output} && dotnet build {solution.SolutionFileName}[/]");
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
