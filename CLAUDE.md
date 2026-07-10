# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 10 CLI tool that converts WinForms projects (`.Designer.cs` + code-behind) into Avalonia projects with MVVM architecture (CommunityToolkit.Mvvm). Targets Avalonia 12.x by default; Avalonia 11.x remains fully supported as an explicit opt-in (`projectGeneration.avaloniaVersion`). It parses WinForms designer code with Roslyn, infers a layout (Grid/StackPanel/DockPanel/Canvas), and generates AXAML views, partial ViewModels, code-behind, and a runnable Avalonia project — plus a migration guide and a conversion report. Packaged as a dotnet tool (`winforms2avalonia`, package id `WinformsToAvalonia.Converter`) — see "Packaging" below.

Core correctness (event handler and data binding extraction, so generated ViewModels/AXAML actually contain real `[ObservableProperty]`/`[RelayCommand]`/property bindings instead of empty shells), CLI/config wiring, the previously-dead subsystems (rollback tracking, incremental hashing, style extraction, checkpoint/resume), a configurable Avalonia/package target version, `.resx` resource conversion, the plugin system (loader, `init-plugin`/`list-plugins` CLI commands, and mapping-registry consumption hooks), and WinForms event-handler *body* migration have all been fixed/implemented — see `Converter.Tests` for coverage.

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

### Packaging

`Converter.Cli` is `PackAsTool`-enabled (`ToolCommandName=winforms2avalonia`, `PackageId=WinformsToAvalonia.Converter`) but not published anywhere — repo-contributor dev workflow stays `dotnet run --project Converter.Cli --` as shown above. To test the packaged dotnet-tool experience specifically (e.g. after touching `Converter.Cli.csproj`, `Program.cs`'s root command wiring, or the `init-plugin` scaffold's `HintPath` logic):

```bash
dotnet pack Converter.Cli/Converter.Cli.csproj -c Release -o /tmp/wf2av-nupkg
dotnet tool install --tool-path /tmp/wf2av-tool --add-source /tmp/wf2av-nupkg WinformsToAvalonia.Converter
/tmp/wf2av-tool/winforms2avalonia convert -i /path/to/WinFormsApp -o /path/to/AvaloniaApp
dotnet tool uninstall --tool-path /tmp/wf2av-tool WinformsToAvalonia.Converter  # cleanup
```

