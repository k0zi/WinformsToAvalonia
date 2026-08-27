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
- **Components have no visual form, but their code can come across.** `Form` and `UserControl`
  artifacts both go through the conversion pipeline (a Form becomes a `Window`, a UserControl an
  Avalonia `UserControl`, and a project's own UserControls are registered as real control mappings
  so a Form hosting one emits the generated View element). A `Component` has nothing to render, so
  it still gets no element - but if its **source names nothing that would not survive**, that
  source is copied into the generated project and the component gets a real field, exactly like
  the in-box ones in `ComponentFieldCatalog`. Designer values are applied, and its own events are
  subscribed: the args type comes from the component's own `event EventHandler`/`EventHandler<T>`
  declaration, since nothing outside the project knows it.

  `Parsing/ComponentSourceAnalyzer` decides, and it **over-rejects on purpose**. The cost is
  asymmetric: a component wrongly refused is reported and left alone, exactly as before this
  existed, while one wrongly accepted breaks the generated build - and there is no semantic model
  here to be sure with. So every simple name the file mentions is checked, not just those in type
  position, against the WinForms control registry and against every other class the project
  declares (which is not carried over with it). A local variable called `Timer` will refuse the
  component; nobody is harmed by that. A custom delegate event is left unsubscribed for the same
  reason - its handler signature cannot be written down with confidence.

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

  A handler *can* reach it: the generated `App` declares one static accessor per emitted icon,
  named after the WinForms field (`App.NotifyIcon1`), and `notifyIcon1.Visible` / `.Text`
  translate to `IsVisible` / `ToolTipText` on it - which is most of what a WinForms app does with
  a NotifyIcon. Only for an icon that **resolved**: an unresolved one has no live `TrayIcon` and
  therefore no accessor, so a handler naming it stays a comment rather than pointing at something
  that is not there. The View needs no `using` for this, because `App` is in the project's root
  namespace and every View in a child of it.
- **File dialogs** have `Unsupported` (guidance-only) *mapping* entries, because Avalonia's
  replacement is an async API called from code, not a control - but the feature works. The
  `if (openFileDialog1.ShowDialog(this) == DialogResult.OK) { ... openFileDialog1.FileName ... }`
  shape is translated **inline** into the picker call (see the handler-body notes below), and a
  dialog opened that way gets no separate method, since nothing would call it. A dialog the
  conversion could not inline still gets its `Show...Async()` helper to call by hand.
  `ColorDialog` and `FontDialog` have no Avalonia built-in equivalent either, but they are
  wrappable: both are translated **inline** onto a bundled dialog this repo ships
  (`ColorDialogFallback`, built around Avalonia's real `ColorView`; `FontDialogFallback`, listing
  `FontManager.Current.SystemFonts`). The print dialogs stay guidance-only - Avalonia has no
  printing API at all, so there is nothing to wrap.
