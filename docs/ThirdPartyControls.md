# Third-party controls: DevExpress and Telerik

DevExpress WinForms and Telerik UI for WinForms are converted **without a mappings file**. A Form or
UserControl built on a vendor base class (`XtraForm`, `RibbonForm`, `ToolbarForm`, `XtraUserControl`,
`RadForm`, `RadRibbonForm`, `RadTabbedForm`, `ShapedForm`) is converted like any other, and the
vendor controls on it become real Avalonia controls.

This file is checked against the catalog it describes
(`src/WinFormsToAvalonia.Core/Mapping/Vendors/`) by `VendorDocumentationTests`, in both directions.
The catalog itself is checked against the vendors' real assemblies (DevExpress 26.1, Telerik
2026.4, read from nuget.org as metadata) by `WinFormsToAvalonia.Vendors.Tests`: every type, every
dotted designer property, every event and every member a handler body may use.

## How it works

A vendor control is converted **as a WinForms control** the converter already maps - a `TextEdit`
is a `TextBox` from then on - so everything that applies to the WinForms control applies to it:
its Avalonia element, its events, its bindable properties, handler-body translation. Nothing here
introduces an Avalonia element of its own.

- **Designer vocabulary.** DevExpress editors keep their options on a `Properties` object
  (`this.textEdit1.Properties.ReadOnly = true;`); these dotted settings are read and renamed to the
  WinForms property. Skins, themes and "use this appearance" switches carry nothing and are dropped
  silently; anything else the vendor control sets is listed among the untranslated properties.
- **Structures.** What a vendor spreads over several objects is rebuilt first
  (`VendorDesignerNormalizer`): a GridView's columns into the grid, a BarManager's bars into
  MenuStrip/ToolStrip/StatusStrip, a ribbon into a menu bar, a layout control's captions into
  labels, a RadioGroup into radio buttons, a Telerik command bar's rows and strips into one toolbar.
- **Designer locals.** Telerik declares grid columns, list items and tree nodes as locals inside
  `InitializeComponent`. They are read too; having no field, they get no `x:Name`.
- **Handler bodies.** Code may name a vendor member only where the vendor's member and the WinForms
  one it stands for have the same type - `checkEdit1.Checked`, `dateEdit1.DateTime` (as `Value`),
  `radCheckBox1.IsChecked` (as `Checked`), `progressBarControl1.Position` (as `Value`). Anything else
  on a vendor control - `EditValue` (typed `object`), a Telerik float-valued `RadTrackBar.Value` -
  stops the translation there and stays a TODO, rather than compiling into something else.
  `XtraMessageBox.Show` and `RadMessageBox.Show` are translated like WinForms' `MessageBox.Show`.
- **Packages.** A `DevExpress.Win*`/`Telerik.UI.for.WinForms*` PackageReference is refused without
  being downloaded - it is a WinForms suite, nothing of it can run in Avalonia, and it is hundreds of
  megabytes on nuget.org. `DevExpress.Data`/XPO are not refused: they are UI-neutral.
- **Your own mapping wins.** A `--mappings` entry for a vendor type, of either kind, replaces the
  built-in one (see `docs/known-limitations.md`).

Not supported (no Avalonia counterpart; reported as having no mapping): charts, schedulers, pivot
grids, gauges, docking managers, and every vendor control not listed below.

## Controls

/tmp/claude-1000/-home-k0zi-Develop-GitHub-WinformsToAvalonia/eb9d2284-b4cf-433f-be8f-093985c77507/scratchpad/doctable/gen.csproj : warning NU1903: Package 'Microsoft.DotNet.ILCompiler' 10.0.13 has a known high severity vulnerability, https://github.com/advisories/GHSA-p3jp-9v83-76w9
/tmp/claude-1000/-home-k0zi-Develop-GitHub-WinformsToAvalonia/eb9d2284-b4cf-433f-be8f-093985c77507/scratchpad/doctable/gen.csproj : warning NU1903: Package 'Microsoft.DotNet.ILCompiler' 10.0.13 has a known high severity vulnerability, https://github.com/advisories/GHSA-p3jp-9v83-76w9
### DevExpress

