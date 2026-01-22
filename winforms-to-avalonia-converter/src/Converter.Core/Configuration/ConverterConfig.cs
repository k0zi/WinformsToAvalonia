using System.Text.Json.Serialization;

namespace Converter.Core.Configuration;

/// <summary>
/// Root configuration model for the converter.
/// </summary>
public class ConverterConfig
{
    /// <summary>
    /// Custom control mappings.
    /// </summary>
    [JsonPropertyName("customMappings")]
    public List<CustomControlMapping> CustomMappings { get; init; } = [];

    /// <summary>
    /// Third-party library mappings.
    /// </summary>
    [JsonPropertyName("thirdPartyMappings")]
    public List<ThirdPartyLibraryMapping> ThirdPartyMappings { get; init; } = [];

    /// <summary>
    /// Style extraction rules.
    /// </summary>
    [JsonPropertyName("styleExtraction")]
    public StyleExtractionConfig StyleExtraction { get; init; } = new();

    /// <summary>
    /// Layout detection thresholds.
    /// </summary>
    [JsonPropertyName("layoutDetection")]
    public LayoutDetectionConfig LayoutDetection { get; init; } = new();

    /// <summary>
    /// Excluded files and folders.
    /// </summary>
    [JsonPropertyName("excludePatterns")]
    public List<string> ExcludePatterns { get; init; } = [];

    /// <summary>
    /// Naming conventions.
    /// </summary>
    [JsonPropertyName("namingConventions")]
    public NamingConventionsConfig NamingConventions { get; init; } = new();

    /// <summary>
    /// Incremental conversion settings.
    /// </summary>
    [JsonPropertyName("incrementalSettings")]
    public IncrementalConversionConfig IncrementalSettings { get; init; } = new();

    /// <summary>
    /// Parallel processing settings.
    /// </summary>
    [JsonPropertyName("parallelProcessing")]
    public ParallelProcessingConfig ParallelProcessing { get; init; } = new();

    /// <summary>
    /// Git integration settings.
    /// </summary>
    [JsonPropertyName("gitIntegration")]
    public GitIntegrationConfig GitIntegration { get; init; } = new();

    /// <summary>
    /// Documentation generation settings.
    /// </summary>
    [JsonPropertyName("documentation")]
    public DocumentationConfig Documentation { get; init; } = new();

    /// <summary>
    /// Plugin settings.
    /// </summary>
    [JsonPropertyName("plugins")]
    public PluginConfig Plugins { get; init; } = new();

    /// <summary>
    /// Default CLI options.
    /// </summary>
    [JsonPropertyName("defaultOptions")]
    public DefaultOptionsConfig DefaultOptions { get; init; } = new();
}

/// <summary>
/// Custom control mapping configuration.
/// </summary>
public class CustomControlMapping
{
    [JsonPropertyName("winFormsType")]
    public required string WinFormsType { get; init; }

    [JsonPropertyName("avaloniaType")]
    public required string AvaloniaType { get; init; }

    [JsonPropertyName("propertyMappings")]
    public Dictionary<string, string> PropertyMappings { get; init; } = [];

    [JsonPropertyName("eventMappings")]
    public Dictionary<string, string> EventMappings { get; init; } = [];

    [JsonPropertyName("customAxaml")]
    public string? CustomAxaml { get; init; }
}

/// <summary>
/// Third-party library mapping configuration.
/// </summary>
public class ThirdPartyLibraryMapping
{
    [JsonPropertyName("assemblyName")]
    public required string AssemblyName { get; init; }

    [JsonPropertyName("controlNamespace")]
    public required string ControlNamespace { get; init; }

    [JsonPropertyName("placeholderTemplate")]
    public string? PlaceholderTemplate { get; init; }

    [JsonPropertyName("generateStubs")]
    public bool GenerateStubs { get; init; } = true;

    [JsonPropertyName("migrationNotes")]
    public string? MigrationNotes { get; init; }
}

/// <summary>
/// Style extraction configuration.
/// </summary>
public class StyleExtractionConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("minimumOccurrence")]
    public int MinimumOccurrence { get; init; } = 3;

    [JsonPropertyName("propertiesToExtract")]
    public List<string> PropertiesToExtract { get; init; } = 
    [
        "FontFamily", "FontSize", "FontWeight", 
        "Background", "Foreground", "BorderBrush", "BorderThickness"
    ];

    [JsonPropertyName("namingPattern")]
    public string NamingPattern { get; init; } = "{ControlType}{PropertyName}Style";
}

