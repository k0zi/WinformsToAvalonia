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
dotnet run --project src/WinFormsToAvalonia.Cli -- convert --source <app.csproj> --output ./Out [--force|--overwrite-all|--dry-run|--verbose|--no-fallback-controls|--skip-code-behind-comments|--log-file <p>]
dotnet run --project src/WinFormsToAvalonia.Cli -- analyze --source <app.csproj>
dotnet run --project src/WinFormsToAvalonia.Cli -- list-mappings [--filter Box]

# end-to-end smoke over the real sample app: converts every samples/WinForms/*.csproj into
# samples/Avalonia/<name>/ and adds each to a generated solution
./samples/convert.sh
```

Integration tests shell out to `dotnet build` on generated output in a temp dir, so they are
slow and need the SDK on `PATH`; `dotnet test` on the solution runs them.

## Architecture

`ConversionPipeline.Run` (`src/WinFormsToAvalonia.Core/Pipeline/ConversionPipeline.cs`) is the
single orchestrator — read it first, every stage below is a field on it:

1. **Parsing** (`Core/Parsing`) — `WinFormsProjectLoader` (legacy vs SDK-style csproj) →
   `DesignerFileLocator` (classifies each type as Form/UserControl/Component from its base list,
   followed transitively through the project's *own* classes so `: MyBaseForm` resolves; a base
   type from a referenced assembly stays `Other` but is reported, not dropped) →
   `DesignerSyntaxWalker` (Roslyn walk of `InitializeComponent`, also
   capturing `EventHandlerBinding`s) → `ControlGraphBuilder` (flat assignments → parent/child
   `FormModel`). `CodeBehindExtractor` grabs the raw `.cs` text *verbatim* for the preserved
   comment block; `CodeBehindAnalyzer` builds the `CodeBehindModel` of handlers/helpers.
   Resources join the same path rather than a parallel one: `ResxReader` + `ResxPropertyProvider`
   turn `.resx` entries into the *same* `PropertyValue` shapes `ExpressionEvaluator` produces, so
   a `Localizable=true` form's `resources.ApplyResources(...)` properties are indistinguishable
   downstream from designer-declared ones. Base64 payloads become `PropertyValue.ResourceReference`,
   which `ConversionPipeline.ResolveResourceAssets` turns into copied `Assets/` files via
   `ResxImageExtractor`.
2. **Mapping** (`Core/Mapping`) — `ControlMappingRegistry` is a `WinFormsTypeName → IControlMapper`
   dictionary. `DefaultControlMappers.All` holds the built-in set: `SimplePropertyMapper` (Direct),
   `FallbackControlMapper` (bundled template), `UnsupportedControlMapper` (guidance-only warning),
   plus the hand-written `ListViewMapper` and `TemplateColumnMapper` for the DataGrid family.
   Per-run mappers are *composed* onto it by the pipeline, never added to the static list:
   `UserControlMapper` for the project's own UserControls, and an `UnsupportedControlMapper` per
   project-defined `Component`.
   Three sibling lookup tables sit alongside the registry. Two feed the planner:
   `EventMappingRegistry` (WinForms event → Avalonia event + args type, with per-control-type
   overrides — `Click` is a real `Click` only on Button/menu-item-like types, a `PointerPressed`
   everywhere else) and `BindablePropertyCatalog` (the deliberately small set of two-way-bindable
   properties that is the *entire* vocabulary a handler body may use to qualify for
   `[RelayCommand]`). The third feeds `AxamlEmitter`: `AvaloniaStylePropertySupport` says which
   of `Background`/`Foreground`/font/`Padding` each **target element name** can carry, and
   `AvaloniaItemsSupport` which targets accept literal item children — both keyed on the Avalonia
   element, not the WinForms type. **Adding a mapper with a new target element means adding it to
   those tables too**, or that element silently gets no styling (and its designer-declared items
   are reported as un-emitted).
3. **Planning** — `FormMigrationPlanner.Plan(formModel, codeBehind)` produces one
   `FormMigrationPlan` per Form, and all three emitters consume that same plan so they cannot
   disagree about where a handler landed. This is where the strict "code-behind by default,
   `[RelayCommand]` only when provable" rules from README live. It also plans the non-control
   pieces: `Timer` components → `DispatcherTimer`, and `OpenFileDialog`/`SaveFileDialog`/
   `FolderBrowserDialog` → the corresponding `StorageProvider` picker call.
   Body translation (`HandlerBodyRewriter`) runs **last**, over the finished decisions — what a
   promoted body may name is only settled once every handler is classified. It has two targets
   (a View still has control fields; a ViewModel has only `[ObservableProperty]`s) and stops at
   the first statement it cannot prove equivalent, so the emitted code is always a faithful
   *prefix* of the original.
4. **Emission** (`Core/Emission`) — `AxamlEmitter` (+ `AxamlDocumentBuilder`),
   `ViewCodeBehindEmitter`, `ViewModelEmitter`. All naming (`Form1` → `Form1View`/`Form1ViewModel`,
   nested-folder namespaces, command names) goes through `NamingConventions` — never hand-roll it.
5. **Scaffolding** (`Core/Scaffolding`) — `AvaloniaProjectScaffolder.BuildProject` writes the fixed
   App/Program/ViewLocator/csproj skeleton plus the Views/ViewModels into a `VirtualFileSystem`
   (`BuildEmptySkeleton` covers the no-Form and nothing-discovered cases);
   `FallbackControlResolver` copies only the *used* fallback templates. `VirtualFileSystem.WriteToDisk`
   is the one and only place bytes hit disk — everything upstream is pure, which is what makes
   `--dry-run` and the tests possible. It defaults to `ExistingFileStrategy.PreserveExisting`:
   a re-run never clobbers a file the user has edited, writing the regenerated version beside it
   as `*.w2a-new` instead (`--overwrite-all` opts out). Keep new write paths going through it.

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
  `w2a:LayoutHint` attached property (`xmlns:w2a` → the generated `Controls/Generated/LayoutHint.cs`),
  never auto-translated to Avalonia layout. This includes `TableLayoutPanel`/`FlowLayoutPanel`.
- **Generated projects must always build and run — warning-free.** A handler body is emitted as
  real code only where `HandlerBodyRewriter` can prove equivalence; the rest stays a comment
  inside a correctly-signed method that calls `MigrationTodo.NotMigrated(...)`, which reports
  rather than throws (Avalonia raises these handlers during XAML init, so throwing killed the app
  before its first window). `MigrationTodo.ThrowOnUnmigratedCall` opts back into throwing for
  smoke tests. Warning-free matters as much as compiling: reads of Avalonia string properties are
  emitted as `(x.Text ?? string.Empty)` because the generated csproj enables nullable. Any change
  here must keep the integration `dotnet build` tests green.
- **Deterministic output.** xmlns prefixes are positional (`uc0`, `uc1`), collections are ordered
  before emission — the golden-file test (`Fixtures/ExpectedAxaml`) depends on it.
- UserControls are always planned/emitted *before* Forms, so a Form hosting one already has its
  mapping and xmlns prefix. Forms need more than ordering: `BuildFormViews` resolves **every**
  Form to its View in a separate pass before emission, because a handler body that opens another
  Form must name a View whose Form may not be converted yet — ordering alone cannot fix a cycle.
- **Extra NuGet packages are allowlisted twice.** A mapper declares `RequiredNuGetPackage` (e.g.
  `Avalonia.Controls.DataGrid`); it flows `AxamlEmitter` → pipeline → `BuildProject`, but the csproj
  writer emits *only* packages present in `AvaloniaProjectScaffolder.ExtraPackageVersions`. Adding a
  package needs both, or it is silently dropped and the generated project fails to compile.
  The generated project's Avalonia / CommunityToolkit.Mvvm versions are `const`s on that same class.
- **App-level components are not per-View.** `NotifyIcon` becomes `TrayIcon.Icons` in `App.axaml`,
  and its icon bytes are copied into the VFS with `AddBinary` — it never reaches an emitter.

## Tests

- `Core.Tests` — per-stage unit tests mirroring the `Core` folder layout. `Fixtures/DesignerCs`
  holds inert WinForms designer files read as raw text, `Fixtures/ExpectedAxaml` the golden AXAML
  (`AxamlEmitterTests`). `TestSupport/TempProjectFixture` builds synthetic projects on disk.
- `Integration.Tests` — `SampleApps/*` are real WinForms fixture projects; each test converts one
  and asserts `dotnet build` on the generated output succeeds. Add a `SampleApps` folder + an
  `[InlineData]` row (in `RealFormConversionBuildTests` or the feature-specific test class) when
  adding a feature that changes generated code.
- Both test csprojs `Compile Remove` their fixture/sample `.cs` files and copy them to output
  instead — fixtures must never be compiled into the test assembly. Keep that when adding fixtures.

## Docs

`docs/Controls.md` (per-type mapping status, hand-maintained — if it disagrees with
`DefaultControlMappers.cs`, the code wins and the doc gets fixed) and `docs/known-limitations.md`.
