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
dotnet test tests/WinFormsToAvalonia.Mapping.Tests
dotnet test tests/WinFormsToAvalonia.Integration.Tests --filter "FullyQualifiedName~RealFormConversionBuildTests"
dotnet test --filter "DisplayName~ConvertedFixtureProject_BuildsSuccessfullyWithDotnetBuild"

# run the tool
# --source takes a .csproj, or a .sln/.slnx to convert every WinForms project in it at once
dotnet run --project src/WinFormsToAvalonia.Cli -- convert --source <app.csproj|app.slnx> --output ./Out [--force|--overwrite-all|--dry-run|--verbose|--no-fallback-controls|--skip-code-behind-comments|--with-web|--log-file <p>]
dotnet run --project src/WinFormsToAvalonia.Cli -- analyze --source <app.csproj|app.slnx>
dotnet run --project src/WinFormsToAvalonia.Cli -- list-mappings [--filter Box]

# --with-web needs the WebAssembly workload, once - the browser-head build tests require it
dotnet workload install wasm-tools

# end-to-end smoke over the real sample app: converts every samples/WinForms/*.csproj into
# samples/Avalonia/<name>/ and adds each to a generated solution
./samples/convert.sh

# pack and install it as the .NET tool it ships as (command: wf2a, package: WinFormsToAvalonia)
dotnet pack src/WinFormsToAvalonia.Cli -c Release            # -> artifacts/*.nupkg (gitignored)
dotnet tool install --global --add-source ./artifacts WinFormsToAvalonia
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
   `ResxImageExtractor` - or, for an `ImageList.ImageStream`, via `ImageListExtractor`, which
   decodes the whole strip into one PNG per image and lets `ResolveImageListReferences` rewrite
   each `ImageIndex` into the asset path (the list is inherited from the owning control, as
   WinForms resolves it). Only `MenuItem.Icon` can actually show one; everywhere else the file is
   written and the warning names it.
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
   `AvaloniaItemsSupport` which targets accept literal item children, and
   `AvaloniaAccessKeySupport` which of them render an underscore as a keyboard access key — all
   keyed on the Avalonia element, not the WinForms type. That last one pairs with
   `WinFormsMnemonicCatalog`, the one table here holding *two* facts at once: whether a WinForms
   control's `Text` is a caption carrying an `&` mnemonic at all, and whether the element it
   becomes can render one — `&File` on a menu item becomes `_File`, on a Label (a `TextBlock`)
   just `File`, and on a TextBox nothing at all, because there it is the user's data.
   Access-key support is the one table that *cannot* be read from Avalonia's metadata: it is a
   template detail, so it was measured by rendering each element headlessly and looking for an
   `AccessText`. **Adding a mapper with a new target element means adding it to
   those tables too**, or that element silently gets no styling (and its designer-declared items
   are reported as un-emitted). `FallbackControlMemberSupport` is the third: which catalog
   members each *bundled template* really exposes, which is what lets a fallback be written to or
   called at all. Two more feed `HandlerBodyRewriter`: `ControlMethodCatalog` (zero-argument
   control methods with an exact equivalent — the method-level counterpart of
   `BindablePropertyCatalog`) and `EventArgsMemberCatalog` (what a handler's `e.X`/`e.Cancel` mean
   on the Avalonia side — the member-level counterpart of `EventMappingRegistry`).
   `HostedControlCatalog` is a table about the *source* only: which WinForms types are plumbing
   around another control and which constructor argument names it. `ToolStripControlHost` has no
   parameterless constructor, so the hosted control is always named there - `ControlGraphBuilder`
   rewrites the parent/child edge and the host disappears.
   `ExtenderProviderCatalog` is the one keyed on *neither* side alone: WinForms' extender
   providers (`ToolTip`, `HelpProvider`) set a property on *another* control through a
   two-argument call, so one row carries both the walker's question
   (`(owner type, method) → property key`) and the emitter's (`property key → attached property`).
   That is why `SetToolTip` became `ToolTip.Tip` and `SetHelpString` becomes
   `AutomationProperties.HelpText`; a setter on a recognised provider with no row is reported by
   name rather than dropped.
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
   error in the **generated** project, which this repo's own build cannot catch — so
   `WinFormsToAvalonia.Mapping.Tests` catches it instead: that project reads Avalonia's reference
   assemblies as metadata and holds **every** table entry up against them (element names,
   attributes, property types, events and their args types, control methods, style groups,
   fallback-template members). Adding a table entry means it will be checked; if it fails there,
   the table is wrong, not the test.
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
   `ModelTypeContext` rides in the same way as `ViewSurfaceContext`, and for the same reason: the
   types a Form declares *inside itself* are lifted into `Models/`, and both the element type of
   the collection a `BindingSource` becomes and the property names a population statement may use
   have to be known while the Form is being planned. So `ConversionPipeline.CarryOverModelTypes`
   runs in the parse pass and only its *file writing* waits — hoisting the warnings too would
   reorder `MIGRATION.md` for every project that carries no model type at all.
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
   `--with-web` is bolted on as a **post-processing pass** over the finished VFS -
   `AvaloniaProjectScaffolder.SplitIntoHeads` (in the `.WebHeads.cs` partial) re-roots everything
   under `{projectName}/`, rewrites that csproj as a library, moves `Program.cs`/`app.manifest`
   into a desktop head, and adds a `net10.0-browser` head plus the `.slnx`. Deliberately last:
   the pipeline keeps adding to the VFS after the scaffolder is done (components, assets,
   `MIGRATION.md`, fallback templates), and every one of those call sites would otherwise have to
   learn where the project root moved to.