- **Non-visual components that are really plain .NET types survive unchanged.**
  `BackgroundWorker`, `FileSystemWatcher`, `Process`, `SerialPort`, `EventLog`,
  `PerformanceCounter`, `ServiceController` and `SoundPlayer` are the same classes in an Avalonia
  project as in a WinForms one, so the generated View declares a **real field** of the same type
  (`Mapping/ComponentFieldCatalog`), reproduces the designer's literal property values in its
  constructor, and **subscribes the component's events** - which is what closes the old "the
  handler is emitted but nothing subscribes it" gap for this group. Handler bodies may then say
  anything about such a field, nested paths (`process1.StartInfo.FileName`) included: a member of
  an unchanged .NET object is ordinary .NET, the same argument that allows members of a
  translated local. That is why this table has no per-member whitelist, unlike every other
  catalog here - nothing is being *translated*.

  What it costs, and what is therefore bounded:
  - **evidence, as everywhere else**: a component gets a field only if a designer-wired event or
    a handler body actually uses it. Declaring the rest would add fields, package references and
    platform constraints for objects the converted app never touches;
  - **only literal designer values** are reproduced. A resource lookup or computed expression has
    no faithful spelling here, so it is reported rather than guessed at;
  - **four of them need a NuGet package**, and a package must be listed *both* in that catalog
    and in `AvaloniaProjectScaffolder.ExtraPackageVersions` - the csproj writer emits only what
    the second one names, so a package in one and not the other is silently dropped;
  - **`EventLog`, `PerformanceCounter`, `ServiceController` and `SoundPlayer` are Windows-only**
    (`[SupportedOSPlatform("windows")]`), and the generated project targets plain `net10.0`. Two
    consequences, and the second one is a correctness rule rather than a cosmetic one:
    - the emitted View carries `#pragma warning disable CA1416` for the whole file, because those
      uses are spread across the field, the constructor and whichever handlers touch them rather
      than confined to one line. Scoped to the one file that needs it, never the project, and the
      conversion **reports each such component by name**;
    - they are **built lazily**, behind a property over a nullable backing field, while
      cross-platform components keep an ordinary eager field. The View's constructor runs inside
      `OnFrameworkInitializationCompleted`, before the first window exists, and `new EventLog()`
      throws `PlatformNotSupportedException` from its *constructor* off Windows - so an eagerly
      built one made the whole converted app unlaunchable on Linux and macOS instead of failing
      where the original code used it. Same reasoning as `MigrationTodo` reporting rather than
      throwing. A designer-wired event on such a component is therefore subscribed on first use
      rather than at construction.

    `SerialPort` is not in this group - it looks Windows-shaped and is genuinely cross-platform;
  - **`Timer` is not in this table** - it is the one component whose target type is *different*
    (`DispatcherTimer`), with its own event wiring and start semantics, so it keeps its own plan.

  Everything else (`BindingSource`, `ImageList`, the dialogs, ...) is still collected into
  `FormModel.Components` by `ControlGraphBuilder` (anything never `Controls.Add`-ed) and run
  through `ControlMappingRegistry.Map` by `ConversionPipeline.Run`; any `Fallback`/`Unsupported`
  result's guidance text is added to the conversion report's warnings - so it surfaces during a
  real conversion, not only in the static `list-mappings` reference table. Those never get a
  visual element or ViewModel stub, since there's nothing to render. `ToolTip` is the one exception: the component field
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
  - a **fallback control** gets no styling from the *designer* (a template does not necessarily
    expose those properties - the same reasoning that stops fallback controls being event-wired),
    and neither does a generated **UserControl** View element. A translated *handler* is the one
    exception, and only for what `FallbackControlMemberSupport` says the template really has:
    that is how a `FontDialog` result reaches a `RichTextBox`, which derives from Avalonia's
    `TextBox` and so inherits the four font properties for real;
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
    reads of those same properties anywhere in the expression. A **read** is converted back to the
    type the WinForms expression had, which is not always the type the Avalonia member is: a
    `{Binding}` converts on its way to the element, but a translated code-behind statement touches
    the member directly. So `Checked` reads as `(x.IsChecked ?? false)` and a Button's `Text` as
    `(x.Content as string ?? string.Empty)` - both faithful, since a two-state `IsChecked` is
    never null and `Content` holds the string this conversion put there. A **three-state**
    CheckBox is refused instead, because WinForms reports Indeterminate as `Checked == true` and
    no coalescing says that. A control the AXAML emits **without an `x:Name`** (a DataGrid column,
    which is a description of a column rather than an element) has no field to name at all, so
    nothing about it translates;
  - `Close()` / `Show()` / `Hide()` / `Activate()` on the form (the View *is* the Window). Only
    `Show()` and `Hide()` survive on a converted **UserControl**, and as a visibility write rather
    than a call: Avalonia's `UserControl` has no `Close`/`Show`/`Activate` at all, so the others
    would not compile there - and WinForms' `Control.Show()` always meant `Visible = true` anyway;
  - the control
    methods in `Mapping/ControlMethodCatalog`: `Focus()`/`Select()`, `Invalidate()`/`Refresh()`,
    `Hide()`, and on the TextBox family `Clear()`, `SelectAll()` and `AppendText(x)`. The last is
    the only entry whose equivalence is of *content* rather than of everything the method did:
    Avalonia has no `AppendText`, and `Text += x` appends exactly the same characters but does not
    reproduce WinForms' side effect of moving the caret to the end (so a converted log box does
    not auto-scroll). It is also why the table names the Avalonia member each entry *touches*
    rather than the WinForms method it came from - `AppendText` reaches `Text`, which a fallback
    template exposes even though it has no `AppendText` of its own, so a `RichTextBox` can carry
    the call too. An overload with a different arity is a different method and is not translated;
  - `var button = (Button)sender!;`. Wired to exactly one control, `sender` provably *is* that
    control, so the local becomes another name for its field and the declaration disappears.
    Wired to **several** - one handler on N buttons, which is how WinForms shares a handler at
    all - there is no field to alias, so the cast survives against the *Avalonia* element type,
    and the local stands for a control of the type they share. That needs every wired control to
    map to the same element; mixed types stay refused, since telling them apart is the whole
    reason such a handler reads `sender`. Either way the cast has to name the control's own
    WinForms type - a base-type cast (`(Control)sender`) is refused rather than widened;
  - the text-entry family's own properties, which are plain renames: `Multiline` →
    `AcceptsReturn`, `ReadOnly` → `IsReadOnly`, plus `MaxLength` and `SelectionStart` unchanged.
    One of them is not a rename at all: `WordWrap` is a `bool` in WinForms and a `TextWrapping`
    enum in Avalonia, so the **value** is rewritten too (`... ? TextWrapping.Wrap :
    TextWrapping.NoWrap` writing, `== TextWrapping.Wrap` reading). A property in that shape is
    deliberately **not** two-way bindable - a `{Binding}` has no converter in between, so it
    cannot carry a promoted `[RelayCommand]` and stays in code-behind, where the conversion is
    written out. A compound assignment to one is refused, since that reads it as well as writing
    it and a read cannot be spliced into a left-hand side;
  - `tabControl1.SelectedTab?.Text`, as
    `((tabControl1.SelectedItem as TabItem)?.Header as string)`. Provable because the
    `TabPage` → `TabItem` mapping is this converter's own: a non-null `SelectedItem` *is* a
    TabItem, because the conversion made every page one. Only the `?.` form - WinForms'
    `SelectedTab` is non-null whenever the control has pages, so `SelectedTab.Text` throws on an
    empty TabControl and any translation of it would quietly return an empty string instead;
  - the Form's own properties that a `Window` spells differently or not at all
    (`Mapping/WindowPropertyCatalog`): `Text` → `Title`, `TopMost` → `Topmost`, `WindowState`
    (with `FormWindowState.Maximized` → `WindowState.Maximized`), `ShowInTaskbar`, `Opacity` -
    written bare or through `this`, and on a local holding another converted Form's View
    (`dialog.Text = "About";`). Not on a converted **UserControl**, which has no title.
    `Size`/`Width`/`Height` are deliberately absent: WinForms measures the outer frame including
    the title bar and borders, Avalonia does not, and there is no fixed conversion between them -
    only a guess that would silently resize every converted window. `FormBorderStyle`,
    `ControlBox` and `StartPosition` are out for the sharper version of the same problem: their
    Avalonia counterparts (`SystemDecorations`, `CanResize`, `WindowStartupPosition`) map
    many-to-many, not one-to-one;
  - the **`DispatcherTimer` this conversion creates itself** for a WinForms `Timer`
    (`Mapping/DispatcherTimerMemberCatalog`): `Enabled` → `IsEnabled`, `Start()`/`Stop()`
    unchanged, and `Interval = n` → `Interval = TimeSpan.FromMilliseconds(n)`. Only for a Timer
    the plan actually emits - one with no `Tick` handler never becomes a field, so naming it
    would produce code referring to something that does not exist. `Interval` is **write-only**:
    WinForms counts int milliseconds and Avalonia holds a `TimeSpan`, so a write can be wrapped
    faithfully but `if (t.Interval > 500)` would compile and quietly mean something else;
  - `MessageBox.Show(text[, caption])` → the bundled `MessageBoxFallback`, which makes the
    generated handler `async`. The owner overloads (`Show(this, text[, caption])`) work too: a
    literal leading `this` is dropped, since the translated call supplies its own owner. Only a
    literal `this` - that is what keeps the arity unambiguous, because `Show(text, caption)` and
    `Show(owner, text)` are otherwise the same shape. The **two-button** overloads are translated
    too, and the whole comparison collapses the way the converted-dialog contract does:
    `MessageBox.Show(text, caption, MessageBoxButtons.YesNo) == DialogResult.No` becomes
    `!await MessageBoxFallback.ShowYesNoAsync(this, text, caption)`. That works because the dialog
    on the other end is one this repo ships, so it can be given a `bool` return. `OKCancel` too;
    `YesNoCancel` and `AbortRetryIgnore` refuse, having no bool answer, and so do the icon
    overloads - the bundled dialog draws no icon, and accepting them would silently drop a cue;
  - `Application.Exit()` → the desktop lifetime's `Shutdown()`;
  - opening another converted Form: `new SettingsForm().ShowDialog([owner]);` →
    `await new SettingsView().ShowDialog(this);` (async, and the target View's namespace is
    imported), and `new SettingsForm().Show();` → `new SettingsView().Show();`. The generated
    View sets its own DataContext, so the call needs nothing the original did not have;
  - anything else in the expression that is plain .NET (`int.Parse`, `string.Empty`,
    `Math`/`Convert`/`DateTime` statics, literals, operators), including **interpolated strings** -
    every hole is translated like any other expression, and one un-translatable hole rejects the
    whole string rather than producing a half-converted message. A call on one of those types is
    taken as a **statement** too, not just as a value - `Thread.Sleep(100);`,
    `File.WriteAllText(path, text);` - which is the shape a handler reaches for right after a save
    dialog;
  - **`if`/`else`** (and a bare `return;`), when the condition *and every branch* translate.
    Braces are always emitted, even where the original had none, so a rewritten branch can never
    change what an `else` binds to; `else if` keeps its shape rather than becoming a nested block;
  - **`control.BackColor` / `ForeColor`** → a `Background`/`Foreground` **brush**
    (`new SolidColorBrush(Color.Parse("#AARRGGBB"))`). The colour goes through the same
    `ExpressionEvaluator` + `PropertyValueFormatters.AsBrush` pair the designer path uses, so a
    colour written in a handler and the same colour written in the designer cannot come out
    differently - and anything they cannot resolve to a literal (a computed colour, another
    control's `BackColor`) is refused rather than guessed at, exactly as in the AXAML. Gated on the
    *element* through `AvaloniaStylePropertySupport`, the same table `AxamlEmitter` consults: a
    `Panel` has a Background but no Foreground, an `Image` has neither, and a **fallback** control
    gets no styling at all. `Font` is deliberately absent - one WinForms value becomes three
    Avalonia properties, which is a different shape of problem;
  - **`errorProvider1.SetError(control, "message")`** → `ErrorProviderFallback.SetError(control, "message")`.
    The one translation whose result is a *static* call on a bundled fallback: everywhere else a
    fallback is something the AXAML instantiates and the handler then talks to, but a WinForms
    `ErrorProvider` has no element at all and its counterpart is an attached property set from
    outside. That is also why it cannot live in `ControlMethodCatalog`, which names members of the
    *target* control. Like `MessageBox.Show`, it pulls the template in from a **handler body**
    rather than from an element. The control it flags has to be one the AXAML really names, or the
    generated View has no field to hand over;
  - **`sender`, on a handler wired to exactly one control.** `var button = (Button)sender;`
    does not become a cast - it becomes *nothing*. In a single-control handler `sender` provably
    is that control, so the local is recorded as another name for its field and every later use
    (`button.Text`) resolves through the ordinary control path. That sidesteps the reason `sender`
    was untranslatable before: casting it correctly needs the Avalonia element type, which is
    exactly what a syntax-only tool does not have. The cast must name the control's own WinForms
    type - a widening `(Control)sender` is refused rather than accepted, since it would let the
    translated code claim something the original did not - and a handler shared by two controls
    has no single answer, so it keeps its comment;
  - **`Clipboard.SetText(x)`** → `await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(x)`,
    which makes the handler `async`. `Clipboard.GetText()` is not translated: Avalonia's
    counterpart returns a `Task`, so a read that can appear anywhere in an expression would need
    the whole expression restructured. Like a message box, this now blocks ViewModel promotion -
    the replacement hangs off the `TopLevel`, which a View has and a ViewModel does not;
  - **`EventArgs` members**, through `Mapping/EventArgsMemberCatalog`. Three kinds show up and
    only two are translated: a member Avalonia's own args type spells identically (`Cancel` on
    `WindowClosingEventArgs`, `NewValue` on `ScrollEventArgs`), and a member of an args type that
    is plain .NET and reached the generated project untouched (`FileSystemEventArgs`, the
    BackgroundWorker ones). The pointer position is the one genuinely *translated* member -
    WinForms' `e.X`/`e.Y` are relative to the control that raised the event, which is exactly
    what `GetPosition(control)` takes, so it needs a handler wired to exactly one control.
    Anything with no exact answer (`DataGridViewCellEventArgs.RowIndex`, whose Avalonia
    counterpart reports a cell object rather than an index pair) is left for a human.

    **Drag and drop** is where the two frameworks diverge most, and only the one shape with an
    exact answer is translated: `e.Effect` → `e.DragEffects` (the `DragDropEffects` members both
    declare - `Scroll` and `All` are WinForms-only and refuse), and
    `e.Data.GetDataPresent(DataFormats.FileDrop)` → `e.DataTransfer.Contains(DataFormat.File)`.
    That second one is matched as a whole shape because not one part of it survives alone:
    Avalonia 12 renamed the property, changed its type (`IDataObject` → `IDataTransfer`, with
    different method names) and replaced the format constants. `e.Data` is deliberately *not* a
    pass-through member for the same reason - letting it through would emit an `IDataObject`
    method against a type that has none. Reading the payload (`GetData`) is left alone: Avalonia
    hands back storage items rather than a `string[]`, which is a change of shape, not spelling.

    Plain `EventArgs` is deliberately **not** treated as pass-through: it is the fallback the
    planner uses when an event has no Avalonia equivalent, so it means "unknown type", and the
    body will be reaching for members of the richer WinForms type it was written against;
  - **`foreach`/`for`/`while`**, when the collection/header *and the whole body* translate. The
    loop variable is scoped to the loop, and `i++`/`i += n` on a local are translated (a local
    holds a plain .NET value, so any operator on it is ordinary .NET). A `for` with a
    comma-separated initializer list is refused;
  - **`?.` and `??`** on something that already translates to a value. The receiver translating as
    an *expression* is exactly what makes the rest safe: everything this rewriter can produce as a
    value is a plain BCL value, so the members hanging off it are ordinary .NET. A control field
    is not a value, so `textBox1?.Text` is refused rather than quietly reinterpreted as the
    property path with the null-check dropped - and the `?.` itself is always preserved, since the
    receiver is only *sometimes* provably non-null. Only member accesses and zero-argument calls
    in the chain: an argument could name a control, and the chain is copied verbatim, so
    `s?.StartsWith(this.prefixBox.Text)` is refused rather than half-rewritten;
  - **local variables**, declared `var` or with a keyword type, when the initializer translates.
    Members of a local are then allowed for the same reason members of a control property are: a
    translatable initializer can only produce a plain .NET value. Locals are block-scoped, so one
    declared inside an `if` branch cannot be used after it. `var dialog = new SomeForm();`
    declares a *View* rather than a value - only the navigation calls accept it - and a
    `using var` on that shape drops the `using`, since an Avalonia Window is not IDisposable and
    there is no disposal to preserve. A `using` on anything else is refused rather than silently
    dropped. `const` locals are not translated.

  Everything outside that list stops the translation, including: `switch`, `try`/`catch`,
  `do`/`while`, `lock`, `using` blocks, calls to code-behind helpers, `Paint`/`Graphics` drawing,
  control APIs with no bindable counterpart (`treeView1.Nodes.Add`), properties on
  *fallback*-mapped controls, and unrecognized static receivers
  (`Clipboard`, `Cursor`, ...). The `MessageBox.Show` overloads that take buttons or icons are
  deliberately excluded: they return a `DialogResult` the caller branches on.

  **The dialog-result contract.** `if (new SettingsForm().ShowDialog(this) == DialogResult.OK)`
  translates to `if (await new SettingsView().ShowDialog<bool>(this))`, because both halves are
  generated together:
  - on the **dialog** side, a control the designer gave a `DialogResult` and no Click handler
    gets one synthesized that calls `Close(true)` (OK/Yes) or `Close(false)`. That is WinForms'
    one piece of designer-declared behaviour - such a button closes its form with no handler
    existing - and Avalonia has nothing equivalent, so it has to become real code. Skipped when
    the designer wired a Click handler of its own, since prepending a Close would change what
    that handler does, and on a UserControl, which has no window to close;
  - on the **caller** side, `== DialogResult.OK` and `!= DialogResult.Cancel` become the awaited
    call, the other two its negation. Only OK and Cancel: a three-way Yes/No/Cancel dialog cannot
    be expressed as a `bool`, and widening the result type would change what every converted
    dialog returns. A dialog closed by its title bar yields `default(bool)` - false - which is
    what WinForms reports for that case too.

  The **hand-written** side of that contract is translated too: `DialogResult = DialogResult.OK;`
  - with or without the `Close();` that usually follows - becomes a single `Close(true)`.
  Matching the *pair* is what makes this correct rather than convenient: translated one statement
  at a time, that trailing bare `Close()` would close the window with `default(bool)` and
  overwrite the result the line above had just set. The two statements are one act.

  Only at the very **end** of the body, and only for a value with a faithful bool (OK/Yes/Cancel/No).
  In WinForms, assigning `DialogResult` on a modal form closes it but the handler keeps running;
  Avalonia's `Close` is the last thing that happens. Where the original still had work to do
  afterwards the two are not equivalent, so the assignment simply does not translate and the
  prefix stops there. A handler that writes `DialogResult` also never promotes to a ViewModel -
  bare or `this.`-qualified alike - since a ViewModel has no window to close and promoting it
  would move the body somewhere it cannot be translated at all.

  **Component file dialogs** look identical at the call site but are not Forms, and take a
  different route: `if (openFileDialog1.ShowDialog(this) == DialogResult.OK)` becomes
  `if (await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()) is [var f, ..])`,
  and `openFileDialog1.FileName` inside that branch becomes `f.Path.LocalPath`. This is the one
  translation that changes an expression's *shape* rather than re-spelling it: Avalonia has no
  dialog object to ask afterwards, so the picker's return value *is* the selection. A list
  pattern keeps it inline, which also scopes the binding to exactly the branch that used to read
  the dialog. `SaveFileDialog` returns a single nullable file and uses `is { } f` instead.
  What is *not* covered there:
  - the designer's `Filter`/`InitialDirectory`: the picker is opened with default options,
    because parsing a WinForms filter string (`"Text|*.txt|All|*.*"`) into `FileTypeFilter`
    entries and getting it subtly wrong is worse than leaving it out;
  - `FileNames` (multi-select) - there is no single path to bind, so the whole branch is left;
  - any use of the dialog's properties *after* the branch, since the selection is a pattern
    variable now rather than an object that outlives the call;
  - print dialogs, which have no Avalonia equivalent at all.

  **The colour and font dialogs** take the same inline route, onto bundled windows rather than a
  platform API: `if (colorDialog1.ShowDialog(this) == DialogResult.OK)` becomes
  `if (await ColorDialogFallback.ShowAsync(this) is { } colorDialog1Color)`, and
  `colorDialog1.Color` inside that branch becomes the pattern variable. `ColorDialogFallback`
  wraps Avalonia's real `ColorView`, which ships in `Avalonia.Controls.ColorPicker` - the first
  time a *bundled template* has needed a package, so `FallbackTemplateDefinition` now declares one
  and it goes through the same double-allowlist as a mapper's. What is not covered:
  - the dialog opens on its **default** value. WinForms seeds `ColorDialog` from the component's
    own `Color`, and carrying that across would mean reading a designer value nothing else in the
    translation needs - the same reason the file pickers open with default options;
  - `control.Font = fontDialog1.Font` is the *only* font assignment translated, and it becomes
    four writes (`FontFamily`/`FontSize`/`FontWeight`/`FontStyle`). Provable only because the
    value comes from a record this repo ships; an arbitrary WinForms `Font` expression would need
    its family and size resolved to literals first. It also obeys the styling rule, so a font
    landing on a **fallback** control is refused - which is why the sample's
    `notesRichTextBox.Font` stays un-migrated;
  - `FontDialogFallback` offers family, size, bold and italic only. Underline and strikeout are
    `TextDecorations` in Avalonia rather than part of the font.

  A Form constructed with arguments is never translated either (the generated View's constructor
  takes none), and a converted **UserControl** cannot translate `ShowDialog` at all, since
  Avalonia needs a `Window` to own a modal dialog and a UserControl is not one - `Show()` still
  works there.

  **Translation stops at the first statement it cannot handle**, and the rest of the body stays
  commented. Emitting statement 1 and 3 while dropping 2 would produce a method that looks
  migrated but silently skips work; a prefix is a faithful partial execution of the original.
  The conversion report says how many statements came across in total. A local declaration left
  at the very end of a partial prefix is dropped from it: the statements that would have used it
  are exactly the ones that did not translate, so it is dead by construction.

  That prefix rule applies at the **top level only**. Inside an `if` branch or a loop body it is
  all-or-nothing,
  because the un-migrated remainder is emitted *after* the whole statement - a partly translated
  branch would silently drop its own tail with nothing at that spot to say so. The practical
  consequence is that one un-translatable call in a branch rejects the entire `if`, which is why
  control-flow support helps validation-shaped handlers far more than WinForms-API-shaped ones.

  **Nothing that needs `await` is translated inside a `Closing` handler**, whatever it is, with
  one named exception. A cancellable event is read the moment the handler returns, and an
  `async void` handler returns at its first `await` - so `e.Cancel = await ...` would compile,
  look right, and never cancel anything, because the window is gone by the time the await resumes.
  WinForms got away with this only because its dialogs block. So the statement is refused and the
  prefix before it still comes across: the one place where a *correct* translation of a statement
  is still rejected because of where it sits.

  The exception is **confirm-on-close**, `e.Cancel = MessageBox.Show(..., YesNo) == DialogResult.No;`,
  which is the shape that motivates almost every `FormClosing` handler ever written. There is no
  statement-level answer for it, so the whole body is rewritten into Avalonia's idiom instead:
  cancel the close, await the answer, and on "yes" close again from code, guarded by a
  `w2aForceClose` field so the second pass returns immediately. That changes **when** the window
  closes - one turn of the loop later - and nothing else: because the guard returns straight away,
  the statements around the confirmation still run exactly once per close attempt, in their
  original order, whether you confirm or cancel. The generated code says so in a comment.

  This is the only whole-body rewrite in the converter, and it is deliberately narrow: any prefix
  that translates without awaiting, then `e.Cancel = <expr>` (bare, or as the single statement of
  an `if` with no `else`) whose expression awaits, then any tail that translates without awaiting.
  A tail that does *not* translate takes the whole shape with it, since emitting the confirmation
  without it would drop work the original did on every attempt - which is why the all-in-one
  sample's own closing handler still does not convert: it ends in `notifyIcon1.Visible = false`,
  and a `NotifyIcon` has no per-View element to write to.

  Reads of string properties are emitted as `(control.Text ?? string.Empty)`: WinForms' string
  properties never return null while Avalonia's are `string?`, so this is both the faithful
  translation and what keeps the generated project's nullable analysis quiet.

  The `MigrationTodo` marker reports instead of throwing on purpose - Avalonia raises these
  events from the framework, including during XAML initialization, so a throwing stub made the
  converted app unlaunchable; flip `MigrationTodo.ThrowOnUnmigratedCall` to get strict failure
  back. It is emitted only when something is actually left to migrate. The one exception is the
  generated file-dialog helper methods, which still throw: nothing calls them until a human
  wires them up, so they can never fire on their own.
