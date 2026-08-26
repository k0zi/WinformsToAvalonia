# Known limitations

Honest, current-state list of what this converter does not (yet) handle, so users and
contributors know what to expect and where to look before filing a duplicate issue.

## Control discovery / classification

- **Base classes from referenced assemblies.** `DesignerFileLocator` classifies
  Forms/UserControls/Components from the base list, still without a semantic model. A base-list
  name that is not `Form`/`UserControl`/`Component` itself is now followed *transitively*
  through the other classes **this project declares**, so the common in-project
  `MyBaseForm : Form` intermediate resolves correctly (to any depth, with a cycle guard).
  What syntax alone cannot follow is a base class defined in a *referenced assembly*
  (`: ThirdParty.RibbonForm`): that still classifies as `Other` and is not converted - but the
  conversion now **reports** it by name instead of dropping it silently, so the fix (convert
  that project too, or temporarily declare the WinForms base type) is discoverable.
  One consequence of matching on simple names: two same-named classes in different namespaces
  merge their base sets, which can only ever make classification *more* inclusive - it converts
  an artifact that should have been left alone, rather than silently losing one.
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
  real, no longer empty bars. `ContextMenuStrip`'s items are parsed the same way, and the
  component *is* wired up: `AxamlEmitter.EmitContextMenuIfPresent` resolves
  `someControl.ContextMenuStrip = this.contextMenuStrip1;` and emits the item tree as a nested
  `<Control.ContextMenu>` on the owning control. The `ContextMenuStrip` field itself still has
  no element of its own - correctly, since a context menu is not part of the visual tree.
- **Resources are read.** `resources.ApplyResources(this.button1, "button1")` - how a
  `Localizable=true` form sets *every* property, `Text`/`Location`/`Size` included - is resolved
  against the form's paired `.resx` (`ResxReader` → `ResxPropertyProvider`), producing the same
  `PropertyValue` shapes designer C# would, so nothing downstream knows the difference. The
  `$this` key configures the form itself. Applied at the call site, so a later explicit
  assignment still wins, exactly as at run time. `resources.GetObject("pictureBox1.Image")` on
  the right of an assignment is recognized too. What is *not* covered:
  - **only the neutral-culture `.resx`.** Satellite files (`MainForm.hu.resx`) are not read, so
    a localized app converts with its default-language strings baked into the AXAML as literals.
    Real localization in the generated app needs a proper Avalonia localization approach.
  - **only the value kinds `ResxPropertyProvider` understands** (string, `Point`, `Size`,
    `Padding`, `Font`, `Color`, `Boolean`, numbers, enum flags). Any other declared type is
    skipped rather than guessed at, so an exotic property simply does not appear.
  - a form that calls `ApplyResources` but has **no `.resx`** beside it converts with those
    properties missing - reported as a warning naming the form, once, rather than silently.
- **Images come out of the `.resx`, best-effort.** A `PictureBox.Image` (and a `NotifyIcon.Icon`)
  is stored as a BinaryFormatter-serialized `System.Drawing.Bitmap`. BinaryFormatter cannot run
  on modern .NET, so `ResxImageExtractor` recovers the file by scanning the decoded payload for a
  PNG/JPEG/GIF/BMP/ICO header and slicing from there - writing the result to
  `Assets/{field}_{property}{ext}`. Consequences:
  - a payload in any other format is **not** written, and is reported instead: pointing at an
    asset the conversion never produced would throw at run time, not at build time;
  - the recovered file can carry a byte or two of serializer trailer, which every decoder
    ignores;
  - `ImageList`, and images referenced from a `.resources`/satellite assembly rather than the
    form's own `.resx`, are still not handled.

## Control mapping

