# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 10 CLI tool that converts WinForms projects (`.Designer.cs` + code-behind) into Avalonia 11.x projects with MVVM architecture (CommunityToolkit.Mvvm). It parses WinForms designer code with Roslyn, infers a layout (Grid/StackPanel/DockPanel/Canvas), and generates AXAML views, partial ViewModels, code-behind, and a runnable Avalonia project — plus a migration guide and a conversion report.

Core correctness (event handler and data binding extraction, so generated ViewModels/AXAML actually contain real `[ObservableProperty]`/`[RelayCommand]`/property bindings instead of empty shells), CLI/config wiring, and the previously-dead subsystems (rollback tracking, incremental hashing, style extraction) have all been fixed — see `Converter.Tests` for coverage. The plugin loader / `init-plugin` / `list-plugins` CLI commands are still stubs ("not yet implemented") — the plugin abstraction layer has no consumption hooks in the mapping registries yet. `.resx` parsing/generation and WinForms event-handler *body* migration (as opposed to the event-to-command mapping, which does work) are also not implemented.

## Repository Layout

The actual solution lives one level down from the repo root:

```
winforms-to-avalonia-converter/src/Converter.sln
```

All `dotnet` commands below assume you `cd winforms-to-avalonia-converter/src` first.

## Common Commands

```bash
cd winforms-to-avalonia-converter/src

# restore + build the whole solution
dotnet restore
dotnet build

# run the full test suite
dotnet test

# run a single test / filter by name
dotnet test Converter.Tests/Converter.Tests.csproj --filter "FullyQualifiedName~WinFormsParserTests"

# run the CLI directly from source
cd Converter.Cli
dotnet run -- --help
dotnet run -- convert -i /path/to/WinFormsApp -o /path/to/AvaloniaApp --layout smart
dotnet run -- init-config -o .converterconfig
```

If `dotnet build`/`dotnet test` fails with "Fatal error. Internal CLR error." (a stale MSBuild/Roslyn compiler server), run `dotnet build-server shutdown` and retry.

`Converter.Tests` is an xUnit project covering the parser, generators, mapping registries, layout analyzer, and the orchestrator end-to-end (via temp-directory integration tests that run real conversions). Test fixtures live in `Converter.Tests/Fixtures/*.Designer.cs.txt` (copied to the test output directory, loaded via `TestSupport.FixturePath`) — write realistic, fully-qualified WinForms designer syntax (`new System.Drawing.Point(...)`, not bare `new Point(...)`) since several parsing/analysis code paths only match the fully-qualified form real Visual Studio designer output uses.

## Architecture

The solution is split into layered projects, referenced roughly in this order (later projects depend on earlier ones):

```
Converter.Plugin.Abstractions  → plugin contracts + shared model types (ControlNode, PropertyValue, DataBinding)
Converter.Core                 → parsing, layout analysis, config, git, checkpoint/rollback/hash services
Converter.Mappings             → built-in control/property/event mapping registries (BuiltIn/); references Converter.Core
Converter.Generator            → AXAML / ViewModel / code-behind / project-file / style generators
Converter.Reporting            → HTML/JSON/Markdown/CSV report builder
Converter.Documentation        → migration guide generator
Converter.Cli                  → System.CommandLine entry point, Spectre.Console UI, orchestrator
Converter.Tests                → xUnit tests; references Core/Plugin.Abstractions/Mappings/Generator/Cli
```

Note the dependency direction: `Converter.Mappings` references `Converter.Core` (not the other way around) — if you need a Mappings type from Core (e.g. widening a parser heuristic against `ControlMappingRegistry`), adding `Converter.Core` → `Converter.Mappings` would create a cycle; duplicate the small piece of data instead (see `WinFormsParser.KnownControlTypeNames`).

`Converter.Plugin.Abstractions.ControlNode` is the central AST-like model: a tree of WinForms controls with `Properties`, `EventHandlers`, `DataBindings`, and `Children`, produced by the parser and consumed by every downstream stage (layout analysis, mapping, generation).

