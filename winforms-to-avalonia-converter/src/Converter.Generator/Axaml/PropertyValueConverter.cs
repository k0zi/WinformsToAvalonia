using System.Text.RegularExpressions;
using Converter.Mappings.BuiltIn;

namespace Converter.Generator.Axaml;

/// <summary>
/// Converts raw WinForms designer property values (captured as C# source text by
/// WinFormsParser) into Avalonia attribute name/value pairs, for properties that
/// PropertyMappingRegistry flags as RequiresConversion or RequiresCustomLogic (i.e.
/// anything that isn't a plain 1:1 DirectMapping).
/// </summary>
public static class PropertyValueConverter
{
    /// <summary>
    /// Converts a mapped property's raw value into zero or more Avalonia attributes.
    /// Returns null if this converter doesn't recognize the mapping/value shape (the
    /// caller should skip emitting anything, same as today's behavior). Returns an empty
    /// list if the mapping is recognized but intentionally produces no attribute (e.g.
    /// Dock="Fill", which Avalonia expresses implicitly via DockPanel.LastChildFill).
    /// </summary>
    public static IReadOnlyList<(string AttributeName, string Value)>? Convert(PropertyMapping mapping, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (mapping.RequiresCustomLogic)
        {
            return mapping.AvaloniaProperty switch
            {
                "FontFamily,FontSize,FontWeight" => TryConvertFont(rawValue),
                "Canvas.Left,Canvas.Top" => TryConvertLocation(rawValue),
                "DockPanel.Dock" => TryConvertDock(rawValue),
                _ => null
            };
        }

        if (mapping.RequiresConversion)
        {
            return mapping.ConversionType switch
            {
                "ColorToBrush" => TryConvertColorToBrush(mapping, rawValue),
                _ => null
            };
        }

        return null;
    }

    private static IReadOnlyList<(string, string)>? TryConvertColorToBrush(PropertyMapping mapping, string rawValue)
    {
        var brush = TryConvertColor(rawValue);
        return brush != null ? [(mapping.AvaloniaProperty, brush)] : null;
    }

    private static readonly Regex FromArgbPattern = new(@"Color\.FromArgb\(\s*(?<args>[^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex NamedColorPattern = new(@"Color\.(?<name>[A-Za-z]+)\s*$", RegexOptions.Compiled);

    private static string? TryConvertColor(string rawValue)
    {
        var argbMatch = FromArgbPattern.Match(rawValue);
        if (argbMatch.Success)
        {
            var parts = argbMatch.Groups["args"].Value
                .Split(',', StringSplitOptions.TrimEntries)
                .Select(p => int.TryParse(p, out var n) ? n : (int?)null)
                .ToArray();

            if (parts.Length == 3 && parts.All(p => p.HasValue))
            {
                return $"#{parts[0]!.Value:X2}{parts[1]!.Value:X2}{parts[2]!.Value:X2}";
            }

            if (parts.Length == 4 && parts.All(p => p.HasValue))
            {
                return $"#{parts[0]!.Value:X2}{parts[1]!.Value:X2}{parts[2]!.Value:X2}{parts[3]!.Value:X2}";
            }

            return null;
        }

        var namedMatch = NamedColorPattern.Match(rawValue);
        // System.Drawing named colors (Color.Red, Color.CornflowerBlue, ...) share names
        // with CSS/Avalonia's named color set in the vast majority of cases.
        return namedMatch.Success ? namedMatch.Groups["name"].Value : null;
    }

    private static readonly Regex FontPattern = new(
        @"new\s+(?:System\.Drawing\.)?Font\s*\(\s*""(?<family>[^""]+)""\s*,\s*(?<size>[\d.]+)F?" +
        @"(\s*,\s*(?:System\.Drawing\.)?FontStyle\.(?<style>[A-Za-z\s,]+))?",
        RegexOptions.Compiled);

    private static IReadOnlyList<(string, string)>? TryConvertFont(string rawValue)
    {
        var match = FontPattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        var results = new List<(string, string)>
        {
            ("FontFamily", match.Groups["family"].Value),
            ("FontSize", match.Groups["size"].Value)
        };

        if (match.Groups["style"].Success)
        {
            var styles = match.Groups["style"].Value.Split(',', StringSplitOptions.TrimEntries);
            if (styles.Contains("Bold"))
            {
                results.Add(("FontWeight", "Bold"));
            }
            if (styles.Contains("Italic"))
            {
                results.Add(("FontStyle", "Italic"));
            }
        }

        return results;
    }

    private static readonly Regex PointPattern = new(
        @"new\s+(?:System\.Drawing\.)?Point\s*\(\s*(?<x>-?\d+)\s*,\s*(?<y>-?\d+)\s*\)",
        RegexOptions.Compiled);

    private static IReadOnlyList<(string, string)>? TryConvertLocation(string rawValue)
    {
        var match = PointPattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        return
        [
            ("Canvas.Left", match.Groups["x"].Value),
            ("Canvas.Top", match.Groups["y"].Value)
        ];
    }

    private static readonly Regex DockStylePattern = new(@"DockStyle\.(?<value>[A-Za-z]+)", RegexOptions.Compiled);

    private static IReadOnlyList<(string, string)>? TryConvertDock(string rawValue)
    {
        var match = DockStylePattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value;

        // Avalonia's DockPanel has no "Fill"/"None" dock value - a fill-docked control is
        // simply the last undocked child when DockPanel.LastChildFill is true, so no
        // attribute needs to be emitted for those.
        if (value is "None" or "Fill")
        {
            return [];
        }

        return [("DockPanel.Dock", value)];
    }
}