`WinFormsToAvalonia.FallbackControls` ships the fallback control sources as **embedded text, not
compiled code** (`Compile Remove` in its csproj): they reference Avalonia types on purpose, and
keeping them uncompiled is what keeps the tool itself free of an Avalonia dependency. Their
namespace is a `__TARGET_NAMESPACE__` placeholder rewritten at copy time. Adding a template means
adding the file *and* a `FallbackControlCatalog.All` entry (with `DependsOnKeys` if it references
another template) — the only real verification is an integration test that builds the output.

The CLI (`src/WinFormsToAvalonia.Cli`) is Spectre.Console.Cli: one command class + settings class
per verb, with all output formatting isolated in `Cli/Rendering`. It is packed as a .NET tool -
`ToolCommandName` is `wf2a` while the `PackageId` stays `WinFormsToAvalonia`, and
`SetApplicationName("wf2a")` keeps every usage line in `--help` matching what the user types.
The bundled fallback templates are embedded resources in a *referenced* project, so they travel
into the tool package automatically - but that is exactly the thing a packaging change can break
silently, so verify a change here by installing the package and converting the sample with it,
not by building.

## Invariants worth not breaking

- **Canvas-everywhere layout.** Every container emits a `Canvas`; children carry absolute
  `Canvas.Left`/`Top`/`Width`/`Height` - *except* where the parent holds items rather than
  positioned children (`AxamlEmitter.HostsItems`: TabControl, Menu/MenuItem/ContextMenu,
  `DataGrid.Columns`). There the WinForms bounds describe the parent's client area, and emitting
  them sizes the wrong thing: nine 992x602 TabPages became nine 602-pixel-tall *tab headers*, so
  the header strip filled the window and every page fell outside it. The sample built, started
  and passed every test while showing nothing.
  Every view also carries a `<Window.Styles>`/`<UserControl.Styles>` block pinning
  `Canvas > :is(Control)` to `MinWidth`/`MinHeight` 0 and `Canvas > :is(TemplatedControl)` to
  `Padding="4,1"`: absolute coordinates only describe a layout if the controls are the size they
  were told to be, and Avalonia's touch-oriented theme makes a 23-pixel TextBox 32 pixels tall,
  which silently covers whatever sits 26 pixels below it. Both numbers were measured headlessly;
  the padding is needed too, or the smaller box just clips its text. A style rather than
  attributes, so a designer-set Padding still wins.
  `Anchor`/`Dock` are *preserved* as an XML comment plus the
  `w2a:LayoutHint` attached property (`xmlns:w2a` → the generated `Controls/Generated/LayoutHint.cs`),
  never auto-translated to Avalonia layout. This includes `TableLayoutPanel`/`FlowLayoutPanel`.
