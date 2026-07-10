namespace Converter.Plugin.Abstractions;

/// <summary>
/// Represents a WinForms control in the abstract syntax tree.
/// </summary>
public class ControlNode
{
    /// <summary>
    /// Control type (e.g., "Button", "TextBox", "Form").
    /// </summary>
    public required string ControlType { get; init; }

    /// <summary>
    /// Fully qualified control type name.
    /// </summary>
    public required string FullTypeName { get; init; }

    /// <summary>
    /// Control instance name/identifier.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Parent control, null if root.
    /// </summary>
    public ControlNode? Parent { get; set; }

    /// <summary>
    /// Child controls.
    /// </summary>
    public List<ControlNode> Children { get; init; } = [];

    /// <summary>
    /// Control properties.
    /// </summary>
    public Dictionary<string, PropertyValue> Properties { get; init; } = [];

    /// <summary>
    /// Event handlers attached to the control.
    /// </summary>
    public Dictionary<string, string> EventHandlers { get; init; } = [];

    /// <summary>
    /// Data bindings for the control.
    /// </summary>
    public List<DataBinding> DataBindings { get; init; } = [];

    /// <summary>
    /// Indicates if this is a third-party control.
    /// </summary>
    public bool IsThirdParty { get; init; }

    /// <summary>
    /// Indicates if this is a custom user control.
    /// </summary>
    public bool IsCustomControl { get; init; }

    /// <summary>
    /// Source file location for this control definition.
    /// </summary>
    public string? SourceFile { get; init; }

    /// <summary>
    /// Source line number.
    /// </summary>
    public int? SourceLine { get; init; }
}

/// <summary>
/// Represents a property value with type information.
/// </summary>
public class PropertyValue
{
    public required string Name { get; init; }
    public object? Value { get; init; }
    public required string Type { get; init; }
    public bool IsResource { get; init; }
    public string? ResourceKey { get; init; }
}

/// <summary>
/// Represents a data binding configuration.
/// </summary>
public class DataBinding
{
    public required string PropertyName { get; init; }
    public required string DataSource { get; init; }
    public required string DataMember { get; init; }
    public string? FormatString { get; init; }
    public bool FormattingEnabled { get; init; }
}
