namespace Converter.Plugin.Abstractions;

/// <summary>
/// Defines a custom event mapping from WinForms to Avalonia. Mirrors
/// <see cref="IControlMapper"/>/<see cref="IPropertyTranslator"/> - without this interface
/// there was no plugin extension point for events at all, only for controls/properties.
/// </summary>
public interface IEventMapper
{
    /// <summary>
    /// Priority for this mapper (higher values execute first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Check if this mapper can handle the given WinForms event.
    /// </summary>
    bool CanMap(string winFormsEvent, ControlNode control);

    /// <summary>
    /// Map the WinForms event to its Avalonia equivalent.
    /// </summary>
    Task<EventMappingResult> MapAsync(string winFormsEvent, ControlNode control, MappingContext context);
}

/// <summary>
/// Result of an event mapping operation.
/// </summary>
public class EventMappingResult
{
    /// <summary>
    /// Target Avalonia event name.
    /// </summary>
    public required string AvaloniaEvent { get; init; }

    /// <summary>
    /// Whether to convert this event to an ICommand in the ViewModel.
    /// </summary>
    public bool ConvertToCommand { get; init; }

    /// <summary>
    /// The suggested command name if converting to command.
    /// </summary>
    public string? CommandName { get; init; }

    /// <summary>
    /// Whether to preserve the event handler instead of converting.
    /// </summary>
    public bool PreserveEventHandler { get; init; }

    /// <summary>
    /// Manual steps required for completion.
    /// </summary>
    public List<string> ManualSteps { get; init; } = [];
}