- **Helper methods are translated too, but only whole.** A private helper (`SetBusy`, `Log`,
  `Describe`) whose **entire** body translates is emitted as real code on the View, and the
  handlers that call it translate with it. Everything else about it stays as today's comment
  block.

  Whole, never a prefix - and that is the one rule worth understanding here. The prefix rule that
  makes a partly-migrated *handler* honest works because the un-migrated remainder sits in a
  comment directly below the code that did translate; a helper has no such place. At its call
  site there would be nothing at all, so a half-translated `SetBusy` would look migrated
  everywhere while silently skipping half its work.

  The **private fields** a helper maintains come across with it - the canonical WinForms pair is
  a `SetBusy(bool)` keeping an `isBusy` flag, and without the field neither the helper nor its
  callers can translate. Keyword types with a literal initializer only, the same bar a helper's
  parameters and a translated local have to clear.

  Translation runs to a **fixed point**, because a helper may call another: a call to one that is
  not promoted *yet* simply fails, so that helper waits for the next round, and when nothing new
  promotes the remainder never will. Recursion needs no special guard - a helper is never in the
  promoted set while its own body is being translated, so a self-call, or a mutually recursive
  pair, refuses on its own. `async` settles the same way: a helper that awaits a message box
  becomes `async Task` (never `async void`, which its callers could not await) and makes every
  caller await it in turn.

  Not covered: a helper with a **named type** anywhere in its signature (it could be a WinForms
  type whose Avalonia counterpart is a different type entirely); a generic one, an
  expression-bodied one, or one with a `ref`/`out`/`params` parameter; a **value-returning helper
  that turns async**, whose `Task<T>` would only be usable inside an expression, which is exactly
  where this converter refuses to await; and nested types and everything else non-handler,
  which stay a comment as before.

