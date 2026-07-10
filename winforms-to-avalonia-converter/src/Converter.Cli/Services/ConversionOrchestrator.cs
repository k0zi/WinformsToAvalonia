using Converter.Core.Analysis;
using Converter.Core.Configuration;
using Converter.Core.Git;
using Converter.Core.Models;
using Converter.Core.Parsing;
using Converter.Core.Plugins;
using Converter.Core.Services;
using Converter.Documentation.Generators;
using Converter.Generator.Axaml;
using Converter.Generator.CodeBehind;
using Converter.Generator.Mapping;
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
    private readonly string? _pluginsDirectory;

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
        bool resume = false,
        string? pluginsDirectory = null)
    {
        _sourcePath = sourcePath;
        _outputPath = outputPath;
        _config = config;
        _logger = logger;
        _layoutMode = layoutMode;
        _force = force;
        _resume = resume;
        _pluginsDirectory = pluginsDirectory;
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

        // Declared outside the try block so the catch blocks below can still reach
        // UnloadAllPluginsAsync() regardless of which exit path is taken.
        var pluginLoader = new PluginLoader(_logger);
        var mappingResolver = MappingResolver.Empty;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger?.LogInformation("Starting conversion: {SourcePath} -> {OutputPath}", _sourcePath, _outputPath);

            // Step 0: Load plugins if a plugins directory is configured. The overwhelmingly
            // common case (no plugins) short-circuits on Directory.Exists before any
            // scanning happens - one directory-existence check per run, not per call.
            var pluginsDirectoryPath = Path.GetFullPath(_pluginsDirectory ?? _config.Plugins.PluginsDirectory);
            if (Directory.Exists(pluginsDirectoryPath))
            {
                var enabledPlugins = _config.Plugins.EnabledPlugins is { Count: > 0 } ? _config.Plugins.EnabledPlugins : null;
                await pluginLoader.LoadAllPluginsAsync(pluginsDirectoryPath, enabledPlugins);
                mappingResolver = new MappingResolver(
                    pluginLoader.GetPlugins<IControlMapper>(),
                    pluginLoader.GetPlugins<IPropertyTranslator>(),
                    pluginLoader.GetPlugins<IEventMapper>());

                if (mappingResolver.HasPlugins)
                {
                    _logger?.LogInformation("Loaded {Count} plugin(s) from {Directory}",
                        pluginLoader.LoadedPlugins.Count, pluginsDirectoryPath);
                }
            }

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
                // Track the cache file for rollback *before* it's (re)written below, so a
                // failed/cancelled non-resume run restores it to its pre-run state (or deletes
                // it if it's brand new) instead of leaving it behind untracked.
                var hashCachePath = Path.Combine(_outputPath, _config.IncrementalSettings.CacheFileName);
                if (File.Exists(hashCachePath))
                {
                    await rollbackManager.TrackFileModificationAsync(hashCachePath);
                }
                else
                {
                    rollbackManager.TrackFileCreation(hashCachePath);
                }

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

            // Resume: skip designer files whose form was already successfully converted in a
            // previous interrupted run. --force wins over this filter (same precedence --force
            // already has over --incremental), but the checkpoint is still created/updated so
            // this run can itself be resumed if interrupted.
            CheckpointManager? checkpointManager = null;
            ConversionState? state = null;
            if (_resume)
            {
                checkpointManager = new CheckpointManager(_outputPath, _config.IncrementalSettings.CheckpointFileName);
                if (!_force)
                {
                    var priorState = await checkpointManager.LoadCheckpointAsync();
                    if (priorState != null)
                    {
                        state = priorState;
                        var beforeResumeFilter = designerFiles.Length;
                        designerFiles = designerFiles.Where(f => !state.CompletedFiles.Contains(f)).ToArray();
                        _logger?.LogInformation(
                            "Resuming: skipping {Count} form(s) already completed in a previous run",
                            beforeResumeFilter - designerFiles.Length);
                    }
                }
                state ??= new ConversionState { ProjectPath = _sourcePath, OutputPath = _outputPath };
            }

            // Resolved sibling-.resx entries per designer file, keyed by that file's path, so
            // ConvertFormAsync can later extract binary/image assets for "resource-binary"
            // properties (the parser resolves string values inline but can't write output
            // files itself - it has no knowledge of the output directory).
            var resxByFile = new Dictionary<string, IReadOnlyDictionary<string, ResxEntry>>();

            foreach (var file in designerFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    IReadOnlyDictionary<string, ResxEntry>? resources = null;
                    if (_config.ResourceConversion.Enabled)
                    {
                        var resxPath = SiblingFileResolver.ResolveResx(file);
                        if (resxPath != null)
                        {
                            resources = await ResxDocument.LoadAsync(resxPath);
                            resxByFile[file] = resources;
                        }
                    }

                    var result = await parser.ParseDesignerFileAsync(file, resources);
                    if (result.RootControl != null)
                    {
                        parseResults.Add(result);
                        statistics.TotalControls += CountControls(result.RootControl);

                        if (_config.EventHandlerMigration.Enabled)
                        {
                            var codeBehindPath = SiblingFileResolver.ResolveCodeBehind(file);
                            if (codeBehindPath != null)
                            {
                                var handlerNames = CollectHandlerMethodNames(result.RootControl);
                                if (handlerNames.Count > 0)
                                {
                                    result.EventHandlerBodies =
                                        await EventHandlerBodyParser.ExtractAsync(codeBehindPath, handlerNames);
                                }
                            }
                        }
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
            var assetsDir = Path.Combine(_outputPath, _config.ResourceConversion.AssetsDirectory);
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

            // --resume forces sequential processing: the parallel path only aggregates
            // outcomes after the entire batch completes, so there's no natural point to save
            // a per-form checkpoint without adding thread-safe incremental-state mutation on
            // top of the existing rollback lock - not worth it for a feature whose whole value
            // is precise, trustworthy resumability. Sequential's one-line per-form save is the
            // simpler, lower-risk choice; --resume is opt-in, so this only costs throughput on
            // runs that explicitly ask for resumability.
            if (_resume && _config.ParallelProcessing.Enabled)
            {
                _logger?.LogInformation(
                    "--resume forces sequential form processing to guarantee precise per-form checkpointing; parallel processing is skipped for this run.");
            }

            var useParallel = !_resume && _config.ParallelProcessing.Enabled && parseResults.Count > 1;

            var mappingContext = new MappingContext { ProjectPath = _sourcePath, OutputPath = _outputPath };

            var outcomes = useParallel
                ? await ConvertFormsInParallelAsync(
                    parseResults, layoutAnalyzer, axamlGenerator, vmGenerator, codeBehindGenerator, styleGenerator,
                    rollbackManager, viewsDir, viewModelsDir, assetsDir, namespaceName, viewModelSuffix, layoutContext,
                    resxByFile, mappingResolver, mappingContext, _config.ParallelProcessing.MaxDegreeOfParallelism, cancellationToken)
                : await ConvertFormsSequentiallyAsync(
                    parseResults, layoutAnalyzer, axamlGenerator, vmGenerator, codeBehindGenerator, styleGenerator,
                    rollbackManager, viewsDir, viewModelsDir, assetsDir, namespaceName, viewModelSuffix, layoutContext,
                    resxByFile, mappingResolver, mappingContext, progress, statistics, totalForms, totalFilesToGenerate,
                    cancellationToken, checkpointManager, state);

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
            checkpointManager?.ClearCheckpoint();
            await pluginLoader.UnloadAllPluginsAsync();

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

            // When resuming, keep whatever succeeded so far instead of wiping it - the
            // per-form checkpoint already saved inside ConvertFormsSequentiallyAsync is what
            // makes it resumable; deleting the files it points to would defeat the feature.
            // CommitTransaction only clears RollbackManager's own tracking/backups, it does
            // not touch the checkpoint file or any already-written output.
            if (_resume)
            {
                rollbackManager.CommitTransaction();
            }
            else
            {
                await rollbackManager.RollbackTransactionAsync();
            }

            // Report cancelled state
            progressState = new ConversionProgress
            {
                CurrentOperation = OperationType.Cancelled,
                ElapsedTime = _stopwatch.Elapsed
            };
            progress?.Report(progressState);

            await pluginLoader.UnloadAllPluginsAsync();

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

            // Same resume-gated behavior as cancellation above: keep partial output so a
            // subsequent --resume run can pick up where this one left off. Default
            // (non-resume) behavior - roll back everything this run wrote - is unchanged.
            if (_resume)
            {
                rollbackManager.CommitTransaction();
            }
            else
            {
                await rollbackManager.RollbackTransactionAsync();
            }

            await pluginLoader.UnloadAllPluginsAsync();

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
        string assetsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResxEntry>> resxByFile,
        MappingResolver mappingResolver,
        MappingContext mappingContext)
    {
        try
        {
            var rootControl = parseResult.RootControl!;
            var className = rootControl.Name;

            var resxManualSteps = new List<ManualStepInfo>();
            if (resxByFile.TryGetValue(parseResult.FilePath, out var formResources))
            {
                // Resolves "resource-binary" property markers into real Assets/... paths (or
                // removes them + records a manual step when extraction isn't possible) -
                // must run before generation, since AxamlGenerator reads control.Properties
                // directly.
                await ExtractResxAssetsAsync(
                    rootControl, className, assetsDir, formResources, rollbackManager, resxManualSteps);
            }

            // Single async pre-pass resolving plugin control/property/event overrides for
            // this form, before generation (which stays fully synchronous) starts.
            var overrides = await mappingResolver.ResolveForFormAsync(rootControl, mappingContext);

            var layoutResult = await layoutAnalyzer.AnalyzeAsync(rootControl, layoutContext);
            var layoutType = layoutResult.LayoutType;

            var axamlContent = axamlGenerator.Generate(rootControl, layoutResult, namespaceName, className, overrides);
            var vmContent = vmGenerator.GeneratePartialClass(
                rootControl, namespaceName, className, overrides, parseResult.EventHandlerBodies);
            var codeBehindContent = codeBehindGenerator.Generate(
                namespaceName, className, rootControl, parseResult.EventHandlerBodies, overrides);

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
                var stylesContent = styleGenerator.GenerateStyles(rootControl, _config.StyleExtraction.MinimumOccurrence, overrides);
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

            var manualSteps = CollectManualSteps(rootControl, parseResult.FilePath, overrides);
            manualSteps.AddRange(resxManualSteps);

            return new FormConversionOutcome(report, controlCount, parseResult.FilePath, null, manualSteps);
        }
        catch (Exception ex)
        {
            return new FormConversionOutcome(null, 0, parseResult.FilePath, ex, []);
        }
    }

    private static readonly (byte[] Magic, string Extension)[] ImageMagicBytes =
    [
        ([0x89, 0x50, 0x4E, 0x47], ".png"),
        ([0x42, 0x4D], ".bmp"),
        ([0x47, 0x49, 0x46, 0x38], ".gif"),
        ([0xFF, 0xD8], ".jpg")
    ];

    /// <summary>
    /// Resolves every "resource-binary" property marker left by WinFormsParser (properties
    /// backed by a binary/external-file resx entry, which the parser can't resolve itself
    /// since it doesn't know the output directory) into a real Assets/... relative path, by
    /// copying/extracting the underlying resource. Entries that can't be extracted (legacy
    /// BinaryFormatter payloads, unrecognized binary formats) are left unmapped - same as any
    /// other property this converter doesn't understand - with a ManualStepInfo explaining
    /// why, rather than fabricating a broken or empty asset file.
    /// </summary>
    private async Task ExtractResxAssetsAsync(
        ControlNode control, string className, string assetsDir,
        IReadOnlyDictionary<string, ResxEntry> resources, RollbackManager rollbackManager,
        List<ManualStepInfo> manualSteps)
    {
        foreach (var propName in control.Properties.Keys.ToList())
        {
            var prop = control.Properties[propName];
            if (prop.Type != "resource-binary" || prop.ResourceKey == null ||
                !resources.TryGetValue(prop.ResourceKey, out var entry))
            {
                continue;
            }

            var assetPath = await TryExtractAssetAsync(
                entry, className, control.Name, propName, assetsDir, rollbackManager);

            if (assetPath != null)
            {
                control.Properties[propName] = new PropertyValue
                {
                    Name = propName,
                    Value = assetPath,
                    Type = "resource-binary",
                    IsResource = true,
                    ResourceKey = prop.ResourceKey
                };
            }
            else
            {
                manualSteps.Add(new ManualStepInfo
                {
                    Category = "Unextractable Binary Resource",
                    Title = $"{control.Name}.{propName} resource \"{prop.ResourceKey}\" could not be extracted",
                    Location = control.SourceFile ?? className,
                    Description = entry.IsBinaryFormatterEnvelope
                        ? "This resource uses the legacy BinaryFormatter serialization format, which cannot be safely deserialized; the value was left unmapped and needs manual migration."
                        : "This resource's binary payload could not be recognized as a supported image format (PNG/BMP/GIF/JPEG); the value was left unmapped and needs manual migration."
                });
                control.Properties.Remove(propName);
            }
        }

        foreach (var child in control.Children)
        {
            await ExtractResxAssetsAsync(child, className, assetsDir, resources, rollbackManager, manualSteps);
        }
    }

    private async Task<string?> TryExtractAssetAsync(
        ResxEntry entry, string className, string controlName, string propertyName, string assetsDir,
        RollbackManager rollbackManager)
    {
        if (entry.IsBinaryFormatterEnvelope)
        {
            return null;
        }

        if (entry.ExternalFilePath != null && File.Exists(entry.ExternalFilePath))
        {
            Directory.CreateDirectory(assetsDir);
            var extension = Path.GetExtension(entry.ExternalFilePath);
            var destPath = Path.Combine(assetsDir, $"{className}_{controlName}_{propertyName}{extension}");
            File.Copy(entry.ExternalFilePath, destPath, overwrite: true);
            TrackFileCreationSafe(rollbackManager, destPath);
            return $"Assets/{Path.GetFileName(destPath)}";
        }

        if (entry.BinaryValue != null)
        {
            var match = ImageMagicBytes.FirstOrDefault(m =>
                entry.BinaryValue.Length >= m.Magic.Length &&
                entry.BinaryValue.AsSpan(0, m.Magic.Length).SequenceEqual(m.Magic));

            if (match.Extension != null)
            {
                Directory.CreateDirectory(assetsDir);
                var destPath = Path.Combine(assetsDir, $"{className}_{controlName}_{propertyName}{match.Extension}");
                await File.WriteAllBytesAsync(destPath, entry.BinaryValue);
                TrackFileCreationSafe(rollbackManager, destPath);
                return $"Assets/{Path.GetFileName(destPath)}";
            }
        }

        return null;
    }

    /// <summary>
    /// Walks the converted control tree to find everything the migration guide should flag
    /// as needing manual attention: controls with no Avalonia mapping (rendered as a TODO
    /// comment by AxamlGenerator), properties whose mapping is flagged RequiresCustomLogic
    /// (dropped or only partially converted), and events whose mapping is flagged
    /// PreserveEventHandler - CodeBehindGenerator emits a correctly-signed stub with the
    /// original body embedded as a reference comment for these, but a human still has to
    /// port the real logic into compiling code, so it's still flagged here. Without this,
    /// GenerateMigrationGuideAsync always reported "no manual steps required" even when these
    /// issues were present.
    /// </summary>
    private static List<ManualStepInfo> CollectManualSteps(ControlNode root, string sourceFile, PluginMappingOverrides overrides)
    {
        var steps = new List<ManualStepInfo>();
        CollectManualStepsRecursive(root, sourceFile, steps, overrides);
        return steps;
    }

    /// <summary>
    /// Collects the distinct handler method names (e.g. "button1_Click") referenced anywhere
    /// in a form's control tree, so EventHandlerBodyParser only has to look for methods that
    /// are actually relevant to this form instead of every method in the sibling code-behind
    /// file.
    /// </summary>
    private static HashSet<string> CollectHandlerMethodNames(ControlNode root)
    {
        var names = new HashSet<string>();
        CollectHandlerMethodNamesRecursive(root, names);
        return names;
    }

    private static void CollectHandlerMethodNamesRecursive(ControlNode control, HashSet<string> names)
    {
        foreach (var handlerName in control.EventHandlers.Values)
        {
            names.Add(handlerName);
        }

        foreach (var child in control.Children)
        {
            CollectHandlerMethodNamesRecursive(child, names);
        }
    }

    private static void CollectManualStepsRecursive(ControlNode control, string sourceFile, List<ManualStepInfo> steps, PluginMappingOverrides overrides)
    {
        // A plugin ControlMapper claiming this control means it isn't actually unmapped -
        // AxamlGenerator.WriteControl consults the same overrides and won't emit a TODO
        // placeholder for it.
        if (!overrides.ControlMappings.ContainsKey(control) && ControlMappingRegistry.GetMapping(control.ControlType) == null)
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
            if (overrides.PropertyTranslations.ContainsKey((control, propName)))
            {
                continue;
            }

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
            if (overrides.EventMappings.ContainsKey((control, eventName)))
            {
                continue;
            }

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
            CollectManualStepsRecursive(child, sourceFile, steps, overrides);
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
        string assetsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResxEntry>> resxByFile,
        MappingResolver mappingResolver,
        MappingContext mappingContext,
        IProgress<ConversionProgress>? progress,
        ConversionStatistics statistics,
        int totalForms,
        int totalFilesToGenerate,
        CancellationToken cancellationToken,
        CheckpointManager? checkpointManager = null,
        ConversionState? state = null)
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
                codeBehindGenerator, styleGenerator, rollbackManager, viewsDir, viewModelsDir, assetsDir,
                namespaceName, viewModelSuffix, layoutContext, resxByFile, mappingResolver, mappingContext);

            outcomes.Add(outcome);
            formsProcessed++;
            if (outcome.Report != null)
            {
                filesGenerated += 3;
            }

            // Incremental checkpoint save: this - not ExecuteAsync's catch-block handling -
            // is the actual resumability guarantee. A hard kill between forms still leaves an
            // accurate on-disk record of exactly which forms finished.
            if (checkpointManager != null && state != null)
            {
                if (outcome.Report != null)
                {
                    state.CompletedFiles.Add(outcome.SourceFile);
                }
                else
                {
                    state.FailedFiles[outcome.SourceFile] = outcome.Error?.Message ?? "Unknown error";
                }

                await checkpointManager.SaveCheckpointAsync(state);
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
        string assetsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResxEntry>> resxByFile,
        MappingResolver mappingResolver,
        MappingContext mappingContext,
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
                codeBehindGenerator, styleGenerator, rollbackManager, viewsDir, viewModelsDir, assetsDir,
                namespaceName, viewModelSuffix, layoutContext, resxByFile, mappingResolver, mappingContext);
        });

        return outcomes.Select(o => o!.Value).ToList();
    }

    private async Task GenerateProjectFilesAsync(RollbackManager rollbackManager)
    {
        var projectGenerator = new ProjectFileGenerator();
        var projectName = Path.GetFileName(_outputPath);

        var csprojContent = projectGenerator.GenerateAvaloniaProject(
            projectName,
            _config.ProjectGeneration.TargetFramework,
            _config.ProjectGeneration.AvaloniaVersion,
            _config.ProjectGeneration.CommunityToolkitMvvmVersion);
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
