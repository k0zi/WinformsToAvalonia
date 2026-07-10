namespace Converter.Plugin.Abstractions;

/// <summary>
/// Defines custom validation rules for conversion.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Rule identifier.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Rule description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Validation severity level.
    /// </summary>
    ValidationSeverity Severity { get; }

    /// <summary>
    /// Validate a control node.
    /// </summary>
    Task<ValidationResult> ValidateAsync(ControlNode control, ValidationContext context);
}

/// <summary>
/// Severity level for validation issues.
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Result of validation.
/// </summary>
public class ValidationResult
{
    public required bool IsValid { get; init; }
    public List<ValidationIssue> Issues { get; init; } = [];
}

/// <summary>
/// Represents a validation issue.
/// </summary>
public class ValidationIssue
{
    public required string RuleId { get; init; }
    public required ValidationSeverity Severity { get; init; }
    public required string Message { get; init; }
    public string? ControlName { get; init; }
    public string? PropertyName { get; init; }
    public string? SourceFile { get; init; }
    public int? SourceLine { get; init; }
    public string? SuggestedFix { get; init; }
}

/// <summary>
/// Context for validation operations.
/// </summary>
public class ValidationContext
{
    public Dictionary<string, object> Options { get; init; } = [];
    public IServiceProvider? Services { get; init; }
}
