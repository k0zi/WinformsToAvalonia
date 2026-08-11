namespace Converter.Mappings.BuiltIn;

/// <summary>
/// Registry of WinForms to Avalonia property mappings.
/// </summary>
public static class PropertyMappingRegistry
{
    private static readonly Dictionary<string, PropertyMapping> _commonMappings = new()
    {
        // Text and Content
        ["Text"] = new("Text") { DirectMapping = true },
        ["Content"] = new("Content") { DirectMapping = true },

        // Size and Position (for Canvas layout)
        ["Width"] = new("Width") { DirectMapping = true },
        ["Height"] = new("Height") { DirectMapping = true },
        ["Size"] = new("Width,Height") { RequiresCustomLogic = true },
        ["Location"] = new("Canvas.Left,Canvas.Top") { RequiresCustomLogic = true },
        ["Left"] = new("Canvas.Left") { DirectMapping = true },
        ["Top"] = new("Canvas.Top") { DirectMapping = true },

        // Layout Properties
        ["Dock"] = new("DockPanel.Dock") { RequiresCustomLogic = true },
        ["Anchor"] = new("Grid.Row,Grid.Column") { RequiresCustomLogic = true, Notes = "Anchor converts to Grid positioning" },

        // TableLayoutPanel cell placement - captured by WinFormsParser from the
        // Controls.Add(control, column, row) 3-arg overload or SetColumn/SetRow/
        // SetColumnSpan/SetRowSpan calls, and written onto the *child* control (not the
        // panel itself, hence the distinct keys from _controlSpecificMappings["TableLayoutPanel"]
        // below, which apply to a TableLayoutPanel's own ColumnSpan/RowSpan properties).
        ["TableLayoutPanel.Column"] = new("Grid.Column") { DirectMapping = true },
        ["TableLayoutPanel.Row"] = new("Grid.Row") { DirectMapping = true },
        ["TableLayoutPanel.ColumnSpan"] = new("Grid.ColumnSpan") { DirectMapping = true },
        ["TableLayoutPanel.RowSpan"] = new("Grid.RowSpan") { DirectMapping = true },
        // WinForms Padding/Margin are `new Padding(l, t, r, b)` (or single-int all-sides)
        // expressions, not literal values - RequiresCustomLogic routes them through
        // PropertyValueConverter.TryConvertThickness instead of emitting the raw C# source
        // text as the attribute value (which Avalonia's Thickness parser can't read).
        ["Padding"] = new("Padding") { RequiresCustomLogic = true },
        ["Margin"] = new("Margin") { RequiresCustomLogic = true },

        // Appearance
        ["BackColor"] = new("Background") { RequiresConversion = true, ConversionType = "ColorToBrush" },
        ["ForeColor"] = new("Foreground") { RequiresConversion = true, ConversionType = "ColorToBrush" },
        ["Font"] = new("FontFamily,FontSize,FontWeight") { RequiresCustomLogic = true },
        ["BorderStyle"] = new("BorderBrush,BorderThickness") { RequiresCustomLogic = true },

        // Visibility and State
        ["Visible"] = new("IsVisible") { DirectMapping = true },
        ["Enabled"] = new("IsEnabled") { DirectMapping = true },
        ["ReadOnly"] = new("IsReadOnly") { DirectMapping = true },
        ["TabIndex"] = new("TabIndex") { DirectMapping = true },
        ["TabStop"] = new("IsTabStop") { DirectMapping = true },

        // Control-specific
        ["Checked"] = new("IsChecked") { DirectMapping = true },
        ["AutoSize"] = new("HorizontalAlignment,VerticalAlignment") { RequiresCustomLogic = true },
        ["MaxLength"] = new("MaxLength") { DirectMapping = true },
        ["Multiline"] = new("AcceptsReturn") { DirectMapping = true, Notes = "For TextBox" },
        ["PasswordChar"] = new("PasswordChar") { DirectMapping = true },
        ["SelectedIndex"] = new("SelectedIndex") { DirectMapping = true },
        ["SelectedItem"] = new("SelectedItem") { DirectMapping = true },

        // Images
        ["Image"] = new("Source") { RequiresConversion = true, ConversionType = "ImageToBitmap" },
        ["ImageList"] = new("Resources") { RequiresCustomLogic = true },
        ["BackgroundImage"] = new("Background") { RequiresConversion = true, ConversionType = "ImageBrush" },

        // Minimum/Maximum
        ["MinimumSize"] = new("MinWidth,MinHeight") { RequiresCustomLogic = true },
        ["MaximumSize"] = new("MaxWidth,MaxHeight") { RequiresCustomLogic = true },
        ["Minimum"] = new("Minimum") { DirectMapping = true },
        ["Maximum"] = new("Maximum") { DirectMapping = true },
        ["Value"] = new("Value") { DirectMapping = true },

        // Alignment
        ["TextAlign"] = new("HorizontalContentAlignment,VerticalContentAlignment") { RequiresCustomLogic = true },

        // DataBinding
        ["DataSource"] = new("ItemsSource") { DirectMapping = true, Notes = "Requires binding context adjustment" },
        ["DisplayMember"] = new("DisplayMemberPath") { DirectMapping = true },
        ["ValueMember"] = new("SelectedValuePath") { DirectMapping = true }
    };

