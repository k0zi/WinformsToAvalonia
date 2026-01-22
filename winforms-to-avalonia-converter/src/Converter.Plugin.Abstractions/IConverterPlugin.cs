namespace Converter.Plugin.Abstractions;

/// <summary>
/// Base interface for all converter plugins.
/// </summary>
public interface IConverterPlugin
{
    /// <summary>
    /// Plugin metadata.
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// Initialize the plugin with the provided configuration.
    /// </summary>
    /// <param name="configuration">Plugin-specific configuration.</param>
    Task InitializeAsync(Dictionary<string, object>? configuration);

    /// <summary>
    /// Configure the plugin with converter services.
    /// </summary>
    /// <param name="services">Service collection for dependency injection.</param>
    void Configure(IServiceProvider services);

    /// <summary>
    /// Cleanup resources when the plugin is unloaded.
    /// </summary>
    Task CleanupAsync();
}