### Conversion pipeline

`Converter.Cli/Services/ConversionOrchestrator.cs` drives the whole flow (`ExecuteAsync`), reporting progress via `IProgress<ConversionProgress>` at each step:

1. **Git init** — `Converter.Core/Git/GitIntegrationManager.cs` optionally creates a feature branch (pattern from `_config.GitIntegration.BranchNamePattern`) if the source is a git repo, git integration is enabled, and `_config.GitIntegration.CreateFeatureBranch` is set.
2. **Parse** — `Converter.Core/Parsing/WinFormsParser.cs` finds every `*.Designer.cs` under the input path (minus anything matching `_config.ExcludePatterns`) and Roslyn-parses it into a `ControlNode` tree (`ParseResult`), including event-handler subscriptions (`ControlNode.EventHandlers`) and `DataBindings.Add(...)` calls (`ControlNode.DataBindings`) — not just static properties. When `IncrementalSettings.Enabled` and `--force` wasn't passed, `Converter.Core/Services/FileHashTracker.cs` skips designer files whose hash hasn't changed since they were last converted.
3. **Analyze layout** — `Converter.Core/Analysis/LayoutAnalyzer.cs` inspects control positioning/bounds per form and scores confidence for Grid/StackPanel/DockPanel/Canvas, weighted by `_config.LayoutDetection.*DetectionWeight`. `TableLayoutPanel`/`FlowLayoutPanel`/`SplitContainer` roots short-circuit straight to their known layout type instead of re-deriving it from child positions.
4. **Convert each form** (`ConversionOrchestrator.ConvertFormAsync`, run either sequentially or via `Parallel.ForEachAsync` depending on `_config.ParallelProcessing.Enabled`) — for each parsed form:
   - `Converter.Generator/Axaml/AxamlGenerator.cs` → `Views/{Name}.axaml` (property values needing conversion, e.g. `BackColor`/`Font`/`Dock`/`Location`, go through `Converter.Generator/Axaml/PropertyValueConverter.cs`; unmapped controls still recurse into mapped children instead of dropping the subtree)
   - `Converter.Generator/ViewModels/ViewModelGenerator.cs` → `ViewModels/{Name}{ViewModelSuffix}.g.cs` (partial class with real `[ObservableProperty]` fields from `DataBindings` and `[RelayCommand]` methods from `EventHandlers`; the non-generated half is meant to be hand-authored and untouched by re-conversion)
   - `Converter.Generator/CodeBehind/CodeBehindGenerator.cs` → `Views/{Name}.axaml.cs`
   - `Converter.Generator/Styles/StyleGenerator.cs` → `Views/{Name}.Styles.axaml`, when `_config.StyleExtraction.Enabled` and enough controls share identical `Font`/`BackColor`/`ForeColor` values
   - Every file written here is tracked via `RollbackManager.TrackFileCreation` (guarded by a lock, since form conversion may run concurrently)
5. **Generate project files** — `Converter.Generator/Project/ProjectFileGenerator.cs` emits the Avalonia `.csproj` (package versions centralized in `ProjectFileGenerator.PackageVersions`), `App.axaml(.cs)`, `Program.cs`, `app.manifest`.
6. **Migration guide** — `Converter.Documentation/Generators/MigrationGuideGenerator.cs` → `MIGRATION_GUIDE.md`, summarizing each converted form, its detected layout, and a "Manual Steps Required" section populated from `ConversionOrchestrator.CollectManualSteps` (unmapped controls, `RequiresCustomLogic` properties, `PreserveEventHandler` events).
7. **Git commit** — if enabled, commits generated output via `GitIntegrationManager`.
8. Returns a `ConversionResult` containing a `ConversionReport` (`Converter.Reporting`) that the CLI renders as tables/panels and can also serialize to HTML/JSON/Markdown/CSV via `ReportBuilder`.

