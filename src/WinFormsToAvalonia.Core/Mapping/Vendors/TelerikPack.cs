namespace WinFormsToAvalonia.Core.Mapping.Vendors;

/// <summary>Telerik UI for WinForms (Telerik.WinControls.UI, Telerik.WinControls.GridView).</summary>
/// <remarks>
/// Telerik's designer writes most properties under their WinForms names already; where it does not
/// (<c>ToggleState</c>, <c>Value1</c>, <c>HeaderText</c>) the entry renames. Grid columns and list
/// items are designer <em>locals</em>, which is what <see cref="VendorControl.AllowedAsLocal"/> is
/// for. Checked against the real assemblies by <c>WinFormsToAvalonia.Vendors.Tests</c>.
/// </remarks>
internal static class TelerikPack
{
    private const string Vendor = "Telerik";
    private const string Ui = "Telerik.WinControls.UI.";

    private static readonly (string, string)[] ControlMembers = [("Visible", "Visible"), ("Enabled", "Enabled"), ("Focus", "Focus")];

    /// <summary>Themes, and the element tree a designer styles through - neither has a counterpart.</summary>
    private static readonly string[] Ignored = ["ThemeName", "RootElement."];

    private static Dictionary<string, string> Names(params (string From, string To)[] pairs) =>
        pairs.ToDictionary(p => p.From, p => p.To, StringComparer.Ordinal);

    private static Dictionary<string, string> Members(params (string From, string To)[] pairs) =>
        Names([.. ControlMembers, .. pairs]);

    private static readonly VendorProperty ToggleStateAsChecked =
        new("ToggleState", "Checked", VendorValues.EnumAsBool("On", "Off"));

    /// <summary>
    /// What Telerik's designer really serializes for a check box's state - ToggleState, IsChecked and
    /// Checked are all hidden from it. Indeterminate declines, and stays reported.
    /// </summary>
    private static readonly VendorProperty CheckStateAsChecked =
        new("CheckState", "Checked", VendorValues.EnumAsBool("Checked", "Unchecked"));

    public static IReadOnlyList<VendorBaseType> BaseTypes { get; } =
    [
        new(Ui + "RadForm", Vendor, "Form", ["ThemeName", "RootElement.", "FormElement."]),
        new(Ui + "RadRibbonForm", Vendor, "Form", ["ThemeName", "RootElement.", "FormElement.", "RibbonBar"]),
        new(Ui + "RadTabbedForm", Vendor, "Form", ["ThemeName", "RootElement.", "FormElement."]),
        new(Ui + "ShapedForm", Vendor, "Form", ["Shape", "BorderColor", "BorderWidth"]),
    ];

