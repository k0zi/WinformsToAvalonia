using System.Globalization;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Turns a .resx entry into the same <see cref="PropertyValue"/> shapes
/// <see cref="ExpressionEvaluator"/> produces from designer C#.
/// </summary>
/// <remarks>
/// Deliberately the *only* thing that is resx-specific about the conversion: once an entry has
/// become a <c>PropertyValue</c> it lands in the ordinary <see cref="ControlModel.Properties"/>
/// dictionary, and every stage downstream - mappers, AxamlEmitter, the visual-style pass -
/// cannot tell whether the value came from `this.button1.Text = "OK";` or from
/// `resources.ApplyResources(this.button1, "button1")`.
///
/// The declared CLR type is what drives the conversion, since the designer always writes one
/// for a non-string value. An entry whose type this class does not understand yields null and
/// is skipped: a wrong value in the generated AXAML is worse than a missing one.
/// </remarks>
public sealed class ResxPropertyProvider
{
    public static PropertyValue? Convert(ResxEntry entry)
    {
        // A base64 payload is an image/icon/serialized object - not a value that can become a
        // XAML attribute. ConversionPipeline handles those separately, as copied assets.
        if (entry.IsBinary)
        {
            return null;
        }

        var value = entry.Value;

        return entry.TypeSimpleName switch
        {
            null => new PropertyValue.Literal(value),
            "String" => new PropertyValue.Literal(value),
            "Point" => TryParseInts(value, 2, out var point) ? new PropertyValue.PointValue(point[0], point[1]) : null,
            "Size" => TryParseInts(value, 2, out var size) ? new PropertyValue.SizeValue(size[0], size[1]) : null,
            "Padding" => ConvertPadding(value),
            "Font" => ConvertFont(value),
            "Color" => ConvertColor(value),
            "Boolean" => bool.TryParse(value, out var flag) ? new PropertyValue.Literal(flag) : null,
            "Int32" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                ? new PropertyValue.Literal(i)
                : null,
            "Single" or "Double" or "Decimal" =>
                double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                    ? new PropertyValue.Literal(d)
                    : null,
            _ => ConvertEnumMembers(value),
        };
    }

    /// <summary>WinForms writes a Padding as "all" or "left, top, right, bottom".</summary>
    private static PropertyValue? ConvertPadding(string value)
    {
        if (TryParseInts(value, 4, out var four))
        {
            return new PropertyValue.PaddingValue(four[0], four[1], four[2], four[3]);
        }

        return TryParseInts(value, 1, out var uniform)
            ? new PropertyValue.PaddingValue(uniform[0], uniform[0], uniform[0], uniform[0])
            : null;
    }

    /// <summary>
    /// WinForms writes a Font as "Segoe UI, 9.75pt" or "Segoe UI, 9.75pt, style=Bold, Italic".
    /// </summary>
    private static PropertyValue? ConvertFont(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var size = parts[1].EndsWith("pt", StringComparison.OrdinalIgnoreCase) ? parts[1][..^2] : parts[1];
        if (!float.TryParse(size, NumberStyles.Float, CultureInfo.InvariantCulture, out var sizeInPoints))
        {
            return null;
        }

        // "style=Bold" opens the style list; every later part is a further flag ("Italic").
        var styleFlags = new List<string>();
        var inStyleList = false;
        foreach (var part in parts.Skip(2))
        {
            if (part.StartsWith("style=", StringComparison.OrdinalIgnoreCase))
            {
                inStyleList = true;
                styleFlags.Add(part["style=".Length..].Trim());
            }
            else if (inStyleList)
            {
                styleFlags.Add(part);
            }
        }

        return new PropertyValue.FontValue(parts[0], sizeInPoints, styleFlags);
    }

    /// <summary>A resx Color is either a known name ("Red", "Control") or "R, G, B".</summary>
    private static PropertyValue? ConvertColor(string value)
    {
        if (TryParseInts(value, 3, out var rgb)
            && rgb.All(c => c is >= 0 and <= 255))
        {
            return new PropertyValue.ColorValue(null, 255, (byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
        }

        return value.Length > 0 && value.All(char.IsLetter)
            ? new PropertyValue.ColorValue(value, null, null, null, null)
            : null;
    }

    /// <summary>
    /// The catch-all for the enum-typed entries (`Anchor`, `Dock`, `TextAlign`, ...) that make up
    /// most of the remaining types. Anything that is not a plain identifier list is skipped
    /// rather than guessed at.
    /// </summary>
    private static PropertyValue? ConvertEnumMembers(string value)
    {
        var members = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return members.Length > 0 && members.All(m => m.All(c => char.IsLetterOrDigit(c) || c == '_'))
            ? new PropertyValue.EnumMembers(members)
            : null;
    }

    private static bool TryParseInts(string value, int expectedCount, out int[] parsed)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        parsed = [];

        if (parts.Length != expectedCount)
        {
            return false;
        }

        var result = new int[expectedCount];
        for (var i = 0; i < expectedCount; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]))
            {
                return false;
            }
        }

        parsed = result;
        return true;
    }
}
