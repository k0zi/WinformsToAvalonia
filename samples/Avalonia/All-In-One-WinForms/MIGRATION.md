# All_In_One_WinForms — migration checklist

Generated from `All-In-One-WinForms.csproj` by WinFormsToAvalonia.

**103 of 106 handler statements (97%)** came across as real Avalonia code.

Everything below is preserved in the generated project as a comment, inside a method that
calls `MigrationTodo.NotMigrated(...)`. The marker reports rather than throws, so the app
runs while you work through this list.

**The wiring is already done.** Each method below exists on the generated View with the
right Avalonia signature, and its event is subscribed - the AXAML carries the attribute, or
the constructor the subscription. What is left is the body: the statement named beside each
one is the first the conversion could not prove equivalent.

## Methods to migrate (1)

### `Views/MainView.axaml.cs`

- [ ] `showBalloonButton_Click` — `this.notifyIcon1.ShowBalloonTip(3000);`

## Needs your attention (13)

Everything the conversion decided not to guess at, and why.

- 'helpProvider1' (HelpProvider) calls 'SetShowHelp(...)', which has no Avalonia equivalent - that setting is not carried over.
- NotifyIcon 'notifyIcon1' has no Icon in Designer.cs, so WinForms never showed it in the tray. App.axaml's TrayIcon is emitted without one, which shows nothing either - set TrayIcon.Icon once the app has an icon to show.
- 'ErrorProvider' has no built-in Avalonia equivalent; using the bundled fallback control 'ErrorProviderFallback'.
- 'notifyIcon1' subscribes 'DoubleClick', which has no Avalonia equivalent - 'notifyIcon1_DoubleClick' is emitted but never subscribed. Avalonia's TrayIcon raises only Clicked - there is no double-click on a tray icon, and a single click is not one.
- Component 'eventLog1' (EventLog) is Windows-only. The generated View declares it and compiles everywhere, with the platform analyser suppressed for that file - but these calls throw on Linux and macOS.
- Component 'fileSystemWatcher1' (FileSystemWatcher): designer property 'SynchronizingObject' was not reproduced - only literal values are, and this one is not.
- Component 'performanceCounter1' (PerformanceCounter) is Windows-only. The generated View declares it and compiles everywhere, with the platform analyser suppressed for that file - but these calls throw on Linux and macOS.
- Component 'serviceController1' (ServiceController) is Windows-only. The generated View declares it and compiles everywhere, with the platform analyser suppressed for that file - but these calls throw on Linux and macOS.
- Component 'soundPlayer1' (SoundPlayer) is Windows-only. The generated View declares it and compiles everywhere, with the platform analyser suppressed for that file - but these calls throw on Linux and macOS.
- 'pictureBox1' subscribes both 'MouseDown' and 'Click', which map to the same Avalonia event 'PointerPressed'. 'pictureBox1_MouseDown' carries the AXAML attribute and 'pictureBox1_Click' is subscribed from the constructor instead, so both run - but they now run at the same moment, which the two WinForms events did not.
- 'toolTip1' (ToolTip) hides its tips after 5000ms (AutoPopDelay). An Avalonia tooltip stays open while the pointer is over the control, so that limit is not carried over.
- 'checkedListBox1' is a CheckedListBox: Avalonia has no such control, so it becomes a ListBox whose ItemTemplate holds a CheckBox, bound to a generated row type in Models/. SetItemChecked/GetItemChecked translate onto it. CheckedItems and CheckedIndices do not - they are WinForms collections with no counterpart - so read the collection instead.
- 'MainForm' sets designer properties nothing in this conversion translates, so they are not carried over: DomainUpDown.Text (domainUpDown1); ErrorProvider.BlinkStyle (errorProvider1); FlowLayoutPanel.BorderStyle (flowLayoutPanel1); FontDialog.ShowColor (fontDialog1); HelpProvider.HelpNamespace (helpProvider1); ImageList.TransparentColor (imageList1); ListView.LargeImageList (itemsListView); ListView.SmallImageList (itemsListView); NotifyIcon.BalloonTipText (notifyIcon1); NotifyIcon.BalloonTipTitle (notifyIcon1); Panel.BorderStyle (contextPanel, plainPanel); PictureBox.BorderStyle (pictureBox1); PrintPreviewControl.Document (printPreviewControl1); PrintPreviewDialog.ClientSize (printPreviewDialog1); PrintPreviewDialog.Text (printPreviewDialog1).

