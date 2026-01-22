using System.CommandLine;
using Converter.Cli.Services;
using Converter.Cli.Models;
using Converter.Cli.UI;
using Converter.Cli.Logging;
using Converter.Core.Configuration;
using Converter.Reporting.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Converter.Cli;

class Program
{
    private static CancellationTokenSource? _cts;

    static async Task<int> Main(string[] args)
    {
        // Setup cancellation handler
        Console.CancelKeyPress += (sender, e) =>
        {
            if (_cts != null)
            {
                AnsiConsole.MarkupLine("[yellow]⏸️  Cancellation requested, cleaning up...[/]");
                _cts.Cancel();
                e.Cancel = true;
            }
        };

        var rootCommand = new RootCommand("WinForms to Avalonia Converter");

        // Convert command
        var convertCommand = CreateConvertCommand();
        rootCommand.AddCommand(convertCommand);

        // Init config command
        var initConfigCommand = CreateInitConfigCommand();
        rootCommand.AddCommand(initConfigCommand);

        // Init plugin command
        var initPluginCommand = CreateInitPluginCommand();
        rootCommand.AddCommand(initPluginCommand);

        // List plugins command
        var listPluginsCommand = CreateListPluginsCommand();
        rootCommand.AddCommand(listPluginsCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateConvertCommand()
    {
        var command = new Command("convert", "Convert a WinForms project to Avalonia");

        var inputOption = new Option<string>(
            aliases: ["--input", "-i"],
            description: "Path to WinForms project file (.csproj) or solution (.sln)");
        inputOption.IsRequired = true;

        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Output directory for converted project");
        outputOption.IsRequired = true;

        var layoutOption = new Option<string>(
            aliases: ["--layout", "-l"],
            getDefaultValue: () => "auto",
            description: "Layout mode: auto, canvas, or smart");

        var reportOption = new Option<string?>(
            aliases: ["--report", "-r"],
            description: "Path to save conversion report");

        var reportFormatOption = new Option<string>(
            aliases: ["--report-format"],
            getDefaultValue: () => "html",
            description: "Report format: html, json, md, or csv");

        var configOption = new Option<string?>(
            aliases: ["--config", "-c"],
            description: "Path to .converterconfig file");

        var pluginsOption = new Option<string?>(
            aliases: ["--plugins", "-p"],
            description: "Path to plugins directory");

        var incrementalOption = new Option<bool>(
            aliases: ["--incremental"],
            getDefaultValue: () => false,
            description: "Enable incremental conversion");

        var forceOption = new Option<bool>(
            aliases: ["--force", "-f"],
            getDefaultValue: () => false,
            description: "Force full reconversion, ignore cache");

        var resumeOption = new Option<bool>(
            aliases: ["--resume"],
            getDefaultValue: () => false,
            description: "Resume from last checkpoint");

        var parallelOption = new Option<bool>(
            aliases: ["--parallel"],
            getDefaultValue: () => true,
            description: "Enable parallel processing");

        var createBranchOption = new Option<bool>(
            aliases: ["--create-branch"],
            getDefaultValue: () => false,
            description: "Create git feature branch");

        var branchNameOption = new Option<string?>(
            aliases: ["--branch-name"],
            description: "Custom git branch name");

        var noGitOption = new Option<bool>(
            aliases: ["--no-git"],
            getDefaultValue: () => false,
            description: "Disable git integration");

        var migrationGuideOption = new Option<bool>(
            aliases: ["--migration-guide"],
            getDefaultValue: () => true,
            description: "Generate migration guide documentation");

        var dryRunOption = new Option<bool>(
            aliases: ["--dry-run"],
            getDefaultValue: () => false,
            description: "Validate without generating files");

        var noInteractiveOption = new Option<bool>(
            aliases: ["--no-interactive"],
            getDefaultValue: () => false,
            description: "Disable interactive prompts, use defaults");

        var themeOption = new Option<string?>(
            aliases: ["--theme"],
            description: "Path to custom theme file (.convertertheme)");

        command.AddOption(inputOption);
        command.AddOption(outputOption);
        command.AddOption(layoutOption);
        command.AddOption(reportOption);
        command.AddOption(reportFormatOption);
        command.AddOption(configOption);
        command.AddOption(pluginsOption);
        command.AddOption(incrementalOption);
        command.AddOption(forceOption);
        command.AddOption(resumeOption);
        command.AddOption(parallelOption);
        command.AddOption(createBranchOption);
        command.AddOption(branchNameOption);
        command.AddOption(noGitOption);
        command.AddOption(migrationGuideOption);
        command.AddOption(dryRunOption);
        command.AddOption(noInteractiveOption);
        command.AddOption(themeOption);

        command.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForOption(inputOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var layout = context.ParseResult.GetValueForOption(layoutOption)!;
            var report = context.ParseResult.GetValueForOption(reportOption);
            var reportFormat = context.ParseResult.GetValueForOption(reportFormatOption)!;
            var config = context.ParseResult.GetValueForOption(configOption);
            var plugins = context.ParseResult.GetValueForOption(pluginsOption);
            var incremental = context.ParseResult.GetValueForOption(incrementalOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var resume = context.ParseResult.GetValueForOption(resumeOption);
            var parallel = context.ParseResult.GetValueForOption(parallelOption);
            var createBranch = context.ParseResult.GetValueForOption(createBranchOption);
            var branchName = context.ParseResult.GetValueForOption(branchNameOption);
            var noGit = context.ParseResult.GetValueForOption(noGitOption);
            var migrationGuide = context.ParseResult.GetValueForOption(migrationGuideOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var noInteractive = context.ParseResult.GetValueForOption(noInteractiveOption);
            var theme = context.ParseResult.GetValueForOption(themeOption);

            await ExecuteConvertAsync(input, output, layout, report, reportFormat, config, plugins,
                incremental, force, resume, parallel, createBranch, branchName, noGit, migrationGuide, dryRun,
                noInteractive, theme);
        });

        return command;
    }

    private static Command CreateInitConfigCommand()
    {
        var command = new Command("init-config", "Generate a .converterconfig template file");

        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            getDefaultValue: () => ".converterconfig",
            description: "Output path for configuration file");

        command.AddOption(outputOption);

        command.SetHandler(async (output) =>
        {
            await Core.Configuration.ConfigurationLoader.GenerateTemplateAsync(output);
            Console.WriteLine($"✓ Generated configuration template: {output}");
        }, outputOption);

        return command;
    }

    private static Command CreateInitPluginCommand()
    {
        var command = new Command("init-plugin", "Generate a plugin project template");

        var nameOption = new Option<string>(
            aliases: ["--name", "-n"],
            description: "Plugin name");
        nameOption.IsRequired = true;

        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Output directory for plugin project");
        outputOption.IsRequired = true;

        command.AddOption(nameOption);
        command.AddOption(outputOption);

        command.SetHandler((name, output) =>
        {
            Console.WriteLine($"Plugin template generation not yet implemented.");
            Console.WriteLine($"Plugin: {name}");
            Console.WriteLine($"Output: {output}");
            return Task.CompletedTask;
        }, nameOption, outputOption);

        return command;
    }

    private static Command CreateListPluginsCommand()
    {
        var command = new Command("list-plugins", "List available converter plugins");

        var pluginsOption = new Option<string?>(
            aliases: ["--plugins", "-p"],
            description: "Path to plugins directory");

        command.AddOption(pluginsOption);

        command.SetHandler((plugins) =>
        {
            Console.WriteLine("Plugin discovery not yet implemented.");
            return Task.CompletedTask;
        }, pluginsOption);

        return command;
    }

    private static async Task ExecuteConvertAsync(
        string? input,
        string? output,
        string layout,
        string? report,
        string reportFormat,
        string? config,
        string? plugins,
        bool incremental,
        bool force,
        bool resume,
        bool parallel,
        bool createBranch,
        string? branchName,
        bool noGit,
        bool migrationGuide,
        bool dryRun,
        bool noInteractive,
        string? themePath)
    {
        // Create cancellation token source
        _cts = new CancellationTokenSource();

        try
        {
            // Load theme
            ConverterTheme theme;
            if (!string.IsNullOrEmpty(themePath))
            {
                try
                {
                    theme = ConverterTheme.LoadFromConfig(themePath);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Failed to load theme: {ex.Message}[/]");
                    Environment.ExitCode = 1;
                    return;
                }
            }
            else
            {
                theme = ConverterTheme.Default;
            }

            // Display header
            AnsiConsole.Write(new FigletText("WinForms → Avalonia")
                .Centered()
                .Color(theme.Primary));
            AnsiConsole.WriteLine();

            // Interactive prompts for missing options
            if (!noInteractive)
            {
                if (string.IsNullOrEmpty(input))
                {
                    input = InteractivePrompts.PromptForInputPath(theme);
                }

                if (string.IsNullOrEmpty(output))
                {
                    output = InteractivePrompts.PromptForOutputPath(input!, theme);
                }

                if (layout == "auto")
                {
                    var layoutChoice = AnsiConsole.Confirm(
                        $"[{theme.Primary}]Use automatic layout detection?[/]",
                        defaultValue: true);
                    
                    if (!layoutChoice)
                    {
                        layout = InteractivePrompts.PromptForLayoutMode(theme);
                    }
                }
            }

            // Validate required options
            if (string.IsNullOrEmpty(input))
            {
                AnsiConsole.MarkupLine($"[{theme.Error}]{theme.ErrorIcon} Input path is required[/]");
                Environment.ExitCode = 1;
                return;
            }

            if (string.IsNullOrEmpty(output))
            {
                AnsiConsole.MarkupLine($"[{theme.Error}]{theme.ErrorIcon} Output path is required[/]");
                Environment.ExitCode = 1;
                return;
            }

            // Ensure output directory exists
            Directory.CreateDirectory(output);

            if (dryRun)
            {
                AnsiConsole.MarkupLine($"[{theme.Info}]🔍 DRY RUN MODE - No files will be generated[/]");
                return;
            }

            // Load or create configuration
            var converterConfig = config != null
                ? await ConfigurationLoader.LoadAsync(config)
                : new ConverterConfig();

            // Create logger with Spectre provider
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new SpectreConsoleLoggerProvider());
                builder.SetMinimumLevel(LogLevel.Information);
            });
            var logger = loggerFactory.CreateLogger<ConversionOrchestrator>();

            // Create orchestrator
            var orchestrator = new ConversionOrchestrator(
                Path.GetFullPath(input),
                Path.GetFullPath(output),
                converterConfig,
                logger);

            // Check terminal capabilities
            bool supportsAnsi = AnsiConsole.Profile.Capabilities.Ansi && 
                               AnsiConsole.Profile.Capabilities.Interactive;

            // Create progress state and callback
            var progressState = new ConversionProgress();
            var progress = new Progress<ConversionProgress>(p =>
            {
                lock (progressState)
                {
                    progressState.CurrentOperation = p.CurrentOperation;
                    progressState.CurrentSubOperation = p.CurrentSubOperation;
                    progressState.FormsProcessed = p.FormsProcessed;
                    progressState.TotalForms = p.TotalForms;
                    progressState.CurrentFormName = p.CurrentFormName;
                    progressState.FilesGenerated = p.FilesGenerated;
                    progressState.TotalFilesToGenerate = p.TotalFilesToGenerate;
                    progressState.TotalControls = p.TotalControls;
                    progressState.ConvertedControls = p.ConvertedControls;
                    progressState.TotalProperties = p.TotalProperties;
                    progressState.MappedProperties = p.MappedProperties;
                    progressState.TotalEvents = p.TotalEvents;
                    progressState.ConvertedEvents = p.ConvertedEvents;
                    progressState.Warnings = p.Warnings;
                    progressState.Errors = p.Errors;
                    progressState.ElapsedTime = p.ElapsedTime;
                    progressState.IsGeneratingReport = p.IsGeneratingReport;
                    progressState.IsRollingBack = p.IsRollingBack;
                }
            });

            ConversionResult result;

            if (supportsAnsi)
            {
                // Use live display
                result = await AnsiConsole.Live(new ConversionStatusDisplay(progressState, _cts.Token, theme))
                    .AutoClear(false)
                    .Overflow(VerticalOverflow.Ellipsis)
                    .StartAsync(async ctx =>
                    {
                        ctx.Refresh();
                        return await orchestrator.ExecuteAsync(progress, _cts.Token);
                    });
            }
            else
            {
                // Use basic progress display
                var basicDisplay = new BasicProgressDisplay();
                var basicProgress = new Progress<ConversionProgress>(p =>
                {
                    basicDisplay.Report(p);
                    
                    // Also update shared state
                    lock (progressState)
                    {
                        progressState.CurrentOperation = p.CurrentOperation;
                        progressState.CurrentSubOperation = p.CurrentSubOperation;
                        progressState.FormsProcessed = p.FormsProcessed;
                        progressState.TotalForms = p.TotalForms;
                        progressState.CurrentFormName = p.CurrentFormName;
                        progressState.FilesGenerated = p.FilesGenerated;
                        progressState.TotalFilesToGenerate = p.TotalFilesToGenerate;
                        progressState.TotalControls = p.TotalControls;
                        progressState.ConvertedControls = p.ConvertedControls;
                        progressState.TotalProperties = p.TotalProperties;
                        progressState.MappedProperties = p.MappedProperties;
                        progressState.TotalEvents = p.TotalEvents;
                        progressState.ConvertedEvents = p.ConvertedEvents;
                        progressState.Warnings = p.Warnings;
                        progressState.Errors = p.Errors;
                        progressState.ElapsedTime = p.ElapsedTime;
                        progressState.IsGeneratingReport = p.IsGeneratingReport;
                        progressState.IsRollingBack = p.IsRollingBack;
                    }
                });

                result = await orchestrator.ExecuteAsync(basicProgress, _cts.Token);
                basicDisplay.Clear();
            }

            // Display results
            AnsiConsole.WriteLine();
            
            if (!result.Success)
            {
                AnsiConsole.Write(new Panel(
                    new Markup($"[{theme.Error}]{theme.ErrorIcon} Conversion failed: {Markup.Escape(result.ErrorMessage ?? "Unknown error")}[/]"))
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(theme.Error),
                    Padding = new Padding(1, 0)
                });
                Environment.ExitCode = 1;
                return;
            }