- **A bundled fallback template that subclasses a templated control must override
  `StyleKeyOverride`.** Avalonia resolves a theme by the *concrete* type, so a `TextBox` subclass
  finds none, gets no template, and renders as **nothing** — the converted MaskedTextBox and
  RichTextBox were simply absent from the window while the project compiled, started and passed
  every test. `FallbackControlTemplateTests` reads Avalonia's metadata to work out which
  templates are affected and requires the override; Panel-derived templates are exempt because a
  Panel has no template to lose.
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
- **Nothing that reports a count or a colour may switch on `MappingStatus` alone.** `Unsupported`
  means "emits no element", which is true of a `Timer` and a `PrintDialog` alike - and for three
  releases every surface that produced a number said "33 unsupported" for a conversion in which 20
  of those worked. `MappedControl.Disposition` carries `UnsupportedDisposition` for exactly this;
  `ConversionReport` counts `ConvertedElsewhereCount` separately, `MIGRATION.md` puts those under
  "Converted differently" rather than "Needs your attention", and `list-mappings` colours them
  green. A new reporting surface asks the disposition, not the status.
- **An `Unsupported` mapping is not an unconverted one.** `MappingStatus.Unsupported` means "emits
  no AXAML element", which most of the registry's `Unsupported` entries are while being thoroughly
  converted somewhere else. `UnsupportedControlMapper` therefore takes a **required**
  `UnsupportedDisposition` - `FeatureElsewhere`, `Unreachable`, `NoAvaloniaApi` - so a new entry
  cannot be added without saying which. `docs/Controls.md` carries it as a fourth column and
  `ControlsDocumentationTests` checks every cell against the registry, in both directions.
- **Extra NuGet packages are allowlisted three times.** A mapper declares `RequiredNuGetPackage` (e.g.
  `Avalonia.Controls.DataGrid`) and `ComponentFieldCatalog` names one per component; both flow
  through the pipeline into `BuildProject`, but the csproj writer emits *only* packages present in
  `AvaloniaProjectScaffolder.ExtraPackageVersions`. Adding a
  package needs both, or it is silently dropped and the generated project fails to compile.
  The third is `PackageStyleIncludes`: a control shipped outside core Avalonia brings its own
  `ControlTheme` in a resource dictionary `App.axaml` has to ask for with a `StyleInclude`.
  Referencing the package is not enough - without the include the control finds no theme, gets no
  template, and renders as **nothing**. Same failure mode as a missing `StyleKeyOverride`, and
  `GeneratedAppStartupTests` now catches the whole class: it walks the booted window and fails on
  any `TemplatedControl` whose `Template` is still null.
  The generated project's Avalonia / CommunityToolkit.Mvvm versions are `const`s on that same class.
- **`--with-web` may not change a single byte of the flag-off output.** The layout split is a
  post-processing pass over the finished VFS, and the View split goes through a `ViewRootKind`
  that *defaults* to what `WinFormsArtifactKind` always chose. That default is what lets the
  golden AXAML, the byte-identical `App.axaml` test and the fixed-file-set test stay untouched -
  keep any new web behaviour behind the same default.
- **In a browser there is no `Window` at all** — not shown, not constructed. Avalonia's browser
  backend installs a single-view lifetime and no windowing platform. That is why `--with-web`
  roots the main Form's View at a `UserControl` with a generated wrapper `Window` for the desktop
  head, why `WindowOnlyEventCatalog` exists (an `Opened=` on a `UserControl` root is an AVLN2000),
  and why `ViewNavigationContext` splits "`this` *is* the Window" from "a Window is *reachable*"
  — the split main View can own a dialog and close itself through `ViewWindow.Of(this)` while
  having none of a Window's own members. Anything emitted on `this` that only a `TopLevel` has
  (`StorageProvider`) needs the same treatment; that one was found by the sample, not the
  fixtures.
- **The browser head's csproj has two properties that fail silently when missing.** Without
  `<RuntimeIdentifier>browser-wasm</RuntimeIdentifier>` the WebAssembly SDK targets never run:
  the project compiles to IL, `dotnet build` exits 0, and no `AppBundle` is produced. Without
  `<WasmExtraFilesToDeploy Include="wwwroot\**" />` the bundle exists with no `index.html`.
  `WebHeadConversionBuildTests` therefore asserts on the bundle's *contents*, never on the exit
  code alone.