- **`NotifyIcon`** has an `Unsupported` (guidance-only) *mapping* entry, because Avalonia's
  tray-icon support (`TrayIcon`) lives in `App.axaml`'s `TrayIcon.Icons` collection - an
  app-level (not per-Form) concept that doesn't fit the per-View `AxamlEmitter` model. The
  feature itself works: `ConversionPipeline` aggregates every form's `NotifyIcon`s and
  `AvaloniaProjectScaffolder` emits them into `App.axaml`. The icon file is resolved either from
  a literal `new Icon("app.ico")` path or - the common case - from the form's `.resx`, and copied
  into `Assets/`. An icon that is neither (a computed `Icon`, or a payload
  `ResxImageExtractor` cannot decode) emits the `TrayIcon.Icons` block commented out with a
  TODO, because Avalonia resolves `TrayIcon.Icon` at run time: referencing an asset the
  conversion cannot produce builds fine and then throws `FileNotFoundException` out of
  `App.Initialize()`, before any window opens.
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
- **Designer-declared list entries are translated; everything else about item content is not.**
  `comboBox1.Items.AddRange(new object[] { "A", "B" })` and `listBox1.Items.Add("A")` become real
  `ComboBoxItem`/`ListBoxItem` children. Gated on the *target* element
  (`Mapping/AvaloniaItemsSupport`), like the styling pass and for the same reason: an item element
  the target does not accept is an AVLN error. Consequences:
  - only plain **literals**; an `Items.Add(someObject)` entry is not a literal and is skipped;
  - a **fallback** control takes no item elements (`DomainUpDown` is the one that has them in
    practice), and neither does any target not listed in that table - those entries are
    **reported** rather than dropped silently, since a missing list is visible content;
  - it is static content, not a binding. `ItemsSource`/`SelectedItem` still need a hand-written
    `ObservableCollection` - only the *selection index/item* properties are in the bindable
    catalog.

  A `ListView` picks its target per-instance (`ListViewMapper`: `View=Details` or any parsed
  `ColumnHeader` children → `DataGrid` with real columns, otherwise `ListBox`) and a
  `MonthCalendar` maps to `Calendar`, but neither gets its rows or selection ranges. All six `DataGridView` column types are translated now: `TextBox`/`CheckBox` to
  Avalonia's two real column types, and `ComboBox`/`Button`/`Image`/`Link` to a
  `DataGridTemplateColumn` with a generated cell template. Those templates are **unbound** -
  Designer.cs records the column but not its `DataPropertyName`-to-view-model mapping - so each
  one carries a `TODO` comment naming what to add.

- **Visual styling is converted, but gated by the target element.** `BackColor`, `ForeColor`,
  `Font` and `Padding` are emitted as `Background`/`Foreground`/`FontFamily`/`FontSize`/
  `FontWeight`/`FontStyle`/`TextDecorations`/`Padding` - universally, on any control, the same
  way `ToolTip.Tip` is, since every WinForms `Control` has them. What actually reaches the
  AXAML is decided by the *Avalonia* element, through `Mapping/AvaloniaStylePropertySupport`:
  a `Panel` (what every WinForms container maps to) has a Background but no Foreground and no
  font properties, and an `Image` (the `PictureBox` target) has none of them - emitting one
  anyway would be an AVLN2000 in the generated project. Consequences worth knowing:
  - a **fallback control** gets no styling at all (its template does not necessarily expose
    those properties - the same reasoning that stops fallback controls being event-wired), and
    neither does a generated **UserControl** View element;
  - a **new mapper target** gets no styling until its element name is added to that table;
  - a value `ExpressionEvaluator` cannot resolve to a literal (a computed color, a resx
    lookup, a `SystemColors` name outside the table) emits **nothing** rather than a guess;
  - font sizes are converted points → device-independent pixels at the fixed 96/72 ratio, and
    `SystemColors.*` resolve through a hand-written ARGB table rather than the host desktop
    palette, so the output stays byte-identical across machines.

  What is *not* converted: `BackgroundImage`, `FlatStyle`/`FlatAppearance`, `BorderStyle`,
  `TextAlign`, `RightToLeft`, and `Font` values whose family/size are not literals.

  Because those literal colors are light-mode colors, the generated `App.axaml` pins
  `RequestedThemeVariant="Light"` instead of following the OS. Otherwise a control that set
  only its `ForeColor` (black text, background left to the framework) would render
  black-on-dark for a user in dark mode - a regression the conversion itself introduced.
  Remove the attribute once the generated views use theme resources instead of fixed colors.

## Code-behind and ViewModel generation

The split between event-driven code-behind and MVVM is decided per handler by
`FormMigrationPlanner`, from a Roslyn analysis of the handler's actual body
(`CodeBehindAnalyzer`). **Code-behind is the default**; a `[RelayCommand]` is the
evidence-backed exception. See the "Code-behind migration" section of `README.md` for the
rule itself; what follows is what the rule does *not* cover yet.

