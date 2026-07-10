namespace Converter.Plugin.Abstractions;

/// <summary>
/// Defines a custom control mapping from WinForms to Avalonia.
/// </summary>
public interface IControlMapper
{
    /// <summary>
    /// Priority for this mapper (higher values execute first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Check if this mapper can handle the given WinForms control.
    /// </summary>
    bool CanMap(ControlNode winFormsControl);

    /// <summary>
    /// Map the WinForms control to Avalonia representation.
    /// </summary>
    Task<ControlMappingResult> MapAsync(ControlNode winFormsControl, MappingContext context);
}

/// <summary>
/// Result of a control mapping operation.
/// </summary>
public class ControlMappingResult
{
    /// <summary>
    /// Target Avalonia control type.
    /// </summary>
    public required string AvaloniaControlType { get; init; }

    /// <summary>
    /// Mapped properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = [];

    /// <summary>
    /// Event to command mappings.
    /// </summary>
    public Dictionary<string, CommandMapping> Commands { get; init; } = [];

    /// <summary>
    /// Whether this mapping is complete or partial.
    /// </summary>
    public bool IsPartialMapping { get; init; }

    /// <summary>
    /// Manual steps required for completion.
    /// </summary>
    public List<string> ManualSteps { get; init; } = [];

    /// <summary>
    /// Custom AXAML fragment to include.
    /// </summary>
    public string? CustomAxaml { get; init; }

    /// <summary>
    /// Custom code-behind fragment to include.
    /// </summary>
    public string? CustomCodeBehind { get; init; }
}

/// <summary>
/// Represents a command mapping from event to ICommand.
/// </summary>
public class CommandMapping
{
    public required string CommandName { get; init; }
    public bool HasParameter { get; init; }
    public string? ParameterType { get; init; }
    public string? CanExecuteMethod { get; init; }
}

/// <summary>
/// Context for mapping operations.
/// </summary>
public class MappingContext
{
    public required string ProjectPath { get; init; }
    public required string OutputPath { get; init; }
    public Dictionary<string, object> Options { get; init; } = [];
    public IServiceProvider? Services { get; init; }
}
