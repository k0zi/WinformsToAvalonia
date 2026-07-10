using Converter.Plugin.Abstractions;

namespace Converter.Tests.SamplePlugin;

/// <summary>
/// Minimal real plugin used by PluginLoader/MappingResolver integration tests to exercise
/// the actual AssemblyLoadContext isolation path against a real compiled assembly, rather
/// than an in-memory fake. Implements IControlMapper directly on the IConverterPlugin entry
/// type - PluginLoader.GetPlugins&lt;T&gt;() only tests the single instantiated entryType
/// instance for "is T", it does not separately scan the assembly for other classes, so a
/// plugin's extension-point interfaces (IControlMapper/IPropertyTranslator/IEventMapper/...)
/// must live on the same class as IConverterPlugin, not on a sibling class.
/// </summary>
public class SamplePlugin : IConverterPlugin, IControlMapper
{
    public PluginManifest Manifest { get; } = new PluginManifest
    {
        Id = "sample-plugin",
        Name = "Sample Plugin",
        Version = "1.0.0",
        EntryAssembly = "Converter.Tests.SamplePlugin.dll",
        EntryType = "Converter.Tests.SamplePlugin.SamplePlugin"
    };

    public Task InitializeAsync(Dictionary<string, object>? configuration) => Task.CompletedTask;

    public void Configure(IServiceProvider services) { }

    public Task CleanupAsync() => Task.CompletedTask;

    /// <summary>Maps the unknown WinForms control type "AcmeGauge" (not in ControlMappingRegistry) to Avalonia's ProgressBar.</summary>
    public int Priority => 0;

    public bool CanMap(ControlNode winFormsControl) => winFormsControl.ControlType == "AcmeGauge";

    public Task<ControlMappingResult> MapAsync(ControlNode winFormsControl, MappingContext context)
    {
        return Task.FromResult(new ControlMappingResult
        {
            AvaloniaControlType = "ProgressBar"
        });
    }
}