- **A converted View carries its public properties.** The surface a WinForms UserControl is made
  of - `Caption { get => captionLabel.Text; set => captionLabel.Text = value; }` - comes across as
  a real property on the generated View, which is what lets a hosting Form's handler say
  `counterControl1.Caption = ...`, and a dialog's caller read `dialog.EnteredText`. Expression
  bodies, expression-bodied accessors and block accessors all qualify; an auto-property does not,
  since it is a field wearing a property's clothes.

  **Whole-or-nothing**, exactly as for a helper: both accessors translate or neither is emitted.
  At a use site there is nowhere to put a remainder, so half a property would read as migrated
  while assigning to it quietly did nothing.

  The accessor body may name **only the artifact's own controls** - no timers, components,
  helpers or carried-over fields. That restriction is what lets the whole project's properties be
  resolved in one pass *before* any handler is translated, which is what a handler naming another
  View's property needs (the same problem Form navigation has, one level down). It also covers the
  shape that actually occurs. A named property type is refused for the usual reason: it could be a
  WinForms type whose Avalonia counterpart is a different type entirely.

  A handler that calls a helper **can** be promoted to a ViewModel now, and the helper moves with
  it. Promotion condition 5 used to refuse any helper call outright; it now asks whether the helper
  could live there too, by merging the helper's own requirements into the caller's - transitively,
  cycle-guarded - and running the same six conditions over the union. The pair moves together or
  not at all.

  That merge is what makes it work at all rather than just makes it stricter. A `Log(string)`
  helper writing `logTextBox.Text` is often the *only* thing that touches that control, so under a
  rule that let a helper use only already-bound properties nothing would ever start: the property
  becomes bindable precisely because the helper's access is counted as the caller's. The same step
  is what lets `logTextBox.AppendText(x)` qualify - `ControlMethodCatalog` says the Avalonia member
  it touches is `Text`, which *is* bindable, so the call survives as a property write on the
  ViewModel while `Focus()` correctly does not.

  Two limits worth knowing:
  - a helper the analysis cannot read - recursive, expression-bodied, generic, or with a
    `ref`/`out` parameter - still blocks its caller, now with that as the stated reason;
  - a helper whose **name** is one the generated class already inherits (`Tag`, `Refresh`,
    `Close`, ...) is not emitted on either target and blocks its caller too. It would be a CS0108
    "hides inherited member" warning in the generated project, which this converter's own build
    cannot see - so `Mapping/ReservedMemberNames` refuses the name instead. Hand-maintained and
    conservative, like every table here: the tool has no Avalonia reference to reflect over.

  A helper can end up on **both** sides - the View for the handlers that stayed event-driven, the
  ViewModel for the commands - because the two address the same control differently: a field there,
  a bound property here.
