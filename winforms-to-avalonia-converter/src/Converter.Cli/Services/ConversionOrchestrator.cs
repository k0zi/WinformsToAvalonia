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
using System.Diagnostics;
using System.Text.RegularExpressions;
using Converter.Cli.Models;

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
    private readonly LayoutMode _layoutMode;
    private readonly bool _force;
    private readonly bool _resume;

    private OperationType _lastReportedOperation = OperationType.GitInit;
    private DateTime _lastProgressReport = DateTime.MinValue;
    private readonly Stopwatch _stopwatch = new();

    /// <summary>
    /// Guards RollbackManager mutations (TrackFileCreation) when forms are converted
    /// concurrently - RollbackManager's internal collections aren't thread-safe on their
    /// own, and this phase intentionally doesn't change RollbackManager itself.
    /// </summary>
    private readonly object _rollbackLock = new();

    public ConversionOrchestrator(
        string sourcePath,
        string outputPath,
        ConverterConfig config,
        ILogger<ConversionOrchestrator>? logger = null,
        LayoutMode layoutMode = LayoutMode.Auto,
        bool force = false,
        bool resume = false)
    {
        _sourcePath = sourcePath;
        _outputPath = outputPath;
        _config = config;
        _logger = logger;
        _layoutMode = layoutMode;
        _force = force;
        _resume = resume;
    }

    /// <summary>
    /// Execute the full conversion process.
    /// </summary>
    public async Task<ConversionResult> ExecuteAsync(
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _stopwatch.Start();
        var startTime = DateTime.Now;
        var statistics = new ConversionStatistics();
        var formReports = new List<FormReportInfo>();
        var errors = new List<ReportMessage>();
        var warnings = new List<ReportMessage>();
        var rollbackManager = new RollbackManager();
        rollbackManager.BeginTransaction();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger?.LogInformation("Starting conversion: {SourcePath} -> {OutputPath}", _sourcePath, _outputPath);

            // Step 1: Initialize git if enabled
            GitIntegrationManager? gitManager = null;
            ReportProgress(OperationType.GitInit, progress, statistics, 0, 0, 0, 0);

            if (_config.GitIntegration.Enabled)
            {
                gitManager = new GitIntegrationManager(_logger as ILogger<GitIntegrationManager>);
                if (_config.GitIntegration.CreateFeatureBranch && gitManager.IsGitRepository(_sourcePath))
                {
                    var branchName = gitManager.CreateFeatureBranch(_sourcePath, _config.GitIntegration.BranchNamePattern);

                    if (branchName != null)
                    {
                        _logger?.LogInformation("Created git branch: {BranchName}", branchName);
                    }
                }
            }

            // Step 2: Parse WinForms files
            cancellationToken.ThrowIfCancellationRequested();
            ReportProgress(OperationType.Parsing, progress, statistics, 0, 0, 0, 0, force: true);

            _logger?.LogInformation("Parsing WinForms files...");
            var parser = new WinFormsParser();
            var parseResults = new List<ParseResult>();

            var designerFiles = Directory.GetFiles(_sourcePath, "*.Designer.cs", SearchOption.AllDirectories)
                .Where(f => !IsExcluded(f, _config.ExcludePatterns))
                .ToArray();
            _logger?.LogInformation("Found {Count} designer files", designerFiles.Length);

            // Incremental conversion: skip files whose hash hasn't changed since they were
            // last converted, unless the user forced a full reconversion.
            FileHashTracker? hashTracker = null;
            if (_config.IncrementalSettings.Enabled && !_force)
            {
                hashTracker = new FileHashTracker(_outputPath, _config.IncrementalSettings.CacheFileName);
                await hashTracker.LoadCacheAsync();

                var toProcess = new List<string>();
                var skipped = 0;
                foreach (var file in designerFiles)
                {
                    if (await hashTracker.HasFileChangedAsync(file))
                    {
                        toProcess.Add(file);
                    }
                    else
                    {
                        skipped++;
                    }
                }

                if (skipped > 0)
                {
                    _logger?.LogInformation("Skipping {Count} unchanged designer file(s) (incremental mode)", skipped);
                }

                designerFiles = toProcess.ToArray();
            }

            foreach (var file in designerFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

            // Calculate total files to generate: 3 per form + 5 project files
            var totalForms = parseResults.Count;
            var totalFilesToGenerate = (totalForms * 3) + 5;
            ReportProgress(OperationType.Parsing, progress, statistics, totalForms, totalFilesToGenerate, 0, 0, force: true);

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
            var styleGenerator = new StyleGenerator();

            var namespaceName = _config.NamingConventions.RootNamespace ?? Path.GetFileName(_outputPath);
            var viewModelSuffix = _config.NamingConventions.ViewModelSuffix;

            var layoutContext = new LayoutAnalysisContext
            {
                AlignmentTolerance = _config.LayoutDetection.AlignmentTolerance,
                ConfidenceThreshold = _config.LayoutDetection.ConfidenceThreshold,
                Mode = _layoutMode,
                GridWeight = _config.LayoutDetection.GridDetectionWeight,
                StackWeight = _config.LayoutDetection.StackDetectionWeight,
                DockWeight = _config.LayoutDetection.DockDetectionWeight
            };

            ReportProgress(OperationType.ConvertingForm, progress, statistics, totalForms, totalFilesToGenerate, 0, 0, force: true);

            var outcomes = _config.ParallelProcessing.Enabled && parseResults.Count > 1
                ? await ConvertFormsInParallelAsync(
                    parseResults, layoutAnalyzer, axamlGenerator, vmGenerator, codeBehindGenerator, styleGenerator,
                    rollbackManager, viewsDir, viewModelsDir, namespaceName, viewModelSuffix, layoutContext,
                    _config.ParallelProcessing.MaxDegreeOfParallelism, cancellationToken)
                : await ConvertFormsSequentiallyAsync(
                    parseResults, layoutAnalyzer, axamlGenerator, vmGenerator, codeBehindGenerator, styleGenerator,
                    rollbackManager, viewsDir, viewModelsDir, namespaceName, viewModelSuffix, layoutContext,
                    progress, statistics, totalForms, totalFilesToGenerate, cancellationToken);

            var filesGenerated = 0;
            var formsProcessed = 0;
            var manualSteps = new List<ManualStepInfo>();
            foreach (var outcome in outcomes)
            {
                formsProcessed++;
                if (outcome.Report != null)
                {
                    formReports.Add(outcome.Report);
                    statistics.ConvertedControls += outcome.ControlCount;
                    filesGenerated += 3;
                    manualSteps.AddRange(outcome.ManualSteps);
                    _logger?.LogInformation("Converted form: {FormName}", outcome.Report.Name);

                    if (hashTracker != null)
                    {
                        await hashTracker.UpdateFileHashAsync(outcome.SourceFile);
                    }
                }
                else
                {
                    _logger?.LogError(outcome.Error, "Failed to convert form from {File}", outcome.SourceFile);
                    errors.Add(new ReportMessage
                    {
                        Location = outcome.SourceFile,
                        Message = $"Conversion failed: {outcome.Error?.Message}"
                    });
                }
            }

            if (hashTracker != null)
            {
                await hashTracker.SaveCacheAsync();
            }

            ReportProgress(OperationType.ConvertingForm, progress, statistics, totalForms, totalFilesToGenerate,
                formsProcessed, filesGenerated, force: true);

            // Step 5: Generate project files
            cancellationToken.ThrowIfCancellationRequested();
            ReportProgress(OperationType.GeneratingProjectFiles, progress, statistics, totalForms, totalFilesToGenerate,
                formsProcessed, filesGenerated, force: true);

            _logger?.LogInformation("Generating project files...");
            await GenerateProjectFilesAsync(rollbackManager);
            filesGenerated += 5; // Project files generated

            ReportProgress(OperationType.GeneratingProjectFiles, progress, statistics, totalForms, totalFilesToGenerate,
                formsProcessed, filesGenerated);

            // Step 6: Generate migration guide if enabled
            if (_config.Documentation.Enabled)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReportProgress(OperationType.GeneratingMigrationGuide, progress, statistics, totalForms, totalFilesToGenerate,
                    formsProcessed, filesGenerated, force: true);

                _logger?.LogInformation("Generating migration guide...");
                await GenerateMigrationGuideAsync(formReports, statistics, rollbackManager, manualSteps);
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

            // Report completion
            ReportProgress(OperationType.Complete, progress, statistics, totalForms, totalFilesToGenerate,
                formsProcessed, filesGenerated, force: true);

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

            rollbackManager.CommitTransaction();

            return new ConversionResult
            {
                Success = true,
                Report = report,
                OutputPath = _outputPath
            };
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Conversion cancelled by user");

            // Report rollback state
            var progressState = new ConversionProgress { IsRollingBack = true };
            progress?.Report(progressState);

            // Perform rollback
            await rollbackManager.RollbackTransactionAsync();

            // Report cancelled state
            progressState = new ConversionProgress
            {
                CurrentOperation = OperationType.Cancelled,
                ElapsedTime = _stopwatch.Elapsed
            };
            progress?.Report(progressState);

            return new ConversionResult
            {
                Success = false,
                ErrorMessage = "Conversion cancelled by user",
                OutputPath = _outputPath
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Conversion failed");

            // Roll back any files already written this run so a failed conversion doesn't
            // leave a half-converted output directory behind.
            await rollbackManager.RollbackTransactionAsync();

            return new ConversionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                OutputPath = _outputPath
            };
        }
    }

    private void ReportProgress(
        OperationType operation,
        IProgress<ConversionProgress>? progress,
        ConversionStatistics statistics,
        int totalForms,
        int totalFiles,
        int formsProcessed,
        int filesGenerated,
        string? formName = null,
        string? subOperation = null,
        bool force = false)
    {
        if (progress == null)
            return;

        // Check if we should report (operation changed, forced, or 100ms elapsed)
        var shouldReport = force ||
                          operation != _lastReportedOperation ||
                          (DateTime.Now - _lastProgressReport).TotalMilliseconds >= 100;

        if (!shouldReport)
            return;

        var progressState = new ConversionProgress
        {
            CurrentOperation = operation,
            CurrentSubOperation = subOperation,
            FormsProcessed = formsProcessed,
            TotalForms = totalForms,
            CurrentFormName = formName,
            FilesGenerated = filesGenerated,
            TotalFilesToGenerate = totalFiles,
            TotalControls = statistics.TotalControls,
            ConvertedControls = statistics.ConvertedControls,
            TotalProperties = statistics.TotalProperties,
            MappedProperties = statistics.MappedProperties,
            TotalEvents = statistics.TotalEvents,
            ConvertedEvents = statistics.ConvertedToCommands,
            Warnings = statistics.CheckpointsSaved, // Placeholder
            Errors = statistics.RollbacksPerformed, // Placeholder
            ElapsedTime = _stopwatch.Elapsed
        };

        progress.Report(progressState);
        _lastProgressReport = DateTime.Now;
        _lastReportedOperation = operation;
    }

    private void TrackFileCreationSafe(RollbackManager rollbackManager, string filePath)
    {
        lock (_rollbackLock)
        {
            rollbackManager.TrackFileCreation(filePath);
        }
    }

    /// <summary>
    /// Result of converting a single form: either a populated Report (success) or an Error
    /// (failure), never both. ManualSteps is always populated (even on failure, as empty)
    /// so the migration guide can be built from a flat concatenation of every outcome's list.
    /// </summary>
    private readonly record struct FormConversionOutcome(
        FormReportInfo? Report,
        int ControlCount,
        string SourceFile,
        Exception? Error,
        IReadOnlyList<ManualStepInfo> ManualSteps);

    /// <summary>
    /// Converts a single form. Pure with respect to shared orchestrator state - reads only
    /// its parameters and writes only its own output files - so it's safe to call
    /// concurrently from multiple tasks (see ConvertFormsInParallelAsync). AxamlGenerator,
    /// ViewModelGenerator, CodeBehindGenerator, StyleGenerator and LayoutAnalyzer hold no
    /// mutable instance state, so sharing single instances across concurrent calls is safe
    /// too. File-creation tracking goes through TrackFileCreationSafe, which locks around
    /// RollbackManager (itself not thread-safe).
    /// </summary>
    private async Task<FormConversionOutcome> ConvertFormAsync(
        ParseResult parseResult,
        LayoutAnalyzer layoutAnalyzer,
        AxamlGenerator axamlGenerator,
        ViewModelGenerator vmGenerator,
        CodeBehindGenerator codeBehindGenerator,
        StyleGenerator styleGenerator,
        RollbackManager rollbackManager,
        string viewsDir,
        string viewModelsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext)
    {
        try
        {
            var rootControl = parseResult.RootControl!;
            var className = rootControl.Name;

            var layoutResult = await layoutAnalyzer.AnalyzeAsync(rootControl, layoutContext);
            var layoutType = layoutResult.LayoutType;

            var axamlContent = axamlGenerator.Generate(rootControl, layoutResult, namespaceName, className);
            var vmContent = vmGenerator.GeneratePartialClass(rootControl, namespaceName, className);
            var codeBehindContent = codeBehindGenerator.Generate(namespaceName, className);

            var axamlPath = Path.Combine(viewsDir, $"{className}.axaml");
            var codeBehindPath = Path.Combine(viewsDir, $"{className}.axaml.cs");
            var vmPath = Path.Combine(viewModelsDir, $"{className}{viewModelSuffix}.g.cs");

            await File.WriteAllTextAsync(axamlPath, axamlContent);
            TrackFileCreationSafe(rollbackManager, axamlPath);

            await File.WriteAllTextAsync(codeBehindPath, codeBehindContent);
            TrackFileCreationSafe(rollbackManager, codeBehindPath);

            await File.WriteAllTextAsync(vmPath, vmContent);
            TrackFileCreationSafe(rollbackManager, vmPath);

            if (_config.StyleExtraction.Enabled)
            {
                var stylesContent = styleGenerator.GenerateStyles(rootControl, _config.StyleExtraction.MinimumOccurrence);
                if (!string.IsNullOrWhiteSpace(stylesContent))
                {
                    var stylesPath = Path.Combine(viewsDir, $"{className}.Styles.axaml");
                    await File.WriteAllTextAsync(stylesPath, stylesContent);
                    TrackFileCreationSafe(rollbackManager, stylesPath);
                }
            }

            var controlCount = CountControls(rootControl);

            var report = new FormReportInfo
            {
                Name = className,
                ControlCount = controlCount,
                Layout = layoutType.ToString(),
                Status = "Converted"
            };

            var manualSteps = CollectManualSteps(rootControl, parseResult.FilePath);

            return new FormConversionOutcome(report, controlCount, parseResult.FilePath, null, manualSteps);
        }
        catch (Exception ex)
        {
            return new FormConversionOutcome(null, 0, parseResult.FilePath, ex, []);
        }
    }

    /// <summary>
    /// Walks the converted control tree to find everything the migration guide should flag
    /// as needing manual attention: controls with no Avalonia mapping (rendered as a TODO
    /// comment by AxamlGenerator), properties whose mapping is flagged RequiresCustomLogic
    /// (dropped or only partially converted), and events whose mapping is flagged
    /// PreserveEventHandler (CodeBehindGenerator doesn't yet migrate handler bodies, so
    /// these still need to be manually ported). Without this, GenerateMigrationGuideAsync
    /// always reported "no manual steps required" even when these issues were present.
    /// </summary>
    private static List<ManualStepInfo> CollectManualSteps(ControlNode root, string sourceFile)
    {
        var steps = new List<ManualStepInfo>();
        CollectManualStepsRecursive(root, sourceFile, steps);
        return steps;
    }

    private static void CollectManualStepsRecursive(ControlNode control, string sourceFile, List<ManualStepInfo> steps)
    {
        if (ControlMappingRegistry.GetMapping(control.ControlType) == null)
        {
            steps.Add(new ManualStepInfo
            {
                Category = "Unmapped Controls",
                Title = $"{control.ControlType} \"{control.Name}\" has no Avalonia mapping",
                Location = sourceFile,
                Description = "This control type has no built-in WinForms-to-Avalonia mapping; it was emitted " +
                    "as a TODO placeholder in the AXAML and needs a manual replacement."
            });
        }

        foreach (var propName in control.Properties.Keys)
        {
            var propMapping = PropertyMappingRegistry.GetMapping(propName, control.ControlType);
            if (propMapping?.RequiresCustomLogic == true)
            {
                steps.Add(new ManualStepInfo
                {
                    Category = "Custom Property Logic",
                    Title = $"{control.Name}.{propName} requires custom conversion logic",
                    Location = sourceFile,
                    Description = propMapping.Notes ??
                        $"Maps toward '{propMapping.AvaloniaProperty}' but the automatic converter could not " +
                        "fully translate this property; review the generated AXAML."
                });
            }
        }

        foreach (var eventName in control.EventHandlers.Keys)
        {
            var eventMapping = EventMappingRegistry.GetMapping(eventName);
            if (eventMapping?.PreserveEventHandler == true)
            {
                steps.Add(new ManualStepInfo
                {
                    Category = "Preserved Event Handlers",
                    Title = $"{control.Name}.{eventName} handler \"{control.EventHandlers[eventName]}\" needs manual porting",
                    Location = sourceFile,
                    Description = eventMapping.Notes ??
                        $"Maps to Avalonia's '{eventMapping.AvaloniaEvent}' event, but the original handler body " +
                        "must be manually migrated to code-behind or the ViewModel."
                });
            }
        }

        foreach (var child in control.Children)
        {
            CollectManualStepsRecursive(child, sourceFile, steps);
        }
    }

    private async Task<List<FormConversionOutcome>> ConvertFormsSequentiallyAsync(
        List<ParseResult> parseResults,
        LayoutAnalyzer layoutAnalyzer,
        AxamlGenerator axamlGenerator,
        ViewModelGenerator vmGenerator,
        CodeBehindGenerator codeBehindGenerator,
        StyleGenerator styleGenerator,
        RollbackManager rollbackManager,
        string viewsDir,
        string viewModelsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext,
        IProgress<ConversionProgress>? progress,
        ConversionStatistics statistics,
        int totalForms,
        int totalFilesToGenerate,
        CancellationToken cancellationToken)
    {
        var outcomes = new List<FormConversionOutcome>(parseResults.Count);
        var formsProcessed = 0;
        var filesGenerated = 0;

        foreach (var parseResult in parseResults)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var formName = parseResult.RootControl?.Name ?? "Unknown";
            ReportProgress(OperationType.ConvertingForm, progress, statistics, totalForms, totalFilesToGenerate,
                formsProcessed, filesGenerated, formName: formName, force: true);

            var outcome = await ConvertFormAsync(parseResult, layoutAnalyzer, axamlGenerator, vmGenerator,
                codeBehindGenerator, styleGenerator, rollbackManager, viewsDir, viewModelsDir, namespaceName,
                viewModelSuffix, layoutContext);

            outcomes.Add(outcome);
            formsProcessed++;
            if (outcome.Report != null)
            {
                filesGenerated += 3;
            }
        }

        return outcomes;
    }

    /// <summary>
    /// Converts forms concurrently. Deliberately does not report per-form progress:
    /// ReportProgress mutates shared instance state (_lastReportedOperation,
    /// _lastProgressReport) that isn't safe to touch from concurrent tasks. Each slot in
    /// `outcomes` is written by exactly one task (indexed by position), so no locking is
    /// needed for that array; the caller reports one aggregate progress update after this
    /// returns. RollbackManager file tracking still happens live (via TrackFileCreationSafe
    /// inside ConvertFormAsync) so files written by tasks that complete before a
    /// cancellation are tracked for rollback even if the loop as a whole is interrupted.
    /// </summary>
    private async Task<List<FormConversionOutcome>> ConvertFormsInParallelAsync(
        List<ParseResult> parseResults,
        LayoutAnalyzer layoutAnalyzer,
        AxamlGenerator axamlGenerator,
        ViewModelGenerator vmGenerator,
        CodeBehindGenerator codeBehindGenerator,
        StyleGenerator styleGenerator,
        RollbackManager rollbackManager,
        string viewsDir,
        string viewModelsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext,
        int? maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var outcomes = new FormConversionOutcome?[parseResults.Count];

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = maxDegreeOfParallelism ?? -1
        };

        await Parallel.ForEachAsync(Enumerable.Range(0, parseResults.Count), options, async (i, _) =>
        {
            outcomes[i] = await ConvertFormAsync(parseResults[i], layoutAnalyzer, axamlGenerator, vmGenerator,
                codeBehindGenerator, styleGenerator, rollbackManager, viewsDir, viewModelsDir, namespaceName,
                viewModelSuffix, layoutContext);
        });

        return outcomes.Select(o => o!.Value).ToList();
    }

    private async Task GenerateProjectFilesAsync(RollbackManager rollbackManager)
    {
        var projectGenerator = new ProjectFileGenerator();
        var projectName = Path.GetFileName(_outputPath);

        var csprojContent = projectGenerator.GenerateAvaloniaProject(projectName);
        var appAxamlContent = projectGenerator.GenerateAppAxaml(projectName);
        var appCodeBehindContent = projectGenerator.GenerateAppCodeBehind(projectName, "MainWindow");
        var programContent = projectGenerator.GenerateProgramFile(projectName);
        var manifestContent = projectGenerator.GenerateAppManifest();

        var csprojPath = Path.Combine(_outputPath, $"{projectName}.csproj");
        await File.WriteAllTextAsync(csprojPath, csprojContent);
        rollbackManager.TrackFileCreation(csprojPath);

        var appAxamlPath = Path.Combine(_outputPath, "App.axaml");
        await File.WriteAllTextAsync(appAxamlPath, appAxamlContent);
        rollbackManager.TrackFileCreation(appAxamlPath);

        var appCodeBehindPath = Path.Combine(_outputPath, "App.axaml.cs");
        await File.WriteAllTextAsync(appCodeBehindPath, appCodeBehindContent);
        rollbackManager.TrackFileCreation(appCodeBehindPath);

        var programPath = Path.Combine(_outputPath, "Program.cs");
        await File.WriteAllTextAsync(programPath, programContent);
        rollbackManager.TrackFileCreation(programPath);

        var manifestPath = Path.Combine(_outputPath, "app.manifest");
        await File.WriteAllTextAsync(manifestPath, manifestContent);
        rollbackManager.TrackFileCreation(manifestPath);
    }

    private async Task GenerateMigrationGuideAsync(
        List<FormReportInfo> formReports,
        ConversionStatistics statistics,
        RollbackManager rollbackManager,
        List<ManualStepInfo> manualSteps)
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
            }).ToList(),
            ManualSteps = manualSteps
        };

        var guideContent = guideGenerator.Generate(context);
        var guidePath = Path.Combine(_outputPath, "MIGRATION_GUIDE.md");

        await File.WriteAllTextAsync(guidePath, guideContent);
        rollbackManager.TrackFileCreation(guidePath);
    }

    private int CountControls(ControlNode node)
    {
        return 1 + node.Children.Sum(c => CountControls(c));
    }

    /// <summary>
    /// Checks a file path against ConverterConfig.ExcludePatterns using simple wildcard
    /// matching (`*`/`?`) plus a plain substring fallback, so patterns like "*.Designer.cs"
    /// or a bare folder name ("Legacy") both work without pulling in a full glob library.
    /// </summary>
    private static bool IsExcluded(string filePath, IReadOnlyList<string> excludePatterns)
    {
        if (excludePatterns.Count == 0)
        {
            return false;
        }

        var normalizedPath = filePath.Replace('\\', '/');

        foreach (var pattern in excludePatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            var normalizedPattern = pattern.Replace('\\', '/');

            if (normalizedPattern.Contains('*') || normalizedPattern.Contains('?'))
            {
                var regexPattern = "^" + Regex.Escape(normalizedPattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                if (Regex.IsMatch(Path.GetFileName(normalizedPath), regexPattern, RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(normalizedPath, regexPattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            else if (normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
