using Converter.Core.Analysis;
using Converter.Core.Configuration;
using Converter.Core.Git;
using Converter.Core.Models;
using Converter.Core.Parsing;
using Converter.Core.Plugins;
using Converter.Core.Project;
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                .Where(f => !ExcludePatternMatcher.IsExcluded(f, _config.ExcludePatterns))
                .ToArray();
            _logger?.LogInformation("Found {Count} designer files", designerFiles.Length);

            // A composite custom control (its own InitializeComponent + child Controls.Add) is
            // commonly a single .cs file with no Foo.Designer.cs split at all (e.g. a
            // hand-written UserControl) - WinFormsParser.ParseDesignerFileAsync works on it
            // exactly the same way it works on a real Designer.cs (it only needs a class +
            // InitializeComponent method), so folding these into designerFiles here means every
            // downstream step (incremental hashing, resume, the parse loop, ViewModel/View
            // generation, "convertedCustomControlClassNames") treats it identically without any
            // further special-casing.
            var singleFileCustomControls = await SingleFileCustomControlDiscovery.DiscoverAsync(
                _sourcePath, new HashSet<string>(designerFiles), _config.ExcludePatterns);
            if (singleFileCustomControls.Count > 0)
            {
                _logger?.LogInformation("Found {Count} single-file custom control(s)", singleFileCustomControls.Count);
                designerFiles = designerFiles.Concat(singleFileCustomControls).ToArray();
            }
            var singleFileCustomControlPaths = new HashSet<string>(singleFileCustomControls);

            // Full set of files the Views/ViewModels pipeline already owns - every designer
            // file discovered this run (regardless of incremental/resume filtering below,
            // which only affects *this run's* reparse, not whether the file conceptually
            // belongs to a Form) plus each one's sibling code-behind, if any. SupportFileScanner
            // uses this to find the source project's *other* .cs files (a "Common"/"Controls"
            // folder of utility classes, typically) that nothing else in the pipeline ever
            // looks at.
            var handledFilePaths = new HashSet<string>(designerFiles);
            foreach (var file in designerFiles)
            {
                var siblingCodeBehind = SiblingFileResolver.ResolveCodeBehind(file);
                if (siblingCodeBehind != null)
                {
                    handledFilePaths.Add(siblingCodeBehind);
                }
            }

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

                    // Real Visual Studio designer output never redeclares the base type on the
                    // .Designer.cs partial itself - resolving it off the sibling .cs file is
                    // what lets a custom UserControl's own Designer.cs be recognized as such
                    // (rather than always defaulting to "Form"), unconditionally - this is
                    // structural correctness, not part of the event-handler-body-migration
                    // feature, so it isn't gated behind _config.EventHandlerMigration.Enabled.
                    var rootBaseTypeOverride = await SiblingFileResolver.ResolveRootBaseTypeAsync(file);
                    var result = await parser.ParseDesignerFileAsync(file, resources, rootBaseTypeOverride);
                    if (result.RootControl != null)
                    {
                        parseResults.Add(result);
                        statistics.TotalControls += CountControls(result.RootControl);

                        if (_config.EventHandlerMigration.Enabled)
                        {
                            // A single-file custom control (see SingleFileCustomControlDiscovery)
                            // has no separate sibling - InitializeComponent, event handlers, and
                            // helper methods all live in the same file that was just parsed, so
                            // that file is its own "code-behind" here.
                            var codeBehindPath = SiblingFileResolver.ResolveCodeBehind(file) ??
                                (singleFileCustomControlPaths.Contains(file) ? file : null);
                            if (codeBehindPath != null)
                            {
                                // A form's own lifecycle events (Load/FormClosing/Resize/...)
                                // are just as commonly wired manually in the regular
                                // constructor as via the Designer - invisible to WinFormsParser
                                // (Designer.cs-only), so without this the handler still gets
                                // migrated but is either dead code or a build error (e.g.
                                // FormClosingEventArgs). Merges into the same EventHandlers
                                // dict Designer.cs wiring populates, before the handler-name
                                // collection below, so every downstream step picks it up for
                                // free.
                                await CodeBehindEventSubscriptionDetector.MergeConstructorEventSubscriptionsAsync(
                                    result.RootControl, codeBehindPath);

                                var handlerNames = CollectHandlerMethodNames(result.RootControl);
                                if (handlerNames.Count > 0)
                                {
                                    result.EventHandlerBodies =
                                        await EventHandlerBodyParser.ExtractAsync(codeBehindPath, handlerNames);
                                }

                                // Only matters (excludes something) when codeBehindPath == file
                                // itself (the single-file case) - a control's own child-control
                                // fields (e.g. "_textBox") must not be re-migrated into the
                                // ViewModel as if they were business fields.
                                var controlFieldNames = CollectControlNames(result.RootControl);
                                result.CodeBehindMembers = await CodeBehindMemberExtractor.ExtractAsync(
                                    codeBehindPath, handlerNames, controlFieldNames);
                            }

                            // Inline lambda bodies live in the .Designer.cs file itself (no
                            // sibling code-behind needed) - merge them in under the same flag.
                            foreach (var (handlerName, body) in result.InlineLambdaBodies)
                            {
                                result.EventHandlerBodies[handlerName] = body;
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

            // Every form whose resolved root maps to Avalonia "UserControl" (see
            // SiblingFileResolver.ResolveRootBaseTypeAsync above) is a custom control this run
            // is independently converting into its own reusable View - deliberately excludes
            // "Window"-rooted forms, since a Window can't be embedded inline in AXAML the way a
            // UserControl can. AxamlGenerator/CollectManualSteps use this to reference such a
            // control correctly from wherever it's embedded, instead of the dead "Unmapped
            // control" TODO placeholder.
            var convertedCustomControlClassNames = parseResults
                .Where(r => r.RootControl != null &&
                    ControlMappingRegistry.GetMapping(r.RootControl.ControlType)?.AvaloniaType == "UserControl")
                .Select(r => r.RootControl!.Name)
                .ToHashSet();

            // For each of those, extract its own simple public auto-properties (e.g.
            // CustomerId) from its own sibling code-behind file - CodeBehindGenerator uses this
            // to re-expose them as real Avalonia bindable properties on the generated
            // UserControl, and embedding sites use it to know which of a custom control's
            // properties can be wired via PropertyTranslations below instead of silently
            // dropped.
            var customControlBindableProperties = new Dictionary<string, CustomControlPropertyExtractionResult>();
            foreach (var className in convertedCustomControlClassNames)
            {
                var designerFilePath = parseResults.First(r => r.RootControl!.Name == className).FilePath;
                // A composite custom control (see SingleFileCustomControlDiscovery above) has no
                // separate "Foo.Designer.cs"/"Foo.cs" split - ResolveCodeBehind's naming
                // convention never matches, so without this fallback its own properties are
                // silently never even attempted (empty, not "no properties found" - the file
                // just never gets read at all). The file parsed as its designer file already IS
                // its own code-behind in this case, same special-casing the earlier parse loop
                // already applies for event-handler-body extraction.
                var codeBehindPath = SiblingFileResolver.ResolveCodeBehind(designerFilePath) ??
                    (singleFileCustomControlPaths.Contains(designerFilePath) ? designerFilePath : null);
                customControlBindableProperties[className] = codeBehindPath != null
                    ? await CustomControlPropertyExtractor.ExtractAsync(codeBehindPath, className)
                    : CustomControlPropertyExtractionResult.Empty;
            }

            // Every class deriving from Control/UserControl with an OnPaint override and no
            // InitializeComponent (an owner-drawn control - no control tree to convert into
            // AXAML at all). Gives an embedded instance's "Unmapped Controls" manual step the
            // same specific message SupportFileScanner's file-level skip already has, instead of
            // a generic "has no Avalonia mapping".
            var ownerDrawnControlClassNames = await SingleFileCustomControlDiscovery
                .DiscoverOwnerDrawnControlClassNamesAsync(_sourcePath, _config.ExcludePatterns);

            // Every (FormName, DialogResultValue) pair where that form has a button whose
            // Designer-declared DialogResult property isn't None - WinForms' fully declarative
            // "OK/Cancel dialog" idiom, auto-closing the form with that result on click, no
            // code needed. Computed once, globally, across every parsed form (not just the
            // current one being generated) because ChildDialogTranspiler needs to know - from
            // a *different* form's calling code - whether the form it's constructing can
            // actually close with the result being compared against.
            var formsWithDialogResultButton = new HashSet<(string FormName, string DialogResultValue)>();
            foreach (var result in parseResults)
            {
                if (result.RootControl != null)
                {
                    CollectDialogResultButtons(result.RootControl, result.RootControl.Name, formsWithDialogResultButton);
                }
            }

            // Calculate total files to generate: 3 per form + 5 project files
            var totalForms = parseResults.Count;
            var totalFilesToGenerate = (totalForms * 3) + 5;
            ReportProgress(OperationType.Parsing, progress, statistics, totalForms, totalFilesToGenerate, 0, 0, force: true);

            // Step 3: Create output directory
            Directory.CreateDirectory(_outputPath);
            var viewsDir = Path.Combine(_outputPath, "Views");
            var viewModelsDir = Path.Combine(_outputPath, "ViewModels");
            // A converted custom control (a UserControl-rooted form) gets its own View/code-behind
            // here instead of Views/, so an embedding form's <controls:Foo/> reference lines up
            // with where the file actually lives - created lazily (see ConvertFormAsync), not
            // upfront, since most conversions have zero custom controls.
            var controlsDir = Path.Combine(_outputPath, "Controls");
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
            var avaloniaMajorVersion = EventSignatureRegistry.ParseMajorVersion(_config.ProjectGeneration.AvaloniaVersion);

            var outcomes = useParallel
                ? await ConvertFormsInParallelAsync(
                    parseResults, layoutAnalyzer, axamlGenerator, vmGenerator, codeBehindGenerator, styleGenerator,
                    rollbackManager, viewsDir, controlsDir, viewModelsDir, assetsDir, namespaceName, viewModelSuffix, layoutContext,
                    resxByFile, mappingResolver, mappingContext, avaloniaMajorVersion, convertedCustomControlClassNames,
                    customControlBindableProperties, ownerDrawnControlClassNames, formsWithDialogResultButton,
                    _config.ParallelProcessing.MaxDegreeOfParallelism, cancellationToken)
                : await ConvertFormsSequentiallyAsync(
                    parseResults, layoutAnalyzer, axamlGenerator, vmGenerator, codeBehindGenerator, styleGenerator,
                    rollbackManager, viewsDir, controlsDir, viewModelsDir, assetsDir, namespaceName, viewModelSuffix, layoutContext,
                    resxByFile, mappingResolver, mappingContext, avaloniaMajorVersion, convertedCustomControlClassNames,
                    customControlBindableProperties, ownerDrawnControlClassNames, formsWithDialogResultButton,
                    progress, statistics, totalForms,
                    totalFilesToGenerate, cancellationToken, checkpointManager, state);

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

            var usesMessageBoxDialogs = outcomes.Any(o => o.UsesMessageBoxDialogs);

            // Global (run-level, not per-form): only actually needed when at least one form
            // has a qualifying button *and* some form's migrated code has the ".ShowDialog("
            // shape at all - see the comment on formsWithDialogResultButton above for why the
            // set itself is computed globally.
            var usesDialogResultButtons = formsWithDialogResultButton.Count > 0;
            var usesChildDialogs = usesDialogResultButtons && outcomes.Any(o => o.UsesChildDialogPattern);

            ReportProgress(OperationType.ConvertingForm, progress, statistics, totalForms, totalFilesToGenerate,
                formsProcessed, filesGenerated, force: true);

            // Step 5: Generate project files
            cancellationToken.ThrowIfCancellationRequested();
            ReportProgress(OperationType.GeneratingProjectFiles, progress, statistics, totalForms, totalFilesToGenerate,
                formsProcessed, filesGenerated, force: true);

            _logger?.LogInformation("Generating project files...");
            await GenerateProjectFilesAsync(
                rollbackManager, formReports, manualSteps, convertedCustomControlClassNames, usesMessageBoxDialogs,
                usesDialogResultButtons, usesChildDialogs);
            filesGenerated += 5; // Project files generated

            _logger?.LogInformation("Copying non-Form support files...");
            await CopySupportFilesAsync(rollbackManager, handledFilePaths, manualSteps);

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
                Errors = errors,
                ManualSteps = manualSteps
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
        IReadOnlyList<ManualStepInfo> ManualSteps,
        bool UsesMessageBoxDialogs = false,
        bool UsesChildDialogPattern = false);

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
        string controlsDir,
        string viewModelsDir,
        string assetsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResxEntry>> resxByFile,
        MappingResolver mappingResolver,
        MappingContext mappingContext,
        int avaloniaMajorVersion,
        IReadOnlySet<string> convertedCustomControlClassNames,
        IReadOnlyDictionary<string, CustomControlPropertyExtractionResult> customControlBindableProperties,
        IReadOnlySet<string> ownerDrawnControlClassNames,
        IReadOnlySet<(string FormName, string DialogResultValue)> formsWithDialogResultButton)
    {
        try
        {
            var rootControl = parseResult.RootControl!;
            var className = rootControl.Name;

            // This form's own root is itself a converted custom control (see
            // convertedCustomControlClassNames above) - its View/code-behind/styles belong in
            // Controls/, not Views/, so an embedding form's <controls:Foo/> reference lines up
            // with where the file actually lives.
            var isCustomControl = convertedCustomControlClassNames.Contains(className);
            var formDir = isCustomControl ? controlsDir : viewsDir;
            if (isCustomControl)
            {
                Directory.CreateDirectory(controlsDir);
            }

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

            // A literal property assignment on an embedded custom-control instance (e.g.
            // "this.customerCard1.CustomerId = 5;") has no PropertyMappingRegistry entry (an
            // app-specific type isn't in the static registry) and would otherwise be silently
            // dropped by AxamlGenerator.WriteControlProperties. When that property is one of the
            // custom control's own simple auto-properties CustomControlPropertyExtractor found
            // (and CodeBehindGenerator re-exposed as a real Avalonia property on that control's
            // own generated code-behind), reuse the exact same PropertyTranslations mechanism a
            // plugin's IPropertyTranslator would populate, so it's written as a plain XAML
            // attribute instead.
            PopulateCustomControlPropertyTranslations(rootControl, overrides, customControlBindableProperties);

            var layoutResult = await layoutAnalyzer.AnalyzeAsync(rootControl, layoutContext);
            var layoutType = layoutResult.LayoutType;

            var codeBehindMembers = parseResult.CodeBehindMembers;

            // Only meaningful when this form's own className is itself a converted custom
            // control - re-exposes ITS OWN simple public properties as real Avalonia bindable
            // properties (see CodeBehindGenerator.Generate).
            var bindableProperties = customControlBindableProperties.TryGetValue(className, out var extraction)
                ? extraction.Bindable
                : null;
            var delegatingProperties = extraction?.Delegating;

            // Many real WinForms apps never call .DataBindings.Add(...) at all (confirmed
            // against WarehouseApp: zero hits across every form), so the DataBindings-only
            // binding machinery above has no reach there even though a control read in one
            // migrated method and written in another is behaviorally a bound property either
            // way. Infer those from usage instead - see UsageInferredBindingDetector for the
            // conservative (2+ distinct members) threshold - and merge them in everywhere a
            // DataBindings-derived binding would otherwise flow: the generated
            // [ObservableProperty] set, the AXAML {Binding} attributes, and the bound-control
            // rewrite inside migrated method bodies.
            var inferredBindings = UsageInferredBindingDetector.DetectInferredBindings(
                parseResult.EventHandlerBodies.Values.Concat(codeBehindMembers.HelperMethods.Values),
                CollectControlNames(rootControl),
                vmGenerator.BuildBoundControlPropertyLookup(rootControl));

            var axamlContent = axamlGenerator.Generate(
                rootControl, layoutResult, namespaceName, className, overrides, convertedCustomControlClassNames,
                inferredBindings);
            var codeBehindContent = codeBehindGenerator.Generate(
                namespaceName, className, rootControl, parseResult.EventHandlerBodies, overrides,
                avaloniaMajorVersion, codeBehindMembers, viewModelSuffix, bindableProperties, delegatingProperties);

            var axamlPath = Path.Combine(formDir, $"{className}.axaml");
            var codeBehindPath = Path.Combine(formDir, $"{className}.axaml.cs");

            await File.WriteAllTextAsync(axamlPath, axamlContent);
            TrackFileCreationSafe(rollbackManager, axamlPath);

            await File.WriteAllTextAsync(codeBehindPath, codeBehindContent);
            TrackFileCreationSafe(rollbackManager, codeBehindPath);

            var vmManualSteps = new List<ManualStepInfo>();

            // Auto-regenerated, ObservableProperty-only, optional file - only written when it
            // would have at least one property; a previously-written one whose bindings have
            // since been removed is deleted rather than left stale.
            var vmGenContent = vmGenerator.GeneratePartialClass(rootControl, namespaceName, className, inferredBindings);
            var vmGenPath = Path.Combine(viewModelsDir, $"{className}{viewModelSuffix}.g.cs");
            if (!string.IsNullOrEmpty(vmGenContent))
            {
                await File.WriteAllTextAsync(vmGenPath, vmGenContent);
                TrackFileCreationSafe(rollbackManager, vmGenPath);
            }
            else if (File.Exists(vmGenPath))
            {
                await rollbackManager.TrackFileModificationAsync(vmGenPath);
                File.Delete(vmGenPath);
            }

            // Hand-editable file: written only once. If it already exists, it is never
            // rewritten (user edits survive reconversion); any command/field/method this run
            // would have seeded but isn't already present is instead surfaced as a manual step.
            var editable = vmGenerator.BuildEditableClass(
                rootControl, namespaceName, className, overrides, parseResult.EventHandlerBodies, codeBehindMembers,
                inferredBindings, formsWithDialogResultButton);
            var vmUserPath = Path.Combine(viewModelsDir, $"{className}{viewModelSuffix}.cs");

            if (!File.Exists(vmUserPath))
            {
                await File.WriteAllTextAsync(vmUserPath, editable.Source);
                TrackFileCreationSafe(rollbackManager, vmUserPath);
            }
            else
            {
                var existingContent = await File.ReadAllTextAsync(vmUserPath);
                foreach (var name in editable.MemberNames.Distinct())
                {
                    if (!existingContent.Contains(name))
                    {
                        vmManualSteps.Add(new ManualStepInfo
                        {
                            Category = "ViewModel File Drift",
                            Title = $"{className}: \"{name}\" is not present in {className}{viewModelSuffix}.cs",
                            Location = parseResult.FilePath,
                            Description = "The hand-editable ViewModel file already exists and was not " +
                                "regenerated (edits are preserved across reconversion), but this run found a " +
                                "command handler, field, or helper method in the WinForms code-behind that " +
                                "isn't present in it yet. Add it manually - automatic merging into an " +
                                "existing hand-edited file is not attempted."
                        });
                    }
                }
                // vmUserPath is intentionally not written - nothing tracked/backed up for it,
                // since this run did not touch it.
            }

            foreach (var overrideName in codeBehindMembers.SkippedOverrideMethodNames)
            {
                vmManualSteps.Add(new ManualStepInfo
                {
                    Category = "Skipped Override Methods",
                    Title = $"{className}.{overrideName} was not migrated (Form-lifecycle override)",
                    Location = parseResult.FilePath,
                    Description = "This method overrides a base Form/Control member (e.g. OnClosing, " +
                        "OnLoad) and has no clean 1:1 ViewModel equivalent, so it was intentionally left " +
                        "out of the generated ViewModel. Port its logic manually into the Window's own " +
                        "lifecycle override or a suitable code-behind/ViewModel hook."
                });
            }

            // A migrated helper method/reclassified override (see CodeBehindMemberExtractor) is
            // real business logic and gets migrated as live code regardless of what it
            // references - correct, it shouldn't be silently dropped - but a body referencing a
            // WinForms type with no Avalonia equivalent (e.g. building TreeNode objects to
            // populate a TreeView) will not compile as-is, and previously gave zero signal that
            // this had happened. Flag it, mirroring "Preserved Event Handlers"'s tone.
            foreach (var (methodName, methodSource) in codeBehindMembers.HelperMethods)
            {
                var referencedTypes = WinFormsTypeUsageDetector.FindReferencedTypeNames(
                    CSharpSyntaxTree.ParseText(methodSource).GetRoot());

                // MessageBoxTranspiler (applied to this exact body by
                // ViewModelGenerator.BuildEditableClass) already rewrites "MessageBox.Show(...)"
                // and any standalone MessageBoxButtons/MessageBoxIcon/DialogResult reference into
                // a real, compiling call against the generated Dialogs helper - once that rewrite
                // actually applies, those names must not still be flagged as "no Avalonia
                // equivalent" (any other, genuinely-unhandled type stays flagged).
                if (MessageBoxTranspiler.TranspileMethod(methodSource, namespaceName).AddedAwait)
                {
                    referencedTypes = referencedTypes
                        .Where(t => t is not ("MessageBox" or "MessageBoxButtons" or "MessageBoxIcon" or "DialogResult"))
                        .ToList();
                }

                if (referencedTypes.Count == 0)
                {
                    continue;
                }

                vmManualSteps.Add(new ManualStepInfo
                {
                    Category = "Migrated Logic May Not Compile",
                    Title = $"{className}.{methodName} references WinForms type(s) with no Avalonia equivalent",
                    Location = parseResult.FilePath,
                    Description = $"This method was migrated as live code, but its body references " +
                        $"{string.Join(", ", referencedTypes)} - which have no Avalonia equivalent (a " +
                        "different UI/control model entirely). It will not compile as-is; review and " +
                        "redesign this logic manually."
                });
            }

            // Same check as above, but for a migrated *field*'s declared type (e.g. "private
            // Form? _popup;", "private readonly System.Windows.Forms.Timer _repeatTimer;") -
            // previously unscanned entirely, so a field like this compiled-errored with zero
            // manual-step warning (found via a real build against WarehouseApp).
            foreach (var field in codeBehindMembers.Fields)
            {
                var referencedTypes = WinFormsTypeUsageDetector.FindReferencedTypeNames(
                    CSharpSyntaxTree.ParseText(field.DeclarationText).GetRoot());
                if (referencedTypes.Count == 0)
                {
                    continue;
                }

                vmManualSteps.Add(new ManualStepInfo
                {
                    Category = "Migrated Logic May Not Compile",
                    Title = $"{className}.{string.Join("/", field.Names)} references WinForms type(s) with no Avalonia equivalent",
                    Location = parseResult.FilePath,
                    Description = $"This field was migrated as live code, but its declared type references " +
                        $"{string.Join(", ", referencedTypes)} - which have no Avalonia equivalent (a " +
                        "different UI/control model entirely). It will not compile as-is; review and " +
                        "redesign this field's type manually."
                });
            }

            // Mirrors the same check RelayCommand/property-changed-hook bodies already get
            // (AddViewOnlyControlReferenceStepIfAny): a helper method just as commonly
            // reads/writes another control directly (e.g. "skuTextBox.Text" in a LoadFromEntity/
            // SaveToEntity/ValidateInput-style method) - ViewModelGenerator.BuildEditableClass
            // now rewrites the bound-property case, but a reference to a still-unbound control
            // is left as-is and needs the same explicit flag instead of a silent compile failure.
            if (codeBehindMembers.HelperMethods.Count > 0)
            {
                var helperMethodControlNames = CollectControlNames(rootControl);
                var helperMethodBoundControlProperties = vmGenerator.BuildBoundControlPropertyLookup(rootControl, inferredBindings);
                foreach (var (methodName, methodSource) in codeBehindMembers.HelperMethods)
                {
                    AddHelperMethodViewOnlyControlReferenceStepIfAny(
                        className, methodName, methodSource, parseResult.FilePath,
                        helperMethodControlNames, helperMethodBoundControlProperties, vmManualSteps);
                }
            }

            if (_config.StyleExtraction.Enabled)
            {
                var stylesContent = styleGenerator.GenerateStyles(rootControl, _config.StyleExtraction.MinimumOccurrence, overrides);
                if (!string.IsNullOrWhiteSpace(stylesContent))
                {
                    var stylesPath = Path.Combine(formDir, $"{className}.Styles.axaml");
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

            var manualSteps = CollectManualSteps(
                rootControl, parseResult.FilePath, overrides, parseResult.EventHandlerBodies, vmGenerator,
                convertedCustomControlClassNames, customControlBindableProperties, ownerDrawnControlClassNames,
                inferredBindings);
            manualSteps.AddRange(resxManualSteps);
            manualSteps.AddRange(vmManualSteps);
            AddCoercionFallbackStepsIfAny(className, bindableProperties, parseResult.FilePath, manualSteps);

            // Cheap presence check (not tied to whether MessageBoxTranspiler's own rewrite
            // actually fires - e.g. an unsupported overload it leaves untouched still needs the
            // generated Dialogs infrastructure copy if literally any call anywhere gets
            // rewritten) - decides whether GenerateProjectFilesAsync needs to emit
            // Common/Dialogs.cs and friends for this run at all.
            var usesMessageBoxDialogs = UsesMessageBoxShow(parseResult.EventHandlerBodies.Values) ||
                UsesMessageBoxShow(codeBehindMembers.HelperMethods.Values);

            // Same cheap-presence-check spirit, for ChildDialogTranspiler's ".ShowDialog(...)"
            // shape - whether the transpile actually fires additionally depends on
            // formsWithDialogResultButton (checked by the transpiler itself), but that's a
            // global, run-level condition already folded into usesDialogResultButtons below.
            var usesChildDialogPattern = UsesShowDialog(parseResult.EventHandlerBodies.Values) ||
                UsesShowDialog(codeBehindMembers.HelperMethods.Values);

            return new FormConversionOutcome(
                report, controlCount, parseResult.FilePath, null, manualSteps, usesMessageBoxDialogs,
                usesChildDialogPattern);
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
    private static List<ManualStepInfo> CollectManualSteps(
        ControlNode root, string sourceFile, PluginMappingOverrides overrides,
        IReadOnlyDictionary<string, string> handlerBodies, ViewModelGenerator vmGenerator,
        IReadOnlySet<string> convertedCustomControlClassNames,
        IReadOnlyDictionary<string, CustomControlPropertyExtractionResult> customControlBindableProperties,
        IReadOnlySet<string> ownerDrawnControlClassNames,
        IReadOnlyDictionary<(string ControlName, string Property), string>? inferredBindings = null)
    {
        var steps = new List<ManualStepInfo>();
        var controlNames = CollectControlNames(root);
        var boundControlProperties = vmGenerator.BuildBoundControlPropertyLookup(root, inferredBindings);
        CollectManualStepsRecursive(
            root, sourceFile, steps, overrides, handlerBodies, controlNames, boundControlProperties,
            convertedCustomControlClassNames, customControlBindableProperties, ownerDrawnControlClassNames, isRoot: true);

        // This form's own className may itself be a converted custom control - flag any of its
        // own public auto-properties CustomControlPropertyExtractor found but couldn't safely
        // auto-wire (custom getter/setter logic, or an unsupported type), so that gap isn't
        // silent either.
        if (customControlBindableProperties.TryGetValue(root.Name, out var extraction))
        {
            foreach (var skipped in extraction.Skipped)
            {
                steps.Add(new ManualStepInfo
                {
                    Category = "Custom Control Property Not Auto-Bound",
                    Title = $"{root.Name}.{skipped.Name} was not auto-bound",
                    Location = sourceFile,
                    Description = $"This public property {skipped.Reason}, so it was left as a plain C# " +
                        "property instead of a bindable Avalonia StyledProperty - it can't be set from a " +
                        "parent's AXAML as an attribute. Wire it up manually if a consumer needs to."
                });
            }
        }

        return steps;
    }

    private static readonly Regex MessageBoxShowPattern = new(@"\bMessageBox\s*\.\s*Show\s*\(", RegexOptions.Compiled);

    private static bool UsesMessageBoxShow(IEnumerable<string> sources) =>
        sources.Any(source => MessageBoxShowPattern.IsMatch(source));

    // Cheap textual presence check mirroring UsesMessageBoxShow's own role - just decides
    // whether ChildDialogTranspiler's infrastructure (Common/Dialogs.cs's ShowChildAsync) is
    // worth generating for this form at all, not whether the transpile will actually fire
    // (that also needs a matching entry in formsWithDialogResultButton, checked by the
    // transpiler itself).
    private static readonly Regex ShowDialogPattern = new(@"\.\s*ShowDialog\s*\(", RegexOptions.Compiled);

    private static bool UsesShowDialog(IEnumerable<string> sources) =>
        sources.Any(source => ShowDialogPattern.IsMatch(source));

    /// <summary>
    /// Every (controlName, dialogResultValue) pair for a control with a Designer-declared
    /// DialogResult property that isn't None and no existing Click wiring (never claim a
    /// control whose Click handler already does something real) - see
    /// CodeBehindGenerator/AxamlGenerator's matching TryGetDialogResultValue-based wiring,
    /// which this must stay in lockstep with, and the module-level
    /// formsWithDialogResultButton pre-pass this feeds.
    /// </summary>
    private static void CollectDialogResultButtons(
        ControlNode control, string formName, HashSet<(string FormName, string DialogResultValue)> results)
    {
        if (!control.EventHandlers.ContainsKey("Click") &&
            DialogResultButtonHelper.TryGetDialogResultValue(control, out var value))
        {
            results.Add((formName, value));
        }

        foreach (var child in control.Children)
        {
            CollectDialogResultButtons(child, formName, results);
        }
    }

    private static HashSet<string> CollectControlNames(ControlNode root)
    {
        var names = new HashSet<string>();
        CollectControlNamesRecursive(root, names);
        return names;
    }

    private static void PopulateCustomControlPropertyTranslations(
        ControlNode control, PluginMappingOverrides overrides,
        IReadOnlyDictionary<string, CustomControlPropertyExtractionResult> customControlBindableProperties)
    {
        if (customControlBindableProperties.TryGetValue(control.ControlType, out var extraction))
        {
            // A Delegating property is a plain settable CLR property (see
            // CodeBehindGenerator) - an embedding site's literal XAML attribute works for it
            // the same way it does for a real StyledProperty.
            foreach (var propertyName in extraction.Bindable.Select(p => (p.Name, p.TypeName))
                .Concat(extraction.Delegating.Select(p => (p.Name, p.TypeName))))
            {
                if (control.Properties.TryGetValue(propertyName.Name, out var propertyValue))
                {
                    overrides.PropertyTranslations[(control, propertyName.Name)] = new PropertyTranslationResult
                    {
                        AvaloniaPropertyName = propertyName.Name,
                        Value = propertyValue.Value,
                        ValueType = propertyName.TypeName
                    };
                }
            }
        }

        foreach (var child in control.Children)
        {
            PopulateCustomControlPropertyTranslations(child, overrides, customControlBindableProperties);
        }
    }

    private static void CollectControlNamesRecursive(ControlNode control, HashSet<string> names)
    {
        names.Add(control.Name);
        foreach (var child in control.Children)
        {
            CollectControlNamesRecursive(child, names);
        }
    }

    /// <summary>
    /// Re-parses a migrated body (already run through EventHandlerBodyParser.ExtractBodyText)
    /// and finds every "controlName.Property" member access whose controlName matches another
    /// control in the same form's tree but has no [ObservableProperty] binding
    /// (ViewModelGenerator.RewriteBoundControlReferences already rewrote the ones that do) - a
    /// reference the ViewModel cannot resolve without reaching into the View. Best-effort,
    /// mirroring EventHandlerBodyParser's own tolerance for unparseable text.
    /// </summary>
    private static List<(string ControlName, string Property)> FindUnresolvedControlReferences(
        string body, IReadOnlySet<string> controlNames,
        IReadOnlyDictionary<(string ControlName, string ControlProperty), string> boundControlProperties)
    {
        var results = new List<(string, string)>();
        try
        {
            var wrapper = $"class __Wrapper {{ void __M() {body} }}";
            var root = CSharpSyntaxTree.ParseText(wrapper).GetRoot();

            foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (memberAccess.Expression is not IdentifierNameSyntax identifier)
                {
                    continue;
                }

                var controlName = identifier.Identifier.Text;
                if (!controlNames.Contains(controlName))
                {
                    continue;
                }

                var property = memberAccess.Name.Identifier.Text;
                if (boundControlProperties.ContainsKey((controlName, property)))
                {
                    continue;
                }

                results.Add((controlName, property));
            }
        }
        catch
        {
            // Best-effort: an unparseable body simply yields no findings, not a hard failure.
        }

        return results.Distinct().ToList();
    }

    /// <summary>
    /// Adds a "Command Logic References View-Only Control" manual step when a migrated,
    /// live-code event-handler body (a ConvertToCommand [RelayCommand], or an autowired
    /// TextChanged/ValueChanged/CheckedChanged property-changed hook) references another
    /// control's property that has no DataBindings-backed [ObservableProperty] to rewrite it
    /// into - ViewModelGenerator emits the reference as-is in that case (RewriteBoundControlReferences
    /// only fixes bound references), which will not compile since the ViewModel cannot reach the
    /// View. Without this, that failure was silent - the only prior signal was a build error with
    /// no indication of why.
    /// </summary>
    private static void AddViewOnlyControlReferenceStepIfAny(
        ControlNode control, string eventName, string handlerName, string sourceFile,
        IReadOnlyDictionary<string, string> handlerBodies, IReadOnlySet<string> controlNames,
        IReadOnlyDictionary<(string ControlName, string ControlProperty), string> boundControlProperties,
        List<ManualStepInfo> steps)
    {
        if (!handlerBodies.TryGetValue(handlerName, out var originalSource))
        {
            return;
        }

        var body = EventHandlerBodyParser.ExtractBodyText(originalSource);
        var unresolved = FindUnresolvedControlReferences(body, controlNames, boundControlProperties);
        if (unresolved.Count == 0)
        {
            return;
        }

        steps.Add(new ManualStepInfo
        {
            Category = "Command Logic References View-Only Control",
            Title = $"{control.Name}.{eventName} handler \"{handlerName}\" references " +
                string.Join(", ", unresolved.Select(r => $"{r.ControlName}.{r.Property}")),
            Location = sourceFile,
            Description = "This handler was migrated as live code into the ViewModel, but it reads/writes " +
                "another control's property directly, and that property has no DataBindings.Add(...) entry " +
                "to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot " +
                "reference the View). Either add a DataBindings.Add(...) for it in the source WinForms " +
                "designer so it is auto-bound on the next conversion, or wire this up manually (e.g. an " +
                "AXAML-side binding)."
        });
    }

    /// <summary>
    /// One note per CoercionFallback property this form's own class declares (only meaningful
    /// when this form's own className is itself a converted custom control - see
    /// CustomControlPropertyExtractor.CustomControlPropertyKind.CoercionFallback) - the
    /// property's original setter logic *looked* like a self-contained validation setter but
    /// wasn't in the safe, mechanically-translatable expression subset, so it was still
    /// converted (a plain, unvalidated StyledProperty, not silently dropped), but its original
    /// logic needs manual porting.
    /// </summary>
    private static void AddCoercionFallbackStepsIfAny(
        string className, IReadOnlyList<CustomControlProperty>? bindableProperties, string sourceFile,
        List<ManualStepInfo> steps)
    {
        if (bindableProperties == null)
        {
            return;
        }

        foreach (var property in bindableProperties)
        {
            if (property.Kind != CustomControlPropertyKind.CoercionFallback)
            {
                continue;
            }

            steps.Add(new ManualStepInfo
            {
                Category = "Custom Control Property",
                Title = $"{className}.{property.Name} was auto-bound without its original validation logic",
                Location = sourceFile,
                Description = $"{className}.{property.Name}'s original setter logic could not be safely " +
                    "auto-translated into an Avalonia property-coercion callback, so it was converted as a " +
                    "plain bindable property with no validation. Port the original setter logic manually " +
                    $"(e.g. into a coerce callback on {property.Name}Property, or an OnPropertyChanged hook)."
            });
        }
    }

    /// <summary>
    /// Same check as AddViewOnlyControlReferenceStepIfAny, for a migrated helper method
    /// (CodeBehindMemberExtractor.HelperMethods) instead of an event-handler body - the source
    /// is already the full method text (not looked up by handler name), so it's re-extracted
    /// down to just the body the same way EventHandlerBodyParser.ExtractBodyText does for the
    /// event-handler case, before scanning for unresolved control references.
    /// </summary>
    private static void AddHelperMethodViewOnlyControlReferenceStepIfAny(
        string className, string methodName, string methodSource, string sourceFile,
        IReadOnlySet<string> controlNames,
        IReadOnlyDictionary<(string ControlName, string ControlProperty), string> boundControlProperties,
        List<ManualStepInfo> steps)
    {
        var body = EventHandlerBodyParser.ExtractBodyText(methodSource);
        var unresolved = FindUnresolvedControlReferences(body, controlNames, boundControlProperties);
        if (unresolved.Count == 0)
        {
            return;
        }

        steps.Add(new ManualStepInfo
        {
            Category = "Command Logic References View-Only Control",
            Title = $"{className}.{methodName} references " +
                string.Join(", ", unresolved.Select(r => $"{r.ControlName}.{r.Property}")),
            Location = sourceFile,
            Description = "This helper method was migrated as live code into the ViewModel, but it " +
                "reads/writes another control's property directly, and that property has no " +
                "DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile " +
                "as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it " +
                "in the source WinForms designer so it is auto-bound on the next conversion, or wire this " +
                "up manually."
        });
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

    private static void CollectManualStepsRecursive(
        ControlNode control, string sourceFile, List<ManualStepInfo> steps, PluginMappingOverrides overrides,
        IReadOnlyDictionary<string, string> handlerBodies, IReadOnlySet<string> controlNames,
        IReadOnlyDictionary<(string ControlName, string ControlProperty), string> boundControlProperties,
        IReadOnlySet<string> convertedCustomControlClassNames,
        IReadOnlyDictionary<string, CustomControlPropertyExtractionResult> customControlBindableProperties,
        IReadOnlySet<string> ownerDrawnControlClassNames,
        bool isRoot = false)
    {
        // A plugin ControlMapper claiming this control means it isn't actually unmapped -
        // AxamlGenerator.WriteControl consults the same overrides and won't emit a TODO
        // placeholder for it. The root control is also exempt: it isn't an embedded instance at
        // all, and AxamlGenerator/CodeBehindGenerator already fall back to "Window" for a root
        // whose ControlType isn't in ControlMappingRegistry (e.g. a project-local generic base
        // like "DetailFormBase<SalesOrder>") - flagging it here would be a false positive on
        // already-correct output.
        if (!isRoot && !overrides.ControlMappings.ContainsKey(control) && ControlMappingRegistry.GetMapping(control.ControlType) == null)
        {
            if (convertedCustomControlClassNames.Contains(control.ControlType))
            {
                // This run also independently converted control.ControlType's own Designer.cs -
                // AxamlGenerator.WriteControl already references it correctly (<controls:.../>)
                // instead of a TODO placeholder, so this isn't "Unmapped Controls". Still worth
                // a lighter-weight note: only properties CustomControlPropertyExtractor could
                // classify as bindable or delegating (see AllSettableNames) get auto-bound via
                // PopulateCustomControlPropertyTranslations - anything else this instance's
                // Designer.cs set on it that isn't in that list is silently dropped, same as any
                // other unmapped property.
                var extraction = customControlBindableProperties.GetValueOrDefault(control.ControlType);
                var boundNames = extraction?.AllSettableNames().ToHashSet() ?? [];
                var droppedProperties = control.Properties.Keys.Where(p => !boundNames.Contains(p)).ToList();

                steps.Add(new ManualStepInfo
                {
                    Category = "Custom Control Instance",
                    Title = $"{control.ControlType} \"{control.Name}\" was converted separately",
                    Location = sourceFile,
                    Description = $"Controls/{control.ControlType}.axaml and its ViewModel were generated from this " +
                        $"control's own Designer.cs. " + (droppedProperties.Count > 0
                            ? $"These properties set on this instance were not simple public auto-properties on " +
                              $"{control.ControlType} (or not found at all) and were not carried over: " +
                              $"{string.Join(", ", droppedProperties)}. Wire them up manually if needed."
                            : "All properties set on this instance were auto-bound.")
                });
            }
            else
            {
                // Mirrors SupportFileScanner's own owner-drawn detection for the exact same
                // control's file-level skip, so an embedded instance gets the same specific,
                // accurate message instead of the generic "has no Avalonia mapping" (which reads
                // as if this were merely an unregistered-but-portable control type).
                var isOwnerDrawn = ownerDrawnControlClassNames.Contains(control.ControlType);
                steps.Add(new ManualStepInfo
                {
                    Category = "Unmapped Controls",
                    Title = $"{control.ControlType} \"{control.Name}\" has no Avalonia mapping",
                    Location = sourceFile,
                    Description = isOwnerDrawn
                        ? $"Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no " +
                          "InitializeComponent/child controls) - there is no control tree to convert into AXAML. " +
                          "Needs a hand-written Avalonia control with its own render logic (e.g. a Control " +
                          "subclass overriding Render(DrawingContext))."
                        : "This control type has no built-in WinForms-to-Avalonia mapping; it was emitted " +
                          "as a TODO placeholder in the AXAML and needs a manual replacement."
                });
            }
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
                // Mirrors AxamlGenerator.WriteControlProperties's own check - PropertyValueConverter
                // already successfully converts several RequiresCustomLogic mappings (Font, Location,
                // Size, Dock, Padding/Margin, FormBorderStyle, WindowState, ...); flagging every
                // RequiresCustomLogic occurrence regardless was a false positive for all of those.
                var rawValue = control.Properties[propName].Value?.ToString();
                var converted = !string.IsNullOrEmpty(rawValue)
                    ? PropertyValueConverter.Convert(propMapping, rawValue)
                    : null;

                if (converted == null)
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
        }

        foreach (var eventName in control.EventHandlers.Keys)
        {
            if (overrides.EventMappings.ContainsKey((control, eventName)))
            {
                continue;
            }

            if (control.EventHandlers[eventName] == WinFormsParser.InlineLambdaHandlerMarker)
            {
                steps.Add(new ManualStepInfo
                {
                    Category = "Inline Lambda Event Handlers",
                    Title = $"{control.Name}.{eventName} is subscribed with an inline lambda",
                    Location = sourceFile,
                    Description = "The original WinForms code subscribes this event with an inline lambda " +
                        "(no stable method name to extract), so it was skipped entirely instead of emitting " +
                        "broken generated code. Wire up the equivalent Avalonia event/command manually."
                });
                continue;
            }

            var eventMapping = EventMappingRegistry.GetMapping(eventName);
            if (eventMapping?.PreserveEventHandler == true)
            {
                var handlerName = control.EventHandlers[eventName];
                var bodyEmbedded = handlerBodies.ContainsKey(handlerName);
                steps.Add(new ManualStepInfo
                {
                    Category = "Preserved Event Handlers",
                    Title = $"{control.Name}.{eventName} handler \"{handlerName}\" needs manual review",
                    Location = sourceFile,
                    Description = eventMapping.Notes ?? (bodyEmbedded
                        ? $"Maps to Avalonia's '{eventMapping.AvaloniaEvent}' event. The original handler body " +
                          "was embedded as live code, with best-effort identifier rewriting for any " +
                          "fields/methods that moved to the ViewModel - verify this compiles (the original " +
                          "code may call WinForms-only APIs that don't exist in Avalonia) and double-check " +
                          "the rewritten identifiers before shipping."
                        : $"Maps to Avalonia's '{eventMapping.AvaloniaEvent}' event, but no original handler " +
                          "body was found in the sibling code-behind file; a TODO stub was generated and " +
                          "must be ported manually.")
                });
            }
            else if (eventMapping?.RequiresCustomLogic == true)
            {
                // TextChanged/ValueChanged/CheckedChanged are automated as a live
                // CommunityToolkit property-changed hook when the same control has a matching
                // DataBindings entry (ViewModelGenerator.ExtractPropertyChangedHooks) - only
                // flag this when that didn't happen. Events with no such automation path at all
                // (e.g. Paint - a fundamentally different rendering model) always land here.
                var handlerName = control.EventHandlers[eventName];
                var autoWired = EventMappingRegistry.FindBoundPropertyName(control, eventName) != null &&
                    handlerBodies.ContainsKey(handlerName);

                if (!autoWired)
                {
                    steps.Add(new ManualStepInfo
                    {
                        Category = "Custom Event Logic",
                        Title = $"{control.Name}.{eventName} handler \"{handlerName}\" requires custom conversion logic",
                        Location = sourceFile,
                        Description = eventMapping.Notes ??
                            $"Maps toward Avalonia's '{eventMapping.AvaloniaEvent}' but the automatic converter " +
                            "could not fully translate this event; review and port the original handler manually."
                    });
                }
                else
                {
                    AddViewOnlyControlReferenceStepIfAny(
                        control, eventName, handlerName, sourceFile, handlerBodies, controlNames,
                        boundControlProperties, steps);
                }
            }
            else if (eventMapping?.ConvertToCommand == true)
            {
                // Mirrors ViewModelGenerator.ExtractCommands' bucket (Click, DoubleClick,
                // SelectedIndexChanged, CellClick, NodeClick, ...) - a found body was already
                // spliced into the generated [RelayCommand] as live code with bound-control
                // references rewritten (ViewModelGenerator.RewriteBoundControlReferences); flag
                // whatever's left over instead of leaving a silent compile failure.
                AddViewOnlyControlReferenceStepIfAny(
                    control, eventName, control.EventHandlers[eventName], sourceFile, handlerBodies,
                    controlNames, boundControlProperties, steps);
            }
        }

        foreach (var child in control.Children)
        {
            CollectManualStepsRecursive(
                child, sourceFile, steps, overrides, handlerBodies, controlNames, boundControlProperties,
                convertedCustomControlClassNames, customControlBindableProperties, ownerDrawnControlClassNames);
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
        string controlsDir,
        string viewModelsDir,
        string assetsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResxEntry>> resxByFile,
        MappingResolver mappingResolver,
        MappingContext mappingContext,
        int avaloniaMajorVersion,
        IReadOnlySet<string> convertedCustomControlClassNames,
        IReadOnlyDictionary<string, CustomControlPropertyExtractionResult> customControlBindableProperties,
        IReadOnlySet<string> ownerDrawnControlClassNames,
        IReadOnlySet<(string FormName, string DialogResultValue)> formsWithDialogResultButton,
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
                codeBehindGenerator, styleGenerator, rollbackManager, viewsDir, controlsDir, viewModelsDir, assetsDir,
                namespaceName, viewModelSuffix, layoutContext, resxByFile, mappingResolver, mappingContext,
                avaloniaMajorVersion, convertedCustomControlClassNames, customControlBindableProperties,
                ownerDrawnControlClassNames, formsWithDialogResultButton);

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
        string controlsDir,
        string viewModelsDir,
        string assetsDir,
        string namespaceName,
        string viewModelSuffix,
        LayoutAnalysisContext layoutContext,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResxEntry>> resxByFile,
        MappingResolver mappingResolver,
        MappingContext mappingContext,
        int avaloniaMajorVersion,
        IReadOnlySet<string> convertedCustomControlClassNames,
        IReadOnlyDictionary<string, CustomControlPropertyExtractionResult> customControlBindableProperties,
        IReadOnlySet<string> ownerDrawnControlClassNames,
        IReadOnlySet<(string FormName, string DialogResultValue)> formsWithDialogResultButton,
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
                codeBehindGenerator, styleGenerator, rollbackManager, viewsDir, controlsDir, viewModelsDir, assetsDir,
                namespaceName, viewModelSuffix, layoutContext, resxByFile, mappingResolver, mappingContext,
                avaloniaMajorVersion, convertedCustomControlClassNames, customControlBindableProperties,
                ownerDrawnControlClassNames, formsWithDialogResultButton);
        });

        return outcomes.Select(o => o!.Value).ToList();
    }

    /// <summary>
    /// Picks which converted form App.axaml.cs should open as the startup window. Prefers the
    /// original WinForms entry point (EntryPointResolver's best-effort `Application.Run(new X())`
    /// scan) when that form was actually converted; otherwise falls back to the first converted
    /// form, so App.axaml.cs always references a type that actually exists - a hardcoded
    /// "MainWindow" almost never matches a real WinForms class name and previously made every
    /// conversion's generated project fail to compile.
    /// </summary>
    private string ResolveMainWindowName(
        List<FormReportInfo> formReports, IReadOnlySet<string> convertedCustomControlClassNames)
    {
        if (formReports.Count == 0)
        {
            return "MainWindow";
        }

        var startupForm = EntryPointResolver.FindStartupFormName(_sourcePath);
        if (startupForm != null && formReports.Any(f => f.Name == startupForm))
        {
            return startupForm;
        }

        // Fallback when the real startup form couldn't be determined: prefer a Window-rooted
        // form over a UserControl-rooted custom control - desktop.MainWindow requires an actual
        // Window, and a custom control (now correctly convertible, see
        // SiblingFileResolver.ResolveRootBaseTypeAsync) could otherwise end up first in
        // formReports.
        return formReports.FirstOrDefault(f => !convertedCustomControlClassNames.Contains(f.Name))?.Name
            ?? formReports[0].Name;
    }

    private async Task GenerateProjectFilesAsync(
        RollbackManager rollbackManager, List<FormReportInfo> formReports, List<ManualStepInfo> manualSteps,
        IReadOnlySet<string> convertedCustomControlClassNames, bool usesMessageBoxDialogs,
        bool usesDialogResultButtons, bool usesChildDialogs)
    {
        var projectGenerator = new ProjectFileGenerator();
        var projectName = Path.GetFileName(_outputPath);

        var projectReferences = ProjectReferenceResolver.Resolve(_sourcePath);
        var relativeReferencePaths = projectReferences.Referenceable
            .Select(r => Path.GetRelativePath(_outputPath, r.AbsolutePath))
            .ToList();
        foreach (var skippedName in projectReferences.SkippedWinFormsProjectNames)
        {
            manualSteps.Add(new ManualStepInfo
            {
                Category = "Sibling WinForms Projects",
                Title = $"\"{skippedName}\" was not automatically referenced",
                Location = _sourcePath,
                Description = "This project is referenced by the source WinForms project but appears to be " +
                    "a WinForms project itself (UseWindowsForms=true) - it needs to be converted separately " +
                    "before the generated Avalonia project can reference it."
            });
        }

        var csprojContent = projectGenerator.GenerateAvaloniaProject(
            projectName,
            _config.ProjectGeneration.TargetFramework,
            _config.ProjectGeneration.AvaloniaVersion,
            _config.ProjectGeneration.CommunityToolkitMvvmVersion,
            relativeReferencePaths);
        var appAxamlContent = projectGenerator.GenerateAppAxaml(projectName);
        var appCodeBehindContent = projectGenerator.GenerateAppCodeBehind(
            projectName, ResolveMainWindowName(formReports, convertedCustomControlClassNames));
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

        // MessageBoxTranspiler needs the enum types + Dialogs.ShowAsync + MessageBoxWindow;
        // a Designer-declared DialogResult-close button (no MessageBox involved at all) still
        // needs the enum types for its Close(...) stub; ChildDialogTranspiler's rewrite needs
        // the enum types + Dialogs.ShowChildAsync but never MessageBoxWindow (that's
        // MessageBox-popup-specific UI, unrelated to showing an arbitrary child form). Kept as
        // three independent flags so a project that only uses one of these idioms doesn't get
        // the other's unused infrastructure as clutter.
        var needsDialogResultTypes = usesMessageBoxDialogs || usesDialogResultButtons;
        var needsDialogsHelper = usesMessageBoxDialogs || usesChildDialogs;

        if (needsDialogResultTypes || needsDialogsHelper)
        {
            var commonDir = Path.Combine(_outputPath, "Common");
            Directory.CreateDirectory(commonDir);

            if (needsDialogResultTypes)
            {
                var messageBoxTypesPath = Path.Combine(commonDir, "MessageBoxTypes.cs");
                await File.WriteAllTextAsync(messageBoxTypesPath, projectGenerator.GenerateMessageBoxTypes(projectName));
                rollbackManager.TrackFileCreation(messageBoxTypesPath);
            }

            if (needsDialogsHelper)
            {
                var dialogsPath = Path.Combine(commonDir, "Dialogs.cs");
                await File.WriteAllTextAsync(dialogsPath, projectGenerator.GenerateDialogsHelper(projectName));
                rollbackManager.TrackFileCreation(dialogsPath);
            }
        }

        if (usesMessageBoxDialogs)
        {
            var viewsDir = Path.Combine(_outputPath, "Views");
            Directory.CreateDirectory(viewsDir);

            var messageBoxWindowAxamlPath = Path.Combine(viewsDir, "MessageBoxWindow.axaml");
            await File.WriteAllTextAsync(messageBoxWindowAxamlPath, projectGenerator.GenerateMessageBoxWindowAxaml(projectName));
            rollbackManager.TrackFileCreation(messageBoxWindowAxamlPath);

            var messageBoxWindowCodeBehindPath = Path.Combine(viewsDir, "MessageBoxWindow.axaml.cs");
            await File.WriteAllTextAsync(messageBoxWindowCodeBehindPath, projectGenerator.GenerateMessageBoxWindowCodeBehind(projectName));
            rollbackManager.TrackFileCreation(messageBoxWindowCodeBehindPath);
        }
    }

    /// <summary>
    /// Copies the source project's own non-Form .cs files (a "Common"/"Controls" folder of
    /// utility classes and custom controls, typically - see SupportFileScanner) into the
    /// generated project, preserving their relative path and original namespace verbatim so
    /// any "using" migrated from a form's code-behind (via CodeBehindMemberExtractor) still
    /// resolves. Files SupportFileScanner determined derive from a WinForms UI base type
    /// (Form/Control/UserControl/...) are left alone and surfaced as a manual step instead -
    /// copying them as-is would just fail to compile (no System.Windows.Forms in the generated
    /// project), and porting a custom control to Avalonia's rendering model isn't a file copy.
    /// </summary>
    private async Task CopySupportFilesAsync(
        RollbackManager rollbackManager, IReadOnlySet<string> handledFilePaths, List<ManualStepInfo> manualSteps)
    {
        var scanResult = await SupportFileScanner.ScanAsync(_sourcePath, handledFilePaths, _config.ExcludePatterns);

        foreach (var file in scanResult.CopyableFiles)
        {
            var destinationPath = Path.Combine(_outputPath, file.RelativePath);
            var destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            if (file.TransformedContent != null)
            {
                await File.WriteAllTextAsync(destinationPath, file.TransformedContent);
            }
            else
            {
                File.Copy(file.AbsolutePath, destinationPath, overwrite: true);
            }
            rollbackManager.TrackFileCreation(destinationPath);
        }

        foreach (var skipped in scanResult.SkippedFiles)
        {
            manualSteps.Add(new ManualStepInfo
            {
                Category = "Unconverted Support Files",
                Title = $"\"{skipped.RelativePath}\" was not copied",
                Location = Path.Combine(_sourcePath, skipped.RelativePath),
                Description = skipped.Reason
            });
        }
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
