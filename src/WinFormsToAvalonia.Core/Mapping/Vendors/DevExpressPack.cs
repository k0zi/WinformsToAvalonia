using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping.Vendors;

/// <summary>
/// DevExpress WinForms (XtraEditors, XtraTab, XtraGrid, XtraBars, XtraTreeList, XtraLayout).
/// </summary>
/// <remarks>
/// Every editor keeps its editing options on a <c>Properties</c> object (a RepositoryItem), which is
/// why so many keys here are dotted: the designer writes <c>this.textEdit1.Properties.ReadOnly = true;</c>.
/// Names and types were read off the 26.1 assemblies and are checked against them by
/// <c>WinFormsToAvalonia.Vendors.Tests</c>.
/// </remarks>
internal static class DevExpressPack
{
    private const string Vendor = "DevExpress";
    private const string Editors = "DevExpress.XtraEditors.";

    /// <summary>What every control can be asked from code, unchanged from System.Windows.Forms.Control.</summary>
    private static readonly (string, string)[] ControlMembers = [("Visible", "Visible"), ("Enabled", "Enabled"), ("Focus", "Focus")];

    /// <summary>A label's or a button's own appearance; an editor keeps the same object under Properties.</summary>
    private static readonly VendorProperty[] Appearance =
    [
        new("Appearance.ForeColor", "ForeColor"),
        new("Appearance.BackColor", "BackColor"),
        new("Appearance.Font", "Font"),
    ];

    private static readonly VendorProperty[] EditorAppearance =
    [
        new("Properties.Appearance.ForeColor", "ForeColor"),
        new("Properties.Appearance.BackColor", "BackColor"),
        new("Properties.Appearance.Font", "Font"),
    ];

    /// <summary>Skins, and the switches that say an appearance value is in use - the value itself is what carries.</summary>
    private static readonly string[] Ignored =
    [
        "LookAndFeel.", "Properties.LookAndFeel.", "Appearance.Options.", "Properties.Appearance.Options.",
        "Properties.AppearanceFocused.", "Properties.AppearanceReadOnly.", "Properties.AppearanceDisabled.",
        "StyleController", "Properties.AutoHeight", "MenuManager",
    ];

    /// <summary>An editor's drop-down/spin buttons: the designer lists the default ones, and the Avalonia control has its own.</summary>
    private static readonly VendorCollection EditorButtons = new("Properties.Buttons", VendorCollectionKind.Registration);

    private static Dictionary<string, string> Names(params (string From, string To)[] pairs) =>
        pairs.ToDictionary(p => p.From, p => p.To, StringComparer.Ordinal);

    private static Dictionary<string, string> Members(params (string From, string To)[] pairs) =>
        Names([.. ControlMembers, .. pairs]);

    private static VendorProperty[] TextEditProperties =>
    [
        new("EditValue", "Text", VendorValues.LiteralOf<string>()),
        new("Properties.ReadOnly", "ReadOnly"),
        new("Properties.MaxLength", "MaxLength"),
        new("Properties.PasswordChar", "PasswordChar"),
        new("Properties.UseSystemPasswordChar", "UseSystemPasswordChar"),
        new("Properties.NullValuePrompt", "PlaceholderText"),
        new("Properties.CharacterCasing", "CharacterCasing"),
        .. EditorAppearance,
    ];

    public static IReadOnlyList<VendorBaseType> BaseTypes { get; } =
    [
        new("DevExpress.XtraEditors.XtraForm", Vendor, "Form", ["LookAndFeel.", "Appearance.Options."]),
        new("DevExpress.XtraBars.Ribbon.RibbonForm", Vendor, "Form", ["LookAndFeel.", "Appearance.Options."]),
        new("DevExpress.XtraBars.ToolbarForm.ToolbarForm", Vendor, "Form", ["LookAndFeel.", "Appearance.Options."]),
        new("DevExpress.XtraEditors.XtraUserControl", Vendor, "UserControl", ["LookAndFeel.", "Appearance.Options."]),
    ];

