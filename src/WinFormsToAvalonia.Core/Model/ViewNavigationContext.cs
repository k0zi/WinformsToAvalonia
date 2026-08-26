namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// What a handler body needs to know to translate a call that opens another Form.
/// </summary>
/// <param name="FormViews">Every Form in the project, keyed by its original WinForms class name.</param>
/// <param name="HostIsWindow">
/// False for a converted UserControl. Avalonia's <c>ShowDialog</c> needs a Window to own the
/// dialog, and a UserControl is not one - so navigation stays un-migrated there.
/// </param>
public sealed record ViewNavigationContext(
    IReadOnlyDictionary<string, FormViewInfo> FormViews,
    bool HostIsWindow)
{
    /// <summary>No Forms resolved - the safe default, which translates no navigation at all.</summary>
    public static ViewNavigationContext None { get; } =
        new(new Dictionary<string, FormViewInfo>(StringComparer.Ordinal), HostIsWindow: true);
}
