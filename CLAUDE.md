# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 10 CLI that reads a WinForms `.csproj` with Roslyn and *generates* a brand-new Avalonia
MVVM application: one `Window` View + `ViewModel` per `Form`, one Avalonia `UserControl` View +
`ViewModel` per WinForms `UserControl`. Nothing is converted in place; the source project is
read-only input.

## Commands

```bash
dotnet build WinFormsToAvalonia.slnx
dotnet test  WinFormsToAvalonia.slnx

# one project / one test
dotnet test tests/WinFormsToAvalonia.Core.Tests
dotnet test tests/WinFormsToAvalonia.Integration.Tests --filter "FullyQualifiedName~RealFormConversionBuildTests"
dotnet test --filter "DisplayName~ConvertedFixtureProject_BuildsSuccessfullyWithDotnetBuild"

# run the tool
dotnet run --project src/WinFormsToAvalonia.Cli -- convert --source <app.csproj> --output ./Out [--force|--dry-run|--verbose|--no-fallback-controls|--skip-code-behind-comments|--log-file <p>]
dotnet run --project src/WinFormsToAvalonia.Cli -- analyze --source <app.csproj>
dotnet run --project src/WinFormsToAvalonia.Cli -- list-mappings [--filter Box]
```

Integration tests shell out to `dotnet build` on generated output in a temp dir, so they are
slow and need the SDK on `PATH`; `dotnet test` on the solution runs them.

## Architecture

`ConversionPipeline.Run` (`src/WinFormsToAvalonia.Core/Pipeline/ConversionPipeline.cs`) is the
single orchestrator — read it first, every stage below is a field on it:

1. **Parsing** (`Core/Parsing`) — `WinFormsProjectLoader` (legacy vs SDK-style csproj) →
   `DesignerFileLocator` (classifies each type as Form/UserControl/Component by its *immediate*
   base-list identifier) → `DesignerSyntaxWalker` (Roslyn walk of `InitializeComponent`) →
   `ControlGraphBuilder` (flat assignments → parent/child `FormModel`). `CodeBehindExtractor`
   grabs the raw `.cs` text; `CodeBehindAnalyzer` builds the `CodeBehindModel` of handlers.
2. **Mapping** (`Core/Mapping`) — `ControlMappingRegistry` is a `WinFormsTypeName → IControlMapper`
   dictionary. `DefaultControlMappers.All` holds the built-in set (`SimplePropertyMapper` = Direct,
   `FallbackControlMapper` = bundled template, `UnsupportedControlMapper` = guidance-only warning).
   Per-run mappers are *composed* onto it by the pipeline, never added to the static list:
   `UserControlMapper` for the project's own UserControls, and an `UnsupportedControlMapper` per
   project-defined `Component`.
3. **Planning** — `FormMigrationPlanner.Plan(formModel, codeBehind)` produces one
   `FormMigrationPlan` per Form, and all three emitters consume that same plan so they cannot
   disagree about where a handler landed. This is where the strict "code-behind by default,
   `[RelayCommand]` only when provable" rules from README live.
4. **Emission** (`Core/Emission`) — `AxamlEmitter` (+ `AxamlDocumentBuilder`),
   `ViewCodeBehindEmitter`, `ViewModelEmitter`. All naming (`Form1` → `Form1View`/`Form1ViewModel`,
   nested-folder namespaces, command names) goes through `NamingConventions` — never hand-roll it.
5. **Scaffolding** (`Core/Scaffolding`) — `AvaloniaProjectScaffolder.BuildProject` writes the fixed
   App/Program/ViewLocator/csproj skeleton plus the Views/ViewModels into a `VirtualFileSystem`;
   `FallbackControlResolver` copies only the *used* fallback templates. `VirtualFileSystem.WriteToDisk`
   is the one and only place bytes hit disk — everything upstream is pure, which is what makes
   `--dry-run` and the tests possible.

`WinFormsToAvalonia.FallbackControls` ships the fallback control sources as **embedded text, not
compiled code** (`Compile Remove` in its csproj): they reference Avalonia types on purpose, and
keeping them uncompiled is what keeps the tool itself free of an Avalonia dependency. Their
namespace is a `__TARGET_NAMESPACE__` placeholder rewritten at copy time. Adding a template means
adding the file *and* a `FallbackControlCatalog.All` entry (with `DependsOnKeys` if it references
another template) — the only real verification is an integration test that builds the output.

The CLI (`src/WinFormsToAvalonia.Cli`) is Spectre.Console.Cli: one command class + settings class
per verb, with all output formatting isolated in `Cli/Rendering`.

## Invariants worth not breaking

- **Canvas-everywhere layout.** Every container emits a `Canvas`; children carry absolute
  `Canvas.Left`/`Top`/`Width`/`Height`. `Anchor`/`Dock` are *preserved* as an XML comment plus the
  `w2a:LayoutHint` attached property, never auto-translated to Avalonia layout.
- **Generated projects must always build and run.** Handler bodies are emitted as comments inside a
  correctly-signed method that calls `MigrationTodo.NotMigrated(...)`, which reports rather than
  throws (Avalonia raises these handlers during XAML init, so throwing killed the app before its
  first window). Any change here must keep the integration `dotnet build` tests green.
- **Deterministic output.** xmlns prefixes are positional (`uc0`, `uc1`), collections are ordered
  before emission — the golden-file test (`Fixtures/ExpectedAxaml`) depends on it.
- UserControls are always planned/emitted *before* Forms, so a Form hosting one already has its
  mapping and xmlns prefix.

## Tests

- `Core.Tests` — per-stage unit tests mirroring the `Core` folder layout. `Fixtures/DesignerCs`
  holds inert WinForms designer files read as raw text, `Fixtures/ExpectedAxaml` the golden AXAML
  (`AxamlEmitterTests`). `TestSupport/TempProjectFixture` builds synthetic projects on disk.
- `Integration.Tests` — `SampleApps/*` are real WinForms fixture projects; each test converts one
  and asserts `dotnet build` on the generated output succeeds. Add a `SampleApps` folder + an
  `[InlineData]` row when adding a feature that changes generated code.
- Both test csprojs `Compile Remove` their fixture/sample `.cs` files and copy them to output
  instead — fixtures must never be compiled into the test assembly. Keep that when adding fixtures.

## Docs

`docs/Controls.md` (per-type mapping status, hand-maintained — if it disagrees with
`DefaultControlMappers.cs`, the code wins and the doc gets fixed) and `docs/known-limitations.md`.