- **Handler bodies are translated statement by statement, and only where that is provable.**
  `HandlerBodyRewriter` recognizes a small, closed set of statement forms and emits them as real
  Avalonia code; everything else stays in the comment block with the
  `MigrationTodo.NotMigrated(...)` marker below it. What it translates today:
  - a write to a control property in `BindablePropertyCatalog`, on a `Direct`-mapped control
    (`this.label1.Text = ...` → `label1.Text = ...`, `Checked` → `IsChecked`, and so on), plus
    reads of those same properties anywhere in the expression;
  - `Close()` / `Show()` / `Hide()` on the form (the View *is* the Window), and
    `control.Focus()`;
  - `MessageBox.Show(text[, caption])` → the bundled `MessageBoxFallback`, which makes the
    generated handler `async`;
  - `Application.Exit()` → the desktop lifetime's `Shutdown()`;
  - opening another converted Form: `new SettingsForm().ShowDialog([owner]);` →
    `await new SettingsView().ShowDialog(this);` (async, and the target View's namespace is
    imported), and `new SettingsForm().Show();` → `new SettingsView().Show();`. The generated
    View sets its own DataContext, so the call needs nothing the original did not have;
  - anything else in the expression that is plain .NET (`int.Parse`, `string.Empty`,
    `Math`/`Convert`/`DateTime` statics, literals, operators), including **interpolated strings** -
    every hole is translated like any other expression, and one un-translatable hole rejects the
    whole string rather than producing a half-converted message.

  Everything outside that list stops the translation, including: local variable declarations,
  `if`/`foreach`/`try` and any other non-expression statement, calls to code-behind helpers,
  control APIs with no bindable counterpart (`treeView1.Nodes.Add`), properties on
  *fallback*-mapped controls, `DialogResult`, and unrecognized static receivers
  (`Clipboard`, `Cursor`, ...). The `MessageBox.Show` overloads that take buttons or icons are
  deliberately excluded: they return a `DialogResult` the caller branches on.

  Navigation has two further gaps, both for the same reason - the result would have to be
  reasoned about, not just re-spelled:
  - `if (new SettingsForm().ShowDialog() == DialogResult.OK) { ... }`, the shape most WinForms
    code actually uses. Avalonia's `ShowDialog` returns a `Task<T>` whose result is whatever the
    dialog passed to `Close(result)`, and the converted dialog does not pass one yet, so
    translating the call without the branch would silently change the control flow.
  - `var dialog = new SettingsForm(); dialog.ShowDialog();` - locals are not supported at all.

  A Form constructed with arguments is never translated either (the generated View's constructor
  takes none), and a converted **UserControl** cannot translate `ShowDialog` at all, since
  Avalonia needs a `Window` to own a modal dialog and a UserControl is not one - `Show()` still
  works there.

  **Translation stops at the first statement it cannot handle**, and the rest of the body stays
  commented. Emitting statement 1 and 3 while dropping 2 would produce a method that looks
  migrated but silently skips work; a prefix is a faithful partial execution of the original.
  The conversion report says how many statements came across in total.

  Reads of string properties are emitted as `(control.Text ?? string.Empty)`: WinForms' string
  properties never return null while Avalonia's are `string?`, so this is both the faithful
  translation and what keeps the generated project's nullable analysis quiet.

  The `MigrationTodo` marker reports instead of throwing on purpose - Avalonia raises these
  events from the framework, including during XAML initialization, so a throwing stub made the
  converted app unlaunchable; flip `MigrationTodo.ThrowOnUnmigratedCall` to get strict failure
  back. It is emitted only when something is actually left to migrate. The one exception is the
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
- **`CanExecute` is derived, but only from the one shape that provably means a guard.** A handler
  whose *entire body* is `someButton.Enabled = <condition>;`, that ignores sender/EventArgs, is
  wired to a single control, and whose button already became a `[RelayCommand]`, is folded into
  that command: the condition is translated against the ViewModel's properties and emitted as
  `[RelayCommand(CanExecute = nameof(CanX))]` + `private bool CanX() => ...;`, every property it
  reads gains `[NotifyCanExecuteChangedFor]`, and **the handler and its subscription are
  removed** - the bindings now do its job declaratively. The button's `IsEnabled` is deliberately
  *not* bound as well: `CanExecute` owns it, and a second binding would fight it.

  Not derived when: the handler does anything else besides that assignment (splitting the body
  would be an unprovable rewrite, so it keeps its imperative `IsEnabled` write instead), the
  condition does not translate completely, the control has no promoted command, or something
  already binds that button's `IsEnabled`.
- **Item sources are still not bound.** See the item-content note above: designer-declared
  literals are emitted as static items, but a real `ItemsSource` needs hand-written code.
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

## Re-converting over output you have already migrated

Conversion is a starting point you then edit by hand, so a second run defaults to **never
destroying an existing file**: identical files are skipped, and a file whose generated content
differs from what is on disk is left alone with the regenerated version written beside it as
`<name>.w2a-new` for you to merge. The conversion summary says how many files that affected;
`--overwrite-all` restores the old clobber-everything behaviour.

This is a per-file safety net, not a merge tool - it cannot combine your edits with newly
generated code, and it does not delete generated files that a later run no longer produces
(a View for a Form you removed from the source project stays behind). `--force` is still
required before anything is written into a non-empty output directory at all.

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