| Vendor control | Converted as | Notes |
|---|---|---|
| `DevExpress.XtraBars.Bar` | — | The BarManager's MainMenu becomes a MenuStrip, its StatusBar a StatusStrip, any other bar a ToolStrip - placed at the bounds of the dock control it sat in. |
| `DevExpress.XtraBars.BarButtonItem` | ToolStripMenuItem / ToolStripButton | Menu entry in a menu, button in a toolbar. Linked twice: two elements, both running the handler; the first one carries the name. |
| `DevExpress.XtraBars.BarCheckItem` | ToolStripMenuItem / ToolStripButton | Toggles on click (CheckOnClick). |
| `DevExpress.XtraBars.BarDockControl` | — | Gives its bounds to the strips, then goes. |
| `DevExpress.XtraBars.BarManager` | — | Plumbing: its bars and links are rebuilt into strips. |
| `DevExpress.XtraBars.BarStaticItem` | ToolStripMenuItem / ToolStripLabel / ToolStripStatusLabel | Status label in a status bar. |
| `DevExpress.XtraBars.BarSubItem` | ToolStripMenuItem / ToolStripDropDownButton | A sub-menu; a drop-down button in a toolbar. |
| `DevExpress.XtraBars.Ribbon.RibbonControl` | MenuStrip | A menu bar: pages become menus, groups sub-menus, item links entries. One row high - the space below is reported. |
| `DevExpress.XtraBars.Ribbon.RibbonPage` | ToolStripMenuItem |  |
| `DevExpress.XtraBars.Ribbon.RibbonPageGroup` | ToolStripMenuItem |  |
| `DevExpress.XtraBars.Ribbon.RibbonStatusBar` | StatusStrip |  |
| `DevExpress.XtraEditors.ButtonEdit` | TextBox | The box comes across; its embedded buttons do not. |
| `DevExpress.XtraEditors.CheckEdit` | CheckBox | `Properties.Caption` is the text; `EditValue` the state. |
| `DevExpress.XtraEditors.CheckedListBoxControl` | CheckedListBox | Items built as `new CheckedListBoxItem(value)` are read by their value. |
| `DevExpress.XtraEditors.ComboBoxEdit` | ComboBox | `Properties.Items` are the items; `TextEditStyle.DisableTextEditor` is a non-editable box. |
| `DevExpress.XtraEditors.DateEdit` | DateTimePicker |  |
| `DevExpress.XtraEditors.GroupControl` | GroupBox |  |
| `DevExpress.XtraEditors.HyperLinkEdit` | LinkLabel |  |
| `DevExpress.XtraEditors.HyperlinkLabelControl` | LinkLabel |  |
| `DevExpress.XtraEditors.LabelControl` | Label |  |
| `DevExpress.XtraEditors.ListBoxControl` | ListBox |  |
| `DevExpress.XtraEditors.LookUpEdit` | ComboBox | Display/value members and data source come across; the drop-down's columns do not. |
| `DevExpress.XtraEditors.MarqueeProgressBarControl` | ProgressBar |  |
| `DevExpress.XtraEditors.MemoEdit` | TextBox | A multi-line text box. |
| `DevExpress.XtraEditors.PanelControl` | Panel |  |
| `DevExpress.XtraEditors.PictureEdit` | PictureBox |  |
| `DevExpress.XtraEditors.ProgressBarControl` | ProgressBar |  |
| `DevExpress.XtraEditors.RadioGroup` | Panel | A Panel of RadioButtons, one per item, the selected one checked. Its SelectedIndexChanged is reported, not wired. |
| `DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit` | — |  |
| `DevExpress.XtraEditors.Repository.RepositoryItemComboBox` | — |  |
| `DevExpress.XtraEditors.Repository.RepositoryItemTextEdit` | — |  |
| `DevExpress.XtraEditors.SidePanel` | Panel |  |
| `DevExpress.XtraEditors.SimpleButton` | Button |  |
| `DevExpress.XtraEditors.SpinEdit` | NumericUpDown |  |
| `DevExpress.XtraEditors.SplitContainerControl` | SplitContainer | `Horizontal = true` (panels side by side) is WinForms' `Orientation.Vertical`. |
| `DevExpress.XtraEditors.TextEdit` | TextBox |  |
| `DevExpress.XtraEditors.TimeEdit` | DateTimePicker | A DateTimePicker with `Format = Time`, which becomes a TimePicker. |
| `DevExpress.XtraEditors.ToggleSwitch` | CheckBox | The state comes across; the on/off texts do not. |
| `DevExpress.XtraEditors.TrackBarControl` | TrackBar |  |
| `DevExpress.XtraEditors.XtraScrollableControl` | Panel |  |
| `DevExpress.XtraGrid.Columns.GridColumn` | DataGridViewTextBoxColumn | `FieldName` is the binding, `Caption` the header. Ordered by `VisibleIndex`; a column without `Visible = true` is hidden and left out. A RepositoryItemCheckEdit/ComboBox editor makes it a check/combo column. |
| `DevExpress.XtraGrid.GridControl` | DataGridView | Takes its MainView's columns; `OptionsBehavior.Editable = false` is IsReadOnly. |
| `DevExpress.XtraGrid.Views.Grid.GridView` | — | Folded into the grid; anything else it set, and its event handlers, are reported. |
| `DevExpress.XtraLayout.EmptySpaceItem` | — |  |
| `DevExpress.XtraLayout.LayoutControl` | Panel | Its controls keep their designer positions. |
| `DevExpress.XtraLayout.LayoutControlGroup` | — |  |
| `DevExpress.XtraLayout.LayoutControlItem` | — | Its caption becomes a Label beside the control it holds; captionless items go. |
| `DevExpress.XtraLayout.SplitterItem` | — |  |
| `DevExpress.XtraTab.XtraTabControl` | TabControl | `TabPages` are the pages. |
| `DevExpress.XtraTab.XtraTabPage` | TabPage |  |
| `DevExpress.XtraTreeList.Columns.TreeListColumn` | — | Reported, not carried. |
| `DevExpress.XtraTreeList.TreeList` | TreeView | Single-column; its columns, and data-bound nodes, are reported. |

