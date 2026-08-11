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
                "Width,Height" => TryConvertSize(rawValue),
                "DockPanel.Dock" => TryConvertDock(rawValue),
                "Padding" or "Margin" => TryConvertThickness(mapping, rawValue),
                "CanResize" => TryConvertFormBorderStyle(rawValue),
                "WindowState" => TryConvertWindowState(mapping, rawValue),
                "MinWidth,MinHeight" => TryConvertMinMaxSize(rawValue, "MinWidth", "MinHeight"),
                "MaxWidth,MaxHeight" => TryConvertMinMaxSize(rawValue, "MaxWidth", "MaxHeight"),
                "HorizontalContentAlignment,VerticalContentAlignment" => TryConvertContentAlignment(rawValue),
                "BorderBrush,BorderThickness" => TryConvertControlBorderStyle(rawValue),
                "HorizontalAlignment,VerticalAlignment" => TryConvertAutoSize(rawValue),
                "TextAlignment,VerticalAlignment" => TryConvertTextBlockTextAlign(rawValue),
                "TextAlignment" => TryConvertTextBoxTextAlign(rawValue),
                "SelectedDate" => TryConvertDateTimePickerValue(rawValue),
                _ => null
            };
        }

        if (mapping.RequiresConversion)
        {
            return mapping.ConversionType switch
            {
                "ColorToBrush" => TryConvertColorToBrush(mapping, rawValue),
                "ImageToBitmap" => TryConvertImagePath(mapping, rawValue),
                "FormStartPosition" => TryConvertStartPosition(rawValue),
                _ => null
            };
        }

        return null;
    }

    /// <summary>
    /// By the time this runs, rawValue is already a relative "Assets/..." path (rewritten by
    /// ConversionOrchestrator.ExtractResxAssetsAsync from the resolved resx entry), not a raw
    /// C# expression - AxamlGenerator further qualifies it into a full avares:// URI, since it
    /// has the target namespace in scope and this converter deliberately stays free of
    /// orchestration-level context, consistent with its pure-function design.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertImagePath(PropertyMapping mapping, string rawValue)
    {
        return rawValue.StartsWith("Assets/", StringComparison.Ordinal)
            ? [(mapping.AvaloniaProperty, rawValue)]
            : null;
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

    private static readonly Regex SizePattern = new(
        @"new\s+(?:System\.Drawing\.)?Size\s*\(\s*(?<width>-?\d+)\s*,\s*(?<height>-?\d+)\s*\)",
        RegexOptions.Compiled);

    private static IReadOnlyList<(string, string)>? TryConvertSize(string rawValue)
    {
        var match = SizePattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        return
        [
            ("Width", match.Groups["width"].Value),
            ("Height", match.Groups["height"].Value)
        ];
    }

    /// <summary>
    /// MinimumSize/MaximumSize are both "new Size(w, h)", exactly like Size itself - only the
    /// target attribute names differ (MinWidth/MinHeight vs MaxWidth/MaxHeight), so this reuses
    /// SizePattern rather than duplicating it.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertMinMaxSize(
        string rawValue, string widthAttribute, string heightAttribute)
    {
        var match = SizePattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        return
        [
            (widthAttribute, match.Groups["width"].Value),
            (heightAttribute, match.Groups["height"].Value)
        ];
    }

    private static readonly Regex ContentAlignmentPattern = new(@"ContentAlignment\.(?<value>[A-Za-z]+)", RegexOptions.Compiled);

    /// <summary>
    /// WinForms ContentAlignment is a single 9-value enum (Top/Middle/Bottom x Left/Center/
    /// Right combined into one name, e.g. "MiddleCenter") - Avalonia splits horizontal/vertical
    /// content alignment into two separate properties.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertContentAlignment(string rawValue)
    {
        var match = ContentAlignmentPattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        var (horizontal, vertical) = match.Groups["value"].Value switch
        {
            "TopLeft" => ("Left", "Top"),
            "TopCenter" => ("Center", "Top"),
            "TopRight" => ("Right", "Top"),
            "MiddleLeft" => ("Left", "Center"),
            "MiddleCenter" => ("Center", "Center"),
            "MiddleRight" => ("Right", "Center"),
            "BottomLeft" => ("Left", "Bottom"),
            "BottomCenter" => ("Center", "Bottom"),
            "BottomRight" => ("Right", "Bottom"),
            _ => (null, null)
        };

        if (horizontal == null)
        {
            return null;
        }

        return
        [
            ("HorizontalContentAlignment", horizontal),
            ("VerticalContentAlignment", vertical!)
        ];
    }

    /// <summary>
    /// Label/ToolStripLabel's TextAlign, targeting Avalonia TextBlock - same source enum shape
    /// as TryConvertContentAlignment (reuses its regex/value table), but TextBlock has no
    /// HorizontalContentAlignment/VerticalContentAlignment at all (not a ContentControl), so
    /// the horizontal component maps to TextAlignment (text alignment within the block) and
    /// the vertical component to the inherited Layoutable VerticalAlignment (positions the
    /// block itself within its parent - the closest approximation available, since TextBlock
    /// has no native vertical-text-alignment concept).
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertTextBlockTextAlign(string rawValue)
    {
        var match = ContentAlignmentPattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        var (horizontal, vertical) = match.Groups["value"].Value switch
        {
            "TopLeft" => ("Left", "Top"),
            "TopCenter" => ("Center", "Top"),
            "TopRight" => ("Right", "Top"),
            "MiddleLeft" => ("Left", "Center"),
            "MiddleCenter" => ("Center", "Center"),
            "MiddleRight" => ("Right", "Center"),
            "BottomLeft" => ("Left", "Bottom"),
            "BottomCenter" => ("Center", "Bottom"),
            "BottomRight" => ("Right", "Bottom"),
            _ => (null, null)
        };

        if (horizontal == null)
        {
            return null;
        }

        return
        [
            ("TextAlignment", horizontal),
            ("VerticalAlignment", vertical!)
        ];
    }

    private static readonly Regex HorizontalAlignmentPattern = new(@"HorizontalAlignment\.(?<value>[A-Za-z]+)", RegexOptions.Compiled);

    /// <summary>
    /// TextBox.TextAlign uses a different WinForms enum from Label's TextAlign -
    /// System.Windows.Forms.HorizontalAlignment (Left/Right/Center only, no vertical
    /// component) - so it needs its own regex/value table, distinct from
    /// TryConvertTextBlockTextAlign's ContentAlignment-based one despite the shared WinForms
    /// property name. Avalonia TextBox's own TextAlignment property uses the same member names.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertTextBoxTextAlign(string rawValue)
    {
        var match = HorizontalAlignmentPattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value switch
        {
            "Left" => "Left",
            "Right" => "Right",
            "Center" => "Center",
            _ => null
        };

        return value == null ? null : [("TextAlignment", value)];
    }

    private static readonly Regex DateTimeConstructorPattern =
        new(@"new\s+System\.DateTime\s*\(\s*(?<y>\d+)\s*,\s*(?<m>\d+)\s*,\s*(?<d>\d+)\s*\)", RegexOptions.Compiled);

    /// <summary>
    /// DateTimePicker.Value's Designer-time literal is almost always the 3-arg date-only
    /// "new System.DateTime(y, m, d)" constructor - a hardcoded default date is uncommon, but
    /// when present this is the shape it takes. Emits a plain ISO "yyyy-MM-dd" string, which
    /// Avalonia's DateTimeOffset XAML converter (backing DatePicker.SelectedDate) accepts.
    /// Any other shape (DateTime.Now, DateTime.Today, the 6-arg date+time constructor, ...) is
    /// not guessed at - returns null, same as every other converter in this file falling back
    /// to null on an unrecognized shape.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertDateTimePickerValue(string rawValue)
    {
        var match = DateTimeConstructorPattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["y"].Value, out var year) ||
            !int.TryParse(match.Groups["m"].Value, out var month) ||
            !int.TryParse(match.Groups["d"].Value, out var day))
        {
            return null;
        }

        try
        {
            var date = new DateTime(year, month, day);
            return [("SelectedDate", date.ToString("yyyy-MM-dd"))];
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static readonly Regex ControlBorderStylePattern = new(@"BorderStyle\.(?<value>[A-Za-z0-9]+)", RegexOptions.Compiled);

    /// <summary>
    /// A plain control's System.Windows.Forms.BorderStyle (None/FixedSingle/Fixed3D) - distinct
    /// from FormBorderStyle (see TryConvertFormBorderStyle), which has its own richer enum and
    /// already-registered converter under a different PropertyMappingRegistry key ("CanResize"),
    /// so Convert's switch already routes each to the right method - no ambiguity to resolve
    /// here despite both raw values containing the substring "BorderStyle.". No BorderBrush is
    /// emitted - there's no color to infer from the WinForms value, and AxamlGenerator
    /// tolerates a partial attribute set.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertControlBorderStyle(string rawValue)
    {
        var match = ControlBorderStylePattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups["value"].Value switch
        {
            "None" => [("BorderThickness", "0")],
            "FixedSingle" or "Fixed3D" => [("BorderThickness", "1")],
            _ => null
        };
    }

    /// <summary>
    /// WinForms AutoSize is about content-based sizing, not alignment - despite
    /// PropertyMappingRegistry's AutoSize entry targeting "HorizontalAlignment,
    /// VerticalAlignment" (a pre-existing, slightly-mismatched mapping target this method just
    /// works with rather than renaming). AutoSize=true needs no explicit attribute at all in
    /// the common case: an Avalonia control with no Width/Height set already sizes to its
    /// content, matching WinForms' behavior - recognized-but-no-attribute, same precedent as
    /// Dock="Fill". AutoSize=false has no single clean equivalent here (disabling auto-size
    /// needs explicit dimensions this property alone doesn't carry) - falls through to null so
    /// the "Custom Property Logic" manual step still fires for that case.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertAutoSize(string rawValue)
    {
        return rawValue.Trim() switch
        {
            "true" => [],
            _ => null
        };
    }

    // WinForms Padding's 4-arg constructor is (left, top, right, bottom) - the exact order
    // Avalonia's Thickness string form ("left,top,right,bottom") expects, so no reordering is
    // needed. The 1-arg constructor sets all four sides equally.
    private static readonly Regex ThicknessAllPattern = new(
        @"new\s+(?:System\.Windows\.Forms\.)?Padding\s*\(\s*(?<all>-?\d+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex ThicknessLtrbPattern = new(
        @"new\s+(?:System\.Windows\.Forms\.)?Padding\s*\(\s*(?<left>-?\d+)\s*,\s*(?<top>-?\d+)\s*,\s*" +
        @"(?<right>-?\d+)\s*,\s*(?<bottom>-?\d+)\s*\)", RegexOptions.Compiled);

    private static IReadOnlyList<(string, string)>? TryConvertThickness(PropertyMapping mapping, string rawValue)
    {
        var ltrbMatch = ThicknessLtrbPattern.Match(rawValue);
        if (ltrbMatch.Success)
        {
            var thickness = $"{ltrbMatch.Groups["left"].Value},{ltrbMatch.Groups["top"].Value}," +
                $"{ltrbMatch.Groups["right"].Value},{ltrbMatch.Groups["bottom"].Value}";
            return [(mapping.AvaloniaProperty, thickness)];
        }

        var allMatch = ThicknessAllPattern.Match(rawValue);
        return allMatch.Success ? [(mapping.AvaloniaProperty, allMatch.Groups["all"].Value)] : null;
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

    private static readonly Regex FormBorderStylePattern = new(@"FormBorderStyle\.(?<value>[A-Za-z0-9]+)", RegexOptions.Compiled);

    /// <summary>
    /// WinForms FormBorderStyle controls both resizability and (for None) whether the window
    /// chrome is drawn at all - Avalonia splits those into CanResize and SystemDecorations.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertFormBorderStyle(string rawValue)
    {
        var match = FormBorderStylePattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups["value"].Value switch
        {
            "FixedSingle" or "FixedDialog" or "Fixed3D" or "FixedToolWindow" => [("CanResize", "False")],
            "Sizable" or "SizableToolWindow" => [("CanResize", "True")],
            "None" => [("CanResize", "False"), ("SystemDecorations", "None")],
            _ => null
        };
    }

    private static readonly Regex FormWindowStatePattern = new(@"FormWindowState\.(?<value>[A-Za-z0-9]+)", RegexOptions.Compiled);

    // WinForms FormWindowState (Normal/Minimized/Maximized) maps 1:1 by name onto Avalonia's
    // WindowState enum (which also has FullScreen, unused by WinForms).
    private static IReadOnlyList<(string, string)>? TryConvertWindowState(PropertyMapping mapping, string rawValue)
    {
        var match = FormWindowStatePattern.Match(rawValue);
        return match.Success ? [(mapping.AvaloniaProperty, match.Groups["value"].Value)] : null;
    }

    private static readonly Regex FormStartPositionPattern = new(@"FormStartPosition\.(?<value>[A-Za-z0-9]+)", RegexOptions.Compiled);

    /// <summary>
    /// CenterParent maps to Avalonia's CenterOwner (only meaningful once an Owner is actually
    /// set before showing the window - same caveat WinForms itself has). Manual/
    /// WindowsDefaultLocation/WindowsDefaultBounds all fall back to Avalonia's own default
    /// (Manual), so no attribute needs to be emitted for those.
    /// </summary>
    private static IReadOnlyList<(string, string)>? TryConvertStartPosition(string rawValue)
    {
        var match = FormStartPositionPattern.Match(rawValue);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups["value"].Value switch
        {
            "CenterScreen" => [("WindowStartupLocation", "CenterScreen")],
            "CenterParent" => [("WindowStartupLocation", "CenterOwner")],
            "Manual" or "WindowsDefaultLocation" or "WindowsDefaultBounds" => [],
            _ => null
        };
    }
}