    private static readonly Dictionary<string, Dictionary<string, PropertyMapping?>> _controlSpecificMappings = new()
    {
        ["Form"] = new()
        {
            ["Text"] = new("Title") { DirectMapping = true },
            ["ClientSize"] = new("Width,Height") { RequiresCustomLogic = true },
            ["FormBorderStyle"] = new("CanResize") { RequiresCustomLogic = true },
            ["WindowState"] = new("WindowState") { RequiresCustomLogic = true },
            ["StartPosition"] = new("WindowStartupLocation") { RequiresConversion = true, ConversionType = "FormStartPosition" },
            ["Icon"] = new("Icon") { RequiresConversion = true, ConversionType = "IconToWindowIcon" },
            ["TopMost"] = new("Topmost") { DirectMapping = true },
            ["ShowInTaskbar"] = new("ShowInTaskbar") { DirectMapping = true }
        },
        ["DataGridView"] = new()
        {
            ["Columns"] = new("Columns") { RequiresCustomLogic = true },
            // Avalonia's DataGrid has no "Items" property - only the bindable "ItemsSource"
            // (matching DataSource's own mapping below/in the common table). "Rows" itself has
            // no clean 1:1 Designer-time equivalent (it's a live collection manipulated at
            // runtime, never a literal Designer.cs property value) - RequiresCustomLogic with
            // no matching PropertyValueConverter case means this is currently inert either
            // way; this just fixes what the target name claims.
            ["Rows"] = new("ItemsSource") { RequiresCustomLogic = true },
            ["AutoGenerateColumns"] = new("AutoGenerateColumns") { DirectMapping = true },
            ["SelectionMode"] = new("SelectionMode") { RequiresConversion = true }
        },
        ["PictureBox"] = new()
        {
            ["Image"] = new("Source") { RequiresConversion = true, ConversionType = "ImageToBitmap" },
            ["SizeMode"] = new("Stretch") { RequiresConversion = true },
            // Avalonia's Image is a bare Control with no BorderThickness - AxamlGenerator
            // wraps a bordered PictureBox in a <Border> instead (see
            // AxamlGenerator.WriteBorderWrappedControl), which computes its own BorderThickness
            // value directly from the common BorderStyle mapping; this null just keeps the
            // normal per-property loop from also (redundantly, invalidly) emitting it on the
            // inner <Image> itself.
            ["BorderStyle"] = null
        },
        ["ProgressBar"] = new()
        {
            ["Style"] = new("IsIndeterminate") { RequiresConversion = true },
            ["Value"] = new("Value") { DirectMapping = true },
            ["Minimum"] = new("Minimum") { DirectMapping = true },
            ["Maximum"] = new("Maximum") { DirectMapping = true }
        },
        ["TableLayoutPanel"] = new()
        {
            ["ColumnSpan"] = new("Grid.ColumnSpan") { DirectMapping = true },
            ["RowSpan"] = new("Grid.RowSpan") { DirectMapping = true },
            // Grid (Avalonia's Panel-derived TableLayoutPanel target) has no Padding or
            // BorderThickness property - the latter is handled the same
            // Border-wrap-instead-of-direct-emission way as PictureBox above.
            ["Padding"] = null,
            ["BorderStyle"] = null
        },

        // Button/CheckBox/RadioButton map to Avalonia ContentControl/ToggleButton-derived
        // types, which expose their caption via Content, not Text (Avalonia's Text property
        // only exists on TextBlock/TextBox-like controls) - reusing the common "Text" mapping
        // for these previously emitted an attribute Avalonia's compiler rejects outright.
        ["Button"] = new() { ["Text"] = new("Content") { DirectMapping = true } },
        ["CheckBox"] = new() { ["Text"] = new("Content") { DirectMapping = true } },
        ["RadioButton"] = new() { ["Text"] = new("Content") { DirectMapping = true } },

        // WinForms TreeView.SelectedNode is single-selection - Avalonia TreeView.SelectedItem
        // is its exact match (SelectedItems is the separate multi-select collection, with no
        // WinForms equivalent here).
        ["TreeView"] = new() { ["SelectedNode"] = new("SelectedItem") { DirectMapping = true } },

        // Label/ToolStripLabel map to Avalonia's TextBlock, which - unlike a ContentControl -
        // has no HorizontalContentAlignment/VerticalContentAlignment at all: the common
        // "TextAlign" mapping below (correct for CheckBox/RadioButton, genuine
        // ContentControl-derived types) would emit attributes that don't compile. TextBlock's
        // own equivalents are TextAlignment (horizontal - text alignment within the block) and
        // the inherited Layoutable VerticalAlignment (positions the block itself within its
        // parent - the closest approximation available, since TextBlock has no native
        // vertical-text-alignment concept of its own).
        ["Label"] = new() { ["TextAlign"] = new("TextAlignment,VerticalAlignment") { RequiresCustomLogic = true } },
        ["ToolStripLabel"] = new() { ["TextAlign"] = new("TextAlignment,VerticalAlignment") { RequiresCustomLogic = true } },

        // TextBox.TextAlign uses a different WinForms enum entirely (System.Windows.Forms.
        // HorizontalAlignment - Left/Right/Center only, no vertical component) from Label's
        // 9-way System.Drawing.ContentAlignment, and TextBox (not ContentControl-derived
        // either) has its own TextAlignment property - a distinct target/converter from
        // Label's above, despite the shared WinForms property name.
        ["TextBox"] = new() { ["TextAlign"] = new("TextAlignment") { RequiresCustomLogic = true } },

        // GroupBox maps to a plain Border (no wrapper support is wired up in AxamlGenerator
        // yet, despite ControlMapping.RequiresWrapper/WrapperType existing on the record), and
        // Border has no Text/Header equivalent - dropped rather than emitted as a broken
        // attribute; recovering the caption needs the (currently unimplemented) wrapper.
        ["GroupBox"] = new() { ["Text"] = null },

        // Every Avalonia target below is Panel-derived (Panel/WrapPanel/Grid/StackPanel) and,
        // unlike Border/ContentControl-derived controls, none of them expose a Padding or
        // BorderThickness property at all - so WinForms Padding is dropped instead of emitted
        // as a broken attribute, and BorderStyle goes through AxamlGenerator's
        // Border-wrap-instead-of-direct-emission path instead (see PictureBox above), for any
        // of the WinForms control types that map to one of these.
        ["Panel"] = new() { ["Padding"] = null, ["BorderStyle"] = null },
        ["FlowLayoutPanel"] = new() { ["Padding"] = null, ["BorderStyle"] = null },
        ["SplitContainer"] = new() { ["Padding"] = null, ["BorderStyle"] = null },
        ["ToolStrip"] = new() { ["Padding"] = null, ["BorderStyle"] = null },
        ["StatusStrip"] = new() { ["Padding"] = null, ["BorderStyle"] = null },

        // DateTimePicker maps to Avalonia's DatePicker, whose selection property is
        // SelectedDate (DateTimeOffset?), not Value - the common "Value"->"Value" mapping
        // (correct for NumericUpDown/TrackBar/ProgressBar, which really do have a Value
        // property) would otherwise emit an attribute that doesn't exist on DatePicker.
        ["DateTimePicker"] = new() { ["Value"] = new("SelectedDate") { RequiresCustomLogic = true } }
    };