### Telerik

| Vendor control | Converted as | Notes |
|---|---|---|
| `Telerik.WinControls.UI.CommandBarButton` | ToolStripButton |  |
| `Telerik.WinControls.UI.CommandBarLabel` | ToolStripLabel |  |
| `Telerik.WinControls.UI.CommandBarRowElement` | — | Folded: a command bar's rows and strips are layout. |
| `Telerik.WinControls.UI.CommandBarSeparator` | ToolStripSeparator |  |
| `Telerik.WinControls.UI.CommandBarStripElement` | — | Folded; a separator between strips. |
| `Telerik.WinControls.UI.CommandBarTextBox` | ToolStripTextBox |  |
| `Telerik.WinControls.UI.CommandBarToggleButton` | ToolStripButton |  |
| `Telerik.WinControls.UI.GridViewCheckBoxColumn` | DataGridViewCheckBoxColumn | Designer local - no field, so no name. |
| `Telerik.WinControls.UI.GridViewComboBoxColumn` | DataGridViewComboBoxColumn |  |
| `Telerik.WinControls.UI.GridViewCommandColumn` | DataGridViewButtonColumn |  |
| `Telerik.WinControls.UI.GridViewDateTimeColumn` | DataGridViewTextBoxColumn |  |
| `Telerik.WinControls.UI.GridViewDecimalColumn` | DataGridViewTextBoxColumn |  |
| `Telerik.WinControls.UI.GridViewImageColumn` | DataGridViewImageColumn |  |
| `Telerik.WinControls.UI.GridViewMaskBoxColumn` | DataGridViewTextBoxColumn |  |
| `Telerik.WinControls.UI.GridViewTextBoxColumn` | DataGridViewTextBoxColumn | Designer local. `IsVisible = false` leaves it out. |
| `Telerik.WinControls.UI.LayoutControlGroupItem` | — |  |
| `Telerik.WinControls.UI.LayoutControlItem` | — | Its caption becomes a Label beside its `AssociatedControl`. |
| `Telerik.WinControls.UI.LayoutControlSeparatorItem` | — |  |
| `Telerik.WinControls.UI.ListViewDataItem` | — | Read for its text. |
| `Telerik.WinControls.UI.RadButton` | Button |  |
| `Telerik.WinControls.UI.RadButtonElement` | ToolStripMenuItem |  |
| `Telerik.WinControls.UI.RadCheckBox` | CheckBox | `CheckState`/`ToggleState` is Checked; `IsChecked` is `Checked` in code. |
| `Telerik.WinControls.UI.RadCheckedListBox` | CheckedListBox |  |
| `Telerik.WinControls.UI.RadCommandBar` | ToolStrip |  |
| `Telerik.WinControls.UI.RadDateTimePicker` | DateTimePicker |  |
| `Telerik.WinControls.UI.RadDropDownList` | ComboBox | Its RadListDataItems (designer locals) are read for their text. |
| `Telerik.WinControls.UI.RadGridView` | DataGridView | `MasterTemplate.Columns` are the columns. |
| `Telerik.WinControls.UI.RadGroupBox` | GroupBox |  |
| `Telerik.WinControls.UI.RadLabel` | Label |  |
| `Telerik.WinControls.UI.RadLabelElement` | ToolStripStatusLabel |  |
| `Telerik.WinControls.UI.RadLayoutControl` | Panel |  |
| `Telerik.WinControls.UI.RadListControl` | ListBox |  |
| `Telerik.WinControls.UI.RadListDataItem` | — | Read for its text. |
| `Telerik.WinControls.UI.RadMaskedEditBox` | MaskedTextBox | Its `Focus()` returns nothing, unlike WinForms', so code cannot call it through the conversion. |
| `Telerik.WinControls.UI.RadMenu` | MenuStrip |  |
| `Telerik.WinControls.UI.RadMenuItem` | ToolStripMenuItem |  |
| `Telerik.WinControls.UI.RadMenuSeparatorItem` | ToolStripSeparator |  |
| `Telerik.WinControls.UI.RadPageView` | TabControl | Its pages are its children. |
| `Telerik.WinControls.UI.RadPageViewPage` | TabPage |  |
| `Telerik.WinControls.UI.RadPanel` | Panel |  |
| `Telerik.WinControls.UI.RadProgressBar` | ProgressBar | `Value1` is the value. |
| `Telerik.WinControls.UI.RadRadioButton` | RadioButton |  |
| `Telerik.WinControls.UI.RadRepeatButton` | Button |  |
| `Telerik.WinControls.UI.RadRibbonBar` | MenuStrip | A menu bar: tabs become menus, groups sub-menus, button elements entries. |
| `Telerik.WinControls.UI.RadRibbonBarGroup` | ToolStripMenuItem |  |
| `Telerik.WinControls.UI.RadSpinEditor` | NumericUpDown |  |
| `Telerik.WinControls.UI.RadSplitContainer` | SplitContainer | Exactly two SplitPanels become the two halves; any other number, a Panel of Panels. |
| `Telerik.WinControls.UI.RadStatusStrip` | StatusStrip |  |
| `Telerik.WinControls.UI.RadTextBox` | TextBox |  |
| `Telerik.WinControls.UI.RadTextBoxControl` | TextBox |  |
| `Telerik.WinControls.UI.RadTimePicker` | DateTimePicker | Its nullable `Value` is not something code may write through the conversion. |
| `Telerik.WinControls.UI.RadToggleButton` | CheckBox |  |
| `Telerik.WinControls.UI.RadTrackBar` | TrackBar | Float-valued: designer values come across, code that reads or writes them does not. |
| `Telerik.WinControls.UI.RadTreeNode` | — | Designer nodes are reported, not carried. |
| `Telerik.WinControls.UI.RadTreeView` | TreeView |  |
| `Telerik.WinControls.UI.RibbonTab` | ToolStripMenuItem |  |
| `Telerik.WinControls.UI.SplitPanel` | — | Folded into a SplitContainer half, or a Panel. |


`—` means the object is folded into its owner (or, for a caption item, into a Label) and has no
element of its own; a value separated by `/` depends on where the item is placed - in a menu, a
toolbar, a status bar.
