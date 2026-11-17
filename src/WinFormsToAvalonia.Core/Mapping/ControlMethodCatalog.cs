namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="StatementFormat">
/// The translated statement, as a format string where <c>{0}</c> is the <em>resolved access</em>
/// to <see cref="AvaloniaMemberName"/> and <c>{1}</c>... are the call's translated arguments. A
/// full statement rather than just a call, because some WinForms methods become a property
/// assignment in Avalonia (<c>Hide()</c> is <c>IsVisible = false</c>).
/// </param>
/// <remarks>
/// <c>{0}</c> is the resolved access rather than the field name so that the same entry works
/// against both targets: on a View it is <c>logTextBox.Text</c>, on a ViewModel the generated
/// <c>LogTextBoxText</c>. That is only possible for entries whose Avalonia member is a bindable
/// property; <c>Focus()</c> has no ViewModel form and correctly refuses there.
/// </remarks>
/// <param name="AvaloniaMemberName">
/// The Avalonia member the translation actually touches, which is not always the one the WinForms
/// method is named after - <c>AppendText</c> reaches <c>Text</c>. This is what decides whether a
/// <em>fallback</em> control can carry the call at all, so naming the WinForms method here instead
/// would ask <see cref="FallbackControlMemberSupport"/> the wrong question.
/// </param>
/// <param name="ArgumentCount">
/// How many arguments the WinForms overload this entry describes takes. An overload with a
/// different arity is a different method, and is not translated.
/// </param>
public readonly record struct ControlMethod(string StatementFormat, string AvaloniaMemberName, int ArgumentCount = 0);

/// <summary>
/// Control methods with an exact Avalonia equivalent - the method-level counterpart of
/// <see cref="BindablePropertyCatalog"/>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately tiny. Most of what a WinForms handler calls on a control has no Avalonia
/// counterpart at all (<c>errorProvider.SetError</c>, <c>treeView.Nodes.Add</c>), or is not a
/// control method in the first place - a great many are non-visual components
/// (<c>process1.Start()</c>, <c>serialPort1.Open()</c>) that Avalonia has nothing to do with.
/// Those are left for a human; this table is only for the handful whose meaning carries over
/// exactly.
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
            ["Focus"] = new("{0}();", "Focus"),

            // WinForms' Select() moves keyboard focus, which is what Focus() does in Avalonia.
            ["Select"] = new("{0}();", "Focus"),

            // Both ask for a repaint; Avalonia spells that InvalidateVisual.
            ["Invalidate"] = new("{0}();", "InvalidateVisual"),
            ["Refresh"] = new("{0}();", "InvalidateVisual"),

            // Visibility is a property in Avalonia, not a pair of methods.
            ["Hide"] = new("{0} = false;", "IsVisible"),
        };

    /// <remarks>Declared before <c>ByControlType</c>: static initializers run in source order.</remarks>
    private static IReadOnlyDictionary<string, ControlMethod> TextBoxMethods { get; } =
        new Dictionary<string, ControlMethod>(StringComparer.Ordinal)
        {
            ["Clear"] = new("{0}();", "Clear"),
            ["SelectAll"] = new("{0}();", "SelectAll"),

            // Avalonia has no AppendText, but appending to the Text property is exactly what it
            // does to the control's contents. The one thing it does not reproduce is the side
            // effect on the caret - WinForms' AppendText also moves it to the end - so this is an
            // equivalence of content rather than of everything the method did.
            ["AppendText"] = new("{0} += {1};", "Text", ArgumentCount: 1),
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

    /// <summary>
    /// Every entry, as (WinForms type or null for a universal one, WinForms method, translation).
    /// </summary>
    /// <remarks>
    /// Exposed so WinFormsToAvalonia.Mapping.Tests can check each <see cref="ControlMethod.AvaloniaMemberName"/>
    /// against Avalonia's real API - this converter never references Avalonia, so nothing else
    /// here can tell whether the member exists.
    /// </remarks>
    public static IEnumerable<(string? WinFormsTypeName, string MethodName, ControlMethod Method)> AllEntries =>
        UniversalMethods.Select(e => ((string?)null, e.Key, e.Value))
            .Concat(ByControlType.SelectMany(t => t.Value.Select(e => ((string?)t.Key, e.Key, e.Value))));
}
