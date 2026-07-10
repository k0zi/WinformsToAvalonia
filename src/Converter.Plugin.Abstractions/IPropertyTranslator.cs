namespace Converter.Plugin.Abstractions;

/// <summary>
/// Defines a custom property translator for WinForms to Avalonia conversions.
/// </summary>
public interface IPropertyTranslator
{
    /// <summary>
    /// Priority for this translator (higher values execute first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Check if this translator can handle the given property.
    /// </summary>
    bool CanTranslate(string controlType, string propertyName, object? value);

    /// <summary>
    /// Translate the WinForms property to Avalonia equivalent.
    /// </summary>
    Task<PropertyTranslationResult> TranslateAsync(
        string controlType, 
        string propertyName, 
        object? value,
        TranslationContext context);
}

/// <summary>
/// Result of a property translation operation.
/// </summary>
public class PropertyTranslationResult
{
    /// <summary>
    /// Avalonia property name.
    /// </summary>
    public required string AvaloniaPropertyName { get; init; }

    /// <summary>
    /// Translated value.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Value type.
    /// </summary>
    public required string ValueType { get; init; }

    /// <summary>
    /// Whether this property needs code-behind initialization.
    /// </summary>
    public bool RequiresCodeBehind { get; init; }

    /// <summary>
    /// Code-behind initialization code.
    /// </summary>
    public string? CodeBehindCode { get; init; }

    /// <summary>
    /// Whether manual intervention is required.
    /// </summary>
    public bool RequiresManualIntervention { get; init; }

    /// <summary>
    /// Notes for manual intervention.
    /// </summary>
    public string? ManualInterventionNotes { get; init; }
}

/// <summary>
/// Context for property translation operations.
/// </summary>
public class TranslationContext
{
    public required ControlNode Control { get; init; }
    public Dictionary<string, object> Options { get; init; } = [];
    public IServiceProvider? Services { get; init; }
}
