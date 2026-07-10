using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Services;

/// <summary>
/// Full-orchestrator regression guard for plugin wiring: a real compiled plugin
/// (Converter.Tests.SamplePlugin, copied into a temp plugins directory the same way
/// PluginLoaderIntegrationTests does) maps the unknown control type "AcmeGauge" to
/// ProgressBar, and the generated AXAML must reflect that instead of the default
/// unmapped-control TODO comment. A companion run with no plugins directory configured
/// proves the "zero plugins configured -> zero behavior change" guarantee.
/// </summary>
public class PluginWiringEndToEndTests
{
    private static string CopySamplePluginTo(string pluginsDirectory)
    {
        var sourceDir = Directory.CreateDirectory(Path.Combine(pluginsDirectory, "sample-plugin"));
        var baseDir = AppContext.BaseDirectory;

        // Copies Converter.Plugin.Abstractions.dll alongside the plugin too, matching what a
        // real framework-dependent plugin build produces (see PluginLoaderIntegrationTests for
        // why this matters - it's what exercises PluginLoadContext's dependency resolution).
        foreach (var fileName in new[]
                 {
                     "Converter.Tests.SamplePlugin.dll", "plugin.json", "Converter.Plugin.Abstractions.dll"
                 })
        {
            File.Copy(Path.Combine(baseDir, fileName), Path.Combine(sourceDir.FullName, fileName));
        }

        return sourceDir.FullName;
    }

    private static string DesignerFileWithAcmeGauge(string className) => $$"""
        namespace SampleApp
        {
            partial class {{className}}
            {
                private AcmeGauge gauge1;

                private void InitializeComponent()
                {
                    this.gauge1 = new AcmeGauge();
                    this.SuspendLayout();
                    this.gauge1.Name = "gauge1";
                    this.Controls.Add(this.gauge1);
                    this.Name = "{{className}}";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private static ConverterConfig BaselineConfig() => new()
    {
        GitIntegration = new GitIntegrationConfig { Enabled = false },
        Documentation = new DocumentationConfig { Enabled = false }
    };

    [Fact]
    public async Task ExecuteAsync_WithPluginsDirectory_PluginMappedControlAppearsInGeneratedAxaml()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;
        var pluginsDir = Directory.CreateTempSubdirectory("wf2av-plugins-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "GaugeForm.Designer.cs"), DesignerFileWithAcmeGauge("GaugeForm"));
            CopySamplePluginTo(pluginsDir);

            var orchestrator = new ConversionOrchestrator(
                sourceDir, outputDir, BaselineConfig(), pluginsDirectory: pluginsDir);

            var result = await orchestrator.ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var axamlContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "GaugeForm.axaml"));

            Assert.Contains("<ProgressBar", axamlContent);
            Assert.DoesNotContain("TODO: Unmapped control", axamlContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
            Directory.Delete(pluginsDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithoutPluginsDirectory_UnknownControlStaysUnmapped()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "GaugeForm.Designer.cs"), DesignerFileWithAcmeGauge("GaugeForm"));

            var orchestrator = new ConversionOrchestrator(sourceDir, outputDir, BaselineConfig());

            var result = await orchestrator.ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var axamlContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "GaugeForm.axaml"));

            Assert.DoesNotContain("<ProgressBar", axamlContent);
            Assert.Contains("TODO: Unmapped control: AcmeGauge", axamlContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithNonexistentPluginsDirectory_BehavesLikeNoPlugins()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "GaugeForm.Designer.cs"), DesignerFileWithAcmeGauge("GaugeForm"));

            var missingPluginsDir = Path.Combine(Path.GetTempPath(), "wf2av-no-such-dir-" + Path.GetRandomFileName());
            var orchestrator = new ConversionOrchestrator(
                sourceDir, outputDir, BaselineConfig(), pluginsDirectory: missingPluginsDir);

            var result = await orchestrator.ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var axamlContent = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "Views", "GaugeForm.axaml"));

            Assert.Contains("TODO: Unmapped control: AcmeGauge", axamlContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