    public static IReadOnlyList<VendorControl> Controls { get; } =
    [
        // ── Buttons, labels, editors ──────────────────────────────────────────────────────────
        new()
        {
            FullName = Ui + "RadButton", Vendor = Vendor, As = "Button",
            Events = Names(("Click", "Click")),
            Members = Members(("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadRepeatButton", Vendor = Vendor, As = "Button",
            Events = Names(("Click", "Click")),
            Members = Members(("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadToggleButton", Vendor = Vendor, As = "CheckBox",
            Properties = [ToggleStateAsChecked, CheckStateAsChecked, new("IsChecked", "Checked")],
            Events = Names(("ToggleStateChanged", "CheckedChanged"), ("CheckStateChanged", "CheckStateChanged")),
            Members = Members(("Text", "Text"), ("IsChecked", "Checked")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadLabel", Vendor = Vendor, As = "Label",
            Properties = [new("TextAlignment", "TextAlign")],
            Members = Members(("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadTextBox", Vendor = Vendor, As = "TextBox",
            Properties = [new("NullText", "PlaceholderText")],
            Events = Names(("TextChanged", "TextChanged")),
            Members = Members(("Text", "Text"), ("ReadOnly", "ReadOnly")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadTextBoxControl", Vendor = Vendor, As = "TextBox",
            Properties = [new("IsReadOnly", "ReadOnly"), new("NullText", "PlaceholderText")],
            Events = Names(("TextChanged", "TextChanged")),
            Members = Members(("Text", "Text"), ("IsReadOnly", "ReadOnly")),
            Ignored = Ignored,
        },
        new()
        {
            // Its Focus() returns nothing where WinForms' returns a bool, so it is left out of the members.
            FullName = Ui + "RadMaskedEditBox", Vendor = Vendor, As = "MaskedTextBox",
            Events = Names(("TextChanged", "TextChanged")),
            Members = Names(("Visible", "Visible"), ("Enabled", "Enabled"), ("Text", "Text")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadCheckBox", Vendor = Vendor, As = "CheckBox",
            Properties = [ToggleStateAsChecked, CheckStateAsChecked, new("IsChecked", "Checked")],
            Events = Names(("ToggleStateChanged", "CheckedChanged"), ("CheckStateChanged", "CheckStateChanged")),
            Members = Members(("Text", "Text"), ("IsChecked", "Checked"), ("Checked", "Checked"), ("CheckState", "CheckState")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadRadioButton", Vendor = Vendor, As = "RadioButton",
            Properties = [ToggleStateAsChecked, CheckStateAsChecked, new("IsChecked", "Checked")],
            Events = Names(("ToggleStateChanged", "CheckedChanged")),
            Members = Members(("Text", "Text"), ("IsChecked", "Checked")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadDropDownList", Vendor = Vendor, As = "ComboBox",
            Properties =
            [
                new("DropDownStyle", "DropDownStyle", VendorValues.Enum(("DropDownList", "DropDownList"), ("DropDown", "DropDown"))),
            ],
            Collections = [new("Items", VendorCollectionKind.Items)],
            Events = Names(("SelectedIndexChanged", "SelectedIndexChanged"), ("SelectedValueChanged", "SelectedValueChanged")),
            Members = Members(("SelectedIndex", "SelectedIndex"), ("Text", "Text"), ("SelectedValue", "SelectedValue")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadListDataItem", Vendor = Vendor, AllowedAsLocal = true, ConstructorTextArgument = 0,
        },
        new()
        {
            FullName = Ui + "RadListControl", Vendor = Vendor, As = "ListBox",
            Collections = [new("Items", VendorCollectionKind.Items)],
            Events = Names(("SelectedIndexChanged", "SelectedIndexChanged")),
            Members = Members(("SelectedIndex", "SelectedIndex")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadCheckedListBox", Vendor = Vendor, As = "CheckedListBox",
            Collections = [new("Items", VendorCollectionKind.Items)],
            Members = Members(),
            Ignored = Ignored,
        },
        new() { FullName = Ui + "ListViewDataItem", Vendor = Vendor, AllowedAsLocal = true, ConstructorTextArgument = 0 },
        new()
        {
            FullName = Ui + "RadSpinEditor", Vendor = Vendor, As = "NumericUpDown",
            Properties = [new("Step", "Increment")],
            Events = Names(("ValueChanged", "ValueChanged")),
            Members = Members(("Value", "Value")),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadDateTimePicker", Vendor = Vendor, As = "DateTimePicker",
            Events = Names(("ValueChanged", "ValueChanged")),
            Members = Members(("Value", "Value")),
            Ignored = Ignored,
        },
        new()
        {
            // Its Value is a nullable DateTime - not the same type, so no body may write it here.
            FullName = Ui + "RadTimePicker", Vendor = Vendor, As = "DateTimePicker",
            Events = Names(("ValueChanged", "ValueChanged")),
            Members = Members(),
            Synthesized = [("Format", "System.Windows.Forms.DateTimePickerFormat.Time")],
            Ignored = [.. Ignored, "Value"],
        },
        new()
        {
            FullName = Ui + "RadGroupBox", Vendor = Vendor, As = "GroupBox",
            Properties = [new("HeaderText", "Text")],
            Members = Members(("HeaderText", "Text")),
            Ignored = Ignored,
        },
        new() { FullName = Ui + "RadPanel", Vendor = Vendor, As = "Panel", Members = Members(), Ignored = Ignored },
        new()
        {
            FullName = Ui + "RadPageView", Vendor = Vendor, As = "TabControl",
            Collections = [new("Pages", VendorCollectionKind.Children)],
            Events = Names(("SelectedPageChanged", "SelectedIndexChanged")),
            Members = Members(),
            Ignored = [.. Ignored, "SelectedPage", "ViewMode", "DefaultPage"],
        },
        new() { FullName = Ui + "RadPageViewPage", Vendor = Vendor, As = "TabPage", Members = Members(("Text", "Text")), Ignored = [.. Ignored, "ItemSize"] },
        new()
        {
            FullName = Ui + "RadProgressBar", Vendor = Vendor, As = "ProgressBar",
            Properties = [new("Value1", "Value")],
            Members = Members(("Value1", "Value")),
            Ignored = Ignored,
        },
        new()
        {
            // Telerik's track bar is float-valued: designer values come across, code that writes them does not.
            FullName = Ui + "RadTrackBar", Vendor = Vendor, As = "TrackBar",
            Events = Names(("ValueChanged", "ValueChanged")),
            Members = Members(),
            Ignored = Ignored,
        },
        new()
        {
            // Folded into a SplitContainer's two halves by VendorDesignerNormalizer.
            FullName = Ui + "RadSplitContainer", Vendor = Vendor, As = "SplitContainer",
            Members = Members(),
            Ignored = Ignored,
        },
        new() { FullName = Ui + "SplitPanel", Vendor = Vendor, Ignored = [.. Ignored, "SizeInfo."] },

        // ── Grid ─────────────────────────────────────────────────────────────────────────────
        new()
        {
            FullName = Ui + "RadGridView", Vendor = Vendor, As = "DataGridView",
            Properties = [new("AllowAddNewRow", "AllowUserToAddRows"), new("MasterTemplate.AllowAddNewRow", "AllowUserToAddRows")],
            Collections = [new("MasterTemplate.Columns", VendorCollectionKind.Children)],
            Members = Members(),
            Ignored = [.. Ignored, "MasterTemplate.ViewDefinition"],
        },
        GridColumn("GridViewTextBoxColumn", "DataGridViewTextBoxColumn"),
        GridColumn("GridViewDecimalColumn", "DataGridViewTextBoxColumn"),
        GridColumn("GridViewDateTimeColumn", "DataGridViewTextBoxColumn"),
        GridColumn("GridViewMaskBoxColumn", "DataGridViewTextBoxColumn"),
        GridColumn("GridViewCheckBoxColumn", "DataGridViewCheckBoxColumn"),
        GridColumn("GridViewComboBoxColumn", "DataGridViewComboBoxColumn"),
        GridColumn("GridViewImageColumn", "DataGridViewImageColumn"),
        GridColumn(
            "GridViewCommandColumn", "DataGridViewButtonColumn",
            new("DefaultText", "Text"), new("UseDefaultText", "UseColumnTextForButtonValue")),

        // ── Menus, ribbon, command bar, status strip ─────────────────────────────────────────
        new()
        {
            FullName = Ui + "RadMenu", Vendor = Vendor, As = "MenuStrip",
            Collections = [new("Items", VendorCollectionKind.Children)],
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadMenuItem", Vendor = Vendor, As = "ToolStripMenuItem",
            Properties = [new("IsChecked", "Checked")],
            Collections = [new("Items", VendorCollectionKind.Children)],
            Events = Names(("Click", "Click")),
            Members = Names(("Text", "Text"), ("Enabled", "Enabled"), ("IsChecked", "Checked")),
            Ignored = [.. Ignored, "Name"],
        },
        new() { FullName = Ui + "RadMenuSeparatorItem", Vendor = Vendor, As = "ToolStripSeparator", Ignored = [.. Ignored, "Name", "Text", "TextAlignment"] },
        new()
        {
            FullName = Ui + "RadRibbonBar", Vendor = Vendor, As = "MenuStrip",
            Collections =
            [
                new("CommandTabs", VendorCollectionKind.Children),
                new("QuickAccessToolBarItems", VendorCollectionKind.Registration),
            ],
            Ignored = [.. Ignored, "ApplicationMenuStyle", "StartButtonImage", "LayoutMode"],
        },
        new()
        {
            FullName = Ui + "RibbonTab", Vendor = Vendor, As = "ToolStripMenuItem",
            Collections = [new("Items", VendorCollectionKind.Children)],
            Ignored = [.. Ignored, "Name", "IsSelected"],
        },
        new()
        {
            FullName = Ui + "RadRibbonBarGroup", Vendor = Vendor, As = "ToolStripMenuItem",
            Collections = [new("Items", VendorCollectionKind.Children)],
            Ignored = [.. Ignored, "Name"],
        },
        new()
        {
            FullName = Ui + "RadButtonElement", Vendor = Vendor, As = "ToolStripMenuItem",
            Events = Names(("Click", "Click")),
            Members = Names(("Text", "Text"), ("Enabled", "Enabled")),
            Ignored = [.. Ignored, "Name"],
        },
        new()
        {
            FullName = Ui + "RadCommandBar", Vendor = Vendor, As = "ToolStrip",
            Collections = [new("Rows", VendorCollectionKind.Children)],
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "CommandBarRowElement", Vendor = Vendor, AllowedAsLocal = true,
            Collections = [new("Strips", VendorCollectionKind.Children)],
        },
        new()
        {
            FullName = Ui + "CommandBarStripElement", Vendor = Vendor,
            Collections = [new("Items", VendorCollectionKind.Children)],
        },
        new()
        {
            FullName = Ui + "CommandBarButton", Vendor = Vendor, As = "ToolStripButton",
            Events = Names(("Click", "Click")),
            Members = Names(("Text", "Text"), ("Enabled", "Enabled")),
            Ignored = [.. Ignored, "Name", "DisplayName", "DrawText", "VisibleInOverflowMenu", "DrawImage"],
        },
        new()
        {
            FullName = Ui + "CommandBarToggleButton", Vendor = Vendor, As = "ToolStripButton",
            Synthesized = [("CheckOnClick", "true")],
            Events = Names(("Click", "Click")),
            Members = Names(("Text", "Text"), ("Enabled", "Enabled")),
            Ignored = [.. Ignored, "Name", "DisplayName", "DrawText", "VisibleInOverflowMenu", "DrawImage"],
        },
        new() { FullName = Ui + "CommandBarSeparator", Vendor = Vendor, As = "ToolStripSeparator", Ignored = [.. Ignored, "Name", "DisplayName", "DrawText", "VisibleInOverflowMenu", "DrawImage"] },
        new() { FullName = Ui + "CommandBarLabel", Vendor = Vendor, As = "ToolStripLabel", Members = Names(("Text", "Text")), Ignored = [.. Ignored, "Name", "DisplayName", "DrawText", "VisibleInOverflowMenu", "DrawImage"] },
        new() { FullName = Ui + "CommandBarTextBox", Vendor = Vendor, As = "ToolStripTextBox", Members = Names(("Text", "Text")), Ignored = [.. Ignored, "Name", "DisplayName", "DrawText", "VisibleInOverflowMenu", "DrawImage"] },
        new()
        {
            FullName = Ui + "RadStatusStrip", Vendor = Vendor, As = "StatusStrip",
            Collections = [new("Items", VendorCollectionKind.Children)],
            Ignored = [.. Ignored, "SizingGrip"],
        },
        new()
        {
            FullName = Ui + "RadLabelElement", Vendor = Vendor, As = "ToolStripStatusLabel",
            Members = Names(("Text", "Text")),
            Ignored = [.. Ignored, "Name", "TextWrap"],
        },

        // ── Tree and layout ──────────────────────────────────────────────────────────────────
        new()
        {
            FullName = Ui + "RadTreeView", Vendor = Vendor, As = "TreeView",
            Collections = [new("Nodes", VendorCollectionKind.Children)],
            Members = Members(),
            Ignored = Ignored,
        },
        new()
        {
            FullName = Ui + "RadTreeNode", Vendor = Vendor, AllowedAsLocal = true, ConstructorTextArgument = 0,
            Collections = [new("Nodes", VendorCollectionKind.Children)],
        },
        new()
        {
            FullName = Ui + "RadLayoutControl", Vendor = Vendor, As = "Panel",
            Collections = [new("Items", VendorCollectionKind.Registration)],
            Members = Members(),
            Ignored = Ignored,
        },
        new() { FullName = Ui + "LayoutControlItem", Vendor = Vendor },
        new()
        {
            FullName = Ui + "LayoutControlGroupItem", Vendor = Vendor,
            Collections = [new("Items", VendorCollectionKind.Registration)],
        },
        new() { FullName = Ui + "LayoutControlSeparatorItem", Vendor = Vendor },
    ];

    private static VendorControl GridColumn(string name, string target, params VendorProperty[] extra) => new()
    {
        FullName = Ui + name, Vendor = Vendor, As = target, AllowedAsLocal = true,
        Properties = [new("FieldName", "DataPropertyName"), new("Width", "Width"), .. extra],
        Ignored = [.. Ignored, "Name", "IsVisible"],
    };
}