- **Promotion is single-control only.** A handler wired to more than one control needs
  `sender` to tell them apart, so it always stays in code-behind - and when the controls'
  Avalonia events have different signatures (a `Button`'s real `Click` vs. a `Label`'s
  `PointerPressed`), the method is split in two, each named after its Avalonia event.
- **Bindable property coverage is deliberately small** (`BindablePropertyCatalog`):
  `Text`/`Content`/`Header`, `Checked`, `Value`, `SelectedItem`/`SelectedIndex`, `Enabled`,
  `Visible` - across the ordinary controls, the ToolStrip items (which are Direct-mapped, so
  their values are as reachable as any other control's) and `CheckedListBox`. A handler touching
  anything outside that vocabulary stays in code-behind, since the property could not be
  expressed as a `{Binding}` anyway. Real WinForms-only properties (`RichTextBox.WordWrap`,
  `LinkLabel.LinkVisited`) are not in it and are not going to be - they have no Avalonia
  counterpart to name.

  The catalog and the control mappers name the same Avalonia property from two separate tables,
  and a disagreement between them is a *generated-project* build error rather than a tool error -
  `BindablePropertyCatalogTests` asserts they agree, one case per (control type, property). It
  was written for this reason and immediately found one: `LinkLabel.Text` claimed `Text` while
  the mapper emits a `HyperlinkButton`, whose text property is `Content`. Any promoted handler
  touching a LinkLabel produced AVLN2000.
- **Every event a `Control` or a `Form` declares is classified.** WinForms' `Control` declares 71
  events and `Form` another 30 - the ones a designer can wire on anything - and the registry used
  to know about 25 of them by name. The rest got a generic "no Avalonia equivalent is registered",
  which is true, explains nothing, and left no way to tell which events had been thought about.
  All of them now have either a real mapping or a specific sentence saying why there is none, and
  `EventCoverageTests` reads WinForms' own reference assembly to make sure none is missed - the
  same technique the Avalonia side uses, pointed the other way.

  Most of them are the `XxxChanged` family, and they share one answer: Avalonia raises nothing
  when a property changes, so you observe the property or bind to it. Three real pairs came out of
  the sweep that had been missing: `Form.Move`/`LocationChanged` → `Window.PositionChanged`,
  `Form.DpiChanged` → `TopLevel.ScalingChanged`, and the obsolete `Form.Closing`/`Closed`
  spellings, which plenty of older designer files still use and which mean exactly what
  `FormClosing`/`FormClosed` do.

  **Type-specific events are deliberately not in that guarantee.** `DataGridView` alone declares
  126 - more than `Control` and `Form` together - and Avalonia's DataGrid is a different shape
  almost throughout. Those get mappings one at a time, when a real counterpart can be proven
  against the reference assembly; `ComboBox.DropDown` → `DropDownOpened` is the first.

- **The mapping tables are checked against Avalonia itself.** Everything this converter knows
  about Avalonia lives in hand-maintained tables, because the tool emits text and never
  references Avalonia — so a wrong entry used to surface only as a build error in the *generated*
  project. `WinFormsToAvalonia.Mapping.Tests` reads Avalonia's reference assemblies as metadata
  and asserts every claim: element names, emitted attributes, property types, events and the args
  types their handlers are signed with, control methods, style groups, and what a bundled
  template inherits. Its first run found four live defects — `GotFocusEventArgs` (renamed to
  `FocusChangedEventArgs` in Avalonia 12), `LostFocus`/`DragLeave` signed with the wrong args
  type, `Enabled` offered on DataGrid columns that have no `IsEnabled`, and eight value/selection
  events mapped *generically* that only specific elements raise.

  Those value/selection events brought a second problem with them, which only showed up at run
  time: Avalonia raises them **while the AXAML is still being populated** - a TabControl selects
  its first tab as it initialises, a CheckBox raises `IsCheckedChanged` when XAML sets
  `IsChecked` - and the handler attribute is wired *before* those properties are set. So the
  handler runs inside `InitializeComponent`, before a single `x:Name` field exists, and touching
  one is a `NullReferenceException` out of the View's constructor. WinForms had no such window:
  the designer assigned every control field at the top of its own `InitializeComponent`, before
  anything could raise. So a handler firing that early is an artifact of the conversion, and the
  generated View guards against it with a `w2aInitialized` flag set at the end of its
  constructor - only for the handlers whose event can fire that early, which is exactly what
  `EventMapping.RaisedDuringInitialization` records.

  That last one is worth stating plainly, because it changed behaviour: `TextChanged`,
  `CheckedChanged`, `SelectedIndexChanged` and friends exist on every WinForms `Control`, but
  Avalonia raises them on a `TextBox`, a `ToggleButton`, a `SelectingItemsControl`. They are now
  translated **only for the control types whose Avalonia element really has them**; anywhere else
  the conversion reports "not on this one" instead of emitting an attribute that would fail at
  XAML compile time. `NumericUpDown.ValueChanged` and a slider's are two different events with
  two different args types, and the split gets that right too.

- **Fallback controls expose only what their template demonstrably has**
  (`FallbackControlMemberSupport`). Everything else about them stays conservative - no styling,
  no event wiring, no item children - because a template need not have the member a mapping
  names. Catalog members are the one safe exception, since these templates ship in this repo:
  `RichTextBoxFallback` and `MaskedTextBoxFallback` derive from Avalonia's `TextBox`, so their
  `Text`, `Clear()`, `SelectAll()` and the four font properties they inherit from
  `TemplatedControl` are known facts. A template absent from that table behaves as before, and a binding
  dropped because of it is reported rather than emitted as a broken attribute.
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
  `VisibleChanged`) get their handler method emitted but nothing subscribes it; the
  conversion report names each one and why. The non-visual components above are no longer in
  this group: `BackgroundWorker.DoWork` and `FileSystemWatcher.Changed` are subscribed from the
  generated constructor, with the handler declared against the real .NET args type rather than
  the "unknown type" `EventArgs` fallback. `Scroll` is in this group only for controls that
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