    public static IReadOnlyList<VendorControl> Controls { get; } =
    [
        // ── Editors ───────────────────────────────────────────────────────────────────────────
        new()
        {
            FullName = Editors + "TextEdit", Vendor = Vendor, As = "TextBox",
            Properties = TextEditProperties,
            Events = Names(("EditValueChanged", "TextChanged")),
            Members = Members(("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "MemoEdit", Vendor = Vendor, As = "TextBox",
            Properties = [.. TextEditProperties, new("Properties.WordWrap", "WordWrap"), new("Properties.ScrollBars", "ScrollBars")],
            Events = Names(("EditValueChanged", "TextChanged")),
            Members = Members(("Text", "Text")),
            Synthesized = [("Multiline", "true")],
            Ignored = Ignored,
        },
        new()
        {
            // The buttons inside the box have no counterpart; the box itself is a text box.
            FullName = Editors + "ButtonEdit", Vendor = Vendor, As = "TextBox",
            Properties = TextEditProperties,
            Events = Names(("EditValueChanged", "TextChanged")),
            Members = Members(("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "SpinEdit", Vendor = Vendor, As = "NumericUpDown",
            Collections = [EditorButtons],
            Properties =
            [
                new("EditValue", "Value", VendorValues.Number),
                new("Properties.MinValue", "Minimum"),
                new("Properties.MaxValue", "Maximum"),
                new("Properties.Increment", "Increment"),
                .. EditorAppearance,
            ],
            Events = Names(("EditValueChanged", "ValueChanged")),
            Members = Members(("Value", "Value")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "CheckEdit", Vendor = Vendor, As = "CheckBox",
            Properties =
            [
                new("Properties.Caption", "Text"),
                new("EditValue", "Checked", VendorValues.LiteralOf<bool>()),
                new("Properties.AllowGrayed", "ThreeState"),
                .. EditorAppearance,
            ],
            Events = Names(("CheckedChanged", "CheckedChanged"), ("CheckStateChanged", "CheckStateChanged")),
            Members = Members(("Checked", "Checked"), ("CheckState", "CheckState")),
            Ignored = Ignored,
        },
        new()
        {
            // On/off text has no counterpart on a CheckBox; the state does.
            FullName = Editors + "ToggleSwitch", Vendor = Vendor, As = "CheckBox",
            Properties = [new("IsOn", "Checked"), new("EditValue", "Checked", VendorValues.LiteralOf<bool>()), .. EditorAppearance],
            Events = Names(("Toggled", "CheckedChanged")),
            Members = Members(("IsOn", "Checked")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "ComboBoxEdit", Vendor = Vendor, As = "ComboBox",
            Properties =
            [
                new("Properties.TextEditStyle", "DropDownStyle",
                    VendorValues.Enum(("DisableTextEditor", "DropDownList"), ("Standard", "DropDown"))),
                new("EditValue", "Text", VendorValues.LiteralOf<string>()),
                .. EditorAppearance,
            ],
            Collections = [new("Properties.Items", VendorCollectionKind.Items), EditorButtons],
            Events = Names(("SelectedIndexChanged", "SelectedIndexChanged")),
            Members = Members(("SelectedIndex", "SelectedIndex"), ("SelectedItem", "SelectedItem"), ("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "LookUpEdit", Vendor = Vendor, As = "ComboBox",
            Collections = [EditorButtons],
            Properties =
            [
                new("Properties.DisplayMember", "DisplayMember"),
                new("Properties.ValueMember", "ValueMember"),
                new("Properties.DataSource", "DataSource"),
                .. EditorAppearance,
            ],
            Events = Names(("EditValueChanged", "SelectedValueChanged")),
            Members = Members(),
            Ignored = [.. Ignored, "Properties.Columns."],
        },
        new()
        {
            FullName = Editors + "DateEdit", Vendor = Vendor, As = "DateTimePicker",
            Collections = [EditorButtons, new("Properties.CalendarTimeProperties.Buttons", VendorCollectionKind.Registration)],
            Properties =
            [
                new("DateTime", "Value"),
                new("EditValue", "Value", VendorValues.LiteralOf<DateTime>()),
                new("Properties.MinValue", "MinDate"),
                new("Properties.MaxValue", "MaxDate"),
                .. EditorAppearance,
            ],
            Events = Names(("DateTimeChanged", "ValueChanged"), ("EditValueChanged", "ValueChanged")),
            Members = Members(("DateTime", "Value")),
            Ignored = [.. Ignored, "Properties.CalendarTimeProperties."],
        },
        new()
        {
            FullName = Editors + "TimeEdit", Vendor = Vendor, As = "DateTimePicker",
            Collections = [EditorButtons],
            Properties = [new("Time", "Value"), new("EditValue", "Value", VendorValues.LiteralOf<DateTime>()), .. EditorAppearance],
            Events = Names(("EditValueChanged", "ValueChanged")),
            Members = Members(("Time", "Value")),
            Synthesized = [("Format", "System.Windows.Forms.DateTimePickerFormat.Time")],
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "LabelControl", Vendor = Vendor, As = "Label",
            Properties = Appearance,
            Members = Members(("Text", "Text")),
            Ignored = [.. Ignored, "AutoSizeMode"],
        },
        new()
        {
            FullName = Editors + "SimpleButton", Vendor = Vendor, As = "Button",
            Properties = Appearance,
            Events = Names(("Click", "Click")),
            Members = Members(("Text", "Text")),
            Ignored = [.. Ignored, "ImageOptions."],
        },
        new()
        {
            FullName = Editors + "HyperlinkLabelControl", Vendor = Vendor, As = "LinkLabel",
            Properties = Appearance,
            Events = Names(("HyperlinkClick", "LinkClicked")),
            Members = Members(("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "HyperLinkEdit", Vendor = Vendor, As = "LinkLabel",
            Properties = [new("EditValue", "Text", VendorValues.LiteralOf<string>()), .. EditorAppearance],
            Events = Names(("OpenLink", "LinkClicked")),
            Members = Members(("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "PictureEdit", Vendor = Vendor, As = "PictureBox",
            Properties =
            [
                new("Properties.SizeMode", "SizeMode",
                    VendorValues.Enum(("Clip", "Normal"), ("Stretch", "StretchImage"), ("Zoom", "Zoom"), ("Squeeze", "Zoom"))),
            ],
            Members = Members(),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "ProgressBarControl", Vendor = Vendor, As = "ProgressBar",
            Properties =
            [
                new("Position", "Value"),
                new("EditValue", "Value", VendorValues.Number),
                new("Properties.Minimum", "Minimum"),
                new("Properties.Maximum", "Maximum"),
                new("Properties.Step", "Step"),
            ],
            Members = Members(("Position", "Value")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "MarqueeProgressBarControl", Vendor = Vendor, As = "ProgressBar",
            Synthesized = [("Style", "System.Windows.Forms.ProgressBarStyle.Marquee")],
            Members = Members(),
            Ignored = [.. Ignored, "Properties.MarqueeAnimationSpeed"],
        },
        new()
        {
            FullName = Editors + "TrackBarControl", Vendor = Vendor, As = "TrackBar",
            Properties =
            [
                new("EditValue", "Value", VendorValues.Number),
                new("Properties.Minimum", "Minimum"),
                new("Properties.Maximum", "Maximum"),
                new("Properties.TickFrequency", "TickFrequency"),
            ],
            Events = Names(("ValueChanged", "ValueChanged"), ("EditValueChanged", "ValueChanged")),
            Members = Members(("Value", "Value")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Editors + "ListBoxControl", Vendor = Vendor, As = "ListBox",
            Collections = [new("Items", VendorCollectionKind.Items)],
            Events = Names(("SelectedIndexChanged", "SelectedIndexChanged")),
            Members = Members(("SelectedIndex", "SelectedIndex"), ("SelectedItem", "SelectedItem")),
            Ignored = Ignored,
        },
        new()
        {
            // `new CheckedListBoxItem("A")` - the value is the caption.
            FullName = Editors + "CheckedListBoxControl", Vendor = Vendor, As = "CheckedListBox",
            Collections = [new("Items", VendorCollectionKind.ConstructedItems, CaptionArgument: 0)],
            Members = Members(("SelectedIndex", "SelectedIndex")),
            Ignored = Ignored,
        },
        new()
        {
            // Becomes a Panel of RadioButtons - see VendorDesignerNormalizer.
            FullName = Editors + "RadioGroup", Vendor = Vendor, As = "Panel",
            Collections = [new("Properties.Items", VendorCollectionKind.ConstructedItems, CaptionArgument: 1)],
            Members = Members(),
            Ignored = Ignored,
        },
        new() { FullName = Editors + "GroupControl", Vendor = Vendor, As = "GroupBox", Members = Members(("Text", "Text")), Ignored = [.. Ignored, "AppearanceCaption.Options."] },
        new() { FullName = Editors + "PanelControl", Vendor = Vendor, As = "Panel", Members = Members(), Ignored = [.. Ignored, "BorderStyle"] },
        new() { FullName = Editors + "XtraScrollableControl", Vendor = Vendor, As = "Panel", Members = Members(), Ignored = Ignored },
        new() { FullName = Editors + "SidePanel", Vendor = Vendor, As = "Panel", Members = Members(), Ignored = Ignored },
        new()
        {
            // Horizontal = true (the default) is panels side by side, which WinForms calls Vertical.
            FullName = Editors + "SplitContainerControl", Vendor = Vendor, As = "SplitContainer",
            Properties =
            [
                new("Horizontal", "Orientation", VendorValues.BoolAsEnum("Vertical", "Horizontal")),
                new("SplitterPosition", "SplitterDistance"),
            ],
            Members = Members(),
            Ignored = [.. Ignored, "Panel1.", "Panel2.", "FixedPanel"],
        },

        // ── Tabs ──────────────────────────────────────────────────────────────────────────────
        new()
        {
            FullName = "DevExpress.XtraTab.XtraTabControl", Vendor = Vendor, As = "TabControl",
            Properties = [new("SelectedTabPageIndex", "SelectedIndex")],
            Collections = [new("TabPages", VendorCollectionKind.Children)],
            Events = Names(("SelectedPageChanged", "SelectedIndexChanged")),
            Members = Members(("SelectedTabPageIndex", "SelectedIndex")),
            Ignored = [.. Ignored, "SelectedTabPage"],
        },
        new() { FullName = "DevExpress.XtraTab.XtraTabPage", Vendor = Vendor, As = "TabPage", Members = Members(("Text", "Text")), Ignored = Ignored },

        // ── Grid (folded together by VendorDesignerNormalizer) ───────────────────────────────
        new()
        {
            FullName = "DevExpress.XtraGrid.GridControl", Vendor = Vendor, As = "DataGridView",
            Collections =
            [
                new("ViewCollection", VendorCollectionKind.Registration),
                new("RepositoryItems", VendorCollectionKind.Registration),
            ],
            Members = Members(),
            Ignored = [.. Ignored, "MainView", "EmbeddedNavigator."],
        },
        new()
        {
            FullName = "DevExpress.XtraGrid.Views.Grid.GridView", Vendor = Vendor,
            Collections = [new("Columns", VendorCollectionKind.Children)],
        },
        new()
        {
            FullName = "DevExpress.XtraGrid.Columns.GridColumn", Vendor = Vendor, As = "DataGridViewTextBoxColumn",
            Properties = [new("FieldName", "DataPropertyName"), new("Caption", "HeaderText"), new("Width", "Width")],
            Ignored = [.. Ignored, "Visible", "VisibleIndex", "ColumnEdit", "Name", "OptionsColumn."],
        },
        new() { FullName = "DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit", Vendor = Vendor },
        new() { FullName = "DevExpress.XtraEditors.Repository.RepositoryItemComboBox", Vendor = Vendor },
        new() { FullName = "DevExpress.XtraEditors.Repository.RepositoryItemTextEdit", Vendor = Vendor },

        // ── Bars (rebuilt into strips by VendorDesignerNormalizer) ────────────────────────────
        new()
        {
            FullName = "DevExpress.XtraBars.BarManager", Vendor = Vendor,
            Collections =
            [
                new("Bars", VendorCollectionKind.Registration),
                new("Items", VendorCollectionKind.Registration),
            ],
        },
        new()
        {
            FullName = "DevExpress.XtraBars.Bar", Vendor = Vendor,
            Collections = [new("LinksPersistInfo", VendorCollectionKind.Links)],
        },
        new() { FullName = "DevExpress.XtraBars.BarDockControl", Vendor = Vendor },
        new()
        {
            FullName = "DevExpress.XtraBars.BarButtonItem", Vendor = Vendor,
            PlacedAs = Names(("Menu", "ToolStripMenuItem"), ("ToolBar", "ToolStripButton"), ("StatusBar", "ToolStripButton")),
            Properties = [new("Caption", "Text")],
            Events = Names(("ItemClick", "Click")),
            Members = Names(("Caption", "Text"), ("Enabled", "Enabled")),
            Ignored = [.. Ignored, "Id", "ImageOptions.", "Name"],
        },
        new()
        {
            FullName = "DevExpress.XtraBars.BarSubItem", Vendor = Vendor,
            PlacedAs = Names(("Menu", "ToolStripMenuItem"), ("ToolBar", "ToolStripDropDownButton"), ("StatusBar", "ToolStripDropDownButton")),
            Properties = [new("Caption", "Text")],
            Collections = [new("LinksPersistInfo", VendorCollectionKind.Links)],
            Members = Names(("Caption", "Text"), ("Enabled", "Enabled")),
            Ignored = [.. Ignored, "Id", "ImageOptions.", "Name"],
        },
        new()
        {
            FullName = "DevExpress.XtraBars.BarStaticItem", Vendor = Vendor,
            PlacedAs = Names(("Menu", "ToolStripMenuItem"), ("ToolBar", "ToolStripLabel"), ("StatusBar", "ToolStripStatusLabel")),
            Properties = [new("Caption", "Text")],
            Members = Names(("Caption", "Text")),
            Ignored = [.. Ignored, "Id", "ImageOptions.", "Name"],
        },
        new()
        {
            FullName = "DevExpress.XtraBars.BarCheckItem", Vendor = Vendor,
            PlacedAs = Names(("Menu", "ToolStripMenuItem"), ("ToolBar", "ToolStripButton"), ("StatusBar", "ToolStripButton")),
            Properties = [new("Caption", "Text")],
            Events = Names(("CheckedChanged", "CheckedChanged"), ("ItemClick", "Click")),
            Members = Names(("Caption", "Text"), ("Checked", "Checked")),
            Synthesized = [("CheckOnClick", "true")],
            Ignored = [.. Ignored, "Id", "ImageOptions.", "Name"],
        },

        // ── Ribbon (flattened into a menu bar) ───────────────────────────────────────────────
        new()
        {
            FullName = "DevExpress.XtraBars.Ribbon.RibbonControl", Vendor = Vendor, As = "MenuStrip",
            Collections =
            [
                new("Pages", VendorCollectionKind.Children),
                new("Items", VendorCollectionKind.Registration),
            ],
            Ignored =
            [
                .. Ignored, "ExpandCollapseItem", "ExpandCollapseItem.", "MaxItemId", "SearchEditItem.", "RibbonStyle",
                "ShowApplicationButton", "ShowPageHeadersMode", "ShowToolbarCustomizeItem", "Toolbar.", "OptionsMenuMinWidth",
                "ShowExpandCollapseButton", "ShowSearchItem", "StatusBar",
            ],
        },
        new()
        {
            FullName = "DevExpress.XtraBars.Ribbon.RibbonPage", Vendor = Vendor, As = "ToolStripMenuItem",
            Collections = [new("Groups", VendorCollectionKind.Children)],
            Ignored = [.. Ignored, "Name"],
        },
        new()
        {
            FullName = "DevExpress.XtraBars.Ribbon.RibbonPageGroup", Vendor = Vendor, As = "ToolStripMenuItem",
            Collections = [new("ItemLinks", VendorCollectionKind.Links)],
            Ignored = [.. Ignored, "Name"],
        },
        new()
        {
            FullName = "DevExpress.XtraBars.Ribbon.RibbonStatusBar", Vendor = Vendor, As = "StatusStrip",
            Collections = [new("ItemLinks", VendorCollectionKind.Links)],
            Ignored = [.. Ignored, "Ribbon"],
        },

        // ── Tree and layout ──────────────────────────────────────────────────────────────────
        new()
        {
            FullName = "DevExpress.XtraTreeList.TreeList", Vendor = Vendor, As = "TreeView",
            Collections = [new("Columns", VendorCollectionKind.Children)],
            Members = Members(),
            Ignored = Ignored,
        },
        new() { FullName = "DevExpress.XtraTreeList.Columns.TreeListColumn", Vendor = Vendor },
        new()
        {
            FullName = "DevExpress.XtraLayout.LayoutControl", Vendor = Vendor, As = "Panel",
            Members = Members(),
            Ignored = [.. Ignored, "Root", "OptionsView.", "OptionsCustomizationForm."],
        },
        new()
        {
            FullName = "DevExpress.XtraLayout.LayoutControlGroup", Vendor = Vendor,
            Collections = [new("Items", VendorCollectionKind.Registration)],
        },
        new() { FullName = "DevExpress.XtraLayout.LayoutControlItem", Vendor = Vendor },
        new() { FullName = "DevExpress.XtraLayout.EmptySpaceItem", Vendor = Vendor },
        new() { FullName = "DevExpress.XtraLayout.SplitterItem", Vendor = Vendor },
    ];
}