            // Success - display summary
            AnsiConsole.Write(new Panel(
                new Markup($"[{theme.Success}]{theme.SuccessIcon} Conversion completed successfully![/]"))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(theme.Success),
                Padding = new Padding(1, 0)
            });

            if (result.Report != null)
            {
                // Display forms table
                if (result.Report.Forms.Count > 0)
                {
                    AnsiConsole.WriteLine();
                    var formsTable = new Table()
                        .Border(TableBorder.Rounded)
                        .BorderColor(theme.Primary)
                        .AddColumn(new TableColumn("[bold]Form[/]"))
                        .AddColumn(new TableColumn("[bold]Controls[/]").Centered())
                        .AddColumn(new TableColumn("[bold]Layout[/]").Centered())
                        .AddColumn(new TableColumn("[bold]Status[/]").Centered());

                    foreach (var form in result.Report.Forms)
                    {
                        var statusIcon = form.Status == "Converted" 
                            ? $"[{theme.Success}]{theme.SuccessIcon}[/]"
                            : $"[{theme.Warning}]{theme.WarningIcon}[/]";

                        formsTable.AddRow(
                            new Markup(Markup.Escape(form.Name)),
                            new Markup($"[{theme.Info}]{form.ControlCount}[/]"),
                            new Markup($"[{theme.Primary}]{form.Layout}[/]"),
                            new Markup(statusIcon)
                        );
                    }

                    AnsiConsole.Write(formsTable);
                }

                // Display summary panel
                AnsiConsole.WriteLine();
                var summaryGrid = new Grid()
                    .AddColumn()
                    .AddColumn()
                    .AddRow("[bold]Forms:[/]", $"[{theme.Success}]{result.Report.Forms.Count}[/]")
                    .AddRow("[bold]Controls:[/]", $"[{theme.Info}]{result.Report.Statistics.ConvertedControls}[/]")
                    .AddRow("[bold]Properties:[/]", $"[{theme.Info}]{result.Report.Statistics.MappedProperties}[/]")
                    .AddRow("[bold]Events:[/]", $"[{theme.Info}]{result.Report.Statistics.ConvertedToCommands}[/]")
                    .AddRow("[bold]Duration:[/]", $"[dim]{result.Report.Duration.TotalSeconds:F2}s[/]")
                    .AddRow("[bold]Output:[/]", $"[dim]{Markup.Escape(result.OutputPath ?? "")}[/]");

                AnsiConsole.Write(new Panel(summaryGrid)
                {
                    Header = new PanelHeader("Conversion Summary", Justify.Left),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(theme.Success)
                });

                // Generate report if requested
                if (report != null)
                {
                    var reportBuilder = new ReportBuilder();
                    var format = reportFormat.ToLower() switch
                    {
                        "json" => ReportFormat.Json,
                        "md" or "markdown" => ReportFormat.Markdown,
                        "csv" => ReportFormat.Csv,
                        _ => ReportFormat.Html
                    };

                    var reportContent = reportBuilder.Generate(result.Report, format);
                    await File.WriteAllTextAsync(report, reportContent);
                    
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Panel(
                        new Markup($"[{theme.Info}]{theme.ReportIcon} Report generated: {Markup.Escape(report)}[/]"))
                    {
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(theme.Info),
                        Padding = new Padding(1, 0)
                    });
                }

                // Display warnings and errors
                if (result.Report.Warnings.Count > 0 || result.Report.Errors.Count > 0)
                {
                    AnsiConsole.WriteLine();
                    var issuesTable = new Table()
                        .Border(TableBorder.Rounded)
                        .AddColumn("[bold]Type[/]")
                        .AddColumn("[bold]Location[/]")
                        .AddColumn("[bold]Message[/]");

                    foreach (var warning in result.Report.Warnings.Take(5))
                    {
                        issuesTable.AddRow(
                            new Markup($"[{theme.Warning}]{theme.WarningIcon} Warning[/]"),
                            new Markup(Markup.Escape(warning.Location ?? "")),
                            new Markup(Markup.Escape(warning.Message ?? ""))
                        );
                    }

                    foreach (var error in result.Report.Errors.Take(5))
                    {
                        issuesTable.AddRow(
                            new Markup($"[{theme.Error}]{theme.ErrorIcon} Error[/]"),
                            new Markup(Markup.Escape(error.Location ?? "")),
                            new Markup(Markup.Escape(error.Message ?? ""))
                        );
                    }

                    if (result.Report.Warnings.Count + result.Report.Errors.Count > 10)
                    {
                        issuesTable.AddRow(
                            new Markup("[dim]...[/]"),
                            new Markup($"[dim]{result.Report.Warnings.Count + result.Report.Errors.Count - 10} more issues[/]"),
                            new Markup("[dim]See report for full details[/]")
                        );
                    }

                    AnsiConsole.Write(new Panel(issuesTable)
                    {
                        Header = new PanelHeader("Issues", Justify.Left),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(theme.Warning)
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel(
                new Markup("[yellow]⏸️  Conversion cancelled by user[/]\n\n" +
                          "[dim]Workspace has been restored to original state[/]"))
            {
                Header = new PanelHeader("Conversion Cancelled", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow),
                Padding = new Padding(1, 1)
            });
            Environment.ExitCode = 130; // Standard exit code for Ctrl+C
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Environment.ExitCode = 1;
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }
}
