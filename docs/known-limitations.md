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
- **The project's own subclasses of WinForms controls are converted as the control they derive
  from.** `RoundButton : Button` had no mapping, so every instance was left out of the window - its
  handlers and its settings with it. A class deriving (directly, or through other classes of the
  project) from a type the registry maps is now re-typed to that base right after the designer walk,
  before anything else looks at it: the mapper, the event table (a Button subclass's `Click` is a
  real `Click` and can become a command), the bindable-property catalog and the mnemonics all answer
  for the control it really is. What the subclass adds itself - overridden members, its own
  painting, behaviour - is not carried, and each such type is reported once with its fields; a
  designer property only the subclass declares (`RoundButton.CornerRadius`) is listed by the
  coverage report under the name the designer wrote. A custom control with a designer half of its
  own is no longer mistaken for a Form whose base lives in another assembly.
- **Visual inheritance is flattened.** A Form (or UserControl) deriving from another of the
  project's *designer* artifacts - directly or through intermediate classes with no designer of
  their own - used to come out with only its own controls: every inherited button, the derived
  designer's overrides of it (`this.okButton.Text = "Save";`) and its wiring were gone, and nothing
  said so. WinForms runs the base's `InitializeComponent` and then the derived one, on the same
  fields; the walker now does exactly that (`DesignerSyntaxWalker.Walk(..., inheritedFrom)`), so
  the derived View carries the whole result and the later value wins. The base class's handlers
  come with it (`CodeBehindModel.WithInherited`): a member the derived class redeclares hides the
  base's, as in C#. Consequences:
  - Avalonia XAML cannot inherit a View, so the derived View is a **copy**, and a later edit to the
    base View does not reach it. Reported once per derived Form, since that is a fact about how
    the generated project has to be maintained.
  - The base keeps a View of its own - it may well be instantiated by itself.
  - The preserved original-code comment block is the derived class's file only.
  - An image the *base* designer took from its own `.resx` is looked up in the derived form's
    `.resx`, misses, and is reported rather than set.
  - A base type from a referenced assembly is still not followed (see above); neither is a base
    whose simple name is ambiguous across namespaces and not beside the derived class.
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

## Designer properties nothing translates

- **They are reported, by name.** A designer property no stage of the conversion asked about
  used to vanish: `OpenFileDialog.Filter`, `PictureBox.SizeMode`, `ToolStripMenuItem.ShortcutKeys`,
  `TabControl.SelectedIndex`, `Panel.AllowDrop` and dozens more converted into nothing, with no
  line anywhere saying so. Each artifact now gets one warning listing them, grouped by
  `Type.Property` with the fields that set it.
- **How it knows.** No table could say which properties are carried - they are read by dozens of
  mappers, universal passes and catalogs. So `ControlModel.Properties`/`FormModel.FormProperties`
  are a `PropertyBag` that records every lookup by name, and what is still unread once the artifact
  has been emitted is what nothing even asked about. A new mapper is covered without declaring
  anything.
- **Its blind spot is "asked, then refused".** A styling pass that reads `BackColor` and finds the
  target has no Background has looked; those passes report their own refusals where they do, and
  this check cannot see the ones that do not.
- **What carries nothing on purpose** is listed in `Mapping/DesignerPropertyCatalog` with the reason
  (`Name`, `AutoScaleMode`, `UseVisualStyleBackColor`, `AutoSize`, a strip's invisible `Text`, a
  `TabPage`'s bounds, ...), some only for a value (`CheckState` unless `Indeterminate`,
  `NotifyIcon.Visible` unless false). `Mapping.Tests` checks each name against WinForms. A type with
  no mapping at all is already reported whole and its properties are not listed again; a Timer
  nothing ticks, or a component nothing uses, becomes no field and has nothing to configure.

## Project files

- **Source files are found the way MSBuild finds them, mostly.** SDK-style globbing with
  `Compile Remove`; explicit `Compile`/`EmbeddedResource` includes, including a wildcard
  (`..\FullFxApp\*.cs`, `Forms\**\*.cs` - not a wildcard in a directory segment); a Shared
  Project's `.projitems` imported with `<Import Project="...projitems" />`, read with its
  `$(MSBuildThisFileDirectory)`. No other MSBuild property is evaluated.
- **Case.** A legacy csproj written on Windows may say `Form1.designer.cs` for `Form1.Designer.cs`;
  a path that does not exist as written is resolved against the disk case-insensitively (when the
  match is unique), or Linux and macOS failed the whole run on "Could not find file".
- **Linked files land at the project root.** A Form included from outside the project folder is
  converted as if it sat at the root - which is also where Visual Studio shows it - rather than
  under a `../Other` folder, which used to become a `..` namespace segment.
- **Two Forms may share a simple name** in different namespaces and folders (`A/MainForm`,
  `B/MainForm`); each gets its own View in its own folder. A handler naming one of them by that
  simple name (`new MainForm()`, `dialog.Value`) is ambiguous to syntax and stays un-migrated.
- **Carried-over fields may be used only by code that did not come across**, so the generated View
  wraps them in `#pragma warning disable CS0169, CS0414, CS0649`, and a field with no initializer
  whose type may be a non-nullable reference starts as `default!` (CS8618 otherwise).
- **A vendor base class** (`MetroForm`, `C1RibbonForm`, `XtraForm`) cannot be traced by syntax, so
  a Form deriving from one is skipped - with a message that says how to fix it: a `--mappings`
  entry `{ "MetroForm": { "as": "Form" } }` (or `"UserControl"`).

## Startup form

- **The main window is the Form `Application.Run` starts.** `Parsing/StartupFormLocator` reads
  it from the project's sources - `Application.Run(new MainForm())`, or a local passed in
  (`var form = new MainForm(); Application.Run(form);`), top-level statements included. It used to
  be simply the first Form discovery listed, so an `AboutForm` or a base form sorting ahead of
  `MainForm` became the converted app's main window: built, launched, wrong, and unreported.
- The same answer picks the Form `--with-web` roots at a `UserControl`, so the split and
  `App.axaml.cs` cannot disagree.
- Where it cannot be read, the first Form is still used, and the choice is **reported**: an
  `ApplicationContext` (which picks its Form at run time), a local given two different Forms, a
  factory call, two `Application.Run` calls naming different Forms (the first wins), or no call
  at all in a project with more than one Form. Syntax only, as elsewhere: a Form constructed in
  a helper method and returned is not followed.

## Designer parsing

- **What is a control and what is a property is decided by the source, not by a list of type
  names.** `this.X = new T(...)` in `InitializeComponent` is a control when `X` is a field the
  designer's own class declares, and a property of the Form/UserControl otherwise - because a
  designer declares a field for every control it creates and never for an inherited property.
  It used to be decided by a four-name list (`Size`, `Point`, `Padding`, `Font`), which made every
  other System.Drawing value a phantom control: `this.AutoScaleDimensions = new SizeF(6F, 13F);`
  registered a control called *AutoScaleDimensions* of type *SizeF* and reported it as
  unsupported, once per Form. Every real Visual Studio designer emits that line; this repo's
  fixtures are hand-written and none of them did, which is why nothing caught it until a real
  project arrived.
  - A class that declares **no** fields at all falls back to the old rule. That is not a fudge: a
    generated designer always declares its controls, but a hand-written `InitializeComponent` can
    keep them in the other half of the partial class, which the walker never sees - and losing
    every control there would be far worse than the bug being fixed.
  - A shape that is neither a declared field nor a known value type is now **reported by name**
    instead of quietly becoming a control.

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
  - images referenced from a `.resources`/satellite assembly rather than the form's own `.resx`
    are still not handled.
  - the Form's own `$this.Icon` comes out the same way and becomes the Window's `Icon`.
- **`Properties.Resources` is carried over.** The designer writes
  `global::App.Properties.Resources.logo` whenever an image was picked from "Project resource
  file" - which Visual Studio offers first - and that used to be read as an *enum member* named
  `logo`: a PictureBox lost its image and a Label showed the resource's name.
  `ProjectResourcesReader` now reads `Properties/Resources.resx` (bare entry names, and
  `ResXFileRef`s to files in the project's `Resources/` folder, which are copied as they are), and:
  - a designer property reading a **string** entry gets the string; one reading an **image** gets
    the file copied to `Assets/Resources/<name>.<ext>`; the Form's `Icon` becomes the Window icon;
  - the generated project gets a `Properties/Resources.cs` (`ProjectResourcesEmitter`) with the
    same member names - strings inlined, images as an Avalonia `Bitmap`, icons as a `WindowIcon`,
    both loaded from those assets - so a handler body reading `Properties.Resources.Greeting`
    translates to `<Project>.Properties.Resources.Greeting`. Only **string** entries are
    translatable in a body: an image entry is now a `Bitmap` where the original code had a
    `System.Drawing.Image`, so a statement assigning one still stops the prefix;
  - an entry of any other type (a `Size`, a serialized object, a byte array), and a file reference
    that does not exist on disk, is left out of the class and reported by name;
  - **translations are not carried.** The strings are inlined from the neutral file, so a
    `Properties/Resources.hu.resx` is reported (with its cultures) rather than read.
- **`Properties.Settings` is carried over, as JSON.** `ProjectSettingsReader` reads
  `Properties/Settings.settings` (the file Visual Studio generates the class from - its type, scope
  and default; an `app.config` override of a default is not read), and `ProjectSettingsEmitter`
  writes a same-shaped `Properties/Settings.cs`: a `Default` instance, user-scoped settings as
  read/write properties, application-scoped ones read-only, and `Save()`/`Reload()`/`Reset()`,
  persisted to `<application data>/<Project>/settings.json`. A handler body reading a setting,
  assigning a user-scoped one (`=`, `+=`, ...) or calling one of the three methods translates onto
  it. What does not come across:
  - **values users already saved.** Their `user.config` is not migrated - reported once.
  - **types JSON cannot store without a converter, or C# cannot write as a literal** - only
    `string`, `bool`, `int`, `long`, `double`, `float`, `decimal`, `DateTime` and `TimeSpan` are
    carried; a `Color`, a `Point`, a `StringCollection` is left out and named. So is a setting
    called `Save`/`Reload`/`Reset`/`Default`, which the class needs itself.
  - **assigning an application-scoped setting**, which has no setter on either side: the prefix stops.
  - **designer bindings to a setting.** `DataBindings.Add(new Binding("Text", Settings.Default,
    "UserName", ...))` is reported (as is every other `DataBindings.Add`, which used to vanish
    silently), and the `Text = Settings.Default.UserName` the designer writes beside it becomes the
    setting's *default* value - the control no longer follows what was saved.
- **An `ImageList`'s images are extracted; where they can be *shown* is the narrower half.**
  `ImageListExtractor` decodes the `ImageStream` payload (RLE + `ILHEAD` + a tiled bitmap strip +
  a 1bpp mask) into one PNG per image under `Assets/<field>_<index>.png`, mask applied as alpha.
  An `ImageIndex` into that list - inherited from the owning control when the item does not name
  the list itself, as WinForms does it - is then set as the element's image. But `MenuItem.Icon`
  is the only per-item image slot Avalonia has: a `TreeViewItem`, `ListBoxItem` or `TabItem` has
  no icon property, and a `Button`'s `Content` is already its text. For those the image is
  written to `Assets/` and the warning names the file, rather than a header layout being invented.
  `ImageKey` is resolved too: the keys are not in the payload but in the designer's
  `Images.SetKeyName(i, "key")` calls, which the walker records (`ControlModel.ImageKeys`); a key
  the list never declares is reported by name.

## Dialog call shapes

- `ColorDialog` and `FontDialog` have no Avalonia counterpart, so a handler's `ShowDialog` is
  translated **inline** onto the bundled `ColorDialogFallback`/`FontDialogFallback` - the Avalonia
  replacement *returns* the choice instead of being an object you ask afterwards, which is what
  makes the shape of the call matter at all.
- Two shapes are accepted, in either operand order:
  `if (dlg.ShowDialog() == DialogResult.OK) { … }`, and the guard clause
  `if (dlg.ShowDialog() != DialogResult.OK) { return; }` followed by the rest of the body. The
  guard is equivalent for a reason worth stating: C# definite assignment guarantees the picked
  value is assigned at every statement the guard falls through to, because the then-branch is an
  unconditional `return` - so the value may outlive the branch here and nowhere else. It is matched
  only at the **top level** of a handler body, where "the rest of the block" and "the rest of the
  body" are the same thing; inside a nested block it would leak into statements after that block,
  so there it refuses. The same shape serves the file dialogs.
- Still refused, each with the reason in the code: a bare `dlg.ShowDialog();` (nothing is done with
  the result, and slipping in an `await` would change the semantics); reading `dlg.Color` after a
  *normal* `if` (the WinForms field keeps the last pick even after Cancel, and here there is
  nothing to keep it); `var r = dlg.ShowDialog(); if (r == …)` (dataflow across a statement
  boundary, which this rewriter deliberately does not do); a ViewModel target (no `Window` to
  parent to); and a branch only part of which translates.
- **A seed value assigned before the call is carried over**, when the translation can express it.
  `colorDialog1.Color = Color.Red;` and `fontDialog1.Font = someLabel.Font;` emit nothing of their
  own: WinForms says the seed as an assignment to the component, Avalonia's replacement takes it
  as an argument, so the statement is absorbed and spent on the `ShowAsync` call. A seed the
  evaluator cannot resolve to a literal colour - or a font read from something with no font
  surface - still **refuses**, and because the body is translated as a prefix that costs the rest
  of the handler too. Absorbing one silently would be worse: the rewriter has no way to report a
  value it dropped.

## Tray-icon events

- A `NotifyIcon` has no element in any View - its `TrayIcon` lives in `App.axaml` - so its
  designer-wired events resolve through `EventMappingRegistry`'s per-type overrides or nowhere.
  Without those rows they fell through to the *generic control* table, which produced a fully
  translated handler on the View that nothing subscribed and nothing reported: a method that reads
  as finished and never runs.
- `Click` → `TrayIcon.Clicked`, subscribed from the generated constructor. Not identical, and the
  guidance says so: WinForms' `NotifyIcon.Click` fires for a right-click too (which is why real
  handlers cast `e` to `MouseEventArgs`), while `Clicked` is activation only and carries no button
  information.
