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
# --source takes a .csproj, or a .sln/.slnx to convert every WinForms project in it at once
dotnet run --project src/WinFormsToAvalonia.Cli -- convert --source <app.csproj|app.slnx> --output ./Out [--force|--overwrite-all|--dry-run|--verbose|--no-fallback-controls|--skip-code-behind-comments|--log-file <p>]
dotnet run --project src/WinFormsToAvalonia.Cli -- analyze --source <app.csproj|app.slnx>
dotnet run --project src/WinFormsToAvalonia.Cli -- list-mappings [--filter Box]

# end-to-end smoke over the real sample app: converts every samples/WinForms/*.csproj into
# samples/Avalonia/<name>/ and adds each to a generated solution
./samples/convert.sh
```

Integration tests shell out to `dotnet build` - and, for the startup smoke tests, `dotnet run` -
on generated output in a temp dir, so they are slow and need the SDK on `PATH`; `dotnet test` on
the solution runs them. Every one of them goes through `TestSupport/DotnetRunner`, which passes
`-nodeReuse:false` and imposes a timeout. **Both are load-bearing.** MSBuild's persistent worker
nodes outlive the build that started them and inherit the redirected stdout/stderr handles, so a
reader draining those pipes never reaches end-of-stream: the suite hung forever on a `dotnet build`
of a *solution* that had already finished, and intermittently failed elsewhere with MSB3026/MSB4018
as leftover nodes fought over files. **Never diagnose this suite with `-v q`** - it hides the build
output the assertions put in their failure message, which is the only evidence of what broke.

## Architecture

`ConversionPipeline.Run` (`src/WinFormsToAvalonia.Core/Pipeline/ConversionPipeline.cs`) is the
single orchestrator — read it first, every stage below is a field on it:

`SolutionConversionPipeline` sits one level above it for a `.sln`/`.slnx` source and runs that same
pipeline once per project, unchanged. The only thing it adds is a `SolutionConversionContext`
second argument: the UserControls of the projects *this* csproj `ProjectReference`s, predicted
into their View names before anything is converted, plus the generated `ProjectReference`s to
match. Options are the user's intent, so this is a parameter, not a field on `ConversionOptions`.

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
   `[RelayCommand]`; each entry carries **two** types, and they answer different questions —
   `ClrTypeName` is what the generated ViewModel property is declared as, with a `{Binding}`
   converting on its way to the element, while `AvaloniaTypeName` is what the member really is,
   which is what a code-behind read has to come out as: see `ReadExpression`). The third feeds
   `AxamlEmitter`: `AvaloniaStylePropertySupport` says which
   of `Background`/`Foreground`/font/`Padding` each **target element name** can carry, and
   `AvaloniaItemsSupport` which targets accept literal item children — both keyed on the Avalonia
   element, not the WinForms type. **Adding a mapper with a new target element means adding it to
   those tables too**, or that element silently gets no styling (and its designer-declared items
   are reported as un-emitted). `FallbackControlMemberSupport` is the third: which catalog
   members each *bundled template* really exposes, which is what lets a fallback be written to or
   called at all. Two more feed `HandlerBodyRewriter`: `ControlMethodCatalog` (zero-argument
   control methods with an exact equivalent — the method-level counterpart of
   `BindablePropertyCatalog`) and `EventArgsMemberCatalog` (what a handler's `e.X`/`e.Cancel` mean
   on the Avalonia side — the member-level counterpart of `EventMappingRegistry`).
   `FileDialogCatalog` rounds it out: the three WinForms file dialogs and their `StorageProvider`
   replacements, used both to emit a picker method and to inline one into a handler. Three
   smaller tables cover what the *conversion itself* creates rather than what it maps:
   `WindowPropertyCatalog` (Form properties a `Window` spells differently - `Text` → `Title`),
   `DispatcherTimerMemberCatalog` (a Timer this run emitted as a `DispatcherTimer` field), and
   `ComponentFieldCatalog` (the non-visual components that are plain .NET types and survive
   unchanged - the one table here that *doesn't* translate, so it has no per-member whitelist),
   and `DialogResultCatalog`, whose two members are deliberately different shapes - a *total*
   `ClosesWithSuccess` for synthesizing a designer-declared button, a *partial* `TryGetBool` for
   a hand-written result that has to round-trip. `BindablePropertyCatalog` and the mappers name the same Avalonia property from two
   places — `BindablePropertyCatalogTests` asserts they agree, because a disagreement is a build
   error in the **generated** project, which this repo's own build cannot catch. The same blind
   spot covers the *types*: nothing here can see Avalonia, so an entry claiming a non-nullable
   type for a nullable member is only caught by an integration test that builds
   (`CodeBehindMigrationTests`). Verify a new entry against the real reference assembly.
3. **Planning** — `FormMigrationPlanner.Plan(formModel, codeBehind)` produces one
   `FormMigrationPlan` per Form, and all three emitters consume that same plan so they cannot
   disagree about where a handler landed. It runs *after* a discovery pass over every artifact:
   `ConversionPipeline` parses them all, asks `PlanProperties` what public surface each converted
   View will carry, and hands the answer back in as a `ViewSurfaceContext` — because a handler
   saying `dialog.EnteredText` names a member of a View that may not be planned yet. Same shape as
   `BuildFormViews`, one level down. This is where the strict "code-behind by default,
   `[RelayCommand]` only when provable" rules from README live. It also plans the non-control
   pieces: `Timer` components → `DispatcherTimer`, and `OpenFileDialog`/`SaveFileDialog`/
   `FolderBrowserDialog` → the corresponding `StorageProvider` picker call.
   Body translation (`HandlerBodyRewriter`) runs **last**, over the finished decisions — what a
   promoted body may name is only settled once every handler is classified. It has two targets
   (a View still has control fields; a ViewModel has only `[ObservableProperty]`s) and stops at
   the first statement it cannot prove equivalent, so the emitted code is always a faithful
   *prefix* of the original. The one exception — `TryMatchCloseConfirmation`, the confirm-on-close
   `FormClosing` handler — is a **whole-body** rewrite rather than a prefix, because Avalonia has
   no synchronous message box and therefore no statement-level answer. Matched before the
   statement loop, all-or-nothing, and it is meant to stay the only one. Two things are planned **before** that rewrite rather than after,
   because a body may *name* them: the `DispatcherTimer`/component fields, and the code-behind
   helpers — `PlanHelpers` translates helper bodies to a fixed point (a call to a not-yet-promoted
   helper simply refuses, which is also what makes recursion and `async` propagation settle by
   themselves) and, unlike a handler, promotes one only when its **whole** body translates: at a
   call site there is nowhere to put the remainder. `PlanFileDialogs` is the one that stays after,
   since what it emits depends on what the rewrite did.
4. **Emission** (`Core/Emission`) — `AxamlEmitter` (+ `AxamlDocumentBuilder`),
   `ViewCodeBehindEmitter`, `ViewModelEmitter`, and `MigrationChecklistEmitter` for the
   `MIGRATION.md` the generated project carries. That last one is built from the plans, never from
   the emitted text: `CodeBehindHandlerPlan.IsUnfinished` is the *same* predicate the code emitters
   use to decide whether to write a `MigrationTodo`, so the checklist cannot drift from the code
   it describes — put any new "is this done" opinion there rather than beside it. All naming (`Form1` → `Form1View`/`Form1ViewModel`,
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
  `Avalonia.Controls.DataGrid`) and `ComponentFieldCatalog` names one per component; both flow
  through the pipeline into `BuildProject`, but the csproj writer emits *only* packages present in
  `AvaloniaProjectScaffolder.ExtraPackageVersions`. Adding a
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
- `GeneratedAppStartupTests` covers the **other half** of "always builds and runs": it replaces the
  generated `Program.cs` with a harness that boots the same `App` on Avalonia's headless platform,
  so `OnFrameworkInitializationCompleted` really constructs the main View and the AXAML is really
  parsed. Building alone missed a Windows-only component whose field initializer threw from the
  View constructor — perfectly compiled, unlaunchable. Add an `[InlineData]` row here for anything
  that emits code into a **constructor** or into `App.axaml`.
- Both test csprojs `Compile Remove` their fixture/sample `.cs` files and copy them to output
  instead — fixtures must never be compiled into the test assembly. Keep that when adding fixtures.

## Docs

`docs/Controls.md` (per-type mapping status, hand-maintained — if it disagrees with
`DefaultControlMappers.cs`, the code wins and the doc gets fixed) and `docs/known-limitations.md`.
