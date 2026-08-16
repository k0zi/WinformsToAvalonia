# Known limitations

Honest, current-state list of what this converter does not (yet) handle, so users and
contributors know what to expect and where to look before filing a duplicate issue.

## Control discovery / classification

- **Custom intermediate base classes.** `DesignerFileLocator` classifies Forms/UserControls/
  Components by the *immediate* base-list identifier (`: Form`, `: UserControl`,
  `: Component`), not a fully resolved semantic model. A form declared as `: MyBaseForm`
  (where `MyBaseForm` itself derives from `Form`) is classified as `Other` and skipped.
  Real designer-generated forms almost always declare the WinForms base type directly, so
  this covers the overwhelming majority of cases.
- **Components are not converted.** `Form` and `UserControl` artifacts both go through the
  conversion pipeline now (a Form becomes a `Window`, a UserControl an Avalonia `UserControl`,
  and a project's own UserControls are registered as real control mappings so a Form hosting
  one emits the generated View element). `Component`-kind artifacts are discovered and reported
  with tailored guidance, but never emitted - they have no visual representation to convert.

## Designer parsing

- **`Items.Add`/`DropDownItems.Add`/`Columns.Add` are now parsed.**
  `DesignerSyntaxWalker.HandleInvocation` generalized its collection-name check from just
  `"Controls"` to `"Controls" or "Items" or "DropDownItems" or "Columns"` - `ToolStripItem`s
  (even though `ToolStripItem` doesn't derive from `Control`) and `DataGridViewColumn`s
  correctly nest under their owning `MenuStrip`/`ToolStrip`/`StatusStrip`/`ContextMenuStrip`/
  `ToolStripMenuItem`/`DataGridView` instead of leaking into `FormModel.Components` as flat,
  parent-less entries. `MenuStrip` is `Direct`-mapped to Avalonia's real `Menu`; every
  `ToolStripItem` subtype except `ToolStripControlHost`, and every `DataGridViewColumn`
  subtype, is `Direct`-mapped too (see `docs/Controls.md`); `ToolStrip`/`StatusStrip` (no
  native Avalonia toolbar/status-bar control) are `StackPanel`-based `Fallback` controls that render their item children for
  real, no longer empty bars. `ContextMenuStrip`'s items are parsed the same way, but still
  never emitted - it's a non-visual `FormModel.Components` entry `AxamlEmitter` never visits
  (see `docs/Controls.md`'s `ContextMenuStrip` implementation-plan entry for what's still
  missing: resolving `someControl.ContextMenuStrip = this.contextMenuStrip1;` and emitting a
  nested `<Control.ContextMenu>`).
- **`resources.ApplyResources(...)` (localization) calls are ignored**, same as any other
  invocation statement the walker doesn't specifically recognize.

## Control mapping

- **`NotifyIcon`** has an `Unsupported` (guidance-only) *mapping* entry, because Avalonia's
  tray-icon support (`TrayIcon`) lives in `App.axaml`'s `TrayIcon.Icons` collection - an
  app-level (not per-Form) concept that doesn't fit the per-View `AxamlEmitter` model. The
  feature itself works: `ConversionPipeline` aggregates every form's `NotifyIcon`s and
  `AvaloniaProjectScaffolder` emits them into `App.axaml`. The **icon file** is the limitation:
  only a literal `new Icon("app.ico")` path that resolves to a real file is copied into
  `Assets/`. Anything else (a resx resource, a computed icon - the common case) emits the
  `TrayIcon.Icons` block commented out with a TODO, because Avalonia resolves `TrayIcon.Icon`
  at run time: referencing an asset the conversion cannot produce builds fine and then throws
  `FileNotFoundException` out of `App.Initialize()`, before any window opens.
- **File dialogs** (`OpenFileDialog`/`SaveFileDialog`/`FolderBrowserDialog`/`ColorDialog`/
  `FontDialog`/print-related dialogs) have `Unsupported` (guidance-only) mapping entries, not
  working ones. Avalonia's replacement (`TopLevel.StorageProvider`, for the file pickers) is
  an async API called from code, not a control - wiring it up needs real ViewModel code
  generation, not a XAML element mapping. The others (`ColorDialog`, `FontDialog`,
  `PrintDialog`, ...) have no Avalonia built-in equivalent at all.
- **Non-visual components** (`BackgroundWorker`, `BindingSource`, `Timer`, `ImageList`,
  dialogs, ...) are collected into `FormModel.Components` by `ControlGraphBuilder` (anything
  never `Controls.Add`-ed) and are run through `ControlMappingRegistry.Map` by
  `ConversionPipeline.Run`; any `Fallback`/`Unsupported` result's guidance text is added to
  the conversion report's warnings - so it now surfaces during a real conversion, not only in
  the static `list-mappings` reference table. They never get a visual element or ViewModel
  stub, since there's nothing to render. `ToolTip` is the one exception: the component field
  itself still gets no element, but `DesignerSyntaxWalker.HandleSetToolTipInvocation` resolves
  `this.toolTip1.SetToolTip(this.control1, "text")` calls onto the *target* control's own
  properties, and `AxamlEmitter` emits them as a universal `ToolTip.Tip` attribute - so the
  actual tooltip feature works even though the `ToolTip` type's own registry entry stays
  `Unsupported`.
- **Item and selection *content* is never translated.** A `ListView` picks its target
  per-instance (`ListViewMapper`: `View=Details` or any parsed `ColumnHeader` children →
  `DataGrid` with real columns, otherwise `ListBox`) and a `MonthCalendar` maps to `Calendar`,
  but neither gets its items or selection ranges - the control is emitted with its columns and
  no rows. All six `DataGridView` column types are translated now: `TextBox`/`CheckBox` to
  Avalonia's two real column types, and `ComboBox`/`Button`/`Image`/`Link` to a
  `DataGridTemplateColumn` with a generated cell template. Those templates are **unbound** -
  Designer.cs records the column but not its `DataPropertyName`-to-view-model mapping - so each
  one carries a `TODO` comment naming what to add.

## Code-behind and ViewModel generation

The split between event-driven code-behind and MVVM is decided per handler by
`FormMigrationPlanner`, from a Roslyn analysis of the handler's actual body
(`CodeBehindAnalyzer`). **Code-behind is the default**; a `[RelayCommand]` is the
evidence-backed exception. See the "Code-behind migration" section of `README.md` for the
rule itself; what follows is what the rule does *not* cover yet.

- **Handler bodies are never translated, only preserved.** Every generated handler has the
  right Avalonia signature and is subscribed for real, but its body is the original WinForms
  code inside a comment followed by a `MigrationTodo.NotMigrated(...)` call. There is no
  statement-level WinForms-to-Avalonia API rewriting (`MessageBox.Show`, `TreeView.Nodes`,
  `Control.Focus()`, ...), so the generated project builds and runs but does nothing until a
  human rewrites each body. The same applies to promoted `[RelayCommand]`s. The marker reports
  instead of throwing on purpose - Avalonia raises these events from the framework, including
  during XAML initialization, so a throwing stub made the converted app unlaunchable; flip
  `MigrationTodo.ThrowOnUnmigratedCall` to get strict failure back. The one exception is the
  generated file-dialog helper methods, which still throw: nothing calls them until a human
  wires them up, so they can never fire on their own.
- **Non-handler members** (helper methods like `SetBusy`, fields, properties) are preserved
  as a comment block, not as compiling code, because they reference WinForms APIs too. A
  handler that calls one is therefore never promoted to the ViewModel.
- **Promotion is single-control only.** A handler wired to more than one control needs
  `sender` to tell them apart, so it always stays in code-behind - and when the controls'
  Avalonia events have different signatures (a `Button`'s real `Click` vs. a `Label`'s
  `PointerPressed`), the method is split in two, each named after its Avalonia event.
- **Bindable property coverage is deliberately small** (`BindablePropertyCatalog`):
  `Text`/`Content`, `Checked`, `Value`, `SelectedItem`/`SelectedIndex`, `Enabled`, `Visible`.
  A handler touching anything outside that vocabulary stays in code-behind, since the
  property could not be expressed as a `{Binding}` anyway.
- **`CanExecute` is not derived.** `someButton.Enabled = ...` assignments are not turned into
  `[RelayCommand(CanExecute = ...)]`.
- **Item sources are not bound.** `ComboBox`/`ListBox`/`TreeView` contents still need a
  hand-written `ObservableCollection` - only the *selection* properties are in the catalog.
- **Events with no Avalonia equivalent** (`Paint`, `Validating`, `Validated`, `ItemCheck`,
  `VisibleChanged`, and the events of non-visual components like `BackgroundWorker.DoWork` or
  `FileSystemWatcher.Changed`) get their handler method emitted but nothing subscribes it; the
  conversion report names each one and why. `Scroll` is in this group only for controls that
  aren't a `TrackBar` or a `ScrollBar`: `EventMappingRegistry` has a per-control-type override
  table (consulted before the generic one) that maps `TrackBar.Scroll` → `Slider.ValueChanged`,
  `HScrollBar`/`VScrollBar`.`Scroll` → `ScrollBar.Scroll`, `DataGridView.CellClick` →
  `DataGrid.CellPointerPressed`, and `LinkLabel.LinkClicked` → `Click` (a `LinkLabel` maps to a
  `HyperlinkButton`, which unlike a `TextBlock` has a real `Click` and `Command`).
- **Events Avalonia merges are not chained automatically.** WinForms distinguishes events
  Avalonia does not - a `PictureBox`'s `Click` and `MouseDown` both become `PointerPressed`.
  Two attributes of the same name on one element is an Avalonia XAML parse error (AVLN1001),
  so only one subscription survives (the exact mapping beats the approximation); both handler
  methods are still emitted, and the report names the one you must call by hand.
- **Fallback controls are never event-wired.** The tool's own bundled fallback controls do not
  necessarily expose the Avalonia event a mapping names, and a wrong attribute is an Avalonia
  XAML compiler error (AVLN2000) that would break the generated build - so the handler is
  emitted and the missing subscription is reported as a warning instead.
- **Inline lambda handlers** captured from `InitializeComponent()`
  (`this.Load += (s, e) => { ... };`) are reported but not migrated.
- **File dialogs** get a `StorageProvider` method on the View, but it is not tied to any
  button: the designer never records which handler opened which dialog, since handler bodies
  are not part of `InitializeComponent()`.

## Layout

- Canvas + absolute positioning is a deliberate, permanent design choice (see the project
  plan), not a limitation to be fixed - `Anchor`/`Dock` are preserved as metadata
  (`w2a:LayoutHint` attached property + XML comment) for manual follow-up, never
  auto-translated to responsive layout.
- `Location`/`Size` parsing only understands literal ints (e.g. `new Point(12, 12)`) and
  `Point.Empty`/`Size.Empty` - anything else (a computed expression, a field reference) can't
  be statically resolved to a fixed value, so `Canvas.Left`/`Canvas.Top`/`Width`/`Height`
  aren't emitted for that control. This is now surfaced as a conversion warning naming the
  field and the unresolved expression, rather than silently dropped.
