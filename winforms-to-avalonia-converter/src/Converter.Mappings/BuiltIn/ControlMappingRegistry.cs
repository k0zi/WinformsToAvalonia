using Converter.Plugin.Abstractions;

namespace Converter.Mappings.BuiltIn;

/// <summary>
/// Registry of built-in WinForms to Avalonia control mappings.
/// </summary>
public static class ControlMappingRegistry
{
    private static readonly Dictionary<string, ControlMapping> _mappings = new()
    {
        // Forms and Containers
        ["Form"] = new("Window", "Avalonia.Controls.Window"),
        ["UserControl"] = new("UserControl", "Avalonia.Controls.UserControl"),
        ["Panel"] = new("Panel", "Avalonia.Controls.Panel"),
        ["GroupBox"] = new("Border", "Avalonia.Controls.Border") 
        { 
            RequiresWrapper = true,
            WrapperType = "HeaderedContentControl"
        },
        ["TabControl"] = new("TabControl", "Avalonia.Controls.TabControl"),
        ["TabPage"] = new("TabItem", "Avalonia.Controls.TabItem"),
        ["SplitContainer"] = new("Grid", "Avalonia.Controls.Grid") 
        { 
            RequiresCustomLogic = true,
            Notes = "Requires Grid with GridSplitter"
        },
        ["FlowLayoutPanel"] = new("WrapPanel", "Avalonia.Controls.WrapPanel"),
        ["TableLayoutPanel"] = new("Grid", "Avalonia.Controls.Grid") 
        { 
            RequiresCustomLogic = true 
        },

        // Basic Controls
        ["Button"] = new("Button", "Avalonia.Controls.Button"),
        ["TextBox"] = new("TextBox", "Avalonia.Controls.TextBox"),
        ["Label"] = new("TextBlock", "Avalonia.Controls.TextBlock"),
        ["CheckBox"] = new("CheckBox", "Avalonia.Controls.CheckBox"),
        ["RadioButton"] = new("RadioButton", "Avalonia.Controls.RadioButton"),
        ["ComboBox"] = new("ComboBox", "Avalonia.Controls.ComboBox"),
        ["ListBox"] = new("ListBox", "Avalonia.Controls.ListBox"),

        // Advanced Controls
        ["DataGridView"] = new("DataGrid", "Avalonia.Controls.DataGrid"),
        ["TreeView"] = new("TreeView", "Avalonia.Controls.TreeView"),
        ["ListView"] = new("ListBox", "Avalonia.Controls.ListBox") 
        { 
            Notes = "ListView maps to ListBox in Avalonia"
        },
        ["RichTextBox"] = new("TextBox", "Avalonia.Controls.TextBox") 
        { 
            Notes = "Limited rich text support in Avalonia"
        },
        ["PictureBox"] = new("Image", "Avalonia.Controls.Image"),
        ["ProgressBar"] = new("ProgressBar", "Avalonia.Controls.ProgressBar"),
        ["TrackBar"] = new("Slider", "Avalonia.Controls.Slider"),
        ["NumericUpDown"] = new("NumericUpDown", "Avalonia.Controls.NumericUpDown"),
        ["DateTimePicker"] = new("DatePicker", "Avalonia.Controls.DatePicker"),
        ["MonthCalendar"] = new("Calendar", "Avalonia.Controls.Calendar"),

        // Menus and Toolbars
        ["MenuStrip"] = new("Menu", "Avalonia.Controls.Menu"),
        ["ToolStrip"] = new("ToolBar", "Avalonia.Controls.ToolBar"),
        ["StatusStrip"] = new("StatusBar", "Avalonia.Controls.Primitives.StatusBar"),
        ["ContextMenuStrip"] = new("ContextMenu", "Avalonia.Controls.ContextMenu"),
        ["ToolStripMenuItem"] = new("MenuItem", "Avalonia.Controls.MenuItem"),
        ["ToolStripButton"] = new("Button", "Avalonia.Controls.Button"),
        ["ToolStripLabel"] = new("TextBlock", "Avalonia.Controls.TextBlock"),

        // Input Controls
        ["MaskedTextBox"] = new("TextBox", "Avalonia.Controls.TextBox") 
        { 
            Notes = "Masking logic needs reimplementation"
        },
        ["LinkLabel"] = new("HyperlinkButton", "Avalonia.Controls.HyperlinkButton"),

        // Components (non-visual)
        ["Timer"] = new("DispatcherTimer", "Avalonia.Threading.DispatcherTimer") 
        { 
            IsComponent = true 
        },
        ["ToolTip"] = new("ToolTip", "Avalonia.Controls.ToolTip") 
        { 
            IsComponent = true 
        },
        ["NotifyIcon"] = new("TrayIcon", "Avalonia.Controls.TrayIcon") 
        { 
            IsComponent = true,
            Notes = "Requires Avalonia.Desktop package"
        },

        // Other Controls
        ["WebBrowser"] = new("WebView", "Avalonia.Controls.WebView") 
        { 
            Notes = "Requires platform-specific implementation"
        },
        ["PrintPreviewControl"] = new("UserControl", "Avalonia.Controls.UserControl") 
        { 
            RequiresCustomLogic = true,
            Notes = "No direct equivalent, needs custom implementation"
        }
    };

    public static ControlMapping? GetMapping(string winFormsControlType)
    {
        // Remove namespace if present
        var simpleType = winFormsControlType.Split('.').Last();
        return _mappings.TryGetValue(simpleType, out var mapping) ? mapping : null;
    }

    public static IReadOnlyDictionary<string, ControlMapping> GetAllMappings()
    {
        return _mappings;
    }

    public static bool IsMapped(string winFormsControlType)
    {
        var simpleType = winFormsControlType.Split('.').Last();
        return _mappings.ContainsKey(simpleType);
    }
}

/// <summary>
/// Represents a control mapping from WinForms to Avalonia.
/// </summary>
public record ControlMapping(string AvaloniaType, string FullTypeName)
{
    /// <summary>
    /// Whether this control requires a wrapper (e.g., GroupBox → HeaderedContentControl).
    /// </summary>
    public bool RequiresWrapper { get; init; }

    /// <summary>
    /// The wrapper type if required.
    /// </summary>
    public string? WrapperType { get; init; }

    /// <summary>
    /// Whether this is a component (non-visual).
    /// </summary>
    public bool IsComponent { get; init; }

    /// <summary>
    /// Whether custom conversion logic is required.
    /// </summary>
    public bool RequiresCustomLogic { get; init; }

    /// <summary>
    /// Additional notes about the mapping.
    /// </summary>
    public string? Notes { get; init; }
}
