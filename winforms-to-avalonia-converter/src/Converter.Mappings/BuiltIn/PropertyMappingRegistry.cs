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
        ["Padding"] = new("Padding") { DirectMapping = true },
        ["Margin"] = new("Margin") { DirectMapping = true },

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

    private static readonly Dictionary<string, Dictionary<string, PropertyMapping>> _controlSpecificMappings = new()
    {
        ["Form"] = new()
        {
            ["Text"] = new("Title") { DirectMapping = true },
            ["FormBorderStyle"] = new("WindowState,CanResize") { RequiresCustomLogic = true },
            ["StartPosition"] = new("WindowStartupLocation") { RequiresConversion = true },
            ["Icon"] = new("Icon") { RequiresConversion = true, ConversionType = "IconToWindowIcon" },
            ["TopMost"] = new("Topmost") { DirectMapping = true },
            ["ShowInTaskbar"] = new("ShowInTaskbar") { DirectMapping = true }
        },
        ["DataGridView"] = new()
        {
            ["Columns"] = new("Columns") { RequiresCustomLogic = true },
            ["Rows"] = new("Items") { RequiresCustomLogic = true },
            ["AutoGenerateColumns"] = new("AutoGenerateColumns") { DirectMapping = true },
            ["SelectionMode"] = new("SelectionMode") { RequiresConversion = true }
        },
        ["PictureBox"] = new()
        {
            ["Image"] = new("Source") { RequiresConversion = true, ConversionType = "ImageToBitmap" },
            ["SizeMode"] = new("Stretch") { RequiresConversion = true }
        },
        ["ProgressBar"] = new()
        {
            ["Style"] = new("IsIndeterminate") { RequiresConversion = true },
            ["Value"] = new("Value") { DirectMapping = true },
            ["Minimum"] = new("Minimum") { DirectMapping = true },
            ["Maximum"] = new("Maximum") { DirectMapping = true }
        }
    };

    public static PropertyMapping? GetMapping(string propertyName, string? controlType = null)
    {
        // Check control-specific mappings first
        if (controlType != null && _controlSpecificMappings.TryGetValue(controlType, out var controlMappings))
        {
            if (controlMappings.TryGetValue(propertyName, out var specificMapping))
            {
                return specificMapping;
            }
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
