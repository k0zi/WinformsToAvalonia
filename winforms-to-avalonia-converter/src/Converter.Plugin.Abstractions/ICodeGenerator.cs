namespace Converter.Plugin.Abstractions;

/// <summary>
/// Defines a custom code generator for specific control types or patterns.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Priority for this generator (higher values execute first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Check if this generator can handle the given control.
    /// </summary>
    bool CanGenerate(ControlNode control, GenerationContext context);

    /// <summary>
    /// Generate code for the control.
    /// </summary>
    Task<CodeGenerationResult> GenerateAsync(ControlNode control, GenerationContext context);
}

/// <summary>
/// Result of code generation.
/// </summary>
public class CodeGenerationResult
{
    /// <summary>
    /// Generated AXAML markup.
    /// </summary>
    public string? AxamlCode { get; init; }

    /// <summary>
    /// Generated code-behind.
    /// </summary>
    public string? CodeBehindCode { get; init; }

    /// <summary>
    /// Generated ViewModel code.
    /// </summary>
    public string? ViewModelCode { get; init; }

    /// <summary>
    /// Additional files to generate.
    /// </summary>
    public Dictionary<string, string> AdditionalFiles { get; init; } = [];

    /// <summary>
    /// Whether generation was successful.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Error messages if generation failed.
    /// </summary>
    public List<string> Errors { get; init; } = [];
}

/// <summary>
/// Context for code generation operations.
/// </summary>
public class GenerationContext
{
    public required string Namespace { get; init; }
    public required string ClassName { get; init; }
    public required LayoutAnalysisResult LayoutInfo { get; init; }
    public Dictionary<string, object> Options { get; init; } = [];
    public IServiceProvider? Services { get; init; }
}
