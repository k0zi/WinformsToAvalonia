using System.Text.Json;

namespace Converter.Core.Configuration;

/// <summary>
/// Service for loading and managing converter configuration.
/// </summary>
public class ConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>
    /// Load configuration from a file.
    /// </summary>
    public static async Task<ConverterConfig> LoadAsync(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configFilePath}");
        }

        try
        {
            var json = await File.ReadAllTextAsync(configFilePath);
            var config = JsonSerializer.Deserialize<ConverterConfig>(json, JsonOptions);
            
            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration file.");
            }

            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Load configuration from a file or return default if not found.
    /// </summary>
    public static async Task<ConverterConfig> LoadOrDefaultAsync(string? configFilePath)
    {
        if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
        {
            return new ConverterConfig();
        }

        return await LoadAsync(configFilePath);
    }

    /// <summary>
    /// Save configuration to a file.
    /// </summary>
    public static async Task SaveAsync(string configFilePath, ConverterConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(configFilePath, json);
    }

    /// <summary>
    /// Generate a default configuration template with comments.
    /// </summary>
    public static async Task GenerateTemplateAsync(string outputPath)
    {
        var defaultConfig = new ConverterConfig
        {
            CustomMappings =
            [
                new CustomControlMapping
                {
                    WinFormsType = "MyApp.CustomControl",
                    AvaloniaType = "MyApp.Avalonia.CustomControl",
                    PropertyMappings = new Dictionary<string, string>
                    {
                        ["CustomProperty"] = "AvaloniaProperty"
                    },
                    EventMappings = new Dictionary<string, string>
                    {
                        ["CustomEvent"] = "AvaloniaEvent"
                    }
                }
            ],
            ThirdPartyMappings =
            [
                new ThirdPartyLibraryMapping
                {
                    AssemblyName = "DevExpress.XtraEditors",
                    ControlNamespace = "DevExpress.XtraEditors",
                    GenerateStubs = true,
                    MigrationNotes = "DevExpress controls need manual migration to Avalonia alternatives"
                }
            ]
        };

        await SaveAsync(outputPath, defaultConfig);
    }

    /// <summary>
    /// Search for configuration file in current and parent directories.
    /// </summary>
    public static string? FindConfigurationFile(string startDirectory, string configFileName = ".converterconfig")
    {
        var currentDir = new DirectoryInfo(startDirectory);

        while (currentDir != null)
        {
            var configPath = Path.Combine(currentDir.FullName, configFileName);
            if (File.Exists(configPath))
            {
                return configPath;
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }
}