- **The two collection shapes are whole-shape matchers, not "the rewriter learned C#".**
  `bindingSource1.DataSource = new BindingList<T> { new T { P = v } , … }` and a Details-mode
  ListView's `Items.Add(new ListViewItem(new[] { … }))` are the only places an object initializer
  or an array is read, and `TryRewriteExpression` gained **no** case for `new`. Every degree of
  freedom is closed by a fact the run already proved: the shape matches only as the RHS of a
  `DataSource` assignment the designer already turned into an `ItemsSource` binding; `T` must be a
  type *this run* lifted into `Models/`, so its settable auto-properties are read off the parsed
  declaration and must agree with the plan's (a disagreement is a CS0029 in the generated project);
  the wrapper must be one of the ordered collections (a `HashSet` reorders, so it is refused); and
  a ListView row's cell count must equal the designer's column count. A carried-over model type is
  lifted `public`, not `internal` — the ViewModel's `ObservableCollection<T>` is a public property,
  and CS0053 is a build error in the **generated** project and nowhere else.
- **`FallbackControlMemberSupport` answers two different questions, and the second one bites.**
  It gates what a handler *body* may name — and it is also what `AxamlEmitter.FilterBindableForTarget`
  asks before writing a planned binding onto a fallback element. An unlisted property is dropped
  there **in silence**. `BindingNavigatorFallback.Position` was deliberately unregistered on the
  grounds that no WinForms `BindingNavigator` has a `Position` (true, and irrelevant): the
  conversion binds it itself, and the binding simply vanished. Registering a template member is
  about what the *template* exposes, not about what a WinForms body could have said.
- **A `DataGridTextColumn` with no `Binding` is a column that can never show a cell.** A
  Details-mode ListView's `ColumnHeader`s used to emit exactly that — a header over a strip
  nothing could fill, not even after a hand migration. `ListViewRowsPlan` is what gives them one:
  a row is the `string[]` of sub-item texts a `ListViewItem` already is, and column *i* binds to
  `[i]`. Deriving named properties from the header text instead would invent domain names the
  original never wrote, and has no answer for a blank or duplicated header.
- **A statement may be *absorbed* only when its value is provably carried elsewhere.**
  `colorDialog1.Color = Color.Red;` before a `ShowDialog` emits nothing of its own - the value
  becomes an argument to the `ShowAsync` that replaces the dialog (`TryAbsorbDialogSeed`). That is
  the only assignment allowed to vanish, and it may only absorb a value it can actually translate:
  the rewriter has no warning channel, so absorbing one it cannot would be a silent loss. Refusing
  is the honest fallback even though the prefix rule makes it cost the rest of the handler.
- **Attributes must all be written before the first child element.** `AxamlDocumentBuilder.Attribute`
  appends to raw text, so the first `OpenElement` closes the parent's start tag and any attribute
  written afterwards lands *outside* it - a document that does not parse. `EmitContextMenuIfPresent`
  emits a child element and therefore runs after every attribute pass, not before.
- **A fallback control's styling and item surface come from its template, not from an element name.**
  A fallback's emitted element name is its template key, which no Avalonia-element-keyed table can
  answer for. `AvaloniaStylePropertySupport.ForFallbackTemplate` derives the style groups from
  `FallbackControlMemberSupport` a member at a time (a group is writable only when every member it
  is made of is listed), and `AvaloniaItemsSupport` is keyed on the template key too. Both the
  emitter and `HandlerBodyRewriter` ask the same method, so they cannot disagree.
- **A mapper and the universal passes can want the same attribute name.** `AxamlEmitter` writes
  the mapper's attributes, then the extender-provider and visual-style passes; all three now skip
  names already emitted (`emittedAttributeNames`). A duplicate XML attribute does not merge, it
  fails to parse - so a mapper that claims a styling property (`GroupBox` claims `Padding`) must
  also lose that flag in `AvaloniaStylePropertySupport`, or two writers race for one name.
- **A `Direct` mapping can still lose something, and must say so.** `mapped.Warnings` is surfaced
  on the Direct path too - as a `TODO` comment in the AXAML and in the conversion report - which
  is how `CheckedListBox` reports the per-item check state it cannot carry. Before this only the
  not-emitted branch read them, so a lossy Direct mapping was silent.
- **`BindablePropertyCatalog` is keyed on the WinForms type alone**, which is not enough for a
  per-instance mapper that picks between two elements. `MappedControl.UnreachableBindableMembers`
  is how such a mapper narrows the catalog's answer - a `Format=Time` `DateTimePicker` is a
  `TimePicker`, which has `SelectedTime` and no `SelectedDate`, and emitting the catalog's answer
  there is a CS1061 in the **generated** project and nowhere else.
