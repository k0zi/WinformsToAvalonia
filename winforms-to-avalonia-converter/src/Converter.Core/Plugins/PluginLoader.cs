using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Converter.Plugin.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Core.Plugins;

/// <summary>
/// Manages plugin discovery, loading, and lifecycle.
/// </summary>
public class PluginLoader
{
    private readonly ILogger<PluginLoader>? _logger;
    private readonly List<LoadedPlugin> _loadedPlugins = [];
    private readonly Dictionary<string, AssemblyLoadContext> _loadContexts = [];

    public IReadOnlyList<LoadedPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    public PluginLoader(ILogger<PluginLoader>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Discover plugins in a directory.
    /// </summary>
    public async Task<List<PluginManifest>> DiscoverPluginsAsync(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            _logger?.LogWarning("Plugins directory does not exist: {Directory}", pluginsDirectory);
            return [];
        }

        var manifests = new List<PluginManifest>();

        // Search for plugin.json manifest files
        var manifestFiles = Directory.GetFiles(pluginsDirectory, "plugin.json", SearchOption.AllDirectories);

        foreach (var manifestPath in manifestFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest != null)
                {
                    manifests.Add(manifest);
                    _logger?.LogInformation("Discovered plugin: {Name} v{Version}", manifest.Name, manifest.Version);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load plugin manifest: {Path}", manifestPath);
            }
        }

        return manifests;
    }

    /// <summary>
    /// Load a plugin from its manifest.
    /// </summary>
    public async Task<LoadedPlugin?> LoadPluginAsync(PluginManifest manifest, string pluginDirectory)
    {
        try
        {
            // Resolve assembly path
            var assemblyPath = Path.Combine(pluginDirectory, manifest.EntryAssembly);
            if (!File.Exists(assemblyPath))
            {
                _logger?.LogError("Plugin assembly not found: {Path}", assemblyPath);
                return null;
            }

            // Create isolated load context
            var loadContext = new PluginLoadContext(assemblyPath);
            _loadContexts[manifest.Id] = loadContext;

            // Load assembly
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            // Find entry type
            var entryType = assembly.GetType(manifest.EntryType);
            if (entryType == null)
            {
                _logger?.LogError("Plugin entry type not found: {Type}", manifest.EntryType);
                return null;
            }

            // Validate it implements IConverterPlugin
            if (!typeof(IConverterPlugin).IsAssignableFrom(entryType))
            {
                _logger?.LogError("Plugin entry type does not implement IConverterPlugin: {Type}", manifest.EntryType);
                return null;
            }

            // Create instance
            var plugin = Activator.CreateInstance(entryType) as IConverterPlugin;
            if (plugin == null)
            {
                _logger?.LogError("Failed to create plugin instance: {Type}", manifest.EntryType);
                return null;
            }

            // Initialize plugin
            await plugin.InitializeAsync(manifest.Configuration);

            var loadedPlugin = new LoadedPlugin
            {
                Manifest = manifest,
                Plugin = plugin,
                Assembly = assembly,
                LoadContext = loadContext
            };

            _loadedPlugins.Add(loadedPlugin);
            _logger?.LogInformation("Loaded plugin: {Name} v{Version}", manifest.Name, manifest.Version);

            return loadedPlugin;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load plugin: {Name}", manifest.Name);
            return null;
        }
    }

    /// <summary>
    /// Load all plugins from a directory.
    /// </summary>
    public async Task<List<LoadedPlugin>> LoadAllPluginsAsync(string pluginsDirectory, List<string>? enabledPlugins = null)
    {
        var manifests = await DiscoverPluginsAsync(pluginsDirectory);
        var loaded = new List<LoadedPlugin>();

        foreach (var manifest in manifests)
        {
            // Skip if not in enabled list
            if (enabledPlugins != null && !enabledPlugins.Contains(manifest.Id))
            {
                _logger?.LogDebug("Skipping disabled plugin: {Id}", manifest.Id);
                continue;
            }

            // Check dependencies
            if (!ValidateDependencies(manifest, manifests))
            {
                _logger?.LogWarning("Plugin dependencies not met: {Name}", manifest.Name);
                continue;
            }

            var pluginDir = Path.GetDirectoryName(
                Directory.GetFiles(pluginsDirectory, "plugin.json", SearchOption.AllDirectories)
                    .FirstOrDefault(f => File.ReadAllText(f).Contains($"\"id\": \"{manifest.Id}\"")) ?? ""
            );

            if (string.IsNullOrEmpty(pluginDir))
            {
                continue;
            }

            var plugin = await LoadPluginAsync(manifest, pluginDir);
            if (plugin != null)
            {
                loaded.Add(plugin);
            }
        }

        return loaded;
    }

    /// <summary>
    /// Validate plugin dependencies.
    /// </summary>
    private bool ValidateDependencies(PluginManifest manifest, List<PluginManifest> availablePlugins)
    {
        foreach (var dependency in manifest.Dependencies)
        {
            if (!availablePlugins.Any(p => p.Id == dependency))
            {
                _logger?.LogWarning("Missing dependency {Dependency} for plugin {Plugin}", dependency, manifest.Name);
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Get all plugins implementing a specific interface.
    /// </summary>
    public IEnumerable<T> GetPlugins<T>() where T : class
    {
        foreach (var loaded in _loadedPlugins)
        {
            if (loaded.Plugin is T typedPlugin)
            {
                yield return typedPlugin;
            }
        }
    }

    /// <summary>
    /// Unload all plugins.
    /// </summary>
    public async Task UnloadAllPluginsAsync()
    {
        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                await plugin.Plugin.CleanupAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cleaning up plugin: {Name}", plugin.Manifest.Name);
            }
        }

        _loadedPlugins.Clear();

        foreach (var context in _loadContexts.Values)
        {
            context.Unload();
        }

        _loadContexts.Clear();
    }
}

/// <summary>
/// Represents a loaded plugin.
/// </summary>
public class LoadedPlugin
{
    public required PluginManifest Manifest { get; init; }
    public required IConverterPlugin Plugin { get; init; }
    public required Assembly Assembly { get; init; }
    public required AssemblyLoadContext LoadContext { get; init; }
}

/// <summary>
/// Isolated assembly load context for plugins.
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
