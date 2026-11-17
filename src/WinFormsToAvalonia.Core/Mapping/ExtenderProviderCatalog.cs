using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="OwnerClrTypeName">The WinForms provider the call is made on, e.g. <c>ToolTip</c>.</param>
/// <param name="SetterMethodName">Its two-argument setter, e.g. <c>SetToolTip</c>.</param>
/// <param name="PropertyKey">
/// Where the value is parked on the <em>target</em> control's properties, so everything downstream
/// treats it as an ordinary designer-set property.
/// </param>
/// <param name="AvaloniaAttributeName">The attached property it becomes in the AXAML.</param>
public sealed record ExtenderProviderSetter(
    string OwnerClrTypeName,
    string SetterMethodName,
    string PropertyKey,
    string AvaloniaAttributeName,
    Func<PropertyValue, string?> Format);

/// <summary>
/// The WinForms components that set a property on <em>another</em> control instead of having one
/// of their own, and the Avalonia attached property each of those becomes.
/// </summary>
/// <remarks>
/// <para>
/// WinForms' own name for this family is an extender provider (<c>IExtenderProvider</c>):
/// <c>ToolTip</c>, <c>HelpProvider</c>, <c>ErrorProvider</c>. Their designer output is neither a
/// <c>Controls.Add</c> nor a property assignment - it is a plain two-argument method call on a
/// non-visual field, <c>this.toolTip1.SetToolTip(this.button1, "text")</c>, and the value belongs
/// to the argument rather than to the field it was called on.
/// </para>
/// <para>
/// One record answers both halves so they cannot drift: the walker asks
/// <c>(owner type, method) -&gt; property key</c>, the emitter asks
/// <c>property key -&gt; attribute</c>. That is the same two-questions-one-row shape
/// <see cref="BindablePropertyCatalog"/> uses, and for the same reason - the two were previously
/// hardcoded in two files, and a change to one would not have been visible from the other.
/// </para>
/// <para>
/// An ordered list rather than a dictionary, because emission order is attribute order and the
/// golden-file test depends on the output being deterministic.
/// </para>
/// </remarks>
public static class ExtenderProviderCatalog
{
    private static readonly IReadOnlyList<ExtenderProviderSetter> Entries =
    [
        new("ToolTip", "SetToolTip", "ToolTipText", "ToolTip.Tip", PropertyValueFormatters.AsText),

        // WinForms shows this on F1 and Avalonia has no such concept - but the string itself is
        // prose describing the control, which is exactly what AutomationProperties.HelpText is
        // for, and it is the one slot that does not collide with a real SetToolTip on the same
        // control. The keyboard gesture is lost; the text is not.
        new("HelpProvider", "SetHelpString", "HelpString", "AutomationProperties.HelpText", PropertyValueFormatters.AsText),
    ];

    /// <summary>
    /// The providers this converter recognizes at all - including their setters it cannot
    /// translate, so those can be reported by name instead of vanishing.
    /// </summary>
    private static readonly IReadOnlySet<string> ProviderTypeNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "ToolTip",
        "HelpProvider",
        "ErrorProvider",
    };

    public static IReadOnlyList<ExtenderProviderSetter> Setters => Entries;

    public static bool IsProvider(string ownerClrTypeName) => ProviderTypeNames.Contains(ownerClrTypeName);

    public static bool TryGetSetter(string ownerClrTypeName, string methodName, out ExtenderProviderSetter setter)
    {
        setter = Entries.FirstOrDefault(e =>
            e.OwnerClrTypeName == ownerClrTypeName && e.SetterMethodName == methodName)!;

        return setter is not null;
    }
}
