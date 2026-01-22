using System.CommandLine;
using Converter.Cli.Services;
using Converter.Core.Configuration;
using Converter.Reporting.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Converter.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
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

            await ExecuteConvertAsync(input, output, layout, report, reportFormat, config, plugins,
                incremental, force, resume, parallel, createBranch, branchName, noGit, migrationGuide, dryRun);
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
        string input,
        string output,
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
        bool dryRun)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   WinForms to Avalonia Converter (NET 10.0)          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            Console.WriteLine($"Input:  {input}");
            Console.WriteLine($"Output: {output}");
            Console.WriteLine($"Layout: {layout}");
            Console.WriteLine();

            if (dryRun)
            {
                Console.WriteLine("🔍 DRY RUN MODE - No files will be generated");
                return;
            }

            // Load or create configuration
            var converterConfig = config != null
                ? await ConfigurationLoader.LoadAsync(config)
                : new ConverterConfig();

            // Create logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            var logger = loggerFactory.CreateLogger<ConversionOrchestrator>();

            // Execute conversion
            Console.WriteLine("🔄 Starting conversion...");
            Console.WriteLine();

            var orchestrator = new ConversionOrchestrator(
                Path.GetFullPath(input),
                Path.GetFullPath(output),
                converterConfig,
                logger);

            var result = await orchestrator.ExecuteAsync();

            if (!result.Success)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Conversion failed: {result.ErrorMessage}");
                Console.ResetColor();
                Environment.ExitCode = 1;
                return;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Conversion completed successfully!");
            Console.ResetColor();
            Console.WriteLine($"   Output: {result.OutputPath}");
            
            if (result.Report != null)
            {
                Console.WriteLine($"   Forms: {result.Report.Forms.Count}");
                Console.WriteLine($"   Controls: {result.Report.Statistics.ConvertedControls}");
                Console.WriteLine($"   Duration: {result.Report.Duration.TotalSeconds:F2}s");

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
                    Console.WriteLine($"   Report: {report}");
                }

                if (result.Report.Warnings.Count > 0)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️  {result.Report.Warnings.Count} warning(s)");
                    Console.ResetColor();
                }

                if (result.Report.Errors.Count > 0)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ {result.Report.Errors.Count} error(s)");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(ex.StackTrace);
            Environment.ExitCode = 1;
        }
    }
}
