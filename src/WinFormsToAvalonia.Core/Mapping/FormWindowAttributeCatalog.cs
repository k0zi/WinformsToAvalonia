using System.Globalization;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The window chrome a Form's designer sets, as the attributes an Avalonia <c>Window</c> needs.
/// </summary>
/// <param name="Attributes">In emission order; every name is one of <see cref="FormWindowAttributeCatalog.AttributeNames"/>.</param>
/// <param name="Warnings">What came across approximately, or not at all.</param>
public sealed record WindowChromeTranslation(
    IReadOnlyList<(string Name, string Value)> Attributes,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Designer-set Form properties that describe the <em>window</em> rather than its content -
/// where it opens, whether it resizes, which caption buttons it has, how small it may get.
/// </summary>
/// <remarks>
/// <para>
/// These were dropped without a word: the root element carried a title and a size and nothing
/// else, so a fixed-size, centred, always-on-top dialog converted into a resizable window opening
/// wherever the platform liked. <see cref="WindowPropertyCatalog"/> already knew four of them -
/// but only for <em>handler</em> bodies, never for the designer's own values.
/// </para>
/// <para>
/// <c>FormBorderStyle</c>/<c>MaximizeBox</c>/<c>MinimizeBox</c> map many-to-many onto
/// <c>CanResize</c>/<c>CanMaximize</c>/<c>CanMinimize</c>/<c>WindowDecorations</c>, which is
/// why the handler side refuses them: an assignment in a body changes one property where Avalonia
/// may need two. The designer side has the whole set at once, so it can be decided as a set.
/// <c>WindowDecorations</c> rather than <c>SystemDecorations</c>, which Avalonia 12 marks
/// <c>[Obsolete]</c> - a warning in a generated project that must build warning-free.
/// </para>
/// <para>
/// Every attribute name and enum value here is checked against Avalonia's reference assemblies by
/// <c>WinFormsToAvalonia.Mapping.Tests</c>.
/// </para>
/// </remarks>
public static class FormWindowAttributeCatalog
{
    /// <summary>Every Window attribute <see cref="Translate"/> can emit.</summary>
    public static IReadOnlyList<string> AttributeNames { get; } =
    [
        "WindowStartupLocation", "Topmost", "ShowInTaskbar", "Opacity", "WindowState",
        "MinWidth", "MinHeight", "MaxWidth", "MaxHeight",
        "CanResize", "CanMinimize", "CanMaximize", "WindowDecorations", "Icon",
    ];

    /// <summary>
    /// <c>FormStartPosition</c> members with an exact <c>WindowStartupLocation</c> counterpart.
    /// <c>CenterParent</c> centres on the owner, which is what Avalonia calls <c>CenterOwner</c>.
    /// </summary>
    public static IReadOnlyDictionary<string, string> StartPositions { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CenterScreen"] = "CenterScreen",
            ["CenterParent"] = "CenterOwner",
        };

    /// <summary>
    /// The Avalonia elements that carry <c>IsDefault</c>/<c>IsCancel</c> - everything deriving from
    /// <c>Button</c> that a mapper emits. A Form's <c>AcceptButton</c>/<c>CancelButton</c> pointing
    /// at anything else (a bundled fallback, say) is reported rather than written onto an element
    /// that has no such property.
    /// </summary>
    public static IReadOnlySet<string> DefaultButtonElements { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "Button", "ToggleButton", "RepeatButton", "HyperlinkButton",
    };

    /// <summary>
    /// Form properties that name one of its buttons, and the property that button gets for it.
    /// Enter clicks the first and Escape the second, in both frameworks.
    /// </summary>
    public static IReadOnlyList<(string FormProperty, string ButtonAttribute)> DialogKeyButtons { get; } =
    [
        ("AcceptButton", "IsDefault"),
        ("CancelButton", "IsCancel"),
    ];

    /// <summary>The <c>FormBorderStyle</c> members that forbid resizing.</summary>
    private static readonly IReadOnlySet<string> FixedBorderStyles = new HashSet<string>(StringComparer.Ordinal)
    {
        "FixedSingle", "Fixed3D", "FixedDialog", "FixedToolWindow",
    };

    /// <summary>The <c>WindowDecorations</c> member a borderless Form becomes.</summary>
    public const string BorderlessDecorations = "None";

    public static WindowChromeTranslation Translate(
        string formName,
        IReadOnlyDictionary<string, PropertyValue> formProperties,
        int? clientWidth,
        int? clientHeight)
    {
        var attributes = new List<(string Name, string Value)>();
        var warnings = new List<string>();

        // Where it opens.
        switch (Enum(formProperties, "StartPosition"))
        {
            case { } position when StartPositions.TryGetValue(position, out var location):
                attributes.Add(("WindowStartupLocation", location));
                break;
            case "Manual" when formProperties.ContainsKey("Location"):
                warnings.Add(
                    $"Form '{formName}' opens at a designer-set Location (StartPosition = Manual). That is not carried "
                    + "over: Avalonia's Window.Position is in physical pixels, so a fixed value would land differently "
                    + "on every display scale. Set Position from code if the placement matters.");
                break;
        }

        // The four properties a handler body could already translate, now from the designer too.
        foreach (var (winFormsName, format) in new (string, Func<PropertyValue, string?>)[]
        {
            ("TopMost", PropertyValueFormatters.AsBool),
            ("ShowInTaskbar", PropertyValueFormatters.AsBool),
            ("Opacity", PropertyValueFormatters.AsNumber),
            ("WindowState", PropertyValueFormatters.AsEnumMember),
        })
        {
            if (formProperties.TryGetValue(winFormsName, out var value)
                && WindowPropertyCatalog.TryGet(winFormsName, out var property)
                && format(value) is { } text
                && (property.EnumMemberNames is null || property.EnumMemberNames.Contains(text)))
            {
                attributes.Add((property.AvaloniaPropertyName, text));
            }
        }

        // Size limits. WinForms measures them on the outer frame and Avalonia on the client area,
        // and the gap is whatever the desktop theme draws - so they are clamped to the designed
        // ClientSize, which is the one size both sides agree on. Without the clamp the usual
        // designer habit (MinimumSize = the form's current outer size) would make the window
        // open larger than it was designed.
        var limitsCarried = false;
        if (formProperties.TryGetValue("MinimumSize", out var minimum) && minimum is PropertyValue.SizeValue min)
        {
            limitsCarried |= AddLimit(attributes, "MinWidth", min.Width, clientWidth, Math.Min);
            limitsCarried |= AddLimit(attributes, "MinHeight", min.Height, clientHeight, Math.Min);
        }

        if (formProperties.TryGetValue("MaximumSize", out var maximum) && maximum is PropertyValue.SizeValue max)
        {
            limitsCarried |= AddLimit(attributes, "MaxWidth", max.Width, clientWidth, Math.Max);
            limitsCarried |= AddLimit(attributes, "MaxHeight", max.Height, clientHeight, Math.Max);
        }

        if (limitsCarried)
        {
            warnings.Add(
                $"Form '{formName}': MinimumSize/MaximumSize measure the outer frame in WinForms and the client area "
                + "in Avalonia. They are carried over as Min/MaxWidth/Height, clamped so they never contradict the "
                + "designed ClientSize - so each limit is off by about the size of the window frame.");
        }

        // The frame. A dictionary because two WinForms properties can ask for the same attribute
        // (FormBorderStyle.None and ControlBox both touch the caption buttons), and a duplicate
        // XML attribute does not merge - it fails to parse.
        var chrome = new Dictionary<string, string>(StringComparer.Ordinal);
        var borderStyle = Enum(formProperties, "FormBorderStyle");
        if (borderStyle == "None")
        {
            chrome["WindowDecorations"] = BorderlessDecorations;
        }
        else if (borderStyle is not null && FixedBorderStyles.Contains(borderStyle))
        {
            chrome["CanResize"] = "False";
        }

        if (borderStyle is "FixedToolWindow" or "SizableToolWindow")
        {
            warnings.Add(
                $"Form '{formName}' is a tool window (FormBorderStyle = {borderStyle}). Avalonia has no tool-window "
                + "frame, so it gets an ordinary one; set ShowInTaskbar=\"False\" if it should stay out of the taskbar.");
        }

        if (IsFalse(formProperties, "MaximizeBox"))
        {
            chrome["CanMaximize"] = "False";
        }

        if (IsFalse(formProperties, "MinimizeBox"))
        {
            chrome["CanMinimize"] = "False";
        }

        if (IsFalse(formProperties, "ControlBox") && borderStyle != "None")
        {
            // WinForms removes every caption button and the icon. Avalonia can disable two of them,
            // but not take the close button away while keeping the title bar.
            chrome["CanMaximize"] = "False";
            chrome["CanMinimize"] = "False";
            warnings.Add(
                $"Form '{formName}' sets ControlBox = false. Its minimize and maximize buttons are disabled, but Avalonia "
                + "cannot remove the close button while keeping the title bar - handle Closing if it must not close.");
        }

        foreach (var name in AttributeNames.Where(chrome.ContainsKey))
        {
            attributes.Add((name, chrome[name]));
        }

        // The icon, once the pipeline has copied it out of the .resx (or Properties.Resources) into
        // Assets/ - a Window takes the asset path as its WindowIcon. Anything else in there is
        // a value it could not recover, which it has already reported.
        if (formProperties.TryGetValue("Icon", out var icon)
            && icon is PropertyValue.Literal { Value: string iconPath }
            && iconPath.StartsWith("/Assets/", StringComparison.Ordinal))
        {
            attributes.Add(("Icon", iconPath));
        }

        // KeyPreview needs no attribute: FormMigrationPlanner carries it by subscribing the Form's own
        // key handlers in the tunnelling phase, which is exactly "before the focused control".

        return new WindowChromeTranslation(attributes, warnings);
    }

    private static bool AddLimit(
        List<(string Name, string Value)> attributes,
        string attributeName,
        int value,
        int? designed,
        Func<int, int, int> clamp)
    {
        // 0 is WinForms' "no limit on this axis".
        if (value <= 0)
        {
            return false;
        }

        var limited = designed is { } size ? clamp(value, size) : value;
        attributes.Add((attributeName, limited.ToString(CultureInfo.InvariantCulture)));
        return true;
    }

    private static string? Enum(IReadOnlyDictionary<string, PropertyValue> properties, string name) =>
        properties.TryGetValue(name, out var value) ? PropertyValueFormatters.AsEnumMember(value) : null;

    private static bool IsFalse(IReadOnlyDictionary<string, PropertyValue> properties, string name) =>
        properties.TryGetValue(name, out var value) && value is PropertyValue.Literal { Value: false };

    private static bool IsTrue(IReadOnlyDictionary<string, PropertyValue> properties, string name) =>
        properties.TryGetValue(name, out var value) && value is PropertyValue.Literal { Value: true };
}
