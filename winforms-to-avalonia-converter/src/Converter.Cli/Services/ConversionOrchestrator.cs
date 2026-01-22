using Converter.Core.Analysis;
using Converter.Core.Configuration;
using Converter.Core.Git;
using Converter.Core.Models;
using Converter.Core.Parsing;
using Converter.Core.Services;
using Converter.Documentation.Generators;
using Converter.Generator.Axaml;
using Converter.Generator.CodeBehind;
using Converter.Generator.Project;
using Converter.Generator.Styles;
using Converter.Generator.ViewModels;
using Converter.Mappings.BuiltIn;
using Converter.Plugin.Abstractions;
using Converter.Reporting.Builders;
using Microsoft.Extensions.Logging;

namespace Converter.Cli.Services;

/// <summary>
/// Orchestrates the conversion process from WinForms to Avalonia.
/// </summary>
public class ConversionOrchestrator
{
    private readonly string _sourcePath;
    private readonly string _outputPath;
    private readonly ConverterConfig _config;
    private readonly ILogger<ConversionOrchestrator>? _logger;

    public ConversionOrchestrator(
        string sourcePath,
        string outputPath,
        ConverterConfig config,
        ILogger<ConversionOrchestrator>? logger = null)
    {
        _sourcePath = sourcePath;
        _outputPath = outputPath;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Execute the full conversion process.
    /// </summary>
    public async Task<ConversionResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var statistics = new ConversionStatistics();
        var formReports = new List<FormReportInfo>();
        var errors = new List<ReportMessage>();
        var warnings = new List<ReportMessage>();

        try
        {
            _logger?.LogInformation("Starting conversion: {SourcePath} -> {OutputPath}", _sourcePath, _outputPath);

            // Step 1: Initialize git if enabled
            GitIntegrationManager? gitManager = null;
            if (_config.GitIntegration.Enabled)
            {
                gitManager = new GitIntegrationManager(_logger as ILogger<GitIntegrationManager>);
                if (gitManager.IsGitRepository(_sourcePath))
                {
                    var branchPattern = "feature/avalonia-migration-{timestamp}";
                    var branchName = gitManager.CreateFeatureBranch(_sourcePath, branchPattern);
                    
                    if (branchName != null)
                    {
                        _logger?.LogInformation("Created git branch: {BranchName}", branchName);
                    }
                }
            }

            // Step 2: Parse WinForms files
            _logger?.LogInformation("Parsing WinForms files...");
            var parser = new WinFormsParser();
            var parseResults = new List<ParseResult>();
            
            var designerFiles = Directory.GetFiles(_sourcePath, "*.Designer.cs", SearchOption.AllDirectories);
            _logger?.LogInformation("Found {Count} designer files", designerFiles.Length);

            foreach (var file in designerFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var result = await parser.ParseDesignerFileAsync(file);
                    if (result.RootControl != null)
                    {
                        parseResults.Add(result);
                        statistics.TotalControls += CountControls(result.RootControl);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse {File}", file);
                    warnings.Add(new ReportMessage
                    {
                        Location = file,
                        Message = $"Parse failed: {ex.Message}"
                    });
                }
            }

            _logger?.LogInformation("Parsed {Count} forms with {TotalControls} total controls", 
                parseResults.Count, statistics.TotalControls);

            // Step 3: Create output directory
            Directory.CreateDirectory(_outputPath);
            var viewsDir = Path.Combine(_outputPath, "Views");
            var viewModelsDir = Path.Combine(_outputPath, "ViewModels");
            Directory.CreateDirectory(viewsDir);
            Directory.CreateDirectory(viewModelsDir);

            // Step 4: Convert each form
            var layoutAnalyzer = new LayoutAnalyzer();
            var axamlGenerator = new AxamlGenerator();
            var vmGenerator = new ViewModelGenerator();
            var codeBehindGenerator = new CodeBehindGenerator();

            foreach (var parseResult in parseResults)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var formReport = await ConvertFormAsync(
                        parseResult,
                        layoutAnalyzer,
                        axamlGenerator,
                        vmGenerator,
                        codeBehindGenerator,
                        viewsDir,
                        viewModelsDir,
                        statistics);

                    formReports.Add(formReport);
                    _logger?.LogInformation("Converted form: {FormName}", formReport.Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to convert form from {File}", parseResult.FilePath);
                    errors.Add(new ReportMessage
                    {
                        Location = parseResult.FilePath,
                        Message = $"Conversion failed: {ex.Message}"
                    });
                }
            }

            // Step 5: Generate project files
            _logger?.LogInformation("Generating project files...");
            await GenerateProjectFilesAsync();

            // Step 6: Generate migration guide if enabled
            if (_config.Documentation.Enabled)
            {
                _logger?.LogInformation("Generating migration guide...");
                await GenerateMigrationGuideAsync(formReports, statistics);
            }

            // Step 7: Commit to git if enabled
            if (gitManager != null)
            {
                var files = Directory.GetFiles(_outputPath, "*", SearchOption.AllDirectories);
                gitManager.CommitChanges(
                    _sourcePath,
                    "feat: Convert WinForms to Avalonia",
                    files);
            }

            var duration = DateTime.Now - startTime;
            _logger?.LogInformation("Conversion completed in {Duration}s", duration.TotalSeconds);

            // Generate report
            var report = new ConversionReport
            {
                ProjectName = Path.GetFileName(_outputPath),
                Timestamp = DateTime.Now,
                Duration = duration,
                Status = errors.Count > 0 ? ConversionStatus.PartialSuccess : ConversionStatus.Success,
                Statistics = statistics,
                Forms = formReports,
                Warnings = warnings,
                Errors = errors
            };

            return new ConversionResult
            {
                Success = true,
                Report = report,
                OutputPath = _outputPath
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Conversion failed");
            
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                OutputPath = _outputPath
            };
        }
    }

    private async Task<FormReportInfo> ConvertFormAsync(
        ParseResult parseResult,
        LayoutAnalyzer layoutAnalyzer,
        AxamlGenerator axamlGenerator,
        ViewModelGenerator vmGenerator,
        CodeBehindGenerator codeBehindGenerator,
        string viewsDir,
        string viewModelsDir,
        ConversionStatistics statistics)
    {
        var rootControl = parseResult.RootControl!;
        var className = rootControl.Name;
        var namespaceName = "ConvertedApp";

        // Analyze layout
        var layoutContext = new LayoutAnalysisContext();
        var layoutResult = await layoutAnalyzer.AnalyzeAsync(rootControl, layoutContext);
        
        var layoutType = layoutResult.LayoutType;

        // Generate AXAML
        var axamlContent = axamlGenerator.Generate(
            rootControl,
            layoutResult,
            namespaceName,
            className);

        // Generate ViewModel
        var vmContent = vmGenerator.GeneratePartialClass(
            rootControl,
            namespaceName,
            className);

        // Generate code-behind
        var codeBehindContent = codeBehindGenerator.Generate(namespaceName, className);

        // Write files
        var axamlPath = Path.Combine(viewsDir, $"{className}.axaml");
        var codeBehindPath = Path.Combine(viewsDir, $"{className}.axaml.cs");
        var vmPath = Path.Combine(viewModelsDir, $"{className}ViewModel.g.cs");

        await File.WriteAllTextAsync(axamlPath, axamlContent);
        await File.WriteAllTextAsync(codeBehindPath, codeBehindContent);
        await File.WriteAllTextAsync(vmPath, vmContent);

        // Update statistics
        var controlCount = CountControls(rootControl);
        statistics.ConvertedControls += controlCount;

        return new FormReportInfo
        {
            Name = className,
            ControlCount = controlCount,
            Layout = layoutType.ToString(),
            Status = "Converted"
        };
    }

    private async Task GenerateProjectFilesAsync()
    {
        var projectGenerator = new ProjectFileGenerator();
        var projectName = Path.GetFileName(_outputPath);

        var csprojContent = projectGenerator.GenerateAvaloniaProject(projectName);
        var appAxamlContent = projectGenerator.GenerateAppAxaml(projectName);
        var appCodeBehindContent = projectGenerator.GenerateAppCodeBehind(projectName, "MainWindow");
        var programContent = projectGenerator.GenerateProgramFile(projectName);
        var manifestContent = projectGenerator.GenerateAppManifest();

        await File.WriteAllTextAsync(
            Path.Combine(_outputPath, $"{projectName}.csproj"),
            csprojContent);

        await File.WriteAllTextAsync(
            Path.Combine(_outputPath, "App.axaml"),
            appAxamlContent);

        await File.WriteAllTextAsync(
            Path.Combine(_outputPath, "App.axaml.cs"),
            appCodeBehindContent);

        await File.WriteAllTextAsync(
            Path.Combine(_outputPath, "Program.cs"),
            programContent);

        await File.WriteAllTextAsync(
            Path.Combine(_outputPath, "app.manifest"),
            manifestContent);
    }

    private async Task GenerateMigrationGuideAsync(
        List<FormReportInfo> formReports,
        ConversionStatistics statistics)
    {
        var guideGenerator = new MigrationGuideGenerator();
        
        var context = new MigrationGuideContext
        {
            ProjectName = Path.GetFileName(_outputPath),
            Statistics = statistics,
            ConvertedForms = formReports.Select(f => new FormConversionInfo
            {
                OriginalName = f.Name,
                AvaloniaName = f.Name,
                ControlCount = f.ControlCount,
                LayoutType = f.Layout,
                LayoutConfidence = 85,
                LayoutReason = "Analyzed control positioning patterns",
                Status = f.Status
            }).ToList()
        };

        var guideContent = guideGenerator.Generate(context);
        var guidePath = Path.Combine(_outputPath, "MIGRATION_GUIDE.md");

        await File.WriteAllTextAsync(guidePath, guideContent);
    }

    private int CountControls(ControlNode node)
    {
        return 1 + node.Children.Sum(c => CountControls(c));
    }
}

/// <summary>
/// Result of the conversion process.
/// </summary>
public class ConversionResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public ConversionReport? Report { get; init; }
    public string? OutputPath { get; init; }
}