## Converted differently (25)

No action needed - these have no Avalonia element, so here is where each one went.

- No runtime equivalent shipped, and none needed: a control whose DataSource named this BindingSource gets an ItemsSource binding, and the ViewModel the ObservableCollection behind it. The rows come across too where the assignment is the literal `bindingSource1.DataSource = new BindingList<Row> { new Row { ... }, ... };` shape - the row type is lifted out of the Form into Models/, so the collection is declared with it rather than with object. Any other shape (a DataTable, a query, a list built with Add calls) is refused and stops the handler there, which is the honest answer: the binding is live, so the grid fills the moment you populate the collection.
- No control mapping - but a DispatcherTimer field, its Interval and its Tick wiring ARE generated on the View whenever the component has a real Tick handler (see FormMigrationPlanner.PlanTimers). A handler body can then drive it: Enabled, Start() and Stop() translate, and Interval can be written but not read - WinForms counts milliseconds, Avalonia holds a TimeSpan.
- No control mapping - use TopLevel.StorageProvider.OpenFilePickerAsync from code instead.
- No control mapping - use TopLevel.StorageProvider.SaveFilePickerAsync from code instead.
- No control mapping - use TopLevel.StorageProvider.OpenFolderPickerAsync from code instead.
- No built-in Avalonia colour picker *dialog*, but there is a real ColorView - so the bundled ColorDialogFallback wraps it, and a handler's ShowDialog IS translated inline onto it: both the `if (dlg.ShowDialog() == DialogResult.OK)` shape and the `if (dlg.ShowDialog() != DialogResult.OK) return;` guard clause. Reading dlg.Color inside them becomes the picked value. Needs the Avalonia.Controls.ColorPicker package. A seed assigned before the call (colorDialog1.Color = ...) is carried over too, as the colour the dialog opens on.
- No Avalonia font picker at all, so the bundled FontDialogFallback provides one over FontManager.Current.SystemFonts - family, size, bold and italic only. A handler's ShowDialog IS translated inline onto it, in the same two shapes as ColorDialog, and `ctrl.Font = dlg.Font` expands to the four Avalonia properties, and a seed assigned before the call (fontDialog1.Font = someControl.Font) opens the dialog on it.
- There is still no printer to choose - Avalonia has no printing API at all. But the shape this dialog appears in does have an answer: `if (printDialog1.ShowDialog(this) == DialogResult.OK) { printDocument1.Print(); }` is translated whole into `await printDocument1.PrintAsync(this)`, which renders the page the PrintPage handler draws and writes it to a file the user picks. The dialog does not vanish - it moves into that destination picker. A branch doing anything other than one Print() call is left alone.
- Paper size, orientation and margins, on the bundled PageSetupDialogFallback - and they now have somewhere to go: they are what the bundled PrintDocumentFallback lays its page out with, so the dialog changes what the export looks like. It was guidance-only for as long as nothing consumed a page setup, which was true while there was no page. The document is resolved from the designer's Document property.
- A window showing the page, on the bundled PrintPreviewDialogFallback. This became possible only once the document could be produced: a preview needs no printing API, it needs a rendered page, and the PrintPage handler really draws one now. The document is resolved from the designer's Document property, so nothing is inferred from the handler.
- Emitted as a field of the bundled PrintDocumentFallback, with its DocumentName and its PrintPage handler wired. That handler is drawing code, and drawing code translates: e.Graphics calls become real DrawingContext calls, so the page is genuinely drawn. Print() renders it and writes it to a file the user picks. What is still missing is the printer - Avalonia has no printing API at all, so sending that page to one is what a library is for. Every page is rendered - PrintPage is raised again while the handler sets HasMorePages - and the export writes one PNG per page. BeginPrint/EndPrint have no counterpart, so a page counter the original reset there has to be reset by hand.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. It predates async/await, so Task.Run with IProgress<T> is usually the better end state - but that is a design improvement, not a migration step: the converted code runs as it is.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Moving it into a service later is a design improvement, not a migration step.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Moving it into a service later is a design improvement, not a migration step.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Windows-only, so it is built lazily: the app starts everywhere, but touching it throws off Windows.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Windows-only, so it is built lazily: the app starts everywhere, but touching it throws off Windows.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Windows-only, so it is built lazily: the app starts everywhere, but touching it throws off Windows.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Moving it into a service later is a design improvement, not a migration step.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Windows-only, so it is built lazily: the app starts everywhere, but touching it throws off Windows. There is no Avalonia audio API either, so a cross-platform library is the eventual answer.
- No per-View mapping - Avalonia's tray-icon support is app-level, configured in App.axaml's TrayIcon.Icons (now generated automatically by ConversionPipeline.Run's cross-form aggregation - see AvaloniaProjectScaffolder.BuildTrayIconsSection). A literal icon path that resolves to a real file is copied into the generated project's Assets/ folder; otherwise (the common case - resx/dynamic icons) the TrayIcon block is emitted commented out with a TODO, since Avalonia resolves TrayIcon.Icon at run time and a dangling asset reference would throw out of App.Initialize(). Designer-wired events: Click becomes TrayIcon.Clicked and is subscribed from the generated View's constructor for an icon that resolved; DoubleClick and the mouse/balloon events have no Avalonia counterpart and are reported by name rather than emitted as a handler nothing subscribes.
- The component itself has no element, but its this.helpProvider1.SetHelpString(this.control1, "text") calls ARE translated - into AutomationProperties.HelpText on the target control, which is the one Avalonia slot that means 'help text about this control'. The F1 gesture itself has no equivalent, so SetShowHelp and HelpNamespace are reported rather than guessed at.
- The ContextMenuStrip component itself has no element - but this.someControl.ContextMenuStrip = this.contextMenuStrip1 assignments ARE now translated automatically into a nested <Control.ContextMenu><ContextMenu>...</ContextMenu></Control.ContextMenu> on the target control (see AxamlEmitter.EmitContextMenuIfPresent). NotifyIcon.ContextMenuStrip is translated too, into App.axaml's TrayIcon.Menu as a NativeMenu - a native menu the OS draws, so an item carries a header, an enabled flag and a submenu and nothing else; a designer-wired Click on one is reported, since NativeMenuItem raises Click as an event XAML cannot point at a method.
- 'DemoComponent' is a Component defined by this project - no visual representation, so no control mapping. Its source names nothing that would not survive the conversion, so it is copied into the generated project and a real field is emitted for it.
- No control mapping, but the images are not lost - each one is written to Assets/<field>_<index>.png and set on the menu items that used it. MenuItem.Icon is the only per-item image slot Avalonia has; anywhere else, place the extracted file by hand.
- The ToolTip component itself has no element - but its this.toolTip1.SetToolTip(this.control1, "text") calls ARE now translated automatically into a ToolTip.Tip attribute on the target control (see DesignerSyntaxWalker.HandleExtenderProviderInvocation, driven by ExtenderProviderCatalog).

## Stayed in code-behind (32)

No action needed - event handlers stay on the View by default, and only one that is provably
bindable becomes a `[RelayCommand]`. Here is what kept each candidate on the View, should you
want to move it by hand.

- Click handler 'newMenuItem_Click' stays in code-behind: it uses 'titleTextBox.Clear', which has no bindable Avalonia equivalent.
- Click handler 'openMenuItem_Click' stays in code-behind: 'openFileDialog1' (OpenFileDialog) has no direct Avalonia element to bind against.
- Click handler 'exitMenuItem_Click' stays in code-behind: it drives the Form itself (Close).
- Click handler 'wordWrapMenuItem_Click' stays in code-behind: its menu item toggles itself on click (CheckOnClick), and that toggle has to run before the handler - which a bound command, run ahead of every Click handler, cannot wait for.
- Click handler 'aboutMenuItem_Click' stays in code-behind: it opens another Form/Dialog, which needs a navigation or dialog service before it can move to a ViewModel.
- Click handler 'demoButton_Click' stays in code-behind: it uses an API whose Avalonia replacement hangs off the TopLevel (a message box, the clipboard) - which the View has and a ViewModel does not.
- Click handler 'sharedButton_Click' stays in code-behind: it is wired to 2 controls, so it needs the 'sender' that told them apart.
- Click handler 'validateButton_Click' stays in code-behind: 'errorProvider1' (ErrorProvider) has no direct Avalonia element to bind against.
- Click handler 'refreshButton_Click' stays in code-behind: it uses 'itemsTreeView.Nodes', which has no bindable Avalonia equivalent.
- Click handler 'clockToggleButton_Click' stays in code-behind: 'clockTimer' (Timer) has no direct Avalonia element to bind against.
- Click handler 'openFileButton_Click' stays in code-behind: 'openFileDialog1' (OpenFileDialog) has no direct Avalonia element to bind against.
- Click handler 'saveFileButton_Click' stays in code-behind: 'saveFileDialog1' (SaveFileDialog) has no direct Avalonia element to bind against.
- Click handler 'folderBrowserButton_Click' stays in code-behind: 'folderBrowserDialog1' (FolderBrowserDialog) has no direct Avalonia element to bind against.
- Click handler 'colorButton_Click' stays in code-behind: 'colorDialog1' (ColorDialog) has no direct Avalonia element to bind against.
- Click handler 'fontButton_Click' stays in code-behind: 'fontDialog1' (FontDialog) has no direct Avalonia element to bind against.
- Click handler 'printButton_Click' stays in code-behind: 'printDialog1' (PrintDialog) has no direct Avalonia element to bind against.
- Click handler 'pageSetupButton_Click' stays in code-behind: 'pageSetupDialog1' (PageSetupDialog) has no direct Avalonia element to bind against.
- Click handler 'printPreviewButton_Click' stays in code-behind: 'printPreviewDialog1' (PrintPreviewDialog) has no direct Avalonia element to bind against.
- Click handler 'startWorkerButton_Click' stays in code-behind: it uses state 'MainForm' keeps in its own members (isBusy), which stays on the View.
- Click handler 'watchButton_Click' stays in code-behind: 'fileSystemWatcher1' (FileSystemWatcher) has no direct Avalonia element to bind against.
- Click handler 'launchProcessButton_Click' stays in code-behind: 'process1' (Process) has no direct Avalonia element to bind against.
- Click handler 'writeEventLogButton_Click' stays in code-behind: 'eventLog1' (EventLog) has no direct Avalonia element to bind against.
- Click handler 'readCounterButton_Click' stays in code-behind: 'performanceCounter1' (PerformanceCounter) has no direct Avalonia element to bind against.
- Click handler 'serviceStatusButton_Click' stays in code-behind: 'serviceController1' (ServiceController) has no direct Avalonia element to bind against.
- Click handler 'serialOpenButton_Click' stays in code-behind: 'serialPort1' (SerialPort) has no direct Avalonia element to bind against.
- Click handler 'playSoundButton_Click' stays in code-behind: 'soundPlayer1' (SoundPlayer) has no direct Avalonia element to bind against.
- Click handler 'showBalloonButton_Click' stays in code-behind: 'notifyIcon1' (NotifyIcon) has no direct Avalonia element to bind against.
- Click handler 'copyContextMenuItem_Click' stays in code-behind: it uses an API whose Avalonia replacement hangs off the TopLevel (a message box, the clipboard) - which the View has and a ViewModel does not.
- Click handler 'openDialogFormButton_Click' stays in code-behind: it opens another Form/Dialog, which needs a navigation or dialog service before it can move to a ViewModel.
- Click handler 'aboutButton_Click' stays in code-behind: it opens another Form/Dialog, which needs a navigation or dialog service before it can move to a ViewModel.
- Click handler 'okButton_Click' stays in code-behind: it drives the Form itself (Close, DialogResult).
- Click handler 'cancelButton_Click' stays in code-behind: it drives the Form itself (Close, DialogResult).

