# WinForms → Avalonia control/component mapping status

Status of every built-in WinForms control/component against this tool's actual mapping
registry, `src/WinFormsToAvalonia.Core/Mapping/DefaultControlMappers.cs`. This table is a
snapshot, not generated automatically — if it drifts from that file (the way
`docs/known-limitations.md`'s `ListView` line once did), trust the code and fix this doc.
See `docs/known-limitations.md` for the structural parsing gaps referenced throughout.

**Legend**: ✅ Direct = maps to a real, working Avalonia control. ✅ Fallback = maps to one of
this tool's bundled placeholder controls (`src/WinFormsToAvalonia.FallbackControls/Templates/`).
❌ Unsupported = registered with guidance text, but produces no Avalonia element — which is a
fact about the *emitter*, not about whether the type was converted; see **Why not** below, and
note that 20 of the 33 are converted, just not as elements. 🚧 Not
converted = a different problem entirely: the whole artifact kind is currently excluded from the
conversion pipeline, so it never reaches the mapping registry at all. — = base/abstract class,
never instantiated directly by designer code, so it has no registry entry at all.

**Why not**: only filled in for ❌ rows, because "no Avalonia element" covers three very different
situations and reading the table gave no way to tell them apart. 🟡 Elsewhere = the feature *is*
converted, just not as an element — a `Timer` becomes a `DispatcherTimer` field, an `ImageList`'s
images become files in `Assets/`; the Notes say where it went, and there is nothing for a reader to
do. ⚪ Unreachable = designer code never instantiates one (a `DataGridViewColumn`'s `CellTemplate`
is set by its own constructor, `ToolStripDropDown` is a base class), so the entry exists only so an
unusual input reports instead of hitting the generic "no mapping registered" message. ❌ No API =
Avalonia has nothing to map to; permanently manual. This column is not free-form — it comes from
`UnsupportedDisposition`, a required constructor argument on `UnsupportedControlMapper`, and
`ControlsDocumentationTests` checks every cell against it.

**Summary**: 47 Direct, 12 Fallback (59 mapped) · 21 converted without an element ·
12 not converted (8 unreachable from designer code, 4 no Avalonia API) ·
10 base classes (not applicable) · `Form` and `UserControl` are both conversion roots (a Form
becomes a `Window`, a UserControl an Avalonia `UserControl`), never looked up in this table.

These counts are checked against the rows below, which are in turn checked against the registry -
they were wrong before anything checked them. Each type appears exactly once, which is also
checked: `LinkLabel` and `PrintPreviewDialog` each had a second, cross-referencing row, and because
the summary counts rows rather than types it agreed with itself while being two too high.

> `MessageBoxFallback` is a bundled template too, but deliberately not part of these counts: it
> is not a control mapping at all. Nothing in the AXAML ever references it — it is pulled in by
> `HandlerBodyRewriter` when a translated handler body calls `MessageBox.Show(...)`.

> `PrintDocument`, `SerialPort` and `SoundPlayer` were not part of the requested control list, so
> they used to be named only in this note. They have real rows now: `ControlsDocumentationTests`
> checks this table against the registry in both directions, and a type the registry maps with no
> row here is a failure — a table that quietly omits something is worse than one that disagrees,
> because there is nothing to notice.

## Controls

### Basic / Container Controls

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `Control` | — | | | Base class, never instantiated directly. |
| `Form` | ✅ Converted | `Window` | | Always the conversion root (becomes a View), never looked up in the mapping table. |
| `UserControl` | ✅ Converted | `UserControl` | | A conversion root like `Form`, not a table entry. A project's *own* UserControls additionally get a per-run `UserControlMapper`, so a Form hosting one emits the generated View element. See [Implementation plan](#usercontrol-conversion). |
| `ScrollableControl` | — | | | Base class. |
| `ContainerControl` | — | | | Base class. |
| `Panel` | ✅ Direct | `Canvas` / `PaintSurfaceFallback` | | A childless Panel with a designer-wired `Paint` handler becomes the bundled paint surface instead — see [Implementation plan](#paint-handlers). |
| `SplitContainer` | ✅ Direct | `Grid` | | `Panel1`/`Panel2` children map either side of a `GridSplitter` (`Orientation=Horizontal` → stacked rows, default `Vertical` → side-by-side columns). |
| `Splitter` | ✅ Direct | `GridSplitter` | | WinForms' standalone docked drag-handle. Under the fixed Canvas layout strategy it is emitted as a positioned element; its original `Dock` stays in the `w2a:LayoutHint` attached property. |
| `TabControl` | ✅ Direct | `TabControl` | | |
| `TabPage` | ✅ Direct | `TabItem` | | Children wrapped in a `Canvas`. |
| `FlowLayoutPanel` | ✅ Direct | `Canvas` | | Flow layout semantics not translated. |
| `TableLayoutPanel` | ✅ Direct | `Canvas` | | Row/column layout semantics not translated. |
| `GroupBox` | ✅ Direct | `GroupBox` | | Children wrapped in a `Canvas`. Avalonia 12 ships a real `GroupBox`, so the bundled fallback is gone. |
| `Label` | ✅ Direct | `TextBlock` | | |
| `LinkLabel` | ✅ Direct | `HyperlinkButton` | | Its `LinkClicked` maps to `Click`; the `LinkLabelLinkClickedEventArgs.Link` payload has no equivalent. |

### Buttons / Selection Controls

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `ButtonBase` | — | | | Base class. |
| `Button` | ✅ Direct | `Button` | | |
| `CheckBox` | ✅ Direct | `CheckBox` | | |
| `RadioButton` | ✅ Direct | `RadioButton` | | |

`LinkLabel` belongs here too and is listed once, under Basic / Container Controls.

### Text Input Controls

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `TextBoxBase` | — | | | Base class. |
| `TextBox` | ✅ Direct | `TextBox` | | |
| `RichTextBox` | ✅ Fallback | `RichTextBoxFallback` | | |
| `MaskedTextBox` | ✅ Direct | `MaskedTextBox` | | Avalonia's own masks for real (`Mask`, `PromptChar`, `AsciiOnly`, …); the bundled fallback stored the mask and ignored it. |

### List / Selection Controls

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `ListControl` | — | | | Base class. |
| `ListBox` | ✅ Direct | `ListBox` | | Designer-declared literal `Items` entries are emitted as `ListBoxItem` children. |
| `CheckedListBox` | ✅ Direct | `ListBox` | | `SelectionMode="Multiple"`, plus a warning: Avalonia has no per-item checkbox list, so ticking is approximated by selection and `CheckedItems` has no equivalent. Literal `Items` entries are translated. |
| `ComboBox` | ✅ Direct | `ComboBox` | | Designer-declared literal `Items` entries are emitted as `ComboBoxItem` children. |
| `ListView` | ✅ Direct | `DataGrid` / `ListBox` | | Per-instance (`ListViewMapper`): `View=Details`, or any parsed `ColumnHeader` children, → `DataGrid` with its columns; otherwise `ListBox`. Items are not translated either way — the control is emitted without rows. |
| `TreeView` | ✅ Direct | `TreeView` | | |
| `PropertyGrid` | ✅ Fallback | `PropertyGridFallback` | | Reflection-based name/value editor. No categories, nested objects or custom editors. See [Implementation plan](#propertygrid). |
| `DataGridView` | ✅ Direct | `DataGrid` | | Requires the `Avalonia.Controls.DataGrid` NuGet package; all six column types nest under `<DataGrid.Columns>`. |
| `DomainUpDown` | ✅ Fallback | `DomainUpDownFallback` | | The fallback takes no item elements, so its designer-declared `Items` are **reported** rather than emitted — populate by hand or bind from the ViewModel. |
| `NumericUpDown` | ✅ Direct | `NumericUpDown` | | |

### Date / Time Controls

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `DateTimePicker` | ✅ Direct | `CalendarDatePicker` / `TimePicker` | | Per-instance (`DateTimePickerMapper`): `Format=Time` becomes a `TimePicker`, everything else a `CalendarDatePicker`. `Format=Custom` keeps the date picker and reports that `CustomFormat` has no counterpart. |
| `MonthCalendar` | ✅ Direct | `Calendar` | | Selection ranges not translated. |

### Visual Controls

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `PictureBox` | ✅ Direct | `Image` / `PaintSurfaceFallback` | | With a `Paint` handler and no image it becomes the bundled paint surface. Otherwise: its `Image` is recovered from the form's `.resx` and copied into `Assets/`, then bound via `Source`. A payload `ResxImageExtractor` can't decode is reported instead of emitted. |
| `ProgressBar` | ✅ Direct | `ProgressBar` | | |
| `TrackBar` | ✅ Direct | `Slider` | | Its `Scroll` maps to `ValueChanged` (which also fires on programmatic changes). |
| `HScrollBar` | ✅ Direct | `ScrollBar` | | Fixed `Orientation="Horizontal"`; its `Scroll` maps to Avalonia's `ScrollBar.Scroll`. |
| `VScrollBar` | ✅ Direct | `ScrollBar` | | Fixed `Orientation="Vertical"`; its `Scroll` maps to Avalonia's `ScrollBar.Scroll`. |

### Menu / Toolbar Controls

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `MenuStrip` | ✅ Direct | `Menu` | | Items are real `MenuItem`/`Separator` children (nested `DropDownItems` become nested `MenuItem`s) — promoted from `Fallback` once item parsing landed; `MenuStripFallback` was deleted as superseded. |
| `ToolStrip` | ✅ Fallback | `ToolStripFallback` | | Now a `StackPanel` hosting its real `ToolStripButton`/`Label`/`ComboBox`/`TextBox`/`ProgressBar` item children. |
| `StatusStrip` | ✅ Fallback | `StatusStripFallback` | | Now a `StackPanel` hosting its real `ToolStripStatusLabel` item children. |
| `ContextMenuStrip` | ❌ Unsupported | | 🟡 Elsewhere | The component itself has no element — but `this.someControl.ContextMenuStrip = this.contextMenuStrip1` assignments ARE now translated automatically into a nested `<Control.ContextMenu>` on the target control. See [Implementation plan](#contextmenustrip). |
| `ToolStripContainer` | ✅ Fallback | `ToolStripContainerFallback` | | Builds the 5-region docked layout; nested content is not placed, but every control added to a region is now **reported by name**. |
| `ToolStripPanel` | ✅ Fallback | `ToolStripPanelFallback` | | |
| `ToolStripContentPanel` | ✅ Fallback | `ToolStripContentPanelFallback` | | |

### ToolStrip Items

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `ToolStripItem` | — | | | Base class. |
| `ToolStripButton` | ✅ Direct | `Button` | | |
| `ToolStripLabel` | ✅ Direct | `TextBlock` | | |
| `ToolStripTextBox` | ✅ Direct | `TextBox` | | |
| `ToolStripComboBox` | ✅ Direct | `ComboBox` | | |
| `ToolStripDropDownButton` | ✅ Direct | `Button` | | `DropDownItems` nest through a two-level `Button.Flyout` > `MenuFlyout` child wrapper. |
| `ToolStripSplitButton` | ✅ Direct | `SplitButton` | | Same two-level `SplitButton.Flyout` > `MenuFlyout` wrapper. |
| `ToolStripSeparator` | ✅ Direct | `Separator` | | |
| `ToolStripControlHost` | ❌ Unsupported | | 🟡 Elsewhere | The host has no element of its own, but `new ToolStripControlHost(this.someControl)` is translated — the hosted control is emitted in its place. Only an argument that is not a designer field reaches this entry. See [Implementation plan](#toolstripcontrolhost). |
| `ToolStripProgressBar` | ✅ Direct | `ProgressBar` | | |
| `ToolStripStatusLabel` | ✅ Direct | `TextBlock` | | |
| `ToolStripDropDown` | ❌ Unsupported | | ⚪ Unreachable | Base class for drop-down surfaces — rarely instantiated directly. |
| `ToolStripMenuItem` | ✅ Direct | `MenuItem` | | Nested `DropDownItems` become nested `MenuItem`/`Separator` children automatically. |

### Web / Document Controls

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `WebBrowser` | ✅ Fallback | `WebBrowserFallback` | | Visible placeholder showing the designer's `Url`. Avalonia ships no webview and the community ones are platform-specific extra dependencies. See [Implementation plan](#webbrowser). |
| `WebBrowserBase` | — | | | Base class. |
| `PrintPreviewControl` | ✅ Fallback | `PrintPreviewControlFallback` | | Page-shaped placeholder — Avalonia has no printing API to preview from. |

The *dialog* it belongs to, `PrintPreviewDialog`, is listed once, under Common Dialog Components.

## Components

### Common Dialog Components

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `OpenFileDialog` | ❌ Unsupported | | 🟡 Elsewhere | Now generates an async `[RelayCommand]` ViewModel stub calling `TopLevel.StorageProvider.OpenFilePickerAsync`. See [Implementation plan](#file-dialogs). |
| `SaveFileDialog` | ❌ Unsupported | | 🟡 Elsewhere | Same as above, calling `SaveFilePickerAsync`. |
| `FolderBrowserDialog` | ❌ Unsupported | | 🟡 Elsewhere | Same as above, calling `OpenFolderPickerAsync`. |
| `ColorDialog` | ❌ Unsupported | | 🟡 Elsewhere | No built-in Avalonia colour picker *dialog*, but there is a real `ColorView` — so a handler's `ShowDialog` is translated inline onto the bundled `ColorDialogFallback`, in both the `== DialogResult.OK` and the guard-clause shape. Needs the `Avalonia.Controls.ColorPicker` package. A seed value assigned before the call is not carried over. |
| `FontDialog` | ❌ Unsupported | | 🟡 Elsewhere | No Avalonia equivalent, so the bundled `FontDialogFallback` provides one, listing `FontManager.Current.SystemFonts`. Family/size/bold/italic only. |
| `PrintDialog` | ❌ Unsupported | | ❌ No API | No Avalonia printing API — not a dialog, not a printer list. Pick a printing library; the `ShowDialog() == DialogResult.OK` handler is left whole. |
| `PageSetupDialog` | ❌ Unsupported | | ❌ No API | Pure data entry, but the `PageSettings` it produces has nothing on the Avalonia side to consume them. |
| `PrintPreviewDialog` | ❌ Unsupported | | ❌ No API | Nothing to preview from. The *control*, `PrintPreviewControl`, does get a placeholder fallback; a dialog over a `PrintDocument` has no honest stand-in. |
| `PrintDocument` | ❌ Unsupported | | ❌ No API | `PrintPage` drew with `System.Drawing.Graphics`. The handler is emitted with its body preserved but nothing subscribes it — the event has no Avalonia counterpart. |

### Data Binding Components

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `BindingSource` | ❌ Unsupported | | 🟡 Elsewhere | Becomes an `ObservableCollection<T>` on the ViewModel plus an `ItemsSource` binding; the row type is lifted into `Models/` and the literal `new BindingList<T> { … }` population is translated. |
| `BindingNavigator` | ✅ Fallback | `BindingNavigatorFallback` | | A `StackPanel` (it is a `ToolStrip` subclass, so its designer-declared items render for real). Bound to a `BindingSource` some control uses, it navigates: `Count` follows the collection, `Position` is shared two-way with the bound control's `SelectedIndex`, and each designer-recorded `Move*Item` button is wired. See [Implementation plan](#bindingnavigator). |

### Timer / Background Components

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `Timer` | ❌ Unsupported | | 🟡 Elsewhere | Generates a `DispatcherTimer` field, its `Interval` and its `Tick` wiring **on the View**, gated on the component actually having a `Tick` handler. A handler body can drive it (`Enabled`, `Start()`, `Stop()`; `Interval` write-only). See [Implementation plan](#timer). |
| `BackgroundWorker` | ❌ Unsupported | | 🟡 Elsewhere | Emitted as a **real field** on the View (`ComponentFieldCatalog`), designer values and events wired, so handler bodies keep working. `Task.Run` with `IProgress<T>` is the better end state, but that is a redesign, not a migration step. |
| `FileSystemWatcher` | ❌ Unsupported | | 🟡 Elsewhere | Emitted as a **real field** on the View — same .NET type, designer values applied, events subscribed. See [Implementation plan](#framework-agnostic-components). |

### Windows / System Components

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `Process` | ❌ Unsupported | | 🟡 Elsewhere | Emitted as a **real field** on the View; nested paths like `process1.StartInfo.FileName` translate too. |
| `EventLog` | ❌ Unsupported | | 🟡 Elsewhere | Real field, but **built lazily** — Windows-only, so eager construction made the converted app unlaunchable elsewhere. Needs the `System.Diagnostics.EventLog` package. |
| `PerformanceCounter` | ❌ Unsupported | | 🟡 Elsewhere | Real field, built lazily (Windows-only). Needs the `System.Diagnostics.PerformanceCounter` package. |
| `ServiceController` | ❌ Unsupported | | 🟡 Elsewhere | Real field, built lazily (Windows-only). Needs the `System.ServiceProcess.ServiceController` package. |
| `SerialPort` | ❌ Unsupported | | 🟡 Elsewhere | Real field, eagerly — it looks Windows-shaped and is not, which was checked against a real build rather than assumed. Needs the `System.IO.Ports` package. |
| `SoundPlayer` | ❌ Unsupported | | 🟡 Elsewhere | Real field, built lazily (Windows-only). There is no Avalonia audio API either, so a cross-platform library is the eventual answer. |

### UI Helper Components

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `ToolTip` | ❌ Unsupported | | 🟡 Elsewhere | The component itself has no element, but `SetToolTip(...)` calls are now translated automatically into a `ToolTip.Tip` attribute on the target control. See [Implementation plan](#tooltip). |
| `HelpProvider` | ❌ Unsupported | | 🟡 Elsewhere | The component has no element, but `SetHelpString(ctrl, "…")` is now translated into `AutomationProperties.HelpText` on the target. The F1 gesture has no equivalent, so `SetShowHelp` and `HelpNamespace` are reported instead. See [Implementation plan](#helpprovider). |
| `ErrorProvider` | ✅ Fallback | `ErrorProviderFallback` | | An attached property rather than a control, so it has no element of its own — and `errorProvider1.SetError(ctrl, "…")` in a handler body is translated into a static call on the template. |
| `NotifyIcon` | ❌ Unsupported | | 🟡 Elsewhere | Aggregated across all forms into App.axaml's `TrayIcon.Icons`. The icon is copied into `Assets/` from either a literal path or the form's `.resx`; only when it is neither — a computed `Icon`, or an undecodable payload — is the block emitted **commented out** with a TODO, because Avalonia resolves `TrayIcon.Icon` at run time and a dangling asset reference throws out of `App.Initialize()`. A designer-wired `Click` becomes `TrayIcon.Clicked`, subscribed from the View's constructor; the events Avalonia's TrayIcon does not have are reported. Its `ContextMenuStrip` becomes `TrayIcon.Menu` as a `NativeMenu`. See [Implementation plan](#notifyicon). |
| `ImageList` | ❌ Unsupported | | 🟡 Elsewhere | The type has no element, but its images are extracted: the `.resx` `ImageStream` is decoded into `Assets/<field>_<index>.png`, and an `ImageIndex` into it becomes a `MenuItem.Icon` — the only per-item image slot Avalonia has. See [Implementation plan](#imagelist). |

## DataGridView Related Types

### DataGridView Columns

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `DataGridViewColumn` | — | | | Base class. |
| `DataGridViewTextBoxColumn` | ✅ Direct | `DataGridTextColumn` | | Nested under `<DataGrid.Columns>`. |
| `DataGridViewCheckBoxColumn` | ✅ Direct | `DataGridCheckBoxColumn` | | |
| `DataGridViewComboBoxColumn` | ✅ Direct | `DataGridTemplateColumn` | | Avalonia has no `DataGridComboBoxColumn` — a `ComboBox` cell template instead. |
| `DataGridViewButtonColumn` | ✅ Direct | `DataGridTemplateColumn` | | `Button` cell template, carrying the column's `Text` as its `Content`. |
| `DataGridViewImageColumn` | ✅ Direct | `DataGridTemplateColumn` | | `Image` cell template. |
| `DataGridViewLinkColumn` | ✅ Direct | `DataGridTemplateColumn` | | `HyperlinkButton` cell template. |

`DataPropertyName` becomes the column's `Binding` on the two types Avalonia gives one to. It is a
`{ReflectionBinding}` rather than a `{Binding}` on purpose: the generated view's root carries an
`x:DataType`, which compiles every `{Binding}` inside it against the *ViewModel*, while a column's
path names a member of the **row** object — a plain `{Binding}` failed the generated build outright
with AVLN2000. The template columns stay unbound (`DataGridTemplateColumn` has no `Binding`, and
which member of the cell element the value belongs to is not decidable), but their `TODO` comment
now names the `DataPropertyName` the designer recorded.

### ListView Columns

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `ColumnHeader` | ✅ Direct | `DataGridTextColumn` | | `Text` → `Header`, `Width` → `Width`; nested under the `<DataGrid.Columns>` of the `DataGrid` a Details-mode `ListView` maps to. |

### DataGridView Cells

| WinForms type | Status | Avalonia target | Why not | Notes |
|---|---|---|---|---|
| `DataGridViewCell` | — | | | Base class. |
| `DataGridViewTextBoxCell` | ❌ Unsupported | | ⚪ Unreachable | See [Implementation plan](#datagridview-columns). |
| `DataGridViewCheckBoxCell` | ❌ Unsupported | | ⚪ Unreachable | " |
| `DataGridViewComboBoxCell` | ❌ Unsupported | | ⚪ Unreachable | " |
| `DataGridViewButtonCell` | ❌ Unsupported | | ⚪ Unreachable | " |
| `DataGridViewImageCell` | ❌ Unsupported | | ⚪ Unreachable | " |
| `DataGridViewLinkCell` | ❌ Unsupported | | ⚪ Unreachable | " |
| `DataGridViewHeaderCell` | ❌ Unsupported | | ⚪ Unreachable | " |

---

## Implementation plan

Everything marked ❌ above, grouped by the actual engineering work it would take — not
per-type, since most types within a family need the exact same change. Grouped this way, most
of the remaining Unsupported entries collapse into a handful of real features. (`DomainUpDown`,
`ToolStripContainer`/`Panel`/`ContentPanel`, `SplitContainer`, `PropertyGrid`, `WebBrowser`,
`BindingNavigator`, `PrintPreviewControl`, the DataGridView column family, the ToolStrip
drop-down buttons, `ListView`'s columns and `UserControl` conversion are all done, and
`ToolTip`'s, `ContextMenuStrip`'s, the file dialogs', `Timer`'s, and `NotifyIcon`'s underlying
features are done even though their registry entries stay `Unsupported`; see their subsections
below for what shipped.)

### ToolStripItem tree and menu/tool/status content

**Done.**
`DesignerSyntaxWalker.HandleInvocation`
(`src/WinFormsToAvalonia.Core/Parsing/DesignerSyntaxWalker.cs`) now also recognizes
`.Items.Add`/`.DropDownItems.Add` (generalized alongside `.Controls.Add`/`.Columns.Add` - see
Change 1 below) - no `ControlGraphBuilder`/`ParentChildEdge` change was needed, since (unlike
`SplitContainer`'s `Panel1`/`Panel2`) `Items`/`DropDownItems` always belong to a real,
separately-`new`'d field. `MenuStrip` was promoted from `Fallback` to `Direct` (Avalonia's real
`Menu`, core, no extra package - `MenuStripFallback.cs` deleted as superseded);
`ToolStripMenuItem`/`Separator`/`Button`/`Label`/`StatusLabel`/`ComboBox`/`TextBox`/
`ProgressBar` are all `Direct`-mapped now. `ToolStrip`/`StatusStrip` have no native Avalonia
toolbar/status-bar control, so they stay `Fallback`, but their templates were rewritten from an
inert `Border` to a `StackPanel` so they can host their item children for real.
`ToolStripDropDownButton`/`ToolStripSplitButton` are `Direct`-mapped too now, to `Button`/
`SplitButton` with a two-level `Button.Flyout` > `MenuFlyout` child wrapper (see
`MappedControl.ChildWrapperElementNames`, generalized from a single wrapper name for exactly
this case) - their already-`MenuItem`-mapped `DropDownItems` nest straight into it.
`ToolStripControlHost`/`ToolStripDropDown` stay `Unsupported` - an embedded-arbitrary-control
problem, not a parsing or wrapper one. `ContextMenuStrip`'s own items are parsed too, and are
emitted as a nested `<Control.ContextMenu>` on the owning control (see its own subsection below).

### DataGridView columns

**Done.**
`DesignerSyntaxWalker.HandleInvocation` now also recognizes `.Columns.Add`/`.Columns.AddRange`
(same generalization as above). `DataGridViewTextBoxColumn`/`CheckBoxColumn` are
`Direct`-mapped to Avalonia's `DataGridTextColumn`/`DataGridCheckBoxColumn`, nested under the
`DataGridView` mapper's `childWrapperElementNames: ["DataGrid.Columns"]` (the same
property-element-wrapper mechanism `TabPage`→`TabItem` already used for its `Canvas` wrapper).

Avalonia's DataGrid ships only those two plus `DataGridTemplateColumn` - there is **no**
`DataGridComboBoxColumn`, and mapping to one was an `AVLN2000` build break in every generated
project that had a combo column. `ComboBox`/`Button`/`Image`/`Link` columns therefore all go
through the new `TemplateColumnMapper`, which emits a `DataGridTemplateColumn` plus a generated
`CellTemplate` (via `MappedControl.NestedElements`, a mapper-prescribed element subtree
`AxamlEmitter` walks recursively). The cell content cannot be bound automatically - Designer.cs
records the column but not its `DataPropertyName`-to-view-model mapping - so each template
carries a `TODO` comment **naming the `DataPropertyName`** when the designer recorded one — which
it does for a bound column; the claim that Designer.cs never records it was simply wrong, and this
repo's own sample was the unfaithful one. The 7 `DataGridViewCell` subtypes are left as-is; in practice they're
essentially never separately instantiated in real Designer.cs (only Columns are - each
column's `CellTemplate` is set internally by its own constructor).

### SplitContainer

**Done.**
`DesignerSyntaxWalker.HandleInvocation` now recognizes the
`this.splitContainer1.Panel1/Panel2.Controls.Add(x)` three-level chain (encoded as a synthetic
`"field.PanelN"` parent id), routed by `ControlGraphBuilder` into two new
`ControlModel.Panel1Children`/`Panel2Children` lists. Maps `Direct` to a plain Avalonia `Grid`
(`src/WinFormsToAvalonia.Core/Mapping/DefaultControlMappers.cs`) - no bespoke `IControlMapper`
needed after all, since the two-slot emission logic lives in `AxamlEmitter.EmitControl`
(special-cased on `ClrTypeName == "SplitContainer"`, not the mapper), which emits a
`GridSplitter` between two `Canvas` regions, using `RowDefinitions` instead of
`ColumnDefinitions` when `Orientation=Horizontal`.

### ContextMenuStrip

**Done.**
`ExpressionEvaluator.Evaluate` (`src/WinFormsToAvalonia.Core/Parsing/ExpressionEvaluator.cs`)
now recognizes a bare `this.<field>` RHS as a new `PropertyValue.ControlReference` case,
fixing what was previously misclassified as an `EnumMembers` entry -
`this.someControl.ContextMenuStrip = this.contextMenuStrip1;` was already routed correctly
by `DesignerSyntaxWalker.HandleAssignment` (a `this.first.second = ...` shape), only the RHS
interpretation was wrong. `AxamlEmitter.EmitControl` resolves that reference via
`FormModel.Controls` (now threaded through the private `EmissionState`) and emits a nested
`<Control.ContextMenu><ContextMenu>...</ContextMenu></Control.ContextMenu>` on the owning
control, reusing the same recursive `EmitControl` already used for ordinary children so
nested `MenuItem`/`Separator`/`DropDownItems` work for free - no bespoke mapper code needed.
A `ContextMenuStrip` shared across multiple owner controls gets its item tree emitted once
per owner (Avalonia's `ContextMenu` isn't a shareable instance the way WinForms' is).
`NotifyIcon.ContextMenuStrip` is wired too, but through a different target: a tray menu is a
**native** menu, so it becomes `App.axaml`'s `<TrayIcon.Menu><NativeMenu>` rather than a
`Control.ContextMenu`. The OS draws it, which is why a `NativeMenuItem` carries only a `Header`,
an `IsEnabled` flag and a nested `NativeMenu` - no styling, no icons, and no `Click` attribute:
`NativeMenuItem.Click` is an event, which XAML cannot point at a method, so a designer-wired
Click on a tray item is reported rather than emitted. `ToolStripSeparator` becomes
`NativeMenuItemSeparator`, and an `&` mnemonic is stripped rather than converted, there being no
`AccessText` to render one.
The registry entry stays `Unsupported` (same pattern as `ToolTip`) since the component itself
is never resolved via `_registry.Map` - the capability ships through the owner control instead.

### ToolStripControlHost

**Done.**
The type exists because a `ToolStrip` only accepts `ToolStripItem`s — it is plumbing around an
ordinary `Control`, and it has **no parameterless constructor**, so
`new ToolStripControlHost(this.hostedTrackBar)` is the only shape a designer can produce and the
hosted control is always named right there. The old guidance called this "too dynamic to translate
generically"; it was wrong, and the price was that the hosted control disappeared from the
conversion entirely while the host left a TODO comment in its place.

`HostedControlCatalog` names the constructor argument, `DesignerSyntaxWalker` records the alias,
and `ControlGraphBuilder` rewrites the parent/child edge so the hosted control lands where the host
was — the same tree-assembly job as its existing `SplitContainer.Panel1` decoding, and the reason
it is not done in the walker: the host keeps taking property assignments until the last statement.

Two things are refused rather than guessed at, both because they would break the generated build or
lose something silently: a hosted control that is *also* added to a container of its own (one
control, two places, `AVLN1001`), and the host's own settings — its `Size` moves only into a gap,
since WinForms keeps the two in sync, while `Alignment`/`Overflow`/`DisplayStyle` and any event
subscribed on the host are reported by name.

### ToolStripContainer, ToolStripPanel, ToolStripContentPanel

**Done.**
Implemented as `Fallback` controls:
`src/WinFormsToAvalonia.FallbackControls/Templates/ToolStripContainerFallback.cs`,
`ToolStripPanelFallback.cs`, `ToolStripContentPanelFallback.cs`. `ToolStripContainerFallback`
is a `DockPanel` that builds the same fixed 5-region layout (`TopToolStripPanel`/
`BottomToolStripPanel`/`LeftToolStripPanel`/`RightToolStripPanel` + `ContentPanel`) WinForms'
own constructor creates, exposed as public properties. `ToolStripPanelFallback` is a
`StackPanel` with an `Orientation` (default Horizontal). Registered as a multi-template
dependency in `FallbackControlCatalog`/`FallbackControlResolver` (`DependsOnKeys`), since
`ToolStripContainerFallback`'s source references the other two types even when no WinForms
control was itself mapped to them. Nested content is still not auto-migrated — that needs the
`.ContentPanel.Controls.Add(...)` three-level member-access parsing, a separate, unpicked
phase.

### File dialogs

**Done**, for `OpenFileDialog`/`SaveFileDialog`/`FolderBrowserDialog`.
`ViewModelEmitter` (`src/WinFormsToAvalonia.Core/Emission/ViewModelEmitter.cs`) now emits one
async `[RelayCommand]` per dialog field found anywhere in the form (`EnumerateAllControls` was
extended to also walk `FormModel.Components`, where dialog/Timer/NotifyIcon fields actually
live, since none of them are ever `Controls.Add`-ed) - not tied to a specific button's Click
handler, since `DesignerSyntaxWalker` never parses handler method bodies (only
`InitializeComponent()`), so there's no reliable signal linking a button to "its" dialog. The
generated body resolves a `TopLevel` via `Application.Current`'s desktop lifetime (a
ViewModel has no `Visual` of its own to call `TopLevel.GetTopLevel` on - the TODO comment
tells the human to replace this with proper DI), calls the matching
`TopLevel.StorageProvider.OpenFilePickerAsync`/`SaveFilePickerAsync`/`OpenFolderPickerAsync`,
then throws a `NotImplementedException` TODO for the actual migrated business logic - unlike
an event handler, this method is only ever reached once a human calls it, so it cannot take the
app down on its own. `RelayCommandStub` was generalized to carry an `IsAsync` flag and a
body-lines list so sync (Click) and async (dialog) commands share one emission path - the
Click-derived output is unchanged. `ColorDialog`/`FontDialog`/print dialogs have no Avalonia
equivalent at all and stay guidance-only permanently.

### NotifyIcon

**Done.**
App-level, not per-View, so it doesn't fit `AxamlEmitter`'s per-Form model.
`ConversionPipeline.Run` (`src/WinFormsToAvalonia.Core/Pipeline/ConversionPipeline.cs`) now
aggregates every `NotifyIcon` found across all of a project's forms' `Components` into a
`NotifyIconInfo` list (field name, icon asset path, tooltip), threaded through
`AvaloniaProjectScaffolder.BuildProject`/`BuildEmptySkeleton`
(`src/WinFormsToAvalonia.Core/Scaffolding/AvaloniaProjectScaffolder.cs`) into `BuildAppAxaml`,
which emits a `<TrayIcon.Icons><TrayIcons>...</TrayIcons></TrayIcon.Icons>` block on
`<Application>` when the list is non-empty (byte-identical output when there are none). No
new NuGet package is needed - `TrayIcon`/`TopLevel` are in core `Avalonia`/`Avalonia.Desktop`
at the pinned `12.1.1`.

Real Designer.cs almost never assigns `NotifyIcon.Icon` as a literal path (it's usually a resx
resource lookup or `Icon.FromHandle(...)`) - `ExpressionEvaluator` only recognizes the literal
`new Icon("app.ico")` shape. When it *does* resolve, and the file is really there, the icon is
copied into the generated project's `Assets/` folder (`VirtualFileSystem.AddBinary`) so the
emitted reference points at something real. Every other shape emits the whole `TrayIcon.Icons`
block **commented out**, with a TODO and a conversion warning: `TrayIcon.Icon` is resolved at
*run time*, so naming an asset the conversion never produced is not a build error but a
`FileNotFoundException` thrown out of `App.Initialize()` - it used to kill the generated app
before it showed a single window.

### Timer

**Done**, gated on real evidence: a `DispatcherTimer` field, constructor wiring
(`new DispatcherTimer { Interval = ... }`, `Tick +=`, and `.Start()` when the WinForms
`Enabled` was `true`), and the Tick handler are generated on the **View** (`FormMigrationPlanner.PlanTimers`
→ `ViewCodeBehindEmitter`) only when the `Timer` field actually has a `Tick` subscription -
`control.Events` already captured it generically (`HandleEventSubscription` records every `+=`,
not just `Click`). `Interval`/`Enabled` were already captured as ordinary
`PropertyValue.Literal`s via the normal assignment path, so no parser changes were needed. The
handler method keeps its original WinForms name verbatim (unlike Click→RelayCommand renaming),
since it's wired directly in the constructor rather than bound from AXAML, preserving 1:1
traceability for whoever migrates the logic.

`PlanTimers` runs *before* the body rewrite, which is what lets a handler drive the timer it
created: `DispatcherTimerMemberCatalog` translates `Enabled` → `IsEnabled`, `Start()`/`Stop()`
unchanged, and `Interval = n` → `TimeSpan.FromMilliseconds(n)`. `Interval` is write-only, because
WinForms counts int milliseconds where Avalonia holds a `TimeSpan` and a read would compile while
quietly meaning something else.

### ImageList

**Done, as far as Avalonia has somewhere to put the result.**
`ImageListExtractor` reads the `.resx` `ImageStream` — a BinaryFormatter envelope around an
RLE-compressed `ILHEAD` plus a bitmap strip and a 1bpp mask — and writes one PNG per image, with
the mask applied as an alpha channel, to `Assets/<field>_<index>.png`. Every image is written,
including ones nothing references: the payload is the one part of a WinForms project a developer
cannot open by hand.

`ConversionPipeline.ResolveImageListReferences` then turns `control.ImageIndex` into that asset
path, inheriting the `ImageList` from the owner when the control does not name one itself — which
is how WinForms resolves it for a `ToolStripItem`.

**Where it stops:** `MenuItem.Icon` is the only element-level image slot in Avalonia.
`TreeViewItem`, `ListBoxItem` and `TabItem` have no icon property at all, and a `Button`'s
`Content` already holds its text — showing an image there means inventing a panel layout, which
this converter does not do. Those controls' images are still extracted, and the warning names the
file. `ImageKey` is not resolved: an ImageList's keys live in the designer's `SetKeyName` calls
rather than in the payload.

### ToolTip

**Done.**
`DesignerSyntaxWalker.HandleSetToolTipInvocation` now recognizes
`this.toolTip1.SetToolTip(this.control1, "text")`, storing the text on the *target* control's
`Properties["ToolTipText"]` (verifying the owner field is actually a `ToolTip` component
first). `AxamlEmitter.EmitControl` emits it as a universal `ToolTip.Tip` attribute on any
control that has it — not per-WinForms-type, so it doesn't go through `IControlMapper` at all.
The `ToolTip` component's own registry entry stays `Unsupported` (there's still no element for
the component field itself), its guidance text updated to explain this.

### DomainUpDown

**Done.**
Implemented as a `Fallback` control:
`src/WinFormsToAvalonia.FallbackControls/Templates/DomainUpDownFallback.cs` — a `DockPanel`
composing a read-only `TextBox` display with two step buttons, an `Items` string list, a
`SelectedIndex` styled property, and a `Wrap` styled property (mapped from the WinForms `Wrap`
literal property). `Items` isn't populated automatically — same `.Items.Add` limitation as the
ToolStripItem/DataGridView column families.

### PropertyGrid

**Done.**
Implemented as a `Fallback` control:
`src/WinFormsToAvalonia.FallbackControls/Templates/PropertyGridFallback.cs` — a reflection-based
name/value editor over a `SelectedObject` styled property, editing the properties whose type a
`TypeConverter` can round-trip from a string. Deliberately small: no category grouping, no
nested/expandable objects, no custom `UITypeEditor`s. Swap in a community Avalonia PropertyGrid
package if you need those; only that one file changes.

### WebBrowser

**Done**, as a placeholder rather than a real webview.
`WebBrowserFallback.cs` shows the designer's `Url` inside a bordered placeholder, keeping the
control's footprint in the Canvas layout. Mapping to a community WebView package (via the same
`requiredNuGetPackage` mechanism `DataGridView`→`DataGrid` uses) was rejected on purpose: those
packages are platform-specific, and this tool's generated projects stay dependency-free apart
from the official `Avalonia.Controls.DataGrid`.

### BindingNavigator

**Done.**
`BindingNavigatorFallback.cs` is a `StackPanel` — `BindingNavigator` is a `ToolStrip` subclass,
so its designer-declared items are parsed and render as real children — with `Position`/`Count`
styled properties and four `Move*` methods.

It navigates now. `bindingNavigator1.BindingSource = this.bindingSource1;` used to be dropped
without a word; it is now matched against the `DataSource` bindings, and when some control is
bound to that same `BindingSource` the navigator gets:

- `Count="{Binding <Collection>.Count}"` — `ObservableCollection<T>` raises `PropertyChanged` for
  `Count`, so it follows the rows;
- `Position="{Binding <Nav>Position, Mode=TwoWay}"`, and the **bound control** gets
  `SelectedIndex="{Binding <Nav>Position, Mode=TwoWay}"`. `BindingSource.Position` was one number
  the navigator and the grid both showed, so it becomes one ViewModel property and moving either
  moves the other;
- one `Click` subscription per designer-recorded `MoveFirstItem`/`MovePreviousItem`/
  `MoveNextItem`/`MoveLastItem`, onto the template's `MoveFirst()`/`MovePrevious()`/`MoveNext()`/
  `MoveLast()`. The clamping lives in the template, so an empty collection lands on `-1` — which is
  both what `BindingSource.Position` reported and what Avalonia reads as "nothing selected".

Three things are deliberately not wired, and each is reported by name: `AddNewItem` and
`DeleteItem` (they change the collection, which needs the row type's own constructor and delete
semantics), a role button that already has its **own** `Click` handler (the developer's code wins),
and a navigator whose designer recorded no roles at all — the bindings are still emitted, since the
count and the selection are worth having, but no button is guessed at from its name or caption.

### Paint handlers

**Done**, for the geometric drawing calls.
Avalonia has no `Paint` event: a control draws by overriding `Render(DrawingContext)`, which is a
subclass. So the bundled `PaintSurfaceFallback` **is** that subclass, and it turns the override
back into the event the WinForms code was written against.

A `Panel` or a `PictureBox` whose designer wired a `Paint` handler is retargeted onto it, and the
handler is subscribed from the generated constructor (a CLR event on a template, not an element
attribute). `e.Graphics.DrawLine/DrawRectangle/FillRectangle/DrawEllipse/FillEllipse` translate to
their `DrawingContext` equivalents, and `Pens.X`/`Brushes.X`/`SystemBrushes.X` resolve through the
same colour pipeline the designer path uses — so a system colour, which has no named Avalonia
brush at all, comes out as explicit ARGB rather than a name that does not exist.

Three cases keep today's behaviour and say why: a control **with children** (Avalonia seals
`Panel.Render`, so the surface derives from `Control` and can draw or contain, not both), a
`PictureBox` that also carries an **Image** (WinForms drew over the picture; there is no honest way
to do both), and any other control type. `DrawString` is refused too — Avalonia's `DrawText` wants
a `Typeface` and an em size where WinForms passed one `Font`, and splitting one argument into two
this converter cannot read is exactly the guess it does not make.

### HelpProvider

**Done, for the half that has a target.**
`ExtenderProviderCatalog` generalises what used to be a hardcoded `SetToolTip` path into a table of
WinForms extender providers — the components that set a property on *another* control instead of
having one of their own. `helpProvider1.SetHelpString(this.notesRichTextBox, "…")` now lands on the
target as `AutomationProperties.HelpText`.

That is the right slot rather than the convenient one: Avalonia has no F1-context-help concept, but
`AutomationProperties.HelpText` means exactly "help text about this control", and unlike
`ToolTip.Tip` it cannot collide with a real `SetToolTip` on the same control. The keyboard gesture
is lost; the prose the developer wrote is not, and it used to vanish without even a warning.

`SetShowHelp(bool)` and `HelpNamespace` (a URL) have no target at all and are reported by name —
as is any other setter on a recognised provider, which is what stops the next one from
disappearing silently.

### Framework-agnostic components

**Done.** `FileSystemWatcher`/`Process`/`SerialPort`/`EventLog`/`PerformanceCounter`/
`ServiceController`/`SoundPlayer`/`BackgroundWorker` are the same .NET classes in an Avalonia app,
so `ComponentFieldCatalog` + `FormMigrationPlanner.PlanComponents` emit each as a real field on the
generated **View** — designer literals applied, designer-wired events subscribed — whenever
something actually uses it, and handler bodies may then name it freely.

What this turned out to cost, and the two things worth remembering:
- four of them need a NuGet package, which must be listed in `ComponentFieldCatalog` **and** in
  `AvaloniaProjectScaffolder.ExtraPackageVersions`, or the csproj writer drops it silently;
- the four Windows-only ones are built **lazily**. Eagerly, `new EventLog()` threw from the View's
  constructor — which Avalonia calls before the first window exists — and took the whole converted
  app down on Linux. `GeneratedAppStartupTests` exists because building alone never saw that.

### UserControl conversion

**Done.**
`ConversionPipeline.Run`'s `pairings` filter now admits `WinFormsArtifactKind.UserControl` too
(UserControls first, so a Form hosting one already has its mapping by the time the Form is
emitted). `AxamlEmitter.EmitView` and `ViewCodeBehindEmitter` take the artifact kind and switch
root element / base class between `Window` and `UserControl` — a UserControl gets no `Title`,
and takes its size from the designer's own `Size` rather than a Form's `ClientSize`.
`AvaloniaProjectScaffolder.BuildProject` picks the first **Form**-kind entry as the startup
window, never a UserControl.

The project's own UserControls also become real mapping entries: `ConversionPipeline` builds one
`UserControlMapper` per discovered UserControl and composes them with `DefaultControlMappers.All`
through `ControlMappingRegistry`'s mapper-sequence constructor, so
`this.demoUserControl1 = new DemoUserControl();` emits `<uc0:DemoUserControlView />` instead of a
TODO comment. Because a UserControl under `Controls/` lands in `{Project}.Views.Controls`, each
distinct View namespace gets its own positional `xmlns:uc{n}` prefix on the root element.

Component-kind artifacts (`: Component`) are still not converted — they have no visual
representation — but they now get a tailored `Unsupported` guidance entry per project-defined
class instead of the registry's generic "no mapping registered" message.
