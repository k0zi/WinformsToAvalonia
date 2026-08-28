namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The built-in mapper set: Direct/Fallback entries for every WinForms control type this
/// tool can meaningfully translate, plus Unsupported (guidance-only) entries for the rest of
/// the built-in WinForms control/component surface, so every type produces tailored
/// migration guidance instead of falling through to <see cref="ControlMappingRegistry.Map"/>'s
/// generic "no mapping registered" message.
/// </summary>
public static class DefaultControlMappers
{
    private const string DataGridViewColumnOrCellGuidance =
        "DataGridView column/cell definitions are added via .Columns.Add/.AddRange, not " +
        "Controls.Add, so they aren't translated automatically; define the equivalent " +
        "Avalonia DataGrid.Columns entries by hand in the generated View.";

    public static IReadOnlyList<IControlMapper> All { get; } =
    [
        new SimplePropertyMapper("Button", "Button",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("Label", "TextBlock",
        [
            ("Text", "Text", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("TextBox", "TextBox",
        [
            ("Text", "Text", PropertyValueFormatters.AsText),
            ("Multiline", "AcceptsReturn", PropertyValueFormatters.AsBool),
            ("ReadOnly", "IsReadOnly", PropertyValueFormatters.AsBool),
            ("PasswordChar", "PasswordChar", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("CheckBox", "CheckBox",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
            ("Checked", "IsChecked", PropertyValueFormatters.AsBool),
        ]),
        new SimplePropertyMapper("RadioButton", "RadioButton",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
            ("Checked", "IsChecked", PropertyValueFormatters.AsBool),
        ]),
        new SimplePropertyMapper("ComboBox", "ComboBox", []),
        new SimplePropertyMapper("ListBox", "ListBox", []),
        new SimplePropertyMapper("CheckedListBox", "ListBox", []),
        new SimplePropertyMapper("TreeView", "TreeView", []),
        new SimplePropertyMapper("Panel", "Canvas", []),
        // TableLayoutPanel/FlowLayoutPanel map to Canvas like every other container, per the
        // project's fixed Canvas-everywhere layout strategy - their original WinForms
        // layout semantics (row/column/flow) are not translated, only their children's
        // absolute positions are preserved.
        new SimplePropertyMapper("TableLayoutPanel", "Canvas", []),
        new SimplePropertyMapper("FlowLayoutPanel", "Canvas", []),
        new SimplePropertyMapper("ProgressBar", "ProgressBar",
        [
            ("Minimum", "Minimum", PropertyValueFormatters.AsNumber),
            ("Maximum", "Maximum", PropertyValueFormatters.AsNumber),
            ("Value", "Value", PropertyValueFormatters.AsNumber),
        ]),
        new SimplePropertyMapper("NumericUpDown", "NumericUpDown",
        [
            ("Minimum", "Minimum", PropertyValueFormatters.AsNumber),
            ("Maximum", "Maximum", PropertyValueFormatters.AsNumber),
            ("Value", "Value", PropertyValueFormatters.AsNumber),
        ]),
        // Image is only set when ConversionPipeline managed to recover the picture from the
        // form's .resx and copy it into Assets/ - by then the property holds the asset path.
        new SimplePropertyMapper("PictureBox", "Image",
        [
            ("Image", "Source", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("MonthCalendar", "Calendar", []),
        // HyperlinkButton (core Avalonia, no extra package) rather than TextBlock: it is the
        // one built-in control that both *looks* like a link and has the real Click/Command
        // surface a LinkLabel's LinkClicked handler needs (see EventMappingRegistry's
        // LinkLabel.LinkClicked -> Click override). A TextBlock has neither.
        new SimplePropertyMapper("LinkLabel", "HyperlinkButton",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("TabControl", "TabControl", []),
        new SimplePropertyMapper("TabPage", "TabItem",
        [
            ("Text", "Header", PropertyValueFormatters.AsText),
        ],
        childWrapperElementNames: ["Canvas"]),
        // Columns are parsed by DesignerSyntaxWalker's generalized `.Columns.Add`/`AddRange`
        // handling straight into the regular Children list (Columns are real, separately-
        // `new`'d fields, unlike SplitContainer's Panel1/Panel2 - no synthetic parent id
        // needed), so childWrapperElementNames wraps them in Avalonia's required
        // <DataGrid.Columns> property-element syntax, same mechanism TabPage->TabItem uses.
        new SimplePropertyMapper("DataGridView", "DataGrid", [],
        childWrapperElementNames: ["DataGrid.Columns"],
        requiredNuGetPackage: "Avalonia.Controls.DataGrid"),
        // DataGrid's column types aren't a Visual/StyledElement (they live in DataGrid.Columns
        // as plain objects, not the visual tree), so Avalonia rejects x:Name on them at
        // compile time (AVLN2000) - supportsName: false tells AxamlEmitter to skip it.
        new SimplePropertyMapper("DataGridViewTextBoxColumn", "DataGridTextColumn",
        [
            ("HeaderText", "Header", PropertyValueFormatters.AsText),
            ("DataPropertyName", "Binding", PropertyValueFormatters.AsBinding),
        ],
        supportsName: false),
        new SimplePropertyMapper("DataGridViewCheckBoxColumn", "DataGridCheckBoxColumn",
        [
            ("HeaderText", "Header", PropertyValueFormatters.AsText),
            ("DataPropertyName", "Binding", PropertyValueFormatters.AsBinding),
        ],
        supportsName: false),
        // Avalonia's DataGrid ships only Text/CheckBox/Template columns - there is no
        // DataGridComboBoxColumn (mapping to one was an AVLN2000 build break), and no
        // button/image/link column at all. All four therefore go through
        // TemplateColumnMapper, which emits a DataGridTemplateColumn + generated CellTemplate.
        new TemplateColumnMapper("DataGridViewComboBoxColumn", "ComboBox"),
        new TemplateColumnMapper("DataGridViewButtonColumn", "Button",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
        ]),
        new TemplateColumnMapper("DataGridViewImageColumn", "Image"),
        new TemplateColumnMapper("DataGridViewLinkColumn", "HyperlinkButton",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
        ]),
        // ListView is two controls in one (Details = grid, everything else = flat list), so it
        // needs a per-instance decision - see ListViewMapper. Its ColumnHeaders are parsed by
        // the same generalized `.Columns.Add` handling DataGridView's columns use.
        new ListViewMapper(),
        new SimplePropertyMapper("ColumnHeader", "DataGridTextColumn",
        [
            ("Text", "Header", PropertyValueFormatters.AsText),
            ("Width", "Width", PropertyValueFormatters.AsNumber),
        ],
        supportsName: false),
        // Avalonia's CalendarDatePicker is the closest built-in analog; DateTimePicker's
        // Format=Time/Custom modes and time-of-day component aren't represented.
        new SimplePropertyMapper("DateTimePicker", "CalendarDatePicker", []),
        new SimplePropertyMapper("TrackBar", "Slider",
        [
            ("Minimum", "Minimum", PropertyValueFormatters.AsNumber),
            ("Maximum", "Maximum", PropertyValueFormatters.AsNumber),
            ("Value", "Value", PropertyValueFormatters.AsNumber),
        ]),
        new SimplePropertyMapper("HScrollBar", "ScrollBar",
        [
            ("Minimum", "Minimum", PropertyValueFormatters.AsNumber),
            ("Maximum", "Maximum", PropertyValueFormatters.AsNumber),
            ("Value", "Value", PropertyValueFormatters.AsNumber),
        ],
        fixedAttributes: [("Orientation", "Horizontal")]),
        new SimplePropertyMapper("VScrollBar", "ScrollBar",
        [
            ("Minimum", "Minimum", PropertyValueFormatters.AsNumber),
            ("Maximum", "Maximum", PropertyValueFormatters.AsNumber),
            ("Value", "Value", PropertyValueFormatters.AsNumber),
        ],
        fixedAttributes: [("Orientation", "Vertical")]),
        // Panel1/Panel2 children are parsed by DesignerSyntaxWalker into
        // ControlModel.Panel1Children/Panel2Children (a synthetic "field.PanelN" parent id -
        // see its HandleInvocation), and AxamlEmitter special-cases ClrTypeName ==
        // "SplitContainer" to emit them either side of a GridSplitter instead of using the
        // regular single-wrapper Children path every other control uses.
        new SimplePropertyMapper("SplitContainer", "Grid", []),
        // MenuStrip's items are parsed by DesignerSyntaxWalker's generalized `.Items.Add`
        // handling straight into the regular Children list, and Avalonia ships a real,
        // interactive Menu/MenuItem (core, no extra package) - so unlike ToolStrip/StatusStrip
        // (which have no native Avalonia toolbar/status-bar equivalent and stay Fallback),
        // MenuStrip gets a real Direct mapping instead of a placeholder.
        new SimplePropertyMapper("MenuStrip", "Menu", []),
        // A ToolStripMenuItem's own DropDownItems become nested MenuItem/Separator XAML
        // children automatically, via the same recursive AxamlEmitter.EmitControl
        // children-walk every other control already uses - no special-casing needed.
        new SimplePropertyMapper("ToolStripMenuItem", "MenuItem",
        [
            ("Text", "Header", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("ToolStripSeparator", "Separator", []),
        new SimplePropertyMapper("ToolStripButton", "Button",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("ToolStripLabel", "TextBlock",
        [
            ("Text", "Text", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("ToolStripStatusLabel", "TextBlock",
        [
            ("Text", "Text", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("ToolStripComboBox", "ComboBox", []),
        new SimplePropertyMapper("ToolStripTextBox", "TextBox",
        [
            ("Text", "Text", PropertyValueFormatters.AsText),
        ]),
        new SimplePropertyMapper("ToolStripProgressBar", "ProgressBar",
        [
            ("Minimum", "Minimum", PropertyValueFormatters.AsNumber),
            ("Maximum", "Maximum", PropertyValueFormatters.AsNumber),
            ("Value", "Value", PropertyValueFormatters.AsNumber),
        ]),
        // The DropDownItems -> popup-menu combination Button.Content genuinely can't express
        // is exactly what a two-level child wrapper solves: the already-parsed, already-
        // MenuItem-mapped DropDownItems children nest inside Button.Flyout > MenuFlyout,
        // which is what MenuFlyout is for. Both targets are core Avalonia, no extra package.
        new SimplePropertyMapper("ToolStripDropDownButton", "Button",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
        ],
        childWrapperElementNames: ["Button.Flyout", "MenuFlyout"]),
        new SimplePropertyMapper("ToolStripSplitButton", "SplitButton",
        [
            ("Text", "Content", PropertyValueFormatters.AsText),
        ],
        childWrapperElementNames: ["SplitButton.Flyout", "MenuFlyout"]),
        // WinForms' Splitter is a docked drag-handle between two sibling controls. Avalonia's
        // GridSplitter is the same idea, but Grid-based - under this tool's fixed Canvas
        // layout strategy it is emitted as a plain positioned element, and the original Dock
        // stays available in the w2a:LayoutHint attached property for the manual follow-up.
        new SimplePropertyMapper("Splitter", "GridSplitter", []),

        new FallbackControlMapper("GroupBox", "GroupBoxFallback",
        [
            ("Text", "Header", PropertyValueFormatters.AsText),
        ]),
        // No native Avalonia toolbar/status-bar control to promote to (unlike MenuStrip) -
        // ToolStripFallback/StatusStripFallback are StackPanel-derived so they can host their
        // now-parsed item children (ToolStripButton/Label/ComboBox/TextBox/ProgressBar/
        // StatusLabel above).
        new FallbackControlMapper("StatusStrip", "StatusStripFallback"),
        new FallbackControlMapper("ToolStrip", "ToolStripFallback"),
        new FallbackControlMapper("MaskedTextBox", "MaskedTextBoxFallback"),
        new FallbackControlMapper("RichTextBox", "RichTextBoxFallback"),
        new FallbackControlMapper("ErrorProvider", "ErrorProviderFallback"),
        new FallbackControlMapper("DomainUpDown", "DomainUpDownFallback",
        [
            ("Wrap", "Wrap", PropertyValueFormatters.AsBool),
        ]),
        new FallbackControlMapper("ToolStripContainer", "ToolStripContainerFallback"),
        new FallbackControlMapper("ToolStripPanel", "ToolStripPanelFallback"),
        new FallbackControlMapper("ToolStripContentPanel", "ToolStripContentPanelFallback"),
        // Avalonia has no built-in equivalent for any of these four, and pulling in a
        // community package (a WebView, a PropertyGrid) would put an external dependency into
        // every generated project. A bundled fallback keeps the generated app dependency-free
        // and, unlike an Unsupported entry, keeps the control in the visual tree so the
        // surrounding Canvas layout still looks like the original form.
        new FallbackControlMapper("PropertyGrid", "PropertyGridFallback"),
        new FallbackControlMapper("BindingNavigator", "BindingNavigatorFallback"),
        new FallbackControlMapper("WebBrowser", "WebBrowserFallback",
        [
            ("Url", "Url", PropertyValueFormatters.AsText),
        ]),
        new FallbackControlMapper("PrintPreviewControl", "PrintPreviewControlFallback"),

        new UnsupportedControlMapper("BackgroundWorker", UnsupportedDisposition.FeatureElsewhere, EmittedAsField + "It predates async/await, so Task.Run with IProgress<T> is usually the better end state - but that is a design improvement, not a migration step: the converted code runs as it is."),
        new UnsupportedControlMapper("BindingSource", UnsupportedDisposition.NoAvaloniaApi, "No runtime equivalent shipped - recommend an ObservableCollection<T> in the ViewModel instead."),

        // Menu/toolbar family - ContextMenuStrip is never Controls.Add-ed (assigned to
        // another control's .ContextMenuStrip property instead), and the container/panel
        // types have no automatic layout translation under this tool's fixed Canvas strategy.
        new UnsupportedControlMapper("ContextMenuStrip", UnsupportedDisposition.FeatureElsewhere, "The ContextMenuStrip component itself has no element - but this.someControl.ContextMenuStrip = this.contextMenuStrip1 assignments ARE now translated automatically into a nested <Control.ContextMenu><ContextMenu>...</ContextMenu></Control.ContextMenu> on the target control (see AxamlEmitter.EmitContextMenuIfPresent). NotifyIcon.ContextMenuStrip is not wired - Avalonia's TrayIcon.Menu needs NativeMenu/NativeMenuItem, a different target."),

        // ToolStripItem family: DropDownButton/SplitButton are Direct-mapped above (Button/
        // SplitButton + a Button.Flyout > MenuFlyout child wrapper); these 2 stay Unsupported.
        new UnsupportedControlMapper("ToolStripControlHost", UnsupportedDisposition.FeatureElsewhere, "The host itself has no element - it is plumbing WinForms needs because a ToolStrip only takes ToolStripItems, and Avalonia does not. new ToolStripControlHost(this.someControl) IS translated: HostedControlCatalog names the constructor argument, and ControlGraphBuilder puts the hosted control where the host was. This entry is only reached when the argument is not a designer field (new ToolStripControlHost(new TrackBar())), which there is nothing to name."),
        new UnsupportedControlMapper("ToolStripDropDown", UnsupportedDisposition.Unreachable, "Base class for drop-down surfaces - rarely instantiated directly by designer code."),

        // DataGridView cell family: in practice these are essentially never separately
        // instantiated in real Designer.cs - only Columns are (each column's CellTemplate is
        // set internally by its own constructor) - so DataGridViewColumnOrCellGuidance's
        // .Columns.Add framing still applies loosely, even though real designer code won't
        // actually hit this path.
        new UnsupportedControlMapper("DataGridViewTextBoxCell", UnsupportedDisposition.Unreachable, DataGridViewColumnOrCellGuidance),
        new UnsupportedControlMapper("DataGridViewCheckBoxCell", UnsupportedDisposition.Unreachable, DataGridViewColumnOrCellGuidance),
        new UnsupportedControlMapper("DataGridViewComboBoxCell", UnsupportedDisposition.Unreachable, DataGridViewColumnOrCellGuidance),
        new UnsupportedControlMapper("DataGridViewButtonCell", UnsupportedDisposition.Unreachable, DataGridViewColumnOrCellGuidance),
        new UnsupportedControlMapper("DataGridViewImageCell", UnsupportedDisposition.Unreachable, DataGridViewColumnOrCellGuidance),
        new UnsupportedControlMapper("DataGridViewLinkCell", UnsupportedDisposition.Unreachable, DataGridViewColumnOrCellGuidance),
        new UnsupportedControlMapper("DataGridViewHeaderCell", UnsupportedDisposition.Unreachable, DataGridViewColumnOrCellGuidance),

        // Dialog family - Avalonia's replacement is an async TopLevel.StorageProvider/printing
        // API called from code, not a XAML element, so none of these get an automatic mapping.
        new UnsupportedControlMapper("OpenFileDialog", UnsupportedDisposition.FeatureElsewhere, "No control mapping - use TopLevel.StorageProvider.OpenFilePickerAsync from code instead."),
        new UnsupportedControlMapper("SaveFileDialog", UnsupportedDisposition.FeatureElsewhere, "No control mapping - use TopLevel.StorageProvider.SaveFilePickerAsync from code instead."),
        new UnsupportedControlMapper("FolderBrowserDialog", UnsupportedDisposition.FeatureElsewhere, "No control mapping - use TopLevel.StorageProvider.OpenFolderPickerAsync from code instead."),
        new UnsupportedControlMapper("ColorDialog", UnsupportedDisposition.FeatureElsewhere, "No built-in Avalonia colour picker *dialog*, but there is a real ColorView - so the bundled ColorDialogFallback wraps it, and a handler's ShowDialog IS translated inline onto it: both the `if (dlg.ShowDialog() == DialogResult.OK)` shape and the `if (dlg.ShowDialog() != DialogResult.OK) return;` guard clause. Reading dlg.Color inside them becomes the picked value. Needs the Avalonia.Controls.ColorPicker package. What is not carried over is a seed value assigned before the call - the fallback opens on its default."),
        new UnsupportedControlMapper("FontDialog", UnsupportedDisposition.FeatureElsewhere, "No Avalonia font picker at all, so the bundled FontDialogFallback provides one over FontManager.Current.SystemFonts - family, size, bold and italic only. A handler's ShowDialog IS translated inline onto it, in the same two shapes as ColorDialog, and `ctrl.Font = dlg.Font` expands to the four Avalonia properties. A seed value assigned before the call is not carried over."),
        new UnsupportedControlMapper("PrintDialog", UnsupportedDisposition.NoAvaloniaApi, "No built-in Avalonia printing API - manual migration required."),
        new UnsupportedControlMapper("PageSetupDialog", UnsupportedDisposition.NoAvaloniaApi, "No built-in Avalonia printing API - manual migration required."),
        new UnsupportedControlMapper("PrintPreviewDialog", UnsupportedDisposition.NoAvaloniaApi, "No built-in Avalonia printing API - manual migration required."),
        new UnsupportedControlMapper("PrintDocument", UnsupportedDisposition.NoAvaloniaApi, "No built-in Avalonia printing API - manual migration required."),

        // Non-visual component family.
        new UnsupportedControlMapper("NotifyIcon", UnsupportedDisposition.FeatureElsewhere, "No per-View mapping - Avalonia's tray-icon support is app-level, configured in App.axaml's TrayIcon.Icons (now generated automatically by ConversionPipeline.Run's cross-form aggregation - see AvaloniaProjectScaffolder.BuildTrayIconsSection). A literal icon path that resolves to a real file is copied into the generated project's Assets/ folder; otherwise (the common case - resx/dynamic icons) the TrayIcon block is emitted commented out with a TODO, since Avalonia resolves TrayIcon.Icon at run time and a dangling asset reference would throw out of App.Initialize(). Designer-wired events: Click becomes TrayIcon.Clicked and is subscribed from the generated View's constructor for an icon that resolved; DoubleClick and the mouse/balloon events have no Avalonia counterpart and are reported by name rather than emitted as a handler nothing subscribes."),
        new UnsupportedControlMapper("Timer", UnsupportedDisposition.FeatureElsewhere, "No control mapping - but a DispatcherTimer field, its Interval and its Tick wiring ARE generated on the View whenever the component has a real Tick handler (see FormMigrationPlanner.PlanTimers). A handler body can then drive it: Enabled, Start() and Stop() translate, and Interval can be written but not read - WinForms counts milliseconds, Avalonia holds a TimeSpan."),
        // The type itself has no Avalonia counterpart, but its contents do: ConversionPipeline
        // unpacks the .resx ImageStream into one PNG per image under Assets/ and resolves every
        // ImageIndex that points into it. This entry is what reports the half that is left.
        new UnsupportedControlMapper("ImageList", UnsupportedDisposition.FeatureElsewhere, "No control mapping, but the images are not lost - each one is written to Assets/<field>_<index>.png and set on the menu items that used it. MenuItem.Icon is the only per-item image slot Avalonia has; anywhere else, place the extracted file by hand."),
        new UnsupportedControlMapper("ToolTip", UnsupportedDisposition.FeatureElsewhere, "The ToolTip component itself has no element - but its this.toolTip1.SetToolTip(this.control1, \"text\") calls ARE now translated automatically into a ToolTip.Tip attribute on the target control (see DesignerSyntaxWalker.HandleExtenderProviderInvocation, driven by ExtenderProviderCatalog)."),
        new UnsupportedControlMapper("HelpProvider", UnsupportedDisposition.FeatureElsewhere, "The component itself has no element, but its this.helpProvider1.SetHelpString(this.control1, \"text\") calls ARE translated - into AutomationProperties.HelpText on the target control, which is the one Avalonia slot that means 'help text about this control'. The F1 gesture itself has no equivalent, so SetShowHelp and HelpNamespace are reported rather than guessed at."),

        // Framework-agnostic .NET components: not WinForms-specific, and the very same class in
        // an Avalonia project - which is why ComponentFieldCatalog emits them rather than asking
        // the user to. These entries stay Unsupported because there is no *control* to map, so
        // the guidance has to say what does happen instead.
        new UnsupportedControlMapper("FileSystemWatcher", UnsupportedDisposition.FeatureElsewhere, EmittedAsField + MoveToAService),
        new UnsupportedControlMapper("Process", UnsupportedDisposition.FeatureElsewhere, EmittedAsField + MoveToAService),
        new UnsupportedControlMapper("SerialPort", UnsupportedDisposition.FeatureElsewhere, EmittedAsField + MoveToAService),
        new UnsupportedControlMapper("EventLog", UnsupportedDisposition.FeatureElsewhere, EmittedAsField + WindowsOnly),
        new UnsupportedControlMapper("PerformanceCounter", UnsupportedDisposition.FeatureElsewhere, EmittedAsField + WindowsOnly),
        new UnsupportedControlMapper("ServiceController", UnsupportedDisposition.FeatureElsewhere, EmittedAsField + WindowsOnly),
        new UnsupportedControlMapper("SoundPlayer", UnsupportedDisposition.FeatureElsewhere, EmittedAsField + WindowsOnly + " There is no Avalonia audio API either, so a cross-platform library is the eventual answer."),
    ];

    /// <summary>
    /// What now happens to a non-visual component that is really a plain .NET type. Shared,
    /// because the alternative is eight copies of a sentence that has already gone stale once:
    /// these used to tell the user to construct the component by hand, long after the conversion
    /// had started doing it for them.
    /// </summary>
    private const string EmittedAsField =
        "Not a control, but this run emits it as a real field on the generated View - same .NET "
        + "type, designer values applied, designer-wired events subscribed - so handler bodies "
        + "keep working as they were. ";

    private const string MoveToAService =
        "Moving it into a service later is a design improvement, not a migration step.";

    private const string WindowsOnly =
        "Windows-only, so it is built lazily: the app starts everywhere, but touching it throws "
        + "off Windows.";
}