## Multiple projects

`--source` takes a `.sln` or `.slnx` as well as a `.csproj`. Every WinForms project the solution
lists is converted into its own folder under the output directory, and a generated `.slnx` ties
them together - which is what `samples/convert.sh` had been doing by hand. A project the pipeline
finds nothing convertible in (a class library, a test project) is reported and skipped rather than
failing the run: a solution with non-WinForms projects in it is the normal case. Solution files are
parsed as text rather than through MSBuild, so a project path built from an MSBuild property is not
understood - and is reported rather than guessed at.

A Form in one project hosting a **UserControl from another** works, which is the case that most
often motivates a multi-project solution in the first place. A pass over the whole solution runs
before any project is converted and predicts what each project's UserControls will be called once
they are Views; a project then resolves the UserControls of the projects **its own csproj
references** - not of every project in the solution, so a control cannot resolve where the C#
compiler would not have seen it either. The hosting View declares that namespace in the
`clr-namespace:Widgets.Views;assembly=Widgets` form (Avalonia's shorter `using:` can only name the
assembly being compiled), and the generated csproj gets a matching `ProjectReference`.

Two edges remain. A project of nothing but UserControls still converts to an **executable** with a
placeholder `MainWindowView` rather than to a library - harmless, since referencing it works
either way, but not what you would have written by hand. And the prediction is by name: if the
referenced project turns out to have no convertible artifacts, or its UserControl is one the
locator could not classify, the host still emits the element and the generated build is what
tells you.

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
