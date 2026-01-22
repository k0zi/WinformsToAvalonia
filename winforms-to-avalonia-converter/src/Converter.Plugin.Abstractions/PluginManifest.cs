namespace Converter.Plugin.Abstractions;

/// <summary>
/// Plugin manifest containing metadata about a converter plugin.
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// Unique identifier for the plugin.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name of the plugin.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Plugin version following semantic versioning.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Plugin author information.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Plugin description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Entry point assembly name.
    /// </summary>
    public required string EntryAssembly { get; init; }

    /// <summary>
    /// Entry point type name (fully qualified).
    /// </summary>
    public required string EntryType { get; init; }

    /// <summary>
    /// List of plugin dependencies (other plugin IDs).
    /// </summary>
    public List<string> Dependencies { get; init; } = [];

    /// <summary>
    /// Minimum converter version required.
    /// </summary>
    public string? MinConverterVersion { get; init; }

    /// <summary>
    /// Plugin-specific configuration section.
    /// </summary>
    public Dictionary<string, object>? Configuration { get; init; }
}
