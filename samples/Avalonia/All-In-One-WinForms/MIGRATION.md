# All_In_One_WinForms — migration checklist

Generated from `All-In-One-WinForms.csproj` by WinFormsToAvalonia.

**91 of 106 handler statements (86%)** came across as real Avalonia code.

Everything below is preserved in the generated project as a comment, inside a method that
calls `MigrationTodo.NotMigrated(...)`. The marker reports rather than throws, so the app
runs while you work through this list.

**The wiring is already done.** Each method below exists on the generated View with the
right Avalonia signature, and its event is subscribed - the AXAML carries the attribute, or
the constructor the subscription. What is left is the body: the statement named beside each
one is the first the conversion could not prove equivalent.

## Methods to migrate (8)

### `Views/MainView.axaml.cs`

- [ ] `MainForm_FormClosing` — `this.notifyIcon1.Visible = false;`
- [ ] `MainForm_Load` — `this.itemsListView.Items.Add(new ListViewItem(new[] { "readme.txt", "2 KB" }));`
- [ ] `pageSetupButton_Click` — `this.pageSetupDialog1.ShowDialog(this);`
- [ ] `pictureBox1_Paint` — `e.Graphics.DrawEllipse(Pens.SteelBlue, 10, 10, 200, 120);`
- [ ] `printButton_Click` — `if (this.printDialog1.ShowDialog(this) == DialogResult.OK)`
- [ ] `printDocument1_PrintPage` — `e.Graphics!.DrawString(`
- [ ] `printPreviewButton_Click` — `this.printPreviewDialog1.ShowDialog(this);`
- [ ] `showBalloonButton_Click` — `this.notifyIcon1.ShowBalloonTip(3000);`

## Conversion notes (70)

Everything the conversion decided not to guess at, and why.