- `DoubleClick`, `MouseDoubleClick`, `MouseClick`, `MouseDown`/`Up`/`Move` and the three
  `BalloonTip*` events have no counterpart at all - Avalonia's `TrayIcon` raises only `Clicked`,
  with no button or coordinate payload, and there is no notification API. Each is refused by name.
- A tray icon whose file did not resolve is emitted **commented out** in `App.axaml`, so it has no
  accessor: any subscription against it is suppressed and reported, since the constructor line
  would not compile.
- The `Command` property is deliberately unused: promoting a tray click to a `[RelayCommand]` would
  have to bind from `App.axaml`, which has no `x:DataType` and no ViewModel.

## Hosted controls

- A `ToolStripControlHost` is plumbing WinForms needs because a `ToolStrip` only accepts
  `ToolStripItem`s. The Avalonia fallback a ToolStrip maps to is an ordinary panel, so the
  faithful conversion is to put the hosted control where the host was - and since the type has
  **no parameterless constructor**, `new ToolStripControlHost(this.trackBar1)` is the only shape a
  designer can emit, with the hosted control always named right there.
- The substitution happens in `ControlGraphBuilder`, not the walker: the host goes on collecting
  property assignments until the last statement of `InitializeComponent`, so collapsing it early
  would silently drop them.
- Two cases refuse rather than guess. A hosted control that is *also* added to a container of its
  own would be emitted twice, and two elements with one `x:Name` is an `AVLN1001` the generated
  build fails on. And the host's own settings have no counterpart once the control is placed
  directly: its `Size` moves only into a gap (WinForms keeps the two in sync), while
  `Alignment`/`Overflow`/`DisplayStyle` and any event subscribed on the host are reported by name
  - the planner iterates the very collection the host is removed from, so silence there would be a
  handler that never runs.

## Extender providers

- WinForms' extender providers set a property on *another* control (`toolTip1.SetToolTip(btn, "x")`),
  so the value belongs to the argument rather than to the field the call was made on.
  `ExtenderProviderCatalog` holds one row per translatable setter, carrying both halves - which
  property key the walker parks the value under, and which Avalonia attached property the emitter
  turns it into - so the two cannot drift apart the way they could when both were hardcoded.
- `ToolTip.SetToolTip` → `ToolTip.Tip`; `HelpProvider.SetHelpString` → `AutomationProperties.HelpText`.
  The latter is the honest target rather than the convenient one: Avalonia has no F1-context-help
  concept, but `AutomationProperties.HelpText` means "help text about this control", and unlike
  `ToolTip.Tip` it cannot collide with a real `SetToolTip` on the same control. The keyboard
  gesture is lost, the prose is not.
- A setter on a recognised provider with no row - `SetShowHelp`, `SetHelpKeyword`, `SetError` - is
  **reported by name**. It used to vanish silently, which is how `HelpProvider`'s entire
  contribution to a form could disappear with nothing to notice.
- Both attributes are only emitted where the target can carry one (`MappedControl.SupportsName`):
  an attached property on a `DataGridTextColumn` is an AVLN2000 in the generated project.

## Captions and keyboard mnemonics

- A WinForms caption carries its keyboard mnemonic inline (`&File` underlines the F, `&&` is a
  literal ampersand). Avalonia uses the same idea with a different character (`_File`, `__`), so
  the conversion transliterates - in both directions at once, since an underscore already in the
  text has to be doubled or it becomes a marker of its own.
- Whether that happens at all is decided by `WinFormsMnemonicCatalog`, per WinForms type, because
  the question has two halves. A `TextBox`'s `Text` is the user's data, so "Smith & Sons" is left
  exactly as it is. A `Label` becomes a `TextBlock`, which renders an underscore literally, so
  the marker is *removed* rather than translated - the caption is right, the keyboard shortcut is
  gone, and Avalonia has nowhere to put it. Only targets that really render an access key
  (`AvaloniaAccessKeySupport` - Button, CheckBox, RadioButton, HyperlinkButton, SplitButton,
  MenuItem, TabItem, Label) get the underscore.
- A `ListView` `ColumnHeader` is deliberately left alone: WinForms does not read a mnemonic out
  of one, so an ampersand there is part of the heading.

## Themes for packaged controls

- `DataGrid` and `ColorView` ship outside core Avalonia and carry their own `ControlTheme` in a
  resource dictionary. `App.axaml` includes it (`AvaloniaProjectScaffolder.PackageStyleIncludes`)
  for exactly the packages a conversion required - an include for a package the csproj does not
  reference is a XAML load failure at startup rather than a build error, so it is conditional.
  The Simple variants, matching the `SimpleTheme` the generated shell uses; switching the shell
  to Fluent means switching these too.

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
  A NotifyIcon the designer gave **no `Icon` at all** is the exception: WinForms shows nothing in
  the tray without one, and a `TrayIcon` without an `Icon` shows nothing either while referencing
  no file that could be missing - so it is emitted live, with an accessor, and reported.

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
  `FontManager.Current.SystemFonts`). The print family has its own section below - it stopped
  being guidance-only once a page could be drawn.
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
  - a target not listed in that table takes none, and those entries are **reported** rather than
    dropped silently, since a missing list is visible content. A bundled template can be listed
    too: `DomainUpDownFallback` holds its entries in a get-only `AvaloniaList<string>`, so they
    are emitted as bare `<sys:String>` elements inside a `<...Items>` property element rather
    than as item elements with a `Content` attribute. The `xmlns:sys` that needs is declared on
    the document root, and only when some control on the form actually needs it - Avalonia's XAML
    compiler rejects an attribute on a property element, which is where it would otherwise go;
  - it is static content, not a binding. `ItemsSource`/`SelectedItem` still need a hand-written
    `ObservableCollection` - only the *selection index/item* properties are in the bindable
    catalog.

  A `ListView` picks its target per-instance (`ListViewMapper`: `View=Details` or any parsed
  `ColumnHeader` children → `DataGrid` with real columns, otherwise `ListBox`) and a
  `MonthCalendar` maps to `Calendar`, but neither gets its rows or selection ranges. All six `DataGridView` column types are translated now: `TextBox`/`CheckBox` to
  Avalonia's two real column types, and `ComboBox`/`Button`/`Image`/`Link` to a
  `DataGridTemplateColumn` with a generated cell template. A **button or link** cell is complete:
  it shows the column's `Text` when `UseColumnTextForButtonValue`/`UseColumnTextForLinkValue` is
  set, and otherwise the row's value - so its `Content` binds to `DataPropertyName` (with neither,
  nothing is invented: WinForms ignores `Text` without the flag). A **ComboBox or Image** cell
  stays unbound, with a `TODO` naming the `DataPropertyName`: which ComboBox member the value
  belongs to depends on `DisplayMember`/`ValueMember`/the items' source, and an `Image.Source`
  wants an `IImage` where the row holds a `System.Drawing.Image` or a byte array.