- **App-level components are not per-View.** `NotifyIcon` becomes `TrayIcon.Icons` in `App.axaml`,
  and its icon bytes are copied into the VFS with `AddBinary` — it never reaches an emitter. Its
  `ContextMenuStrip` goes the same way, as `TrayIcon.Menu`: a **native** menu the OS draws, so a
  `NativeMenuItem` carries a `Header`, an `IsEnabled` flag and a submenu and nothing else - and
  `Click` is an event, not an attribute, so a designer-wired one is reported.

## Tests

- `Mapping.Tests` — the only project that references **either** framework, and it never runs a
  line of either. `AvaloniaMetadata` reads Avalonia's reference assemblies; `WinFormsMetadata`
  reads WinForms' (from `Microsoft.WindowsDesktop.App.Ref`, copied beside the test assembly), so
  "is every event mapped?" is a test rather than a question about memory — every event `Control`
  and `Form` declare must be classified by name. Every test asserts one mapping-table claim
  against the real API. This exists because the converter
  emits *text*, so a wrong table entry is a build error in the generated project and nowhere
  else — three such entries were found by hand before this project existed, and four more the
  first time it ran. Its package versions must equal `AvaloniaProjectScaffolder`'s, which is
  itself one of the tests.
- `Core.Tests` — per-stage unit tests mirroring the `Core` folder layout. `Fixtures/DesignerCs`
  holds inert WinForms designer files read as raw text, `Fixtures/ExpectedAxaml` the golden AXAML
  (`AxamlEmitterTests`). `TestSupport/TempProjectFixture` builds synthetic projects on disk.
- `Integration.Tests` — `SampleApps/*` are real WinForms fixture projects; each test converts one
  and asserts `dotnet build` on the generated output succeeds. Add a `SampleApps` folder + an
  `[InlineData]` row (in `RealFormConversionBuildTests` or the feature-specific test class) when
  adding a feature that changes generated code.
- `GeneratedAppStartupTests` does three things per app, not one: constructs the View (so every
  field initializer, event subscription and the AXAML itself really run), executes every generated
  `[RelayCommand]` (safe by construction - a promoted body touches nothing but ObservableProperties),
  and raises `Click` on every button so the code-behind handler bodies run too. The sample is the
  one app whose buttons are *not* clicked: its handlers open a serial port and write to the OS
  event log, dependencies the conversion neither introduced nor can remove.
- `GeneratedAppStartupTests` boots **the all-in-one sample** as well as the fixtures, and that is
  the row that matters most: a handler Avalonia raised *during* XAML population crashed the sample
  at startup while every fixture passed, because none happened to have a TabControl with a handler
  on it. Fixture coverage is only ever what someone thought to write down.
- `GeneratedAppStartupTests` covers the **other half** of "always builds and runs": it replaces the
  generated `Program.cs` with a harness that boots the same `App` on Avalonia's headless platform,
  so `OnFrameworkInitializationCompleted` really constructs the main View and the AXAML is really
  parsed. Building alone missed a Windows-only component whose field initializer threw from the
  View constructor — perfectly compiled, unlaunchable. Add an `[InlineData]` row here for anything
  that emits code into a **constructor** or into `App.axaml`.
- The `--with-web` output has three test classes, one per risk: `AvaloniaProjectScaffolderWebTests`
  (the split's exact file set and csproj contents), `WebHeadConversionBuildTests` (all three
  projects build, the browser bundle really exists — including a row for the all-in-one sample,
  which is the one that caught `StorageProvider` being emitted bare), and
  `WebHeadSingleViewStartupTests` (the main View constructs and templates with no Window of its
  own). `ISingleViewApplicationLifetime` cannot be implemented outside Avalonia, so that last one
  does what the browser backend would do with the View rather than faking the lifetime.
- Both test csprojs `Compile Remove` their fixture/sample `.cs` files and copy them to output
  instead — fixtures must never be compiled into the test assembly. Keep that when adding fixtures.

## Docs

`docs/Controls.md` (per-type mapping status — `ControlsDocumentationTests` now checks it against
the registry in **both** directions, so a drifting row and a missing one both fail; the code still
wins when they disagree, it just no longer takes a human to notice) and `docs/known-limitations.md`.