/// <summary>
/// Layout detection configuration.
/// </summary>
public class LayoutDetectionConfig
{
    [JsonPropertyName("alignmentTolerance")]
    public int AlignmentTolerance { get; init; } = 5;

    [JsonPropertyName("confidenceThreshold")]
    public int ConfidenceThreshold { get; init; } = 70;

    [JsonPropertyName("gridDetectionWeight")]
    public double GridDetectionWeight { get; init; } = 1.0;

    [JsonPropertyName("stackDetectionWeight")]
    public double StackDetectionWeight { get; init; } = 1.0;

    [JsonPropertyName("dockDetectionWeight")]
    public double DockDetectionWeight { get; init; } = 1.0;
}

/// <summary>
/// Naming conventions configuration.
/// </summary>
public class NamingConventionsConfig
{
    [JsonPropertyName("namespaceTransformations")]
    public Dictionary<string, string> NamespaceTransformations { get; init; } = [];

    [JsonPropertyName("viewModelSuffix")]
    public string ViewModelSuffix { get; init; } = "ViewModel";

    [JsonPropertyName("viewSuffix")]
    public string ViewSuffix { get; init; } = "";

    [JsonPropertyName("outputDirectoryPattern")]
    public string OutputDirectoryPattern { get; init; } = "{ProjectName}.Avalonia";
}

/// <summary>
/// Incremental conversion configuration.
/// </summary>
public class IncrementalConversionConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("hashAlgorithm")]
    public string HashAlgorithm { get; init; } = "SHA256";

    [JsonPropertyName("checkpointFrequency")]
    public int CheckpointFrequency { get; init; } = 10;

    [JsonPropertyName("cacheFileName")]
    public string CacheFileName { get; init; } = ".converter-cache.json";

    [JsonPropertyName("checkpointFileName")]
    public string CheckpointFileName { get; init; } = ".converter-checkpoint.json";
}

/// <summary>
/// Parallel processing configuration.
/// </summary>
public class ParallelProcessingConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("maxDegreeOfParallelism")]
    public int? MaxDegreeOfParallelism { get; init; }

    [JsonPropertyName("batchSize")]
    public int BatchSize { get; init; } = 5;
}

/// <summary>
/// Git integration configuration.
/// </summary>
public class GitIntegrationConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("createFeatureBranch")]
    public bool CreateFeatureBranch { get; init; } = true;

    [JsonPropertyName("branchNamePattern")]
    public string BranchNamePattern { get; init; } = "feature/avalonia-migration-{timestamp}";

    [JsonPropertyName("autoCommitCheckpoints")]
    public bool AutoCommitCheckpoints { get; init; } = false;

    [JsonPropertyName("generateGitignore")]
    public bool GenerateGitignore { get; init; } = true;

    [JsonPropertyName("gitignoreEntries")]
    public List<string> GitignoreEntries { get; init; } = 
    [
        ".converter-cache.json",
        ".converter-checkpoint.json"
    ];
}

/// <summary>
/// Documentation generation configuration.
/// </summary>
public class DocumentationConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("format")]
    public string Format { get; init; } = "markdown";

    [JsonPropertyName("includeCodeSamples")]
    public bool IncludeCodeSamples { get; init; } = true;

    [JsonPropertyName("outputFileName")]
    public string OutputFileName { get; init; } = "MIGRATION_GUIDE.md";

    [JsonPropertyName("templatePath")]
    public string? TemplatePath { get; init; }
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfig
{
    [JsonPropertyName("pluginsDirectory")]
    public string PluginsDirectory { get; init; } = "plugins";

    [JsonPropertyName("enabledPlugins")]
    public List<string> EnabledPlugins { get; init; } = [];

    [JsonPropertyName("pluginSettings")]
    public Dictionary<string, Dictionary<string, object>> PluginSettings { get; init; } = [];
}

/// <summary>
/// Default CLI options configuration.
/// </summary>
public class DefaultOptionsConfig
{
    [JsonPropertyName("layoutMode")]
    public string LayoutMode { get; init; } = "auto";

    [JsonPropertyName("reportFormat")]
    public string ReportFormat { get; init; } = "html";

    [JsonPropertyName("generateViewModels")]
    public bool GenerateViewModels { get; init; } = true;

    [JsonPropertyName("extractStyles")]
    public bool ExtractStyles { get; init; } = true;
}