`--tool-path` (not `--global`) keeps this fully sandboxed. No NuGet publish automation exists (no CI, no nuget.org listing) — this is deliberate scope, not an oversight; see the `init-plugin` scaffold's `HintPath` comment in `Program.cs` for the one place this currently matters (a plugin scaffolded against an installed tool references that tool's `.store/...` `Converter.Plugin.Abstractions.dll` path directly, which can go stale across `dotnet tool update`/`uninstall` until that assembly is published as its own package).

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
Converter.Tests                → xUnit tests; references Core/Plugin.Abstractions/Mappings/Generator/Cli/Tests.SamplePlugin
Converter.Tests.SamplePlugin   → real compiled plugin fixture (IConverterPlugin + IControlMapper) for plugin-loading integration tests
```

Note the dependency direction: `Converter.Mappings` references `Converter.Core` (not the other way around) — if you need a Mappings type from Core (e.g. widening a parser heuristic against `ControlMappingRegistry`), adding `Converter.Core` → `Converter.Mappings` would create a cycle; duplicate the small piece of data instead (see `WinFormsParser.KnownControlTypeNames`).

`Converter.Plugin.Abstractions.ControlNode` is the central AST-like model: a tree of WinForms controls with `Properties`, `EventHandlers`, `DataBindings`, and `Children`, produced by the parser and consumed by every downstream stage (layout analysis, mapping, generation).

### Conversion pipeline

`Converter.Cli/Services/ConversionOrchestrator.cs` drives the whole flow (`ExecuteAsync`), reporting progress via `IProgress<ConversionProgress>` at each step:

1. **Git init** — `Converter.Core/Git/GitIntegrationManager.cs` optionally creates a feature branch (pattern from `_config.GitIntegration.BranchNamePattern`) if the source is a git repo, git integration is enabled, and `_config.GitIntegration.CreateFeatureBranch` is set.
2. **Parse** — `Converter.Core/Parsing/WinFormsParser.cs` finds every `*.Designer.cs` under the input path (minus anything matching `_config.ExcludePatterns`) and Roslyn-parses it into a `ControlNode` tree (`ParseResult`), including event-handler subscriptions (`ControlNode.EventHandlers`) and `DataBindings.Add(...)` calls (`ControlNode.DataBindings`) — not just static properties. When `_config.ResourceConversion.Enabled` and a sibling `.resx` exists (`Converter.Core/Parsing/SiblingFileResolver.cs`), it's parsed by `Converter.Core/Parsing/ResxDocument.cs` (hand-rolled `System.Xml.Linq`, no `System.Windows.Forms`/Windows dependency) and passed into the parser so `resources.GetObject(...)`/`GetString(...)`/`ApplyResources(...)` calls resolve to real values instead of opaque C# text; binary/image entries are resolved to `Assets/...` files later by `ConversionOrchestrator.ExtractResxAssetsAsync` (legacy BinaryFormatter payloads and unrecognized formats are left unmapped with a migration-guide manual step, not faked). When `IncrementalSettings.Enabled` and `--force` wasn't passed, `Converter.Core/Services/FileHashTracker.cs` skips designer files whose hash hasn't changed since they were last converted. When `--resume` is passed, `Converter.Core/Services/CheckpointManager.cs` skips designer files already completed in a prior interrupted run (see the Resume paragraph below).
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

`RollbackManager` (`Converter.Core/Services/RollbackManager.cs`) wraps the whole run in a transaction (`BeginTransaction`/`CommitTransaction`); by default, cancellation or any unhandled exception triggers `RollbackTransactionAsync()`, which deletes every tracked file so a failed/cancelled run doesn't leave a half-converted output directory.

**Resume (`--resume`):** when passed, `ConversionOrchestrator` forces sequential form conversion (never parallel, regardless of `_config.ParallelProcessing.Enabled`) so it can save an accurate `CheckpointManager` checkpoint (`.converter-checkpoint.json`, listing `CompletedFiles`/`FailedFiles`) after *every* form — this per-form save, not the catch-block handling, is what makes a hard kill mid-run resumable. On cancellation/failure while `--resume` is active, the catch blocks call `rollbackManager.CommitTransaction()` instead of rolling back, deliberately keeping whatever succeeded so the next `--resume` run can pick up where it left off (the checkpoint's `CompletedFiles` filters those forms out of the next run's designer-file list); `--force` overrides this filter (starts over) but still checkpoints in case that run also gets interrupted. Without `--resume`, none of this triggers — behavior is unchanged from the default rollback-everything path.

### Mapping registries

`Converter.Mappings/BuiltIn/` holds three static registries used during generation: `ControlMappingRegistry` (WinForms control type → Avalonia control type, e.g. `Form`→`Window`, `DataGridView`→`DataGrid`), `PropertyMappingRegistry` (e.g. `BackColor`→`Background`, `Font`→`FontFamily`/`FontSize`/`FontWeight`), and `EventMappingRegistry` (e.g. `Click`→a generated `[RelayCommand]`, `MouseDown`→`PointerPressed`). Custom/third-party mappings can additionally be supplied via `.converterconfig` (`customMappings`, `thirdPartyMappings` in `Converter.Core/Configuration/ConverterConfig.cs`) and are intended to be layered on top of the built-ins.

### Plugin system

`Converter.Plugin.Abstractions` defines the extension points: `IConverterPlugin` (lifecycle), `IControlMapper`, `IPropertyTranslator`, `IEventMapper`, `ILayoutAnalyzer`, `ICodeGenerator`, `IValidationRule`. `Converter.Core/Plugins/PluginLoader.cs` discovers/loads plugins via `AssemblyLoadContext` isolation (`PluginLoadContext`) and a `plugin.json` manifest (`PluginManifest.cs`); `PluginLoader.GetPlugins<T>()` only tests the single instantiated `EntryType` object for `is T` — it does not separately scan the assembly for other classes, so **every extension-point interface a plugin implements must live on the same class as `IConverterPlugin`**, not on a sibling class (the `init-plugin` scaffold below follows this).

A subtle but important fix lives in `PluginLoadContext.Load()`: a framework-dependent plugin build (via `ProjectReference` or a raw `<Reference HintPath>`, both copy-local by default) always ships its own local copy of `Converter.Plugin.Abstractions.dll` next to the plugin DLL. Left unhandled, `AssemblyDependencyResolver` would resolve and load that local copy into the plugin's isolated `AssemblyLoadContext`, producing a *second*, distinct `IConverterPlugin`/`IControlMapper`/etc. type — making `typeof(IConverterPlugin).IsAssignableFrom(entryType)` in `PluginLoader.LoadPluginAsync` always fail. `PluginLoadContext.Load()` special-cases the shared contracts assembly by name and always returns `null` for it, forcing the CLR to fall back to the Default context (where the host's own copy already lives), so the plugin's types share identity with the host's. `PluginLoaderIntegrationTests`/`PluginWiringEndToEndTests` fixtures deliberately copy `Converter.Plugin.Abstractions.dll` alongside the sample plugin DLL to keep exercising this path.

Orchestrator wiring: `ConversionOrchestrator` takes an optional `pluginsDirectory` constructor parameter (from `--plugins`, or `_config.Plugins.PluginsDirectory` if unset). If the directory exists, all plugins are loaded once per run into a `Converter.Generator/Mapping/MappingResolver.cs`, which — once per form, before generation starts — walks the control tree in a single async pre-pass and records the highest-priority plugin match per control/property/event into a `PluginMappingOverrides` (keyed by `ControlNode` reference identity). Generation itself (`AxamlGenerator`/`ViewModelGenerator`/`StyleGenerator`) stays fully synchronous, consuming `PluginMappingOverrides` as a plain read-only lookup passed as an explicit parameter (never stored on the generator instance, preserving their no-mutable-state/parallel-safety invariant) — checking it before falling back to the static `ControlMappingRegistry`/`PropertyMappingRegistry`/`EventMappingRegistry`. Zero plugins configured means `MappingResolver.Empty` short-circuits with no tree walk at all — zero behavior change from before plugins existed.

`convert init-plugin --name <name> --output <dir>` scaffolds a `plugin.json`, a `.csproj` referencing this checkout's `Converter.Plugin.Abstractions` build output via `HintPath` (no published NuGet feed exists yet), and a stub `IConverterPlugin, IControlMapper` class — the generated `.csproj` copies `plugin.json` into the build output directory (`CopyToOutputDirectory`), since `PluginLoader` resolves `EntryAssembly` relative to wherever `plugin.json` ends up, not the project source root. Point `--plugins` at that build output directory (`bin/<Configuration>/<TargetFramework>`), not the scaffolded project root. `convert list-plugins --plugins <dir>` discovers manifests and prints a table.

### Event-handler body migration

Gated by `_config.EventHandlerMigration.Enabled` (default on). For each parsed form, the orchestrator resolves the sibling non-designer `.cs` file (`SiblingFileResolver.ResolveCodeBehind`, shared with `.resx` resolution) and runs `Converter.Core/Parsing/EventHandlerBodyParser.cs` — a narrow, best-effort Roslyn pass separate from `WinFormsParser` (a code-behind file is arbitrary user code, unlike `InitializeComponent`'s machine-generated shape) that extracts the full original source of every handler method referenced in the tree, verbatim, into `ParseResult.EventHandlerBodies` (missing/unparseable sibling file → simply no entries, never a hard failure). The extracted text is **never emitted as live/compiled code** — always an inert, individually `//`-prefixed comment block inside a correctly-signed, compiling stub, so a broken or unrecognized original body can't produce plausible-looking-but-wrong generated code. `Converter.Mappings/BuiltIn/EventSignatureRegistry.cs` maps each Avalonia event name to its fully-qualified `EventArgs` type (falling back to `RoutedEventArgs` for anything unlisted) so the stub signature actually compiles — and is version-aware: `GetSignature(avaloniaEvent, avaloniaMajorVersion)` applies a v12-only override table for the one confirmed Avalonia 11→12 breaking change that affects generated code (`GotFocus`/`LostFocus` both move from `GotFocusEventArgs`/`RoutedEventArgs` to a unified `FocusChangedEventArgs`); `ConversionOrchestrator` computes `avaloniaMajorVersion` once per run via `EventSignatureRegistry.ParseMajorVersion(_config.ProjectGeneration.AvaloniaVersion)` and threads it into `CodeBehindGenerator.Generate`. For `PreserveEventHandler` events (e.g. `MouseDown`/`KeyDown`), `CodeBehindGenerator` emits the stub under the *original* handler method name, and `AxamlGenerator.WriteEventAttributes` wires the matching AXAML event attribute (e.g. `KeyDown="textBox1_KeyDown"`) so the stub is actually reachable rather than dead code — closing a gap that existed even before this feature (events were extracted into `ControlNode.EventHandlers` but never wired into either output). For `ConvertToCommand` events (e.g. `Click`), the same original-body-as-comment treatment replaces the old generic `// TODO: Implement {event} logic` line inside the generated `[RelayCommand]` stub in `ViewModelGenerator`. Both generator changes are gated on a plugin not already having claimed that event via `PluginMappingOverrides.EventMappings` (checked the same way `CollectManualSteps` already did).

