namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="StatementFormat">
/// The translated statement, as a format string where <c>{0}</c> is the control's field name.
/// A full statement rather than just a call, because some WinForms methods become a property
/// assignment in Avalonia (<c>Hide()</c> is <c>IsVisible = false</c>).
/// </param>
public readonly record struct ControlMethod(string StatementFormat);

/// <summary>
/// Zero-argument control methods with an exact Avalonia equivalent - the method-level counterpart
/// of <see cref="BindablePropertyCatalog"/>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately tiny, and only zero-argument methods. Most of what a WinForms handler calls on a
/// control has no Avalonia counterpart at all (<c>errorProvider.SetError</c>,
/// <c>treeView.Nodes.Add</c>), or is not a control method in the first place - a great many are
/// non-visual components (<c>process1.Start()</c>, <c>serialPort1.Open()</c>) that Avalonia has
/// nothing to do with. Those are left for a human; this table is only for the handful whose
/// meaning carries over exactly.
/// </para>
/// <para>
/// Like the property catalog, this only applies to a control that really is the Avalonia element
/// it maps to - a fallback control has to declare the member in
/// <see cref="FallbackControlMemberSupport"/> instead.
/// </para>
/// </remarks>
public static class ControlMethodCatalog
{
    /// <summary>Methods every WinForms <c>Control</c> has, whatever its concrete type.</summary>
    private static readonly IReadOnlyDictionary<string, ControlMethod> UniversalMethods =
        new Dictionary<string, ControlMethod>(StringComparer.Ordinal)
        {
            ["Focus"] = new("{0}.Focus();"),

            // WinForms' Select() moves keyboard focus, which is what Focus() does in Avalonia.
            ["Select"] = new("{0}.Focus();"),

            // Both ask for a repaint; Avalonia spells that InvalidateVisual.
            ["Invalidate"] = new("{0}.InvalidateVisual();"),
            ["Refresh"] = new("{0}.InvalidateVisual();"),

            // Visibility is a property in Avalonia, not a pair of methods.
            ["Hide"] = new("{0}.IsVisible = false;"),
        };

    /// <remarks>Declared before <c>ByControlType</c>: static initializers run in source order.</remarks>
    private static IReadOnlyDictionary<string, ControlMethod> TextBoxMethods { get; } =
        new Dictionary<string, ControlMethod>(StringComparer.Ordinal)
        {
            ["Clear"] = new("{0}.Clear();"),
            ["SelectAll"] = new("{0}.SelectAll();"),
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, ControlMethod>> ByControlType =
        new Dictionary<string, IReadOnlyDictionary<string, ControlMethod>>(StringComparer.Ordinal)
        {
            ["TextBox"] = TextBoxMethods,
            ["MaskedTextBox"] = TextBoxMethods,
            ["RichTextBox"] = TextBoxMethods,
        };

    public static bool TryGet(string winFormsControlTypeName, string methodName, out ControlMethod method)
    {
        if (ByControlType.TryGetValue(winFormsControlTypeName, out var typeMethods)
            && typeMethods.TryGetValue(methodName, out method))
        {
            return true;
        }

        return UniversalMethods.TryGetValue(methodName, out method);
    }
}