    public static PropertyMapping? GetMapping(string propertyName, string? controlType = null)
    {
        // Check control-specific mappings first. A control type present in this dictionary
        // short-circuits the lookup even when its value is an explicit `null` override (e.g.
        // "Panel"/"Padding") - that null means "this control has no mapping for this
        // property", distinct from "no control-specific override exists", which instead falls
        // through to the common mappings below.
        if (controlType != null && _controlSpecificMappings.TryGetValue(controlType, out var controlMappings) &&
            controlMappings.TryGetValue(propertyName, out var specificMapping))
        {
            return specificMapping;
        }

        // Fallback to common mappings
        return _commonMappings.TryGetValue(propertyName, out var commonMapping) ? commonMapping : null;
    }

    public static bool IsMapped(string propertyName, string? controlType = null)
    {
        return GetMapping(propertyName, controlType) != null;
    }

    public static IReadOnlyDictionary<string, PropertyMapping> GetCommonMappings()
    {
        return _commonMappings;
    }
}

/// <summary>
/// Represents a property mapping from WinForms to Avalonia.
/// </summary>
public record PropertyMapping(string AvaloniaProperty)
{
    /// <summary>
    /// Whether this is a direct 1:1 mapping.
    /// </summary>
    public bool DirectMapping { get; init; }

    /// <summary>
    /// Whether custom conversion logic is required.
    /// </summary>
    public bool RequiresCustomLogic { get; init; }

    /// <summary>
    /// Whether type conversion is required.
    /// </summary>
    public bool RequiresConversion { get; init; }

    /// <summary>
    /// The type of conversion required (e.g., "ColorToBrush").
    /// </summary>
    public string? ConversionType { get; init; }

    /// <summary>
    /// Additional notes about the mapping.
    /// </summary>
    public string? Notes { get; init; }
}