### CLI / UI layer

`Converter.Cli` uses `System.CommandLine` for argument parsing and `Spectre.Console` for interactive prompts and live progress rendering:
- `UI/ConversionStatusDisplay.cs` — live-updating panel while ANSI/interactive terminal support is detected.
- `UI/BasicProgressDisplay.cs` — fallback for non-interactive terminals.
- `UI/InteractivePrompts.cs` — prompts for missing `--input`/`--output`/`--layout` when `--no-interactive` is not set.
- `UI/ConverterTheme.cs` — colors/icons, overridable via a `.convertertheme` JSON file (see `winforms-to-avalonia-converter/custom-theme.convertertheme` for the schema: `Success`, `Warning`, `Error`, `Info`, `Debug`, `Primary`, `Secondary` colors plus icon glyphs).
- `Logging/SpectreConsoleLogger*.cs` — bridges `Microsoft.Extensions.Logging` into Spectre console output.

### Configuration

`.converterconfig` (JSON) is loaded via `Converter.Core/Configuration/ConfigurationLoader.cs` into `ConverterConfig` (`Converter.Core/Configuration/ConverterConfig.cs`), which is the single source of truth for custom mappings, layout-detection thresholds/weights, style extraction, naming conventions (including `RootNamespace`), exclude patterns, incremental/parallel settings, git integration, documentation, plugin settings, generated-project target versions (`projectGeneration.avaloniaVersion`/`communityToolkitMvvmVersion`/`targetFramework`, defaulting to `12.0.0`/`8.3.2`/`net10.0`), `.resx` resource conversion (`resourceConversion.enabled`/`assetsDirectory`, both defaulted on), and event-handler body migration (`eventHandlerMigration.enabled`, defaulted on). Run `convert init-config` to scaffold one; when no `--config` is passed, the orchestrator starts from `new ConverterConfig()` (all defaults).

`Converter.Cli/Services/CliConfigMerger.cs` merges explicit CLI flags (`--no-git`, `--create-branch`, `--branch-name`, `--parallel`, `--incremental`) on top of the loaded config in `Program.cs`, *before* the orchestrator is constructed — a flag only overrides a config-file value when the user actually typed it (checked via `System.CommandLine`'s `OptionResult.IsImplicit`), so a bare option default never silently clobbers a `.converterconfig` setting. `--layout`, `--force`, and `--resume` aren't config-shaped (they're per-invocation directives, not persistable settings) and are instead passed straight to the `ConversionOrchestrator` constructor.
