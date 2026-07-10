using Spectre.Console;
using System.Text.Json;

namespace Converter.Cli.UI;

/// <summary>
/// Defines the color scheme and icons for the converter CLI
/// </summary>
public class ConverterTheme
{
    public Color Success { get; init; }
    public Color Warning { get; init; }
    public Color Error { get; init; }
    public Color Info { get; init; }
    public Color Debug { get; init; }
    public Color Primary { get; init; }
    public Color Secondary { get; init; }

    public string SuccessIcon { get; set; } = "✓";
    public string ErrorIcon { get; set; } = "✗";
    public string WarningIcon { get; set; } = "⚠";
    public string InfoIcon { get; set; } = "ℹ";
    public string DebugIcon { get; set; } = "🔍";
    public string PauseIcon { get; set; } = "⏸️";
    public string ReportIcon { get; set; } = "📊";
    public string RollbackIcon { get; set; } = "🔄";

    public static ConverterTheme Default => new()
    {
        Success = Color.Green,
        Warning = Color.Yellow,
        Error = Color.Red,
        Info = Color.Blue,
        Debug = Color.Grey,
        Primary = Color.Cyan,
        Secondary = Color.Grey
    };

    /// <summary>
    /// Loads a theme from a JSON configuration file
    /// </summary>
    /// <param name="path">Path to the theme configuration file</param>
    /// <returns>Loaded theme</returns>
    /// <exception cref="InvalidOperationException">Thrown if theme file is invalid</exception>
    public static ConverterTheme LoadFromConfig(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Theme file not found: {path}");
        }

        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("colors", out var colors))
            {
                throw new InvalidOperationException("Theme file must contain 'colors' section");
            }

            var theme = new ConverterTheme
            {
                Success = ParseColor(colors, "success"),
                Warning = ParseColor(colors, "warning"),
                Error = ParseColor(colors, "error"),
                Info = ParseColor(colors, "info"),
                Debug = ParseColor(colors, "debug"),
                Primary = ParseColor(colors, "primary"),
                Secondary = ParseColor(colors, "secondary")
            };

            // Load icons if present (optional)
            if (root.TryGetProperty("icons", out var icons))
            {
                theme.SuccessIcon = GetStringProperty(icons, "success", "✓");
                theme.ErrorIcon = GetStringProperty(icons, "error", "✗");
                theme.WarningIcon = GetStringProperty(icons, "warning", "⚠");
                theme.InfoIcon = GetStringProperty(icons, "info", "ℹ");
                theme.DebugIcon = GetStringProperty(icons, "debug", "🔍");
                theme.PauseIcon = GetStringProperty(icons, "pause", "⏸️");
                theme.ReportIcon = GetStringProperty(icons, "report", "📊");
                theme.RollbackIcon = GetStringProperty(icons, "rollback", "🔄");
            }

            return theme;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in theme file: {ex.Message}", ex);
        }
    }

    private static Color ParseColor(JsonElement colors, string name)
    {
        if (!colors.TryGetProperty(name, out var colorElement))
        {
            throw new InvalidOperationException($"Missing required color property: {name}");
        }

        var colorName = colorElement.GetString();
        if (string.IsNullOrWhiteSpace(colorName))
        {
            throw new InvalidOperationException($"Color property '{name}' cannot be empty");
        }

        try
        {
            // Try to parse as a named color
            return Color.FromConsoleColor(Enum.Parse<ConsoleColor>(colorName, ignoreCase: true));
        }
        catch
        {
            // Try standard Spectre color names
            var normalizedName = colorName.ToLowerInvariant();
            return normalizedName switch
            {
                "green" => Color.Green,
                "yellow" => Color.Yellow,
                "red" => Color.Red,
                "blue" => Color.Blue,
                "cyan" => Color.Cyan,
                "magenta" => Color.Magenta,
                "white" => Color.White,
                "black" => Color.Black,
                "grey" or "gray" => Color.Grey,
                "darkgrey" or "darkgray" => Color.Grey,
                "darkgreen" => Color.DarkGreen,
                "darkyellow" => Color.Olive,
                "darkred" => Color.Maroon,
                "darkblue" => Color.Navy,
                "darkcyan" => Color.Teal,
                "darkmagenta" => Color.Purple,
                _ => throw new InvalidOperationException(
                    $"Invalid color '{colorName}' for property '{name}'. Use color names like 'green', 'red', 'blue', 'cyan', 'yellow', 'grey', etc.")
            };
        }
    }

    private static string GetStringProperty(JsonElement element, string name, string defaultValue)
    {
        if (element.TryGetProperty(name, out var prop))
        {
            return prop.GetString() ?? defaultValue;
        }
        return defaultValue;
    }
}
