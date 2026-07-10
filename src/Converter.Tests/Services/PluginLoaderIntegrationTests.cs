using Converter.Core.Plugins;
using Converter.Plugin.Abstractions;

namespace Converter.Tests.Services;

/// <summary>
/// Exercises PluginLoader against a real compiled plugin assembly (Converter.Tests.SamplePlugin,
/// built as a normal project reference so its output lands next to this test assembly) rather
/// than an in-memory fake - the actual AssemblyLoadContext isolation path. Critically, the
/// fixture copies Converter.Plugin.Abstractions.dll alongside the plugin DLL (matching what a
/// real framework-dependent plugin build produces via ProjectReference or a raw
/// &lt;Reference HintPath&gt; - both copy-local by default), because a plugin shipped WITHOUT
/// a local copy would never exercise PluginLoadContext's dependency resolution at all and
/// wouldn't catch the "shared contracts assembly gets loaded twice into two different
/// AssemblyLoadContexts, producing two distinct IConverterPlugin types that fail
/// IsAssignableFrom" regression PluginLoadContext.Load() now guards against.
/// </summary>
public class PluginLoaderIntegrationTests
{
    private static string CopySamplePluginTo(string pluginsDirectory)
    {
        var sourceDir = Directory.CreateDirectory(Path.Combine(pluginsDirectory, "sample-plugin"));
        var baseDir = AppContext.BaseDirectory;

        foreach (var fileName in new[]
                 {
                     "Converter.Tests.SamplePlugin.dll", "plugin.json", "Converter.Plugin.Abstractions.dll"
                 })
        {
            File.Copy(Path.Combine(baseDir, fileName), Path.Combine(sourceDir.FullName, fileName));
        }

        return sourceDir.FullName;
    }

    [Fact]
    public async Task DiscoverPluginsAsync_FindsManifestInSubdirectory()
    {
        var pluginsDir = Directory.CreateTempSubdirectory("wf2av-plugins-").FullName;
        try
        {
            CopySamplePluginTo(pluginsDir);

            var loader = new PluginLoader();
            var manifests = await loader.DiscoverPluginsAsync(pluginsDir);

            var manifest = Assert.Single(manifests);
            Assert.Equal("sample-plugin", manifest.Id);
            Assert.Equal("Converter.Tests.SamplePlugin.SamplePlugin", manifest.EntryType);
        }
        finally
        {
            Directory.Delete(pluginsDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAllPluginsAsync_LoadsRealAssembly_AndGetPlugins_ReturnsWorkingControlMapper()
    {
        var pluginsDir = Directory.CreateTempSubdirectory("wf2av-plugins-").FullName;
        try
        {
            CopySamplePluginTo(pluginsDir);

            var loader = new PluginLoader();
            var loaded = await loader.LoadAllPluginsAsync(pluginsDir);

            Assert.Single(loaded);
            Assert.Single(loader.LoadedPlugins);

            var controlMappers = loader.GetPlugins<IControlMapper>().ToList();
            var mapper = Assert.Single(controlMappers);

            var acmeGauge = new ControlNode { ControlType = "AcmeGauge", FullTypeName = "AcmeGauge", Name = "gauge1" };
            var unrelated = new ControlNode { ControlType = "Button", FullTypeName = "Button", Name = "button1" };

            Assert.True(mapper.CanMap(acmeGauge));
            Assert.False(mapper.CanMap(unrelated));

            var result = await mapper.MapAsync(
                acmeGauge, new MappingContext { ProjectPath = "unused", OutputPath = "unused" });
            Assert.Equal("ProgressBar", result.AvaloniaControlType);

            await loader.UnloadAllPluginsAsync();
            Assert.Empty(loader.LoadedPlugins);
        }
        finally
        {
            Directory.Delete(pluginsDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAllPluginsAsync_EnabledPluginsListExcludesUnlistedPlugin()
    {
        var pluginsDir = Directory.CreateTempSubdirectory("wf2av-plugins-").FullName;
        try
        {
            CopySamplePluginTo(pluginsDir);

            var loader = new PluginLoader();
            var loaded = await loader.LoadAllPluginsAsync(pluginsDir, enabledPlugins: ["some-other-plugin"]);

            Assert.Empty(loaded);
            Assert.Empty(loader.LoadedPlugins);
        }
        finally
        {
            Directory.Delete(pluginsDir, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverPluginsAsync_NonexistentDirectory_ReturnsEmpty()
    {
        var loader = new PluginLoader();
        var manifests = await loader.DiscoverPluginsAsync(
            Path.Combine(Path.GetTempPath(), "wf2av-does-not-exist-" + Path.GetRandomFileName()));

        Assert.Empty(manifests);
    }
}
