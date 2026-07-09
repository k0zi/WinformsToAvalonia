# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 10 CLI tool that converts WinForms projects (`.Designer.cs` + code-behind) into Avalonia 11.x projects with MVVM architecture (CommunityToolkit.Mvvm). It parses WinForms designer code with Roslyn, infers a layout (Grid/StackPanel/DockPanel/Canvas), and generates AXAML views, partial ViewModels, code-behind, and a runnable Avalonia project — plus a migration guide and a conversion report.

The project is early-stage: parsing, layout analysis, mapping registries, and generators exist and produce output, but there is **no test project yet** (`Converter.Tests` is referenced in the docs/READMEs as aspirational but does not exist in `Converter.sln`), and the plugin loader / `init-plugin` / `list-plugins` CLI commands are stubs ("not yet implemented").

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

# run the CLI directly from source
cd Converter.Cli
dotnet run -- --help
dotnet run -- convert -i /path/to/WinFormsApp -o /path/to/AvaloniaApp --layout smart
dotnet run -- init-config -o .converterconfig
```

There is no test project in the solution currently — do not assume `dotnet test` has anything to run unless one has been added.

## Architecture

The solution is split into layered projects, referenced roughly in this order (later projects depend on earlier ones):

```
Converter.Plugin.Abstractions  → plugin contracts + shared model types (ControlNode, PropertyValue, DataBinding)
Converter.Core                 → parsing, layout analysis, config, git, checkpoint/rollback/hash services
Converter.Mappings             → built-in control/property/event mapping registries (BuiltIn/)
Converter.Generator            → AXAML / ViewModel / code-behind / project-file / style generators
Converter.Reporting            → HTML/JSON/Markdown/CSV report builder
Converter.Documentation        → migration guide generator
Converter.Cli                  → System.CommandLine entry point, Spectre.Console UI, orchestrator
```

`Converter.Plugin.Abstractions.ControlNode` is the central AST-like model: a tree of WinForms controls with `Properties`, `EventHandlers`, `DataBindings`, and `Children`, produced by the parser and consumed by every downstream stage (layout analysis, mapping, generation).

### Conversion pipeline

`Converter.Cli/Services/ConversionOrchestrator.cs` drives the whole flow (`ExecuteAsync`), reporting progress via `IProgress<ConversionProgress>` at each step:

1. **Git init** — `Converter.Core/Git/GitIntegrationManager.cs` optionally creates a feature branch (`feature/avalonia-migration-{timestamp}`) if the source is a git repo and git integration is enabled in config.
2. **Parse** — `Converter.Core/Parsing/WinFormsParser.cs` finds every `*.Designer.cs` under the input path and Roslyn-parses it into a `ControlNode` tree (`ParseResult`).
3. **Analyze layout** — `Converter.Core/Analysis/LayoutAnalyzer.cs` inspects control positioning/bounds per form and scores confidence for Grid/StackPanel/DockPanel/Canvas.
4. **Generate per form** (`ConvertFormAsync`) — for each parsed form:
   - `Converter.Generator/Axaml/AxamlGenerator.cs` → `Views/{Name}.axaml`
   - `Converter.Generator/ViewModels/ViewModelGenerator.cs` → `ViewModels/{Name}ViewModel.g.cs` (partial class; the non-generated half is meant to be hand-authored and untouched by re-conversion)
   - `Converter.Generator/CodeBehind/CodeBehindGenerator.cs` → `Views/{Name}.axaml.cs`
5. **Generate project files** — `Converter.Generator/Project/ProjectFileGenerator.cs` emits the Avalonia `.csproj`, `App.axaml(.cs)`, `Program.cs`, `app.manifest`.
6. **Migration guide** — `Converter.Documentation/Generators/MigrationGuideGenerator.cs` → `MIGRATION_GUIDE.md`, summarizing each converted form and its detected layout.
7. **Git commit** — if enabled, commits generated output via `GitIntegrationManager`.
8. Returns a `ConversionResult` containing a `ConversionReport` (`Converter.Reporting`) that the CLI renders as tables/panels and can also serialize to HTML/JSON/Markdown/CSV via `ReportBuilder`.

Cancellation (Ctrl+C) triggers `RollbackManager.RollbackTransactionAsync()` via `Converter.Core/Services/RollbackManager.cs`; `CheckpointManager` and `FileHashTracker` (same folder) exist to support `--incremental`/`--resume` but are not fully wired into the orchestrator yet — check current state before relying on them.

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

`.converterconfig` (JSON) is loaded via `Converter.Core/Configuration/ConfigurationLoader.cs` into `ConverterConfig` (`Converter.Core/Configuration/ConverterConfig.cs`), which is the single source of truth for custom mappings, layout-detection thresholds, style extraction, naming conventions, incremental/parallel settings, git integration, documentation, and plugin settings. Run `convert init-config` to scaffold one; when no `--config` is passed, the orchestrator uses `new ConverterConfig()` (all defaults).
