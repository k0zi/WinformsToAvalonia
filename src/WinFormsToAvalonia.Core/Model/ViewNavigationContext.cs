namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// What a handler body needs to know to translate a call that reaches the Window it lives in -
/// closing it, reading its Title, or opening another Form as a dialog it owns.
/// </summary>
/// <param name="FormViews">Every Form in the project, keyed by its original WinForms class name.</param>
/// <param name="HostIsWindow">
/// Whether <c>this</c> is itself the Window, in which case every such call is emitted bare, as it
/// always has been. False for a converted UserControl.
/// </param>
/// <param name="WindowExpression">
/// How to name the Window when <paramref name="HostIsWindow"/> is false but one is still
/// reachable - the browser head's main View, which is rooted at a UserControl so it can be shown
/// under a single-view lifetime, yet on the desktop head sits inside a Window that can be closed
/// and can own a dialog. Null means no Window is reachable and such calls stay un-migrated, which
/// is the answer for a converted UserControl.
/// </param>
public sealed record ViewNavigationContext(
    IReadOnlyDictionary<string, FormViewInfo> FormViews,
    bool HostIsWindow,
    string? WindowExpression = null)
{
    /// <summary>Whether a Window can be named from here at all.</summary>
    public bool ReachesWindow => HostIsWindow || WindowExpression is not null;

    /// <summary>The Window as an expression - `this` where the host is one.</summary>
    public string ResolvedWindowExpression => HostIsWindow ? "this" : WindowExpression ?? "this";

    /// <summary>No Forms resolved - the safe default, which translates no navigation at all.</summary>
    public static ViewNavigationContext None { get; } =
        new(new Dictionary<string, FormViewInfo>(StringComparer.Ordinal), HostIsWindow: true);
}