`RollbackManager` (`Converter.Core/Services/RollbackManager.cs`) wraps the whole run in a transaction (`BeginTransaction`/`CommitTransaction`); cancellation or any unhandled exception triggers `RollbackTransactionAsync()`, which deletes every tracked file so a failed/cancelled run doesn't leave a half-converted output directory. `CheckpointManager` (`--resume`) is still unwired — resuming a *parallelized* form-conversion batch needs its own design pass, deliberately deferred.

### Mapping registries

`Converter.Mappings/BuiltIn/` holds three static registries used during generation: `ControlMappingRegistry` (WinForms control type → Avalonia control type, e.g. `Form`→`Window`, `DataGridView`→`DataGrid`), `PropertyMappingRegistry` (e.g. `BackColor`→`Background`, `Font`→`FontFamily`/`FontSize`/`FontWeight`), and `EventMappingRegistry` (e.g. `Click`→a generated `[RelayCommand]`, `MouseDown`→`PointerPressed`). Custom/third-party mappings can additionally be supplied via `.converterconfig` (`customMappings`, `thirdPartyMappings` in `Converter.Core/Configuration/ConverterConfig.cs`) and are intended to be layered on top of the built-ins.

### Plugin system

`Converter.Plugin.Abstractions` defines the extension points: `IConverterPlugin` (lifecycle), `IControlMapper`, `IPropertyTranslator`, `ILayoutAnalyzer`, `ICodeGenerator`, `IValidationRule`. `Converter.Core/Plugins/PluginLoader.cs` is meant to discover/load plugins via `AssemblyLoadContext` isolation and a `plugin.json` manifest (`PluginManifest.cs`), but the CLI's `init-plugin` and `list-plugins` commands are currently unimplemented placeholders in `Converter.Cli/Program.cs`.

### CLI / UI layer

`Converter.Cli` uses `System.CommandLine` for argument parsing and `Spectre.Console` for interactive prompts and live progress rendering:
- `UI/ConversionStatusDisplay.cs` — live-updating panel while ANSI/interactive terminal support is detected.
- `UI/BasicProgressDisplay.cs` — fallback for non-interactive terminals.
- `UI/InteractivePrompts.cs` — prompts for missing `--input`/`--output`/`--layout` when `--no-interactive` is not set.
- `UI/ConverterTheme.cs` — colors/icons, overridable via a `.convertertheme` JSON file (see `winforms-to-avalonia-converter/custom-theme.convertertheme` for the schema: `Success`, `Warning`, `Error`, `Info`, `Debug`, `Primary`, `Secondary` colors plus icon glyphs).
- `Logging/SpectreConsoleLogger*.cs` — bridges `Microsoft.Extensions.Logging` into Spectre console output.

### Configuration

`.converterconfig` (JSON) is loaded via `Converter.Core/Configuration/ConfigurationLoader.cs` into `ConverterConfig` (`Converter.Core/Configuration/ConverterConfig.cs`), which is the single source of truth for custom mappings, layout-detection thresholds/weights, style extraction, naming conventions (including `RootNamespace`), exclude patterns, incremental/parallel settings, git integration, documentation, and plugin settings. Run `convert init-config` to scaffold one; when no `--config` is passed, the orchestrator starts from `new ConverterConfig()` (all defaults).

`Converter.Cli/Services/CliConfigMerger.cs` merges explicit CLI flags (`--no-git`, `--create-branch`, `--branch-name`, `--parallel`, `--incremental`) on top of the loaded config in `Program.cs`, *before* the orchestrator is constructed — a flag only overrides a config-file value when the user actually typed it (checked via `System.CommandLine`'s `OptionResult.IsImplicit`), so a bare option default never silently clobbers a `.converterconfig` setting. `--layout`, `--force`, and `--resume` aren't config-shaped (they're per-invocation directives, not persistable settings) and are instead passed straight to the `ConversionOrchestrator` constructor.