- 'ToolStrip' has no built-in Avalonia equivalent; using the bundled fallback control 'ToolStripFallback'.
- No runtime equivalent shipped - recommend an ObservableCollection<T> in the ViewModel instead.
- No control mapping - but a DispatcherTimer field, its Interval and its Tick wiring ARE generated on the View whenever the component has a real Tick handler (see FormMigrationPlanner.PlanTimers). A handler body can then drive it: Enabled, Start() and Stop() translate, and Interval can be written but not read - WinForms counts milliseconds, Avalonia holds a TimeSpan.
- No control mapping - use TopLevel.StorageProvider.OpenFilePickerAsync from code instead.
- No control mapping - use TopLevel.StorageProvider.SaveFilePickerAsync from code instead.
- No control mapping - use TopLevel.StorageProvider.OpenFolderPickerAsync from code instead.
- No built-in Avalonia color picker dialog - recommend a community package or a custom dialog.
- No built-in Avalonia font picker dialog - recommend a community package or a custom dialog.
- No built-in Avalonia printing API - manual migration required.
- No built-in Avalonia printing API - manual migration required.
- No built-in Avalonia printing API - manual migration required.
- No built-in Avalonia printing API - manual migration required.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. It predates async/await, so Task.Run with IProgress<T> is usually the better end state - but that is a design improvement, not a migration step: the converted code runs as it is.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Moving it into a service later is a design improvement, not a migration step.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Moving it into a service later is a design improvement, not a migration step.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Windows-only, so it is built lazily: the app starts everywhere, but touching it throws off Windows.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Windows-only, so it is built lazily: the app starts everywhere, but touching it throws off Windows.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Windows-only, so it is built lazily: the app starts everywhere, but touching it throws off Windows.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Moving it into a service later is a design improvement, not a migration step.
- Not a control, but this run emits it as a real field on the generated View - same .NET type, designer values applied, designer-wired events subscribed - so handler bodies keep working as they were. Windows-only, so it is built lazily: the app starts everywhere, but touching it throws off Windows. There is no Avalonia audio API either, so a cross-platform library is the eventual answer.
- No per-View mapping - Avalonia's tray-icon support is app-level, configured in App.axaml's TrayIcon.Icons (now generated automatically by ConversionPipeline.Run's cross-form aggregation - see AvaloniaProjectScaffolder.BuildTrayIconsSection). A literal icon path that resolves to a real file is copied into the generated project's Assets/ folder; otherwise (the common case - resx/dynamic icons) the TrayIcon block is emitted commented out with a TODO, since Avalonia resolves TrayIcon.Icon at run time and a dangling asset reference would throw out of App.Initialize().
- NotifyIcon 'notifyIcon1': couldn't resolve a literal icon file path from Designer.cs (it is usually a resx resource) - App.axaml's TrayIcon is emitted commented out, since referencing an icon file the conversion cannot produce would throw at startup. Copy the real icon into Assets/ and uncomment the block.
- No built-in Avalonia equivalent - manual migration required.
- The ContextMenuStrip component itself has no element - but this.someControl.ContextMenuStrip = this.contextMenuStrip1 assignments ARE now translated automatically into a nested <Control.ContextMenu><ContextMenu>...</ContextMenu></Control.ContextMenu> on the target control (see AxamlEmitter.EmitContextMenuIfPresent). NotifyIcon.ContextMenuStrip is not wired - Avalonia's TrayIcon.Menu needs NativeMenu/NativeMenuItem, a different target.
- 'DemoComponent' is a Component defined by this project - no visual representation, so no control mapping. Its source names nothing that would not survive the conversion, so it is copied into the generated project and a real field is emitted for it.
- No control mapping, but the images are not lost - each one is written to Assets/<field>_<index>.png and set on the menu items that used it. MenuItem.Icon is the only per-item image slot Avalonia has; anywhere else, place the extracted file by hand.
- The ToolTip component itself has no element - but its this.toolTip1.SetToolTip(this.control1, "text") calls ARE now translated automatically into a ToolTip.Tip attribute on the target control (see DesignerSyntaxWalker.HandleSetToolTipInvocation).
- 'ErrorProvider' has no built-in Avalonia equivalent; using the bundled fallback control 'ErrorProviderFallback'.
- Click handler 'newMenuItem_Click' stays in code-behind: it uses 'titleTextBox.Clear', which has no bindable Avalonia equivalent.
- Click handler 'openMenuItem_Click' stays in code-behind: it drives the Form itself (DialogResult).
- Click handler 'exitMenuItem_Click' stays in code-behind: it drives the Form itself (Close).
- Click handler 'wordWrapMenuItem_Click' stays in code-behind: 'notesRichTextBox' (RichTextBox) has no direct Avalonia element to bind against.
- Click handler 'aboutMenuItem_Click' stays in code-behind: it opens another Form/Dialog, which needs a navigation or dialog service before it can move to a ViewModel.
- Click handler 'demoButton_Click' stays in code-behind: it uses an API whose Avalonia replacement hangs off the TopLevel (a message box, the clipboard) - which the View has and a ViewModel does not.
- Click handler 'sharedButton_Click' stays in code-behind: it is wired to 2 controls, so it needs the 'sender' that told them apart.
- Click handler 'validateButton_Click' stays in code-behind: 'errorProvider1' (ErrorProvider) has no direct Avalonia element to bind against.
- Click handler 'refreshButton_Click' stays in code-behind: it uses 'itemsTreeView.Nodes', which has no bindable Avalonia equivalent.
- Click handler 'clockToggleButton_Click' stays in code-behind: 'clockTimer' (Timer) has no direct Avalonia element to bind against.
- 'pictureBox1' subscribes 'Paint', which has no Avalonia equivalent - 'pictureBox1_Paint' is emitted but never subscribed. Avalonia has no Paint event - override Control.Render(DrawingContext) on a custom control, or use a Path/Shape.
- Click handler 'openFileButton_Click' stays in code-behind: it drives the Form itself (DialogResult).
- Click handler 'saveFileButton_Click' stays in code-behind: it drives the Form itself (DialogResult).
- Click handler 'folderBrowserButton_Click' stays in code-behind: it drives the Form itself (DialogResult).
- Click handler 'colorButton_Click' stays in code-behind: it drives the Form itself (DialogResult).
- Click handler 'fontButton_Click' stays in code-behind: it drives the Form itself (DialogResult).
- Click handler 'printButton_Click' stays in code-behind: it drives the Form itself (DialogResult).
- Click handler 'pageSetupButton_Click' stays in code-behind: 'pageSetupDialog1' (PageSetupDialog) has no direct Avalonia element to bind against.
- Click handler 'printPreviewButton_Click' stays in code-behind: 'printPreviewDialog1' (PrintPreviewDialog) has no direct Avalonia element to bind against.
- 'printDocument1' subscribes 'PrintPage', which has no Avalonia equivalent - 'printDocument1_PrintPage' is emitted but never subscribed. No Avalonia equivalent is registered for the WinForms 'PrintPage' event.
- Click handler 'startWorkerButton_Click' stays in code-behind: it drives the Form itself (isBusy).
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
- Component 'eventLog1' (EventLog) is Windows-only. The generated View declares it and compiles everywhere, with the platform analyser suppressed for that file - but these calls throw on Linux and macOS.
- Component 'fileSystemWatcher1' (FileSystemWatcher): designer property 'SynchronizingObject' was not reproduced - only literal values are, and this one is not.
- Component 'performanceCounter1' (PerformanceCounter) is Windows-only. The generated View declares it and compiles everywhere, with the platform analyser suppressed for that file - but these calls throw on Linux and macOS.
- Component 'serviceController1' (ServiceController) is Windows-only. The generated View declares it and compiles everywhere, with the platform analyser suppressed for that file - but these calls throw on Linux and macOS.
- Component 'soundPlayer1' (SoundPlayer) is Windows-only. The generated View declares it and compiles everywhere, with the platform analyser suppressed for that file - but these calls throw on Linux and macOS.
- 'pictureBox1' subscribes both 'MouseDown' and 'Click', which map to the same Avalonia event 'PointerPressed' - only 'pictureBox1_MouseDown' is subscribed; call 'pictureBox1_Click' from it by hand.
- field 'domainUpDown1' (DomainUpDown) has 3 designer-declared item(s), but 'controls:DomainUpDownFallback' does not take item elements - add them by hand, or bind ItemsSource.
- field 'toolStripControlHost1' (ToolStripControlHost) has no Avalonia mapping: Hosts an arbitrary embedded WinForms Control - too dynamic to translate generically; recreate manually with the equivalent Avalonia control.
- Click handler 'okButton_Click' stays in code-behind: it drives the Form itself (Close, DialogResult).
- Click handler 'cancelButton_Click' stays in code-behind: it drives the Form itself (Close, DialogResult).