- **Visual styling is converted, but gated by the target element.** `BackColor`, `ForeColor`,
  `Font` and `Padding` are emitted as `Background`/`Foreground`/`FontFamily`/`FontSize`/
  `FontWeight`/`FontStyle`/`TextDecorations`/`Padding` - universally, on any control, the same
  way `ToolTip.Tip` is, since every WinForms `Control` has them. What actually reaches the
  AXAML is decided by the *Avalonia* element, through `Mapping/AvaloniaStylePropertySupport`:
  a `Panel` (what every WinForms container maps to) has a Background but no Foreground and no
  font properties, and an `Image` (the `PictureBox` target) has none of them - emitting one
  anyway would be an AVLN2000 in the generated project. Consequences worth knowing:
  - a **fallback control** gets exactly the style groups its bundled template really has. These
    templates ship in this repo, so that is a known fact rather than a guess:
    `AvaloniaStylePropertySupport.ForFallbackTemplate` reads it out of
    `FallbackControlMemberSupport` a member at a time, and a group is writable only when every
    member it is made of is there - so a `StackPanel`-derived toolbar gets `Background` and
    nothing else, while `RichTextBoxFallback` (a `TextBox`) gets the whole surface. The emitter
    and a translated handler now ask that same method, so they cannot disagree. A generated
    **UserControl** View element still gets none. The members a handler may touch:
    that is how a `FontDialog` result reaches a `RichTextBox`, which derives from Avalonia's
    `TextBox` and so inherits the four font properties for real;
  - a **new mapper target** gets no styling until its element name is added to that table;
  - a value `ExpressionEvaluator` cannot resolve to a literal (a computed color, a resx
    lookup, a `SystemColors` name outside the table) emits **nothing** rather than a guess;
  - font sizes are converted points → device-independent pixels at the fixed 96/72 ratio, and
    `SystemColors.*` resolve through a hand-written ARGB table rather than the host desktop
    palette, so the output stays byte-identical across machines.

  What is *not* converted: `BackgroundImage`, `FlatStyle`/`FlatAppearance`, and `Font` values
  whose family/size are not literals.

  `RightToLeft` **is** converted, to Avalonia's `FlowDirection` - but only on a control with no
  positioned children, and that gate is the whole point. Avalonia mirrors an element's entire
  subtree when its `FlowDirection` differs from its parent's, and this conversion positions
  everything with absolute `Canvas.Left`; WinForms moves nothing for `RightToLeft` alone (that is
  `RightToLeftLayout`, a separate property). So on a leaf the two mean the same thing and the
  attribute is emitted; on a container it is reported instead. `Inherit` emits nothing, which is
  exact - Avalonia registers `FlowDirection` as an inheriting property.

  `TextAlign` **is** converted, but only its horizontal component. On a `TextBox`/`MaskedTextBox`/
  `NumericUpDown` it is already just a horizontal alignment and maps exactly onto Avalonia's
  `TextAlignment`. On a `Label`, `Button`, `CheckBox` or `RadioButton` it is a `ContentAlignment`,
  which is two facts in one: the button family spends them on `HorizontalContentAlignment` and
  `VerticalContentAlignment`, but a `Label` becomes a `TextBlock`, which has `TextAlignment` and
  no vertical counterpart - Avalonia aligns the block, not the text inside it. So a
  `BottomRight` label right-aligns and does not bottom-align.

  `BorderStyle` is converted only where the target element can draw a border at all - a `TextBox`,
  `ListBox` or `TreeView` - as a `BorderThickness`. Avalonia draws one border rather than three
  styles, so `FixedSingle` and `Fixed3D` both become a one-pixel border and the sunken 3D look is
  gone; `None` is the value that carries real information, turning off the border the theme would
  otherwise draw. A `Panel` becomes a `Canvas` and a `PictureBox` an `Image`, neither of which has
  a `BorderThickness`, so their `BorderStyle` is not carried - and reported as such.

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
    WinForms type - a base-type cast (`(Control)sender`) is refused rather than widened.
    `var button = sender as Button;` is the same thing on the same terms (`as` never yields null
    when every wired control is a Button). A type test - `if (sender is Label card)`, the guard
    `if (sender is not Button b) return;`, or a bare `sender is Button` - is kept against the
    Avalonia element type (`sender is TextBlock card`), again only when every wired control is of
    exactly that type, so the test is as always-true as it was; the pattern variable is a control
    for the rest of the body. Used in place, `((Button)sender).Text` and `(sender as Button)` are
    the control's field on a single-control handler, and refused on a shared one;
  - the text-entry family's own properties, which are plain renames: `Multiline` →
    `AcceptsReturn`, `ReadOnly` → `IsReadOnly`, plus `MaxLength` and `SelectionStart` unchanged.
    One of them is not a rename at all: `WordWrap` is a `bool` in WinForms and a `TextWrapping`
    enum in Avalonia, so the **value** is rewritten too (`... ? TextWrapping.Wrap :
    TextWrapping.NoWrap` writing, `== TextWrapping.Wrap` reading). A property in that shape is
    deliberately **not** two-way bindable - a `{Binding}` has no converter in between, so it
    cannot carry a promoted `[RelayCommand]` and stays in code-behind, where the conversion is
    written out. A compound assignment (`label1.Text += x`, `box.WordWrap |= y`) is expanded into
    the read and the write it abbreviates - `a op= b` is `a = a op b` - so both halves go through
    their own conversion: `label1.Text = (label1.Text ?? string.Empty) + x`;
  - the cross-thread idiom: `InvokeRequired` (on the View or any control) as
    `(!Dispatcher.UIThread.CheckAccess())`, `Invoke(...)` as `Dispatcher.UIThread.Invoke(...)` and
    `BeginInvoke(...)` as `Dispatcher.UIThread.Post(...)` - the same synchronous/queued contract.
    The delegate is rebuilt as a lambda: a parameterless lambda or anonymous method (bare, cast,
    or wrapped in `new MethodInvoker(...)`/`new Action(...)`) whose body translates **whole**,
    since it runs somewhere else and has no place for a remainder - or a helper that became real
    code, with the arguments passed after it (bare or as `new object[] { ... }`). A delegate held
    in a local first, a lambda with parameters, and a body that would await stay un-migrated;
  - a stock cursor, `this.Cursor = Cursors.WaitCursor;` / `control.Cursor = Cursors.Hand;`, as
    `new Cursor(StandardCursorType.Wait)` (`Cursors.Default` as `Cursor.Default`) -
    `Mapping/CursorCatalog`. Only exact counterparts: the splitter, diagonal-resize and pan
    cursors have none and are refused, as is the global `Cursor.Current`;
  - a ComboBox's or ListBox's own list: `Items.Clear()`, `Add(x)`, `Insert(i, x)`, `RemoveAt(i)`,
    `Count`, and `AddRange(array)` as the `foreach ... Items.Add` loop it stands for. Reading an
    item back (`(string)box.Items[i]`), `Remove(x)`, `IndexOf(x)` and `Contains(x)` find items by
    value, so they translate only where the AXAML declares **no** item elements - designer items
    are `ComboBoxItem`s now, not the strings WinForms held. A list bound to an `ItemsSource`
    refuses all of it: Avalonia throws on a write to `Items` while one is set;
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
    on the other end is one this repo ships, so it can be given a `bool` return. `OKCancel` too.
    `YesNoCancel` has an entry point of its own returning `bool?` - Yes `true`, No `false`, Cancel
    `null`, which is also what closing it by the title bar answers, as in WinForms - and is
    translated in the three shapes it is asked in: compared inline (`== DialogResult.No` →
    `== false`), kept in a local (`var answer = MessageBox.Show(...)`, after which only comparisons
    with `DialogResult` members and a `switch` with `case DialogResult.Yes:` labels may use it),
    and switched on directly. Not inside a `Closing` handler, where nothing may await - which is
    where "save changes?" usually lives, so that one still needs a human. `AbortRetryIgnore` and
    `RetryCancel` refuse, and so do the icon overloads - the bundled dialog draws no icon, and
    accepting them would silently drop a cue;
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
    differently - and anything they cannot resolve to a literal (a computed colour) is refused
    rather than guessed at, exactly as in the AXAML. **Another control's colour is read** where it
    means the same: `label.ForeColor` is the element's `Foreground`, which Avalonia inherits just as
    WinForms' ambient `ForeColor` does, and `label.BackColor` is `AmbientBackColorFallback.Of(label)` -
    a bundled helper walking up to the first element with a background of its own, because an unset
    Avalonia `Background` reads `null` where WinForms answers the parent's colour. It skips a fully
    transparent one: that is what the conversion gives a clickable element so the pointer can hit it.
    At the window it answers the theme's background where WinForms says `SystemColors.Control`. A
    choice between two colours (`open ? Color.Black : label.BackColor`) is a choice between the two
    brushes. A `SolidBrush` *local* still takes only a literal colour - its `Color` may be written
    later, and a control's brush is shared with the control. Gated on the
    *element* through `AvaloniaStylePropertySupport`, the same table `AxamlEmitter` consults: a
    `Panel` has a Background but no Foreground, an `Image` has neither, and a **fallback** control
    gets no styling at all. `Font` is deliberately absent - one WinForms value becomes three
    Avalonia properties, which is a different shape of problem;
  - **A `TableLayoutPanel` asked about its cells** - `GetColumn(label)`, `GetRow(label)`,
    `GetControlFromPosition(c, r)`, `Controls.Count` and `(Label)Controls[i]`, the whole of what
    dotnet/samples' MatchingGame says about its board. The panel is still a Canvas, but every child
    the designer placed with `Controls.Add(child, column, row)` carries that cell as an inert
    `Grid.Column`/`Grid.Row`, so `Grid.GetColumn(label)` reads back exactly what WinForms returned,
    and the position lookup is `Children.OfType<T>().FirstOrDefault(...)` - null for an empty cell,
    as in WinForms. Only for a panel the designer filled completely and plainly: every child in a
    cell of its own, none moved or spanned afterwards (`SetColumn`, `SetColumnSpan`, ...), all of one
    WinForms type mapped to one element - which is also what makes `Controls[i]` *be* `Children[i]`
    and the cast unable to fail where it did not. A control asked about must be one of the
    children: a field, a shared handler's `sender`, or a child already read from the panel. The
    cell number is an `int` the semantic check cannot type (the call it came from is WinForms), so
    the rewriter vouches for it - and only as a whole argument of a call that resolves to one
    member taking it, which is how `_game.GetCard(column, row)` comes across. MatchingGame itself
    still stops at `CheckForWinner`: its `GameSettings` reads the Windows registry;
  - **`errorProvider1.SetError(control, "message")`** → `ErrorProviderFallback.SetError(control, "message")`.
    The one translation whose result is a *static* call on a bundled fallback: everywhere else a
    fallback is something the AXAML instantiates and the handler then talks to, but a WinForms
    `ErrorProvider` has no element at all and its counterpart is an attached property set from
    outside. That is also why it cannot live in `ControlMethodCatalog`, which names members of the
    *target* control. Like `MessageBox.Show`, it pulls the template in from a **handler body**
    rather than from an element. The control it flags has to be one the AXAML really names, or the
    generated View has no field to hand over. The template puts a red indicator in the window's
    adorner layer with the message as its tooltip, and an empty message removes it - which is how
    WinForms clears an error. Not Avalonia's `DataValidationErrors`: that takes an `Exception`
    rather than a message, and only draws where the target's `ControlTheme` hosts a presenter for
    it, which under the Simple theme this generator uses means text inputs and nothing else. What
    is not carried: the blink, a custom `Icon`, and `SetIconAlignment`/`SetIconPadding`;
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
    which makes the handler `async`. `Clipboard.GetText()` is translated too, and awaited right
    where it was called - `(await TopLevel.GetTopLevel(this)!.Clipboard!.TryGetTextAsync() ??
    string.Empty)` - which C# allows anywhere in an async method's expression and which keeps the
    evaluation order. (This was once refused on the grounds that the expression would need
    restructuring; it does not.) The `??` is WinForms' empty string for a clipboard with no text,
    where Avalonia returns null. Not inside a `Closing` handler, where nothing may await. Like a message box, this now blocks ViewModel promotion -
    the replacement hangs off the `TopLevel`, which a View has and a ViewModel does not;
  - **`EventArgs` members**, through `Mapping/EventArgsMemberCatalog`. Three kinds show up and
    only two are translated: a member Avalonia's own args type spells identically (`Cancel` on
    `WindowClosingEventArgs`, `NewValue` on `ScrollEventArgs`), and a member of an args type that
    is plain .NET and reached the generated project untouched (`FileSystemEventArgs`, the
    BackgroundWorker ones). The pointer position is the one genuinely *translated* member -
    WinForms' `e.X`/`e.Y` are relative to the control that raised the event, which is exactly
    what `GetPosition(control)` takes, so it needs a handler wired to exactly one control.
    A grid cell's `RowIndex`/`ColumnIndex` are translated too, and were another thing called
    inexact without being looked at: Avalonia reports the row and column *objects*, the row knows
    its own `Index`, and the column index comes from asking the grid that raised the event
    (`DataGridColumn.OwningGrid` is not public, so it has to be the source control) - which makes
    `ColumnIndex` another member that needs a handler wired to exactly one control. Anything with
    no exact answer at all is still left for a human.

    **Drag and drop** is where the two frameworks diverge most, and only the one shape with an
    exact answer is translated: `e.Effect` → `e.DragEffects` (the `DragDropEffects` members both
    declare - `Scroll` and `All` are WinForms-only and refuse), and
    `e.Data.GetDataPresent(DataFormats.FileDrop)` → `e.DataTransfer.Contains(DataFormat.File)`.
    That second one is matched as a whole shape because not one part of it survives alone:
    Avalonia 12 renamed the property, changed its type (`IDataObject` → `IDataTransfer`, with
    different method names) and replaced the format constants. `e.Data` is deliberately *not* a
    pass-through member for the same reason - letting it through would emit an `IDataObject`
    method against a type that has none. Reading the payload *is* translated, in the one shape
    that has an exact answer: `(string[])e.Data.GetData(DataFormats.FileDrop)` becomes
    `e.DataTransfer.TryGetFiles()!.Select(f => f.Path.LocalPath).ToArray()`. It is a change of
    shape rather than of spelling - storage items instead of paths - but the content is the same
    set of files and `IStorageItem.Path.LocalPath` is exactly the string WinForms would have
    given. The null-forgiving operator is kept rather than dropped, unusually: both sides return
    null when the drop carried no files, and the original treats the result as non-null, so
    emitting a `string[]?` would make the next line a nullable warning in a project that must
    build warning-free. Any other format, or a cast to anything but `string[]`, still refuses.

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
  - **local variables**, declared `var` or with a type from `PlainDotNetTypeCatalog` (keyword types, `string?`, `DateTime`, ...), when the initializer translates.
    Members of a local are then allowed for the same reason members of a control property are: a
    translatable initializer can only produce a plain .NET value. Locals are block-scoped, so one
    declared inside an `if` branch cannot be used after it. `var dialog = new SomeForm();`
    declares a *View* rather than a value - only the navigation calls accept it - and a
    `using var` on that shape drops the `using`, since an Avalonia Window is not IDisposable and
    there is no disposal to preserve. A `using` on anything else is refused rather than silently
    dropped. `const` locals are not translated.

  - **`try`/`catch`/`finally`, `switch`, `do`/`while`, `break`, `continue` and a bare `throw;`**,
    each translated whole like an `if` (the remainder could only ever be emitted after the
    statement). A `catch` may name an exception from a closed table of BCL types - whose namespace
    the View then imports, since the original file's `using`s do not come across and an unqualified
    `IOException` would otherwise be a CS0246 in the generated project - or any `System.`-qualified
    one; its variable is a value local scoped to the block, and a `when` filter translates like any
    condition. `switch` labels must be literals: a label is matched against the governing value's
    own type, and only a literal is certain to mean the same on both sides. `throw new X()` refuses,
    because `new` is not something this rewriter translates;

  Everything outside that list stops the translation, including: `lock`, `using` blocks, `throw`
  with an expression, calls to code-behind helpers, `Paint`/`Graphics` drawing,
  control APIs with no bindable counterpart (`treeView1.Nodes.Add`), properties on
  *fallback*-mapped controls, and unrecognized static receivers
  (`Cursor`, `Screen`, ...). The `MessageBox.Show` overloads that take buttons or icons are
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
  - nothing about the designer's settings any more: `Title`, `Filter` (→ `FileTypeFilter` /
    `FileTypeChoices`), `FileName` (→ `SuggestedFileName`), `InitialDirectory` (→ an awaited
    `SuggestedStartLocation`), `Multiselect`, `DefaultExt` and `OverwritePrompt` all reach the
    picker (`Mapping/FileDialogOptions`), on the inline path and in the generated helper alike;
    a folder dialog's `Description` becomes its title and its `SelectedPath` its start folder. The
    filter grammar is name/patterns pairs and is parsed exactly; a string that is not that shape is
    reported and left out. `FilterIndex` other than 1 is reported - Avalonia takes the preselected
    type as an instance an inline initializer cannot point back at;
  - `FileNames` (multi-select) - there is no single path to bind, so the whole branch is left;
  - any use of the dialog's properties *after* the branch, since the selection is a pattern
    variable now rather than an object that outlives the call;
  - the print dialogs, which have no Avalonia equivalent at all - see the printing note above.

  **The colour and font dialogs** take the same inline route, onto bundled windows rather than a
  platform API: `if (colorDialog1.ShowDialog(this) == DialogResult.OK)` becomes
  `if (await ColorDialogFallback.ShowAsync(this) is { } colorDialog1Color)`, and
  `colorDialog1.Color` inside that branch becomes the pattern variable. `ColorDialogFallback`
  wraps Avalonia's real `ColorView`, which ships in `Avalonia.Controls.ColorPicker` - the first
  time a *bundled template* has needed a package, so `FallbackTemplateDefinition` now declares one
  and it goes through the same double-allowlist as a mapper's. What is not covered:
  - the dialog opens on the seed the handler set, if there was one it could translate (see the
    dialog section above); a *designer*-set `Color` is still not read, only a handler assignment;
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
  A tail that does *not* translate no longer takes the whole shape with it. It moves into a
  **local function** that both paths call, and that indirection is the point rather than a
  detail: the confirmation runs the tail on the path that asks and on the path that does not, so
  a remainder appended to the end of the method would sit on one of them only, and a human fixing
  it would silently leave the other broken. There is exactly one of it to edit, and the checklist
  names the statement rather than the whole handler. Whatever part of the tail *does* translate
  goes into the same function above the remainder, exactly as a partly-translated handler body
  would.

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

  Types: keyword types, plus the `System` types that are the same type on both sides and need no
  import - `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Guid`, `Uri`,
  `Version` - and the nullable and array forms of all of them (`Mapping/PlainDotNetTypeCatalog`;
  the same gate decides a carried-over field, property and local). `string?` was refused along with
  every named type until then, because it parses as a nullable type rather than a keyword one.
  `Mapping.Tests` checks that none of the accepted names is also a WinForms type, which was the whole
  reason named types were refused.

  Not covered: a helper with any **other named type** in its signature (it could be a WinForms
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
- **A handler that stays in code-behind is not a warning.** Code-behind is the default, so the
  reason a `[RelayCommand]` candidate stayed there goes to its own "Stayed in code-behind" section
  of `MIGRATION.md` (and `ConversionReport.CodeBehindNotes`), after the real warnings rather than
  among them - on the all-in-one sample it had been 30 of the 43 "Needs your attention" lines.
  The reason itself is meant to be exact: `DialogResult.OK` is the enum, not the Form's
  `DialogResult` property, and `this.isBusy` is the class's own state rather than the Form being
  driven.
- **Promotion is single-control only.** A handler wired to more than one control needs
  `sender` to tell them apart, so it always stays in code-behind - and when the controls'
  Avalonia events have different signatures (a `Button`'s real `Click` vs. a `Label`'s
  `PointerPressed`), the method is split in two, each named after its Avalonia event.
- **Bindable property coverage is deliberately small** (`BindablePropertyCatalog`):
  `Text`/`Content`/`Header`, `Checked`, `Value`, `SelectedItem`/`SelectedIndex`, `Enabled`,
  `Visible` - across the ordinary controls, the ToolStrip items (which are Direct-mapped, so
  their values are as reachable as any other control's) and `CheckedListBox`. A handler touching
  anything outside that vocabulary stays in code-behind, since the property could not be
  expressed as a `{Binding}` anyway.

  This paragraph used to name `RichTextBox.WordWrap` and `LinkLabel.LinkVisited` as WinForms-only
  properties with "no Avalonia counterpart to name". Both were wrong, and both are in the catalog
  now: `WordWrap` is a `TextWrapping` enum on the Avalonia side, and a `HyperlinkButton` really
  does have an `IsVisited`. Neither claim had ever been checked against the API - which is the
  argument for checking a table rather than remembering it.

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

  One correction came out of running the converted sample rather than reading the table:
  **`Form.Load` pairs with `Window.Opened`, and `Form.Shown` with `Loaded`** - crossed over, not
  matched by name. WinForms runs `Load` *before* the form is displayed and `Shown` once it first
  is; Avalonia raises `Opened` as the window opens and `Loaded` only after layout and render are
  complete. Pairing them by name put a `Load` handler after the window was already on screen -
  visibly late for anything that populates controls or sizes the window - and ran a form's two
  handlers in the opposite order to the original. The remaining difference is honest and reported:
  `Opened` is raised each time the window is shown, where `Load` ran once per form instance.

  Most of the rest are the `XxxChanged` family, and they share one answer: Avalonia raises nothing
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

- **A tree built at run time comes across.** `treeView1.Nodes.Add("x")` becomes
  `treeView1.Items.Add(new TreeViewItem { Header = "x" })`, `Nodes.Clear()` becomes
  `Items.Clear()`, and the node an `Add` hands back is usable as a parent - the one statement
  becomes the two it stood for (`var root = new TreeViewItem { … }; treeView1.Items.Add(root);`).
  This was refused for a long time as "an application design decision", which was wrong: Avalonia's
  `ItemsControl.Items` is a real mutable collection and a `TreeViewItem.Header` is an `object`, so
  the shape has an exact counterpart. Populating an `ObservableCollection` and binding
  `ItemsSource` is the better *end state*, but it is not what the original said.

  Only a string header - a `TreeNode` object carries an image index, a tag and children of its own,
  none of which a bare TreeViewItem has.

  `TreeView.ExpandAll()` comes across as the loop Avalonia needs for it: `ExpandSubTree` expands
  the item **and every descendant**, so running it over the root items is exactly what ExpandAll
  means. This was written off as having no counterpart, which confused "no one-call equivalent"
  with "no equivalent" - they are not the same answer.

- **Controls created at run time come across, for the plain types.** `var b = new Button { Text =
  "Go" };` becomes `var b = new Button(); b.Content = "Go";` - the initializer is the property
  writes it stands for, each through the same catalog a designer control's goes through, so an
  unknown member still stops the handler. `Location`/`Size`/`Left`/`Top` become
  `Canvas.SetLeft`/`SetTop` and `Width`/`Height`, because every container is emitted as a Canvas
  (a designer control only where its parent really is one). `Controls.Add` hands the control to the
  View's root Canvas or to a Panel that stayed a Canvas, `statusStrip.Items.Add`/`AddRange` to the
  strip's `StatusStripFallback`. A Form field of such a type (`private ToolStripStatusLabel label;`)
  is declared on the View as the Avalonia element.

  The limits are deliberate:
  - only a type whose *undecorated* mapping is the bare element (`RuntimeControlSupport`: Direct,
    no attribute, no wrapper, no package, no warning) - a fallback template, a ListView, a
    DataGridView stay refused;
  - no constructor arguments, no event subscription on the new control (`b.Click += …` - the
    handler would need its Avalonia signature, and nothing planned it);
  - only a control the body created may be given a parent: a designer control already has one,
    and where WinForms re-parents silently, Avalonia throws. A runtime *field* is detached from any
    earlier parent first, which is the WinForms behaviour spelled out;
  - a runtime field is kept only if **every** assignment to it came across (handlers, constructor,
    helpers). A field declared on the View but created in a statement that stayed a comment would
    turn each translated use into a `NullReferenceException`, so the planner settles that the same
    way it settles carried fields;
  - a GroupBox, TabPage or DataGridView as the parent, `Controls.Clear()`/`RemoveAt` and reading a
    position back (`Canvas.GetLeft` is `NaN` where WinForms had `0`) are not translated.

- **A ListView's items, on the half of the mapping that has an answer.** A ListView with neither
  `View.Details` nor parsed `ColumnHeader`s becomes a `ListBox`, and there
  `listView1.Items.Add(new ListViewItem("x"))` is exactly
  `listBox.Items.Add(new ListBoxItem { Content = "x" })`. The *other* half becomes a `DataGrid`,
  whose rows are data objects bound through columns - turning a `ListViewItem` into one would mean
  inventing a row type, so it is refused rather than flattened. A multi-column item is refused on
  both halves.

- **Fallback controls expose only what their template demonstrably has**
  (`FallbackControlMemberSupport`). Everything else about them stays conservative - no styling,
  no event wiring, no item children - because a template need not have the member a mapping
  names. Catalog members are the one safe exception, since these templates ship in this repo:
  `RichTextBoxFallback` derives from Avalonia's `TextBox`, so its
  `Text`, `Clear()`, `SelectAll()` and the four font properties it inherits from
  `TemplatedControl` are known facts.

  The templates' **own** properties were missing from that table for as long as it existed, so
  `propertyGrid1.SelectedObject = x` and the
  DomainUpDown pair all refused - not because there was nowhere to translate them, but because
  nobody had written the names down. They are registered now, and
  `FallbackControlMemberSupportTests` checks the reverse direction too: a property a template
  declares is either registered or listed as deliberately out of reach with the reason. Three are:
  `WebBrowser.Url` (a `Uri` in WinForms, a `string` on the template - a change of value shape, and
  half a pair is worse than none) and `PrintPreviewControl.Document` (a `PrintDocument`, which the
  converted code cannot produce). `BindingNavigator.Position`/`Count` used to be a third: no
  WinForms BindingNavigator has either, so no *body* can name them. That turned out to be the wrong
  question - this same table is what the AXAML emitter consults before writing a binding onto a
  fallback, and an unlisted property is dropped there in silence, which is exactly what happened to
  the navigator's own `Position` binding until it was registered. A template absent from that table behaves as before, and a binding
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
- **Inline handlers are lifted into named methods.** A lambda or an anonymous `delegate { }`
  subscribed in `InitializeComponent()` (`this.box.TextChanged += (s, e) => { ... };`) used to be
  reported and dropped - its body did not reach the generated project even as a comment.
  `Parsing/InlineHandlerLifter` now names it `{field}_{Event}` (`{Class}_{Event}` on the Form),
  turns it into a method on the class before the code-behind is analysed, and from there it is an
  ordinary handler: translated, promoted, guarded and checklisted by the same rules. An expression
  body becomes a statement, discards and a parameterless `delegate` get the conventional
  `sender`/`e`, and `async` is kept. On a name the class already declares the marker goes on the
  owner half (`goButtonInline_Click`), because command names are derived by dropping the `_Event`
  suffix. A body naming a local of `InitializeComponent` itself - designer code has none worth
  capturing - fails to translate and stays a comment like any other unknown name.
- **File dialogs** get a `StorageProvider` method on the View, but it is not tied to any
  button: the designer never records which handler opened which dialog, since handler bodies
  are not part of `InitializeComponent()`.

## Code that touches no UI: the semantic pass-through

The syntactic rules above refuse any name they cannot place - a type of the project's own, a field
of one, a BCL call outside a short list - because it *could* be a WinForms API. A second, semantic
stage answers that question instead of assuming it (`Semantics/SemanticProjectContext`): the
original project is compiled against the .NET 10 base class library **without** WinForms, so every
WinForms name binds to an error type, and code in which everything binds is provably the same code
in the generated project.

- **The project's other source files come across** when they are UI-neutral as a whole - models,
  services, a cache class - unchanged, at the same relative path. A referenced class library that
  is not itself a WinForms project comes along the same way, under a folder named after it, rather
  than as a reference to the original csproj (a .NET Framework library cannot be referenced from
  `net10.0` as it stands). A file is left behind, with the reason in `MIGRATION.md`, if it uses
  WinForms, `System.Drawing` (which binds - it is in the BCL - but means what Avalonia spells as
  brushes and `Avalonia.Point`), an `[Obsolete]`, Windows-only or experimental API (each a warning
  in a project that builds warning-free; `Microsoft.Win32.Registry` is the common one), a type of a
  file that was itself left behind, or if it would compile with **any** warning. Never copied:
  `*.Designer.cs`, `AssemblyInfo.cs`, files with assembly attributes or an entry point, and a type
  in a namespace the generated project uses itself (`{Root}.Views`, `.ViewModels`, ...). A
  `using System.Windows.Forms;` left over from a template is dropped from the copy, and the file
  gets `#nullable disable` unless the original project enabled nullable reference types.
- **A handler statement comes across verbatim** when it is UI-neutral - the last resort, after
  every rule that knows a WinForms shape has had its chance, so the translations above still win.
  It may name the Form's own members only where the View declares them too (a carried field, a
  helper that became real code and is not `async`); never `sender`/`e`, which are Avalonia objects
  in the generated method; and never `this` except as the receiver of such a member -
  `this.GetType()` asks a different object. Types outside `System` and the implicit-using
  namespaces are written namespace-qualified, so nothing can bind to an Avalonia type of the same
  name (`Mapping.Tests` checks that the simple names left alone mean the same thing in a View).
  The prefix rule is unchanged - a passed-through local at the end of a partial prefix is still
  trimmed - and a View carrying such code starts with `#nullable disable warnings`, since the code
  was not written with nullable reference types in mind.
- **Fields and helper signatures of named types.** A field whose type and initializer are
  UI-neutral is carried (`private Game _game;`, `string[] formats = { ... };`), and a helper whose
  return and parameter types are, is a candidate like a keyword-typed one.
- **The constructor's own statements** (everything but `InitializeComponent()`) are translated
  with the prefix rule too, into the generated View's constructor after its own setup. Only for a
  class whose sole constructor is parameterless - another constructor's statements would run where
  they never did. A carried field that the **un-migrated** rest of the constructor still mentions
  is not carried after all: the constructor is usually where it is created
  (`rm = new ResourceManager(...)`), and carrying it uninitialized would turn every use of it into
  a `NullReferenceException` where there used to be an honest TODO.
- **NuGet packages come across** when the code uses them and a `net10.0` project can: every
  `PackageReference` (version from the item, its `<Version>` child, or `Directory.Packages.props`)
  and every `packages.config` entry - of the project and of the class libraries it references - is
  looked up in the NuGet global packages folder, a legacy solution's `packages/` folder, and
  otherwise downloaded from nuget.org into a cache of the tool's own (`--offline` turns that off).
  Its assemblies for the best-fitting framework (`net5`-`net10`, then `netcoreapp`, then
  `netstandard`; `ref/` before `lib/`) join the semantic compilation, so a statement calling into it
  binds and passes through like BCL code, and the package is added to the generated csproj at the
  original's version - **only** if some code of the project names a type or member of it. A package
  is left out, and said so, when it has only a .NET Framework or `-windows` library (every
  WinForms control suite), when it references WinForms or WPF, when it is platform-specific
  (`[assembly: SupportedOSPlatform]`), when its version is a range or floating, or when it cannot be
  found. A polyfill .NET ships itself (`Microsoft.CSharp`, `System.Data.DataSetExtensions`) and a
  `PrivateAssets="all"` tool are dropped without a word, and a meta-package
  (`Microsoft.Windows.Compatibility`) is replaced by those of its dependencies the code uses. A
  package the conversion adds itself for a component (`System.IO.Ports` for a `SerialPort`) is not
  reported as left out.
- Not covered: extension methods from a namespace the
  generated View does not import; a static member reached through `using static`; overloaded
  helpers and fields; property accessors; and a referenced WinForms project's own plain classes,
  which belong to that project's conversion.

## Paint handlers and custom drawing

- **A `Paint` handler needs a subclass, so the conversion supplies one.** Avalonia has no `Paint`
  event; a control draws by overriding `Render(DrawingContext)`. The bundled `PaintSurfaceFallback`
  is that subclass and raises the override back as an event, and a `Panel` or `PictureBox` whose
  designer wired `Paint` is retargeted onto it. The subscription is made in the generated
  constructor - it is a CLR event on a template, not an element attribute.
- **A surface can draw or contain, not both - so a container gets a paint layer.**
  `Panel.Render` is `sealed` in Avalonia, so the surface derives from `Control` and hosts no
  children. A WinForms container's Paint draws *under* its children, though, and that is what a
  childless surface placed first in the container's Canvas, at its origin and its size, does:
  `PaintLayerInserter` moves the Paint binding of a `Panel` or `GroupBox` that has children - and
  of the Form or UserControl itself - onto a synthetic `<field>PaintLayer` there. The drawing and
  the children share one coordinate system, whatever offset the Avalonia container (a GroupBox's
  header) puts on its content. The handler's `sender` is the layer now, so a body casting it to the
  container type refuses. A Form's layer has the designed client size and does not follow a resize.
  `container.Invalidate()`/`Refresh()`, and a bare `Invalidate()` on the Form, are redirected to the
  layer: invalidating the container itself would re-render only its own chrome.
- **Other containers are not layered.** A `TabPage`, `FlowLayoutPanel` or `SplitContainer` panel
  with children keeps its children and reports that its Paint was not wired; a PictureBox that also
  carries an Image keeps the picture, since WinForms drew the handler's output over it and there is
  no honest way to do both here.
- **`protected override void OnX(...)` becomes the `X` handler it is equivalent to**
  (`OverrideHandlerLifter`), for every event the generated root raises plus `OnPaint`. The View's
  Avalonia base has no `OnLoad` to override, so the override used to stay a comment among the
  helpers. The one difference between an override and a handler is where the override's own code
  runs relative to `base.OnX(e)`, which raises the event to everyone else - so the base call must be
  the first or the last statement (or absent, when nothing else subscribes the event), and when the
  designer also wires a handler to that event it must be first: the lifted handler is subscribed
  after the designer's, from the constructor, and so runs after it. A body naming any other `base.`
  member or a `sender` stays as it was.
- **The geometric calls and every `DrawString` placement translate.** `DrawLine`,
  `DrawRectangle`, `FillRectangle`, `DrawEllipse` and `FillEllipse` in their four-coordinate form,
  plus `DrawString` at `(x, y)`, at a `PointF`/`Point`, and inside a `RectangleF`/`Rectangle` - or
  the surface's own `e.ClipRectangle`, which is already an Avalonia `Rect`. WinForms overloads the
  geometric calls over `Rectangle`, `RectangleF` and point arrays too; a different arity refuses
  rather than being guessed at, and the prefix rule leaves the rest of the handler to a human.
- **Arity cannot tell the `DrawString` overloads apart** - `(x, y)` and `(point, format)` are both
  five arguments - so the dispatch is on the *shape* of the fourth. A point or rectangle has to be
  **constructed in the call**, or be `e.ClipRectangle`: a local of type `PointF` could not survive
  the conversion at all, since `System.Drawing` is not there to declare it.
- **A layout rectangle is an origin plus a box, and the box is not decoration.** It is what made
  the text wrap, so it becomes `MaxTextWidth`/`MaxTextHeight` on the `FormattedText` - the same
  instruction, in the two properties Avalonia spells it with.
- **A `StringFormat` is only accepted alongside a layout rectangle**, and that is the substance of
  the rule rather than a simplification: alignment and trimming both describe how text behaves
  *inside a box*, and Avalonia applies neither without a `MaxTextWidth`. On the point and
  two-coordinate overloads the setting would be emitted and silently do nothing. `Alignment` and
  `Trimming` translate (see `TextFormatCatalog`); `LineAlignment` does not - vertical placement is
  something a `FormattedText` does not do at all, since `MaxTextHeight` clips rather than centres -
  nor do `FormatFlags`, the ellipsis-free `Character`/`Word` trimming modes (Avalonia has none),
  or the opaque `Generic*` statics.
- **A WinForms `Font` is one object where Avalonia wants two values.** `DrawString` needs a
  `Typeface` and an em size, and there are three shapes it can get them from: a `new Font(...)`
  literal, which goes through the same evaluator and formatters the designer path uses - so the
  point-to-pixel conversion is the one already written down, and 12pt comes out as 16px, not 12; a
  control's `Font`, read back off the four Avalonia properties this converter splits a Font into
  everywhere else, and only where that element really carries them; and the Form's own `Font`,
  which is the View's - a `Window` or a `UserControl`, both `TemplatedControl`s, which is where
  those four actually live. A `Control` has none of them, so the paint surface's own font is
  refused rather than invented.
- **`Underline` and `Strikeout` refuse.** In Avalonia they are not slant or weight but a
  `TextDecorations` collection set on the `FormattedText` afterwards, which a `Typeface` cannot
  express - so a font carrying either is refused rather than emitted without the decoration.
- **Both colour palettes resolve through the colour pipeline, not a palette table.**
  `Pens.SteelBlue`, `Brushes.SteelBlue`, `SystemBrushes.Control` and `SystemPens.Control` all
  evaluate to the same `KnownColor` the designer path already understood, and come out as explicit
  ARGB. That is deliberate: Avalonia's `Brushes` has no system colours at all, so translating a
  name to a name would emit `Brushes.Control`, which does not exist there.
- **A pen or brush the handler builds itself comes across.** `var p = new Pen(color[, width])` and
  `new SolidBrush(color)` - plain, `using var`, or a `using (...) { }` block - are built as the
  Avalonia `Pen`/`SolidColorBrush` straight away, and every drawing call (and `DrawString`'s brush)
  accepts the local where the matching argument goes. The `using` is dropped and its scope kept as
  a block: Avalonia's are managed objects, not `IDisposable`, and keeping it would be a CS1674.
  `Width` becomes `Thickness`, `Color` the brush, and `DashStyle` GDI+'s own pattern
  (`Dash` 3-1, `Dot` 1-1, `DashDot`, `DashDotDot`) in pen widths - which is how Avalonia measures
  dashes too, so the rhythm is the same, not merely "dashed". Still refused: `StartCap`/`EndCap`
  (GDI+ caps each end separately and has arrow anchors, Avalonia one cap for both ends and no
  arrows), `DashStyle.Custom`/`DashPattern`, joins, gradient and hatch brushes, and `DrawCurve`
  (a cardinal spline Avalonia has no primitive for).

## MDI

- **Avalonia has no multiple-document interface at all** - no child-window surface, no
  `LayoutMdi`, nothing to arrange. This is not a gap the conversion can close: MDI is a
  window-management model, and Avalonia's is separate top-level windows.
- What the conversion does is **say so**. `MdiMemberCatalog` names the five members
  (`IsMdiContainer`, `MdiParent`, `MdiChildren`, `ActiveMdiChild`, `LayoutMdi`) and the planner
  reports once per Form when the designer or any handler body uses one, naming the usual
  replacements: separate Windows, a `TabControl` if the children were really pages, or a docking
  library.
- Once per Form, not once per member: an MDI form names four or five of them, and the reader needs
  the fact rather than a tally. Before this, an MDI application converted in complete silence -
  `IsMdiContainer` was a Form property with no counterpart so it was dropped without a word, and
  `LayoutMdi(...)` simply stopped a handler with no explanation.

## Printing

- **There is still no printing API, and there never was one to wrap.** Measured against Avalonia's
  reference assemblies there is not one `Print*` type in the framework - no dialog, no document, no
  printer list. Everything below is the half that *is* expressible; sending a page to a printer
  needs a library, and which one is your choice.
- **What changed is that a `PrintPage` handler is drawing code.** Once `e.Graphics` calls
  translated into `DrawingContext` calls, a `PrintDocument` could render a real page - and a
  rendered page is all a preview, a page setup and an export ever needed. That is the whole reason
  this family moved from "no Avalonia API" to "converted elsewhere"; the framework did not change.
- **`PrintDialog` has no printer to offer, so it is not translated as a dialog at all.** Its one
  real shape - `if (printDialog1.ShowDialog(this) == DialogResult.OK) { printDocument1.Print(); }` -
  is matched **whole** and becomes `await printDocument1.PrintAsync(this)`, whose destination
  picker is where the dialog went. A branch doing anything other than a single `Print()` refuses,
  and the prefix rule then leaves the handler alone.
- **Every page is rendered, as WinForms printed them.** `PrintDocumentFallback.RenderPages` raises
  `PrintPage` with fresh arguments for as long as the handler sets `HasMorePages` (capped at
  `MaxPages`, 1000, where WinForms would have printed forever). `PrintAsync` writes the first page to
  the file the user picks and the others beside it as `name-2.png`, `name-3.png`, ...; where the
  storage gives no access to that folder (a browser, a sandbox) they are stacked into the one file.
  The preview scrolls through all of them. What does not come across:
  - **`BeginPrint`/`EndPrint`/`QueryPageSettings`** have no counterpart on the bundled document.
    A handler that pages with a counter on the Form (`pageNumber++`, which now translates) usually
    resets it in `BeginPrint` - so a second render (a preview, then an export) continues where the
    first stopped. Reset it before calling `PrintAsync`/showing the preview.
- **Nothing carries over from `PrinterSettings`/`PageSettings`.** Those are WinForms objects that
  do not survive; the bundled document's page size and margins start at US Letter with one-inch
  margins, in device-independent pixels at 96 dpi, and `PageSetupDialogFallback` is what changes
  them.

## Container regions

- **A `SplitContainer`'s two halves and a `ToolStripContainer`'s five regions are placed; any
  other nested region is reported.** `this.container.Region.Controls.Add(x)` is a three-level
  member access, encoded by the designer walker as a synthetic `"field.Region"` parent id and split
  apart again by `ControlGraphBuilder`. A region name it does not recognise still warns, naming
  each child - such a control used to vanish from the AXAML *and* from the report.
- **The two containers are emitted differently, because they are different shapes.** A
  SplitContainer becomes a `Grid` with a `GridSplitter` between two `Canvas` regions. A
  ToolStripContainer keeps its own five regions, each emitted as a XAML property element holding
  the bundled panel it is - which is why those properties on `ToolStripContainerFallback` are
  settable, and why each setter rebuilds the child list: a `DockPanel` lays its children out in
  order, and the content panel has to stay last to get the space the strips did not take.

## Multiple projects

`--source` takes a `.sln` or `.slnx` as well as a `.csproj`. Every WinForms project the solution
lists is converted into its own folder under the output directory, and a generated `.slnx` ties
them together - which is what `samples/convert.sh` had been doing by hand. A project the pipeline
finds nothing convertible in (a class library, a test project) is reported and skipped rather than
failing the run: a solution with non-WinForms projects in it is the normal case. Solution files are
parsed as text rather than through MSBuild, so a project path built from an MSBuild property is not
understood - and is reported rather than guessed at.

**A class library converts into a class library.** A project declaring
`<OutputType>Library</OutputType>` gets a library csproj with its Views, ViewModels and the
generated helpers - no `Program.cs`, no `App.axaml`, and none of the placeholder `MainWindowView`
an application without a Form gets. What lives in an application's `App.axaml` is reported instead:
a packaged control's `StyleInclude` (the referencing app needs it, or the control renders as
nothing) and a `NotifyIcon`. `--with-web` is not applied to a library. Only the explicit value
counts - an SDK-style csproj with no `OutputType` is a library to MSBuild too, but it is converted
as an application, because guessing from an absence is how a hand-written app csproj would become
unlaunchable.

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

## Third-party controls (`--mappings`)

A vendor control (DevExpress, Telerik, Infragistics, ...) is a type this converter cannot know, and
on its own it gets "No mapping registered" and no element. `--mappings <file.json>` lets you say
what it is:

```json
{
  "controls": {
    "TextEdit":     { "as": "TextBox", "properties": { "EditValue": "Text" }, "events": { "EditValueChanged": "TextChanged" } },
    "GaugeControl": { "element": "ProgressBar", "properties": { "Value": "Value", "Maximum": "Maximum" } }
  }
}
```

- **`as`** names a WinForms control this converter maps (`list-mappings` lists them). The vendor
  control is re-typed to it right after the designer walk - the same step that handles the project's
  own `MyButton : Button` - so every table applies: its mapper, its events, its bindable properties,
  and handler bodies that read it. `properties`/`events` rename the vendor's names to the WinForms
  control's first; anything else the vendor added is listed among the untranslated properties.
  Handler bodies are not renamed: `nameEdit.EditValue` in code still stops the prefix.
- **`as": "Form"`** (or `"UserControl"`) is for a vendor *base class* instead: a Form deriving from
  `MetroForm` is converted as a Form at all only once the mapping says what `MetroForm` is. It takes
  no `properties`/`events` - the derived Form's designer properties are read as the Form's.
- **`element`** names an Avalonia element to emit directly, with `properties` mapping designer
  property → attribute. Values are formatted by their own shape (text, number, bool, colour, a single
  enum member); anything else is declined and reported. Nothing else is known about such a control,
  so its events are only what the generic event table makes of their names. The element name cannot
  be checked - the tool never references Avalonia - so a misspelt one is a build error in the
  generated project. Only core Avalonia elements: there is no way to declare an xmlns for a package.
- The file is checked as a whole and **rejected on any error** (an unknown key, both or neither of
  `as`/`element`, an `as` target that is not mapped, a type the converter already maps): a mapping
  that silently did not apply is what the option exists to prevent. A mapped type the project
  declares itself is ignored, with a warning. JSON comments and trailing commas are allowed.

## Re-converting over output you have already migrated

Conversion is a starting point you then edit by hand, so a second run defaults to **never
destroying an existing file**: identical files are skipped, and a file whose generated content
differs from what is on disk is left alone with the regenerated version written beside it as
`<name>.w2a-new` for you to merge. The conversion summary says how many files that affected;
`--overwrite-all` restores the old clobber-everything behaviour.

This is a per-file safety net, not a merge tool - it cannot combine your edits with newly
generated code. `--force` is still required before anything is written into a non-empty output
directory at all.

**Stale files.** Every run writes `.w2a-manifest.json` into the output root: each generated path
and the SHA-256 of the content it was generated with. A file the previous manifest lists and this
run no longer produces (the View of a Form you removed or renamed in the source) is reported in the
summary and left in place. `--prune` deletes it - together with a leftover `*.w2a-new` beside it
and any folder that ends up empty - **only** if its bytes still match the recorded hash. A stale
file you have edited is never deleted; it stays listed (and reported) until you remove it. Output
written by a version without a manifest has nothing to compare against, so its first re-run cannot
prune anything.

## Window chrome and keyboard

- **The Form's designer-set window properties reach the `Window`** (`Mapping/FormWindowAttributeCatalog`).
  They used to be dropped without a word - the root element carried a title and a size, so a
  fixed, centred dialog became a resizable window opening wherever the platform liked.
  - `StartPosition`: `CenterScreen` → `WindowStartupLocation="CenterScreen"`, `CenterParent` →
    `CenterOwner`. `Manual` with a `Location` is **reported**: Avalonia's `Position` is in physical
    pixels, so a fixed value lands differently on every display scale. The `WindowsDefault*`
    values are what Avalonia does anyway.
  - `TopMost`, `ShowInTaskbar`, `Opacity`, `WindowState` - the same four `WindowPropertyCatalog`
    already translated in handler bodies, now from the designer too.
  - `FormBorderStyle`/`MaximizeBox`/`MinimizeBox`/`ControlBox`, decided as a set: a `Fixed*` style
    → `CanResize="False"`, `None` → `WindowDecorations="None"`, the two boxes → `CanMaximize`/
    `CanMinimize`. `ControlBox = false` disables both and **reports** the close button, which
    Avalonia cannot remove while keeping the title bar; a tool-window style gets an ordinary frame
    and is reported. `WindowDecorations`, not `SystemDecorations`, which Avalonia 12 marks obsolete.
  - `MinimumSize`/`MaximumSize` → `Min`/`MaxWidth`/`Height`, **approximately**, and said so: WinForms
    measures them on the outer frame and Avalonia on the client area. They are clamped to the
    designed `ClientSize`, so the usual habit of setting `MinimumSize` to the form's outer size does
    not make the window open bigger than it was drawn.
  - `KeyPreview` is carried, not as an attribute: the Form's own key handlers are subscribed in the
    tunnelling phase, which is exactly "before the focused control" (see *Keyboard handlers*).
- **`AcceptButton`/`CancelButton` → `IsDefault`/`IsCancel` on the button.** The same keypress,
  turned round: WinForms names the button on the Form, Avalonia flags the button itself. Only on
  an element that derives from `Button`; pointing at anything else is reported.
- **`TabIndex`/`TabStop` → `TabIndex`/`IsTabStop`**, on every named element (both are declared on
  `InputElement`). Avalonia orders Tab by `TabIndex` within a container just as WinForms does;
  without it the order was the order the designer *added* the controls.

## Defaults the two frameworks disagree on

A designer writes a property only when it differs from WinForms' default - so where Avalonia's
default is a different one, a control the designer said *nothing* about still converted wrong.
Each of these is now pinned on the element (and checked against Avalonia's metadata; the defaults
themselves were measured at run time, since a reference assembly does not carry them):

- **`PictureBox` → `Image` with `Stretch="None"`.** WinForms draws a picture unscaled; Avalonia's
  `Image` defaults to `Uniform`, so every converted picture was rescaled. `SizeMode` maps
  `StretchImage` → `Fill`, `Zoom` → `Uniform`, and `Normal`/`CenterImage`/`AutoSize` → `None`.
  `Normal` anchors top-left and `None` centres - they agree whenever the picture fills its box.
- **`ComboBox` → `IsEditable="True"`.** WinForms' default `DropDownStyle` is `DropDown`, which is
  editable; `DropDownList` → `False`.
- **`TrackBar` → `TickPlacement="BottomRight" TickFrequency="1"`.** WinForms draws ticks by
  default and Avalonia draws none; `TickStyle` and `TickFrequency` map when set (`Both` → `Outside`).
- **A multi-line `TextBox` wraps.** WinForms' `WordWrap` defaults to true; Avalonia's
  `TextWrapping` to `NoWrap`. An explicit `WordWrap = false` still wins. `ScrollBars` maps to the
  two `ScrollViewer.*ScrollBarVisibility` attached properties.
- **Designer decimals are read.** Every decimal a designer writes is `new decimal(new int[] { … })`
  - every `NumericUpDown`'s `Minimum`, `Maximum`, `Value` and `Increment` - and none of them was
  understood, so every converted spinner ran 0..100. A bound numeric value now also reaches the
  ViewModel initializer (as `100m` for a decimal) instead of the catalog default. On the way:
  `-2147483648` - the sign word of a negative decimal - had evaluated as *positive*, because C#
  types `2147483648` as a uint and the negation fell through.

- **`DataGridView`** carries `ReadOnly` → `IsReadOnly` (from a handler too), `MultiSelect` → `SelectionMode`
  (`Extended`/`Single`) and `AutoGenerateColumns`. The last is the case where the defaults disagree
  the other way round: WinForms generates columns unless told not to and Avalonia (measured) does
  not, so the designer's habitual `= false` is what Avalonia already does.
- **A `SplitContainer`'s splitter sits where the designer put it.** It used to be a flat
  `*,Auto,*`, so every converted splitter opened in the middle. `SplitterDistance` is Panel1's size
  in pixels and `FixedPanel` says which side keeps its pixels on a resize: `None` (the default) is
  star sizing in the designed proportion, `Panel1`/`Panel2` a fixed size on that side.
  `SplitterWidth` sizes the `GridSplitter`. `Panel1MinSize`/`Panel2MinSize` and `IsSplitterFixed`
  are not carried, and are reported.
- **A ToolTip's delays follow each tip.** WinForms keeps them on the component and Avalonia on the
  control, so `InitialDelay` → `ToolTip.ShowDelay` and `ReshowDelay` → `ToolTip.BetweenShowDelay`
  are written on every control the provider gives a tip to (`AutomaticDelay` sets both the
  WinForms way, and an explicit value wins). Only when the provider sets any of them. `AutoPopDelay`
  has no counterpart - an Avalonia tooltip stays up while the pointer is on the control - and is
  reported once per provider.

## Keyboard handlers

- **They are subscribed in the tunnelling phase.** WinForms raises `KeyDown` before the control acts
  on the key; an Avalonia `TextBox` acts on Back, Delete and the arrows (and Enter, when multi-line)
  in its own class handler and marks them handled - so a handler wired by AXAML attribute simply
  never saw those keys. Measured headlessly: attribute-wired got `A, Return, Escape, Tab`, tunnelled
  got all seven. Control-level `KeyDown`/`KeyUp` are therefore subscribed from the constructor with
  `AddHandler(InputElement.KeyDownEvent, handler, RoutingStrategies.Tunnel)`; the Form's own, only
  with `KeyPreview` (that *is* tunnelling - without it a WinForms form hears what nothing took,
  closer to Avalonia's bubbling). A side effect worth knowing: `e.Handled = true` now really keeps
  the key from the control, as it did in WinForms.
- **What a body may say.** `e.Control`/`e.Shift`/`e.Alt` → `e.KeyModifiers.HasFlag(...)`, `e.Handled`
  unchanged, and comparisons against `Keys` constants - `e.KeyCode == Keys.Enter`,
  `e.KeyData == (Keys.Control | Keys.S)` (key *and* exact modifiers), `e.Modifiers == Keys.Alt`,
  with `!=` as the negation. Only as a comparison: the two key enums name members differently
  (`Next` is `PageDown`), so `e.KeyCode.ToString()` or a `Keys` local refuses, and so does a key
  outside the same table the menu shortcuts use.
- **`KeyPress` is `TextInput`, tunnelled too - and that one was worse.** An attribute-wired
  `TextInput` handler on a TextBox saw *no typed text at all* (measured): the box takes it in its own
  handler first. So every converted character filter never ran and let everything through. It is
  now subscribed like `KeyDown`, and `e.Handled = true` really keeps the text out (measured as well).
  In the body: `char.IsDigit(e.KeyChar)` and the other `char` classifiers become
  `(e.Text ?? string.Empty).All(char.IsDigit)` - exact for a keystroke, which is one character, and
  "every character passes" for an IME committing several - and `e.KeyChar == 'x'` becomes
  `e.Text == "x"` for a printable character. A comparison with a control character (`'\r'`,
  `'\b'`) refuses: WinForms raises `KeyPress` for Enter and Backspace, Avalonia raises no
  `TextInput` for them, so the translation would compile and never be true - those belong in
  `KeyDown`. (`!char.IsControl(c) && !char.IsDigit(c)`, the classic digit filter, translates, since
  `IsControl` is simply always false here.)
- **Not translated:** `SuppressKeyPress` - Avalonia raises `TextInput` apart from `KeyDown`, so
  handling one does not cancel the other - and `KeyChar` anywhere outside those two shapes.

## Menu items

- `Checked` → `IsChecked` plus `ToggleType="CheckBox"`, without which Avalonia draws no tick.
- `ShortcutKeys` → `InputGesture` (which only *shows* it) **and** `HotKey` (which makes pressing it
  click the item - measured headlessly, with the submenu closed). Modifiers and a closed set of
  keys (letters, digits, F1-F24, navigation and editing keys); anything else is reported.
- `CheckOnClick` is reproduced in code, because an Avalonia `MenuItem` does not toggle itself
  (measured: a real activation ran the Click handler and left `IsChecked` alone). WinForms flips
  `Checked` *before* raising `Click`, so the View subscribes the toggle in its constructor and
  chains the item's own Click handlers after it, out of the AXAML - an attribute is wired during
  `InitializeComponent` and would run first. Such a handler is never promoted to a
  `[RelayCommand]`: Avalonia runs a bound command from a class handler, ahead of every instance
  handler, so it would read the state from before the click.

A designer value that every mapping of its property declines - a shortcut on a key outside the
table, say - is reported by name (`… sets ShortcutKeys = Control | BrowserBack, which a MenuItem
has no spelling for`). It was looked up, so the coverage report alone would not have seen it.

## Pointer input: drop targets and hit testing

- **`AllowDrop` → `DragDrop.AllowDrop`.** Without it Avalonia raises none of the drag events, so a
  converted drop target's handlers - translated, subscribed, compiling - never ran.
- **An element something listens on is made hit-testable.** A WinForms control is hit anywhere in
  its bounds. An Avalonia `Panel`/`Canvas`/`Border` with no `Background` is hit *nowhere*, and a
  `TextBlock` only on its glyphs - measured headlessly, not assumed. Every container becomes the
  first and every Label the second, and the designer writes `BackColor` only when it differs from
  the default; so a Panel with a `MouseDown` handler, a drop target, a Label with a `Click`, a
  tooltip or a context menu converted into something the pointer mostly went straight through.
  Such an element now gets `Background="Transparent"` (`Mapping/AvaloniaHitTestSupport`) - unless
  the designer gave it a colour, which already makes it hit-testable. An element nothing listens
  on is left exactly as it was.
- The startup tests hit-test every converted drop target at its centre and fail if the pointer
  lands anywhere else. Not covered: an `Image` (a PictureBox) has no `Background` to set, so a
  click on its transparent pixels still goes through.

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
- Controls are pinned to the size the designer recorded, with a style block per view
  (`Canvas > :is(Control)` gets `MinWidth`/`MinHeight` 0, `Canvas > :is(TemplatedControl)` gets
  `Padding="4,1"`). Avalonia's default theme is built for touch: a 23-pixel TextBox renders 32
  pixels tall with padding to match, and on an absolutely-positioned Canvas that silently covers
  whatever the designer put 26 pixels below it. Both setters are needed - dropping the minimum
  alone only trades the overlap for clipped text. A designer-set `Padding` on the element itself
  still wins over the style.
- **Two WinForms events that map onto one Avalonia event both run now.** A PictureBox's `Click`
  and `MouseDown` are both `PointerPressed`, and emitting both as attributes is a duplicate XML
  attribute - which does not merge, it fails to parse. Only the *attribute* is exclusive though:
  an event takes any number of handlers, so the second is subscribed from the constructor with a
  `+=` instead of being dropped. It is still reported, because the two now run at the same moment
  where WinForms raised `Click` on the button coming back up.
- **A bundled fallback can carry event attributes.** It used to carry none: the emitter's "is this
  a direct Avalonia element" test doubled as its "can this be wired" test, so a designer-wired
  handler on a converted ToolStrip button or paint surface was reported and dropped. Only the
  events `Control` itself declares are allowed, listed in `FallbackControlMemberSupport` and
  checked against Avalonia's real `Control`; an event a specific template adds is still refused.
- Absolute layout is emitted everywhere *except* where the parent element holds items rather
  than positioned children - a `TabControl`'s pages, a `Menu`'s items, a `DataGrid`'s columns
  (`AxamlEmitter.HostsItems`). A `TabPage`'s WinForms bounds are the tab control's client area,
  not a position anyone chose, and in Avalonia `Width`/`Height` on a `TabItem` size its *tab*:
  the sample's nine 992x602 pages each produced a 602-pixel-tall tab header, which filled the
  window and pushed every page out of it. Adding a mapper whose target holds items means adding
  it to that set.

## Controls promoted out of a bundled fallback

Avalonia 12 ships two controls this converter used to approximate with a bundled template, so
both are now Direct mappings and the templates are gone.

- **`GroupBox`.** The old `GroupBoxFallback` was a bare `Canvas`: no border, no caption on screen,
  and - less obviously - the wrong origin for its children. A WinForms child's `Location` is
  relative to its parent's *client* area, and a `GroupBox`'s client area starts below the border
  and caption; a plain Canvas measured those coordinates from the outer corner instead. A real
  `GroupBox` puts its content below the header, so this is closer as well as prettier. What
  remains is a few pixels: the Simple theme spends 6px on its border columns where WinForms spends
  3, and the caption row's height depends on the font. The mapper pins `Padding="0"` so the
  view-level `Canvas > :is(TemplatedControl)` style does not add another 4px on top of that; a
  designer-set `Padding` still wins.
- **`MaskedTextBox`.** The old fallback stored the `Mask` string and ignored it, and the mapper
  did not even emit it. Avalonia's own `MaskedTextBox` really masks - it overrides `OnTextInput`
  and `OnKeyDown` - so `Mask`, `PromptChar`, `AsciiOnly`, `HidePromptOnLeave`, `ResetOnPrompt` and
  `ResetOnSpace` all now do something. `TextMaskFormat` and `Culture` are not carried.

Two more mappings gained precision rather than a new control:

- **`DateTimePicker`** picks its element per instance: `Format=Time` becomes a `TimePicker`
  (which is what the designer asked for and a calendar cannot express), everything else a
  `CalendarDatePicker`. `Format=Custom` keeps the date picker and reports that `CustomFormat` has
  no counterpart. A handler binding `Value` on a `Format=Time` picker is refused rather than
  emitted, because the catalog's answer (`SelectedDate`) does not exist on a `TimePicker`.
- **`CheckedListBox`** becomes a `ListBox` whose `ItemTemplate` holds a real `CheckBox`. Avalonia
  has no checkbox list, but it has a template - so the tick gets a row object of its own, generated
  into `Models/<Field>Item.cs` as `{ Text, IsChecked }`. The shape is not invented: a
  CheckedListBox item *is* a caption and a tick, which is the control's own contract. (Contrast a
  Details-mode ListView, where deriving names from column headers would have invented domain
  vocabulary, so the row is a `string[]`.)
  - The row type is an `ObservableObject`, and that is load-bearing rather than idiom. The binding
    writes the tick back when the user clicks it either way - but a handler calling
    `SetItemChecked` writes it from the other side, and without a change notification the box on
    screen would keep showing the old state.
  - `SelectionMode="Multiple"` is **gone**, and its going is the point. It stood in for "several
    items are ticked at once", which was defensible only while the tick had nowhere else to live.
    WinForms tracked checked and selected separately, and so does this now.
  - The designer's `Items` entries become the collection's initial contents rather than literal
    item elements: a templated ListBox binds its rows, it does not host them.
  - `SetItemChecked`/`GetItemChecked` translate, in both directions, onto the row. `CheckedItems`
    and `CheckedIndices` do not - they are WinForms collection types, and handing back a LINQ query
    that merely looks like one would let `.Add` or `.Count` compile against something that is not
    the same object. Read the generated collection instead.

## Data binding: `BindingSource`

A control whose designer `DataSource` pointed at a `BindingSource` now gets a real
`ItemsSource="{Binding …}"`, and the ViewModel gets the `ObservableCollection<object>` behind it.
The generated `DataGridTextColumn`s already bind with `{ReflectionBinding}`, so they resolve
against whatever the rows turn out to be - the columns start working the moment rows exist.

**One population shape does come across.**
`bindingSource1.DataSource = new BindingList<T> { new T { P = v, … }, … };` becomes a `Clear()`
plus one `Add` per element on the ViewModel collection. That is *not* the body rewriter learning
object initializers - every degree of freedom is closed by a fact this run already proved:

- it matches only as the right-hand side of a `DataSource` assignment the **designer** already
  turned into an `ItemsSource` binding - never a free-standing expression, a local initializer or
  an argument, and `TryRewriteExpression` gains no new case at all;
- the element type must be one **this run** lifted into `Models/`, so its settable auto-properties
  are read off the parsed declaration rather than guessed, and it must be the same type the plan
  already recorded - a disagreement would be a `CS0029` in the generated project;
- every initializer name must be one of those properties, named once;
- every value must already translate on its own;
- the wrapper must be a `BindingList`/`List`/`ObservableCollection`/`Collection`. A `HashSet<T>`
  is refused: copying it into an ordered collection element by element is not the same program.

Everything else still refuses, and still stops the handler at that statement: a row type from a
referenced assembly, a constructor argument, a nested initializer, a list built with `Add` calls or
a loop, and any `DataSource` that is not this literal shape (a `DataTable`, a query, a field).

**A grid handed its rows directly does come across.** `dataGridView1.DataSource = orders;` - on a
grid with no designer `DataSource` - becomes `dataGridView1.ItemsSource = orders;`, and `= null`
becomes `ItemsSource = null`. The value is proven rather than guessed: it must pass through as
UI-neutral code, and the semantic model must say it is a plain `IList` (a `List<T>`, an array, an
`ObservableCollection<T>`), which WinForms shows one row per element of, reading public properties -
the same thing Avalonia's grid does. Refused, and still stopping the handler:

- a `DataTable`, `DataSet` or `DataView` - WinForms reads those through property descriptors
  (`IListSource`), Avalonia's grid has no counterpart, so their rows would show no columns;
- a bare `IEnumerable` such as an unmaterialised LINQ query - WinForms throws on it too;
- a `BindingSource`, or anything else that names a WinForms type;
- a grid with designer columns *and* WinForms' default `AutoGenerateColumns = true`: WinForms adds
  only the properties those columns do not already show, Avalonia would add every one again.

Where the grid had no columns of its own and the designer left the default, the translation also
writes `AutoGenerateColumns = true`: WinForms generates the columns unless told not to, Avalonia
only when told to - so without it the rows arrive and nothing shows them. A handler that sets
`AutoGenerateColumns` itself still stops there.

What *is* now possible is writing that population by hand, because the row type exists: a type
declared **inside** a Form or UserControl is lifted into `Models/<Name>.cs` as a `public` type
in the `<project>.Models` namespace. `public`, not `internal`: the ViewModel's generated
`ObservableCollection<T>` is a public property, and a public member cannot expose an internal type
(`CS0053`) - a build error in the *generated* project and nowhere else. WinForms forms routinely keep their row type as a private
nested class, and until now it reached the generated project only inside the "NOT COMPILED"
comment block - so the code a human then migrated had nothing to name. A nested type that mentions
something which does not survive the conversion is refused and reported, the same rule a
carried-over `Component` follows.

`BindingSource` is therefore `FeatureElsewhere` rather than `NoAvaloniaApi`: it emits no element,
but it is genuinely converted - binding, collection, element type and, for the shape above, the
rows. It stayed guidance-only for as long as the collection was generated empty, because a seam is
not a converted feature.

## Details-mode ListView rows

A `View.Details` ListView becomes a `DataGrid`, and its rows are the sub-item texts of its
`ListViewItem`s - so a row is a `string[]` and column *i* binds to `[i]`. `Items.Add(...)` and
`Items.Clear()` translate onto the ViewModel collection behind it, the same place a `BindingSource`
puts its rows.

No row type is derived from the column headers, deliberately: that would invent domain names the
original never wrote, and has no answer for a blank or duplicated header. The array length must
equal the designer's column count - a mismatch is refused rather than padded or truncated - and a
Details ListView with **no** columns gets no collection at all, because there is no row shape to
translate into.

This also fixed a silent hole. Those `DataGridTextColumn`s previously carried a `Header` and **no
`Binding`**, so the grid could never show a row - not after any amount of hand migration, until
somebody noticed and wrote the bindings themselves.

## Browser (WebAssembly) head

Only produced by `--with-web`. Building it needs the `wasm-tools` workload
(`dotnet workload install wasm-tools`); nothing below applies to the desktop head, which behaves
exactly as the single-project output always did.

- **There is no `Window` in a browser.** Avalonia's browser backend installs a single-view
  lifetime and has no windowing platform at all, so a `Window` cannot be instantiated there -
  not shown, not constructed. This is the constraint everything else here follows from.
- The startup Form's View is therefore rooted at a `UserControl`, with a generated
  `<Name>Window` wrapping it for the desktop head. That wrapper carries the title, the size and
  the events only a `Window` raises, and forwards those into the View - so a `FormClosing`
  confirmation still runs on the desktop and simply never fires in the browser, where nothing
  closes a window.
- **Every other Form is still a `Window`.** Opening one - `Show()`, `ShowDialog()`, a
  `DialogResult` comparison - works on the desktop head and throws in the browser. Converting
  them all would mean inventing an overlay-based dialog stack the original never described, which
  is not something this converter does.
- Anything reaching the hosting window from the main View (`Close`, `Activate`, `Title`,
  `Topmost`, `WindowState`, `ShowInTaskbar`, a dialog owner) is emitted through the generated
  `ViewWindow.Of(this)` helper. Same story: fine on the desktop, throws in the browser, with a
  message naming what to rework.
- `NotifyIcon` → `TrayIcon` is emitted into `App.axaml` and is inert in a browser - there is no
  system tray.
- The Windows-flavoured packages a component can pull in - `System.IO.Ports`,
  `System.Diagnostics.EventLog`, `System.Diagnostics.PerformanceCounter`,
  `System.ServiceProcess.ServiceController`, `System.Windows.Extensions` - are `net10.0`
  libraries, so a `net10.0-browser` project references them happily and the build says nothing.
  They throw `PlatformNotSupportedException` the first time the converted code touches one. The
  conversion lists whichever ones are present in `MIGRATION.md`; none of them can be fixed by
  the conversion, because a serial port and the Windows event log genuinely are not in a browser.
- File dialogs already go through `TopLevel.StorageProvider`, which the browser backend
  implements over the File System Access API - so those do work, subject to what the browser
  allows.
- The two csproj properties that make the head build are easy to lose and both fail *silently*:
  without `<RuntimeIdentifier>browser-wasm</RuntimeIdentifier>` the WebAssembly SDK targets never
  run, the project compiles to plain IL and reports success while producing no `AppBundle`; and
  without `<WasmExtraFilesToDeploy Include="wwwroot\**" />` the bundle is built with no
  `index.html` to open. `WebHeadConversionBuildTests` asserts on the bundle's contents rather
  than the exit code for exactly this reason.
