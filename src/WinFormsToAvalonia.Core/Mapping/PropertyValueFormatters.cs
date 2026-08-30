using System.Globalization;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>Small, composable value -> XAML-attribute-string transforms used by <see cref="SimplePropertyMapper"/> property mapping specs.</summary>
public static class PropertyValueFormatters
{
    /// <summary>
    /// WinForms serializes font sizes in points; Avalonia's FontSize is in device-independent
    /// pixels (1/96 inch). The WinForms designer itself lays out at 96 DPI, so the conversion is
    /// the fixed 96/72 ratio - 9pt becomes 12, 8.25pt becomes 11.
    /// </summary>
    private const double PointsToDeviceIndependentPixels = 96.0 / 72.0;

    /// <summary>
    /// The <c>SystemColors.*</c> palette, as explicit ARGB.
    /// </summary>
    /// <remarks>
    /// Deliberately a hand-written table rather than <c>System.Drawing.Color.FromName</c>: for
    /// *system* colors that API resolves against the host's actual desktop palette on Windows
    /// while falling back to these constants elsewhere, so the same input project would emit
    /// different AXAML on different machines - breaking the project's deterministic-output
    /// invariant (and the golden-file test with it). Non-system known colors have no such
    /// problem and do go through <c>Color.FromName</c>.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, string> SystemColorArgb = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ActiveBorder"] = "#FFB4B4B4",
        ["ActiveCaption"] = "#FF99B4D1",
        ["ActiveCaptionText"] = "#FF000000",
        ["AppWorkspace"] = "#FFABABAB",
        ["ButtonFace"] = "#FFF0F0F0",
        ["ButtonHighlight"] = "#FFFFFFFF",
        ["ButtonShadow"] = "#FFA0A0A0",
        ["Control"] = "#FFF0F0F0",
        ["ControlDark"] = "#FFA0A0A0",
        ["ControlDarkDark"] = "#FF696969",
        ["ControlLight"] = "#FFE3E3E3",
        ["ControlLightLight"] = "#FFFFFFFF",
        ["ControlText"] = "#FF000000",
        ["Desktop"] = "#FF000000",
        ["GradientActiveCaption"] = "#FFB9D1EA",
        ["GradientInactiveCaption"] = "#FFD7E4F2",
        ["GrayText"] = "#FF6D6D6D",
        ["Highlight"] = "#FF3399FF",
        ["HighlightText"] = "#FFFFFFFF",
        ["HotTrack"] = "#FF0066CC",
        ["InactiveBorder"] = "#FFF4F7FC",
        ["InactiveCaption"] = "#FFBFCDDB",
        ["InactiveCaptionText"] = "#FF434E54",
        ["Info"] = "#FFFFFFE1",
        ["InfoText"] = "#FF000000",
        ["Menu"] = "#FFF0F0F0",
        ["MenuBar"] = "#FFF0F0F0",
        ["MenuHighlight"] = "#FF3399FF",
        ["MenuText"] = "#FF000000",
        ["ScrollBar"] = "#FFC8C8C8",
        ["Window"] = "#FFFFFFFF",
        ["WindowFrame"] = "#FF646464",
        ["WindowText"] = "#FF000000",
    };

    public static string? AsText(PropertyValue value) => value switch
    {
        PropertyValue.Literal { Value: string s } => s,
        PropertyValue.Literal { Value: { } v } => Convert.ToString(v, CultureInfo.InvariantCulture),
        _ => null,
    };

    public static string? AsBool(PropertyValue value) =>
        value is PropertyValue.Literal { Value: bool b } ? (b ? "True" : "False") : null;

    public static string? AsNumber(PropertyValue value) => value switch
    {
        PropertyValue.Literal { Value: string } => null,
        PropertyValue.Literal { Value: { } v } => Convert.ToString(v, CultureInfo.InvariantCulture),
        _ => null,
    };

    /// <summary>
    /// A WinForms <c>Color</c> as an explicit <c>#AARRGGBB</c> literal. Explicit ARGB rather
    /// than the color's name because Avalonia's named-color vocabulary is not identical to
    /// <c>System.Drawing</c>'s, and an unparseable color string is a XAML error in the
    /// *generated* project - the failure mode this converter works hardest to avoid.
    /// Returns null (emit nothing) for a name neither palette knows.
    /// </summary>
    public static string? AsBrush(PropertyValue value)
    {
        if (value is not PropertyValue.ColorValue color)
        {
            return null;
        }

        if (color is { A: { } a, R: { } r, G: { } g, B: { } b })
        {
            return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
        }

        if (color.NamedColor is not { } name)
        {
            return null;
        }

        if (SystemColorArgb.TryGetValue(name, out var systemArgb))
        {
            return systemArgb;
        }

        // ExpressionEvaluator drops the qualifier, keeping only the member name ("Red" for
        // `Color.Red`, "Control" for `SystemColors.Control`). That is unambiguous because the
        // two palettes share no member names - so a name that isn't in the system table above
        // can only be a plain known color.
        var known = System.Drawing.Color.FromName(name);
        return known.IsKnownColor && !known.IsSystemColor
            ? $"#{known.A:X2}{known.R:X2}{known.G:X2}{known.B:X2}"
            : null;
    }

    public static string? AsFontFamily(PropertyValue value) =>
        value is PropertyValue.FontValue { FamilyName: var family } && !string.IsNullOrWhiteSpace(family)
            ? family
            : null;

    public static string? AsFontSize(PropertyValue value) =>
        value is PropertyValue.FontValue { SizeInPoints: > 0 and var points }
            ? Math.Round(points * PointsToDeviceIndependentPixels, 2).ToString("0.##", CultureInfo.InvariantCulture)
            : null;

    /// <summary>Only emitted when the designer actually asked for Bold - Avalonia's default is Normal.</summary>
    public static string? AsFontWeight(PropertyValue value) =>
        value is PropertyValue.FontValue font && font.StyleFlags.Contains("Bold", StringComparer.Ordinal)
            ? "Bold"
            : null;

    /// <summary>
    /// A WinForms <c>DataPropertyName</c> as the Avalonia binding that displays it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <c>ReflectionBinding</c>, not a <c>Binding</c>, and that is the whole difficulty. The
    /// generated view carries an <c>x:DataType</c> on its root, which makes Avalonia compile every
    /// <c>{Binding}</c> inside it against the ViewModel - and a grid column's path names a member
    /// of the *row* object, which is not the ViewModel and is not carried over at all. A plain
    /// <c>{Binding Name}</c> therefore failed the generated build outright with AVLN2000, caught
    /// by the integration test that runs <c>dotnet build</c> on the output. Reflection binding
    /// says "resolve this at run time", which is exactly the situation: the row type is genuinely
    /// unknown here.
    /// </para>
    /// <para>
    /// Refuses anything that is not a plain identifier. A dotted path (<c>Order.Total</c>) would
    /// in fact be valid Avalonia, but WinForms does not resolve one either - its own binding is
    /// against a single property on the row object - so accepting it would invent behaviour
    /// rather than carry it over.
    /// </para>
    /// </remarks>
    public static string? AsBinding(PropertyValue value) =>
        value is PropertyValue.Literal { Value: string path }
            && path.Length > 0
            && (char.IsLetter(path[0]) || path[0] == '_')
            && path.All(c => char.IsLetterOrDigit(c) || c == '_')
                ? $"{{ReflectionBinding {path}}}"
                : null;

    /// <summary>Only emitted when the designer actually asked for Italic - Avalonia's default is Normal.</summary>
    public static string? AsFontStyle(PropertyValue value) =>
        value is PropertyValue.FontValue font && font.StyleFlags.Contains("Italic", StringComparer.Ordinal)
            ? "Italic"
            : null;

    /// <summary>
    /// WinForms' <c>Underline</c>/<c>Strikeout</c> font styles are not font *weight* or *slant*
    /// in Avalonia but a separate TextDecorations collection, which only text-hosting elements
    /// understand - hence its own formatter rather than a branch of <see cref="AsFontStyle"/>.
    /// </summary>
    public static string? AsTextDecorations(PropertyValue value)
    {
        if (value is not PropertyValue.FontValue font)
        {
            return null;
        }

        if (font.StyleFlags.Contains("Underline", StringComparer.Ordinal))
        {
            return "Underline";
        }

        return font.StyleFlags.Contains("Strikeout", StringComparer.Ordinal) ? "Strikethrough" : null;
    }

    public static string? AsThickness(PropertyValue value) =>
        value is PropertyValue.PaddingValue(var left, var top, var right, var bottom)
            ? string.Create(CultureInfo.InvariantCulture, $"{left},{top},{right},{bottom}")
            : null;

    /// <summary>
    /// A <c>char</c> designer literal as the one-character string an attribute needs.
    /// </summary>
    /// <remarks>
    /// NUL is refused rather than emitted: it is not a legal XML character at all, so an explicit
    /// <c>PromptChar = '\0'</c> would produce a document that cannot be parsed - and escaping
    /// cannot help, because XML 1.0 has no representation for it.
    /// </remarks>
    public static string? AsChar(PropertyValue value) =>
        value is PropertyValue.Literal { Value: char c } && c != '\0'
            ? c.ToString(CultureInfo.InvariantCulture)
            : null;

    /// <summary>
    /// A single enum member, passed through by name - for the pairs whose members Avalonia spells
    /// identically, like WinForms' <c>HorizontalAlignment</c> and Avalonia's <c>TextAlignment</c>.
    /// </summary>
    /// <remarks>
    /// Deliberately does not translate: a name that does not exist on the Avalonia side would be
    /// an AVLN error in the generated project, so this is only ever used where the two enums have
    /// been checked to agree - and <c>ControlMapperTests</c> checks the property itself exists.
    /// </remarks>
    public static string? AsEnumMember(PropertyValue value) =>
        value is PropertyValue.EnumMembers { MemberNames: [var member] } ? member : null;

    /// <summary>
    /// The horizontal third of a WinForms <c>ContentAlignment</c> (<c>MiddleLeft</c> -> <c>Left</c>).
    /// </summary>
    public static string? AsContentAlignmentHorizontal(PropertyValue value) =>
        AsEnumMember(value) switch
        {
            "TopLeft" or "MiddleLeft" or "BottomLeft" => "Left",
            "TopCenter" or "MiddleCenter" or "BottomCenter" => "Center",
            "TopRight" or "MiddleRight" or "BottomRight" => "Right",
            _ => null,
        };

    /// <summary>
    /// The vertical third of a WinForms <c>ContentAlignment</c> (<c>MiddleLeft</c> -> <c>Center</c>).
    /// </summary>
    public static string? AsContentAlignmentVertical(PropertyValue value) =>
        AsEnumMember(value) switch
        {
            "TopLeft" or "TopCenter" or "TopRight" => "Top",
            "MiddleLeft" or "MiddleCenter" or "MiddleRight" => "Center",
            "BottomLeft" or "BottomCenter" or "BottomRight" => "Bottom",
            _ => null,
        };

    /// <summary>
    /// WinForms' <c>BorderStyle</c> as an Avalonia <c>BorderThickness</c>.
    /// </summary>
    /// <remarks>
    /// Avalonia draws one border, not three styles, so <c>Fixed3D</c> and <c>FixedSingle</c> both
    /// become a one-pixel border - the sunken 3D look is a Win32 chrome convention with no
    /// counterpart here. <c>None</c> is the one that carries real information: it turns off the
    /// border the theme would otherwise draw.
    /// </remarks>
    public static string? AsBorderThickness(PropertyValue value) =>
        AsEnumMember(value) switch
        {
            "None" => "0",
            "FixedSingle" or "Fixed3D" => "1",
            _ => null,
        };
}
