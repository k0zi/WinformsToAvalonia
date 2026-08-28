namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The WinForms types that are pure plumbing around another control, and which constructor
/// argument names it.
/// </summary>
/// <remarks>
/// <para>
/// <c>ToolStripControlHost</c> exists so that an ordinary <c>Control</c> can sit in a
/// <c>ToolStrip</c>. It has no appearance of its own and, crucially, <b>no parameterless
/// constructor</b> - so <c>new ToolStripControlHost(this.hostedTrackBar)</c> is the only shape a
/// designer can emit, and the hosted control is always named right there. The registry's old
/// guidance called it "too dynamic to translate generically"; that was wrong, and the cost of
/// believing it was that the hosted control vanished from the conversion without a trace.
/// </para>
/// <para>
/// A table rather than a check for one type name, for the usual two reasons: a project's own host
/// subclass is one line, and WinFormsToAvalonia.Mapping.Tests can hold every row up against the
/// real WinForms API - which is where the "no parameterless constructor" claim is verified rather
/// than asserted.
/// </para>
/// <para>
/// Deliberately excludes <c>ToolStripComboBox</c>, <c>ToolStripTextBox</c> and
/// <c>ToolStripProgressBar</c>. They derive from <c>ToolStripControlHost</c>, but they have
/// parameterless constructors, the designer never passes them a control, and they are already
/// mapped directly to real Avalonia elements.
/// </para>
/// </remarks>
public static class HostedControlCatalog
{
    private static readonly IReadOnlyDictionary<string, int> Hosts =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["ToolStripControlHost"] = 0,
        };

    /// <summary>The position of the hosted control in the host's constructor argument list.</summary>
    public static bool TryGetHostedArgumentIndex(string winFormsTypeName, out int argumentIndex) =>
        Hosts.TryGetValue(winFormsTypeName, out argumentIndex);

    /// <summary>Exposed so the table can be checked against WinForms itself.</summary>
    public static IReadOnlyDictionary<string, int> All => Hosts;
}
