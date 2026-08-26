namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The one piece of WinForms' <c>DialogResult</c> enum that survives the conversion: which
/// members mean "accepted".
/// </summary>
/// <remarks>
/// <para>
/// A converted dialog closes with a <c>bool</c> - <c>Close(true)</c> / <c>Close(false)</c> - and
/// its caller reads that back through <c>ShowDialog&lt;bool&gt;</c>. Only OK/Yes/Cancel/No can be
/// expressed that way: a three-way Yes/No/Cancel dialog has no bool answer, and widening the
/// result type would change what *every* converted dialog returns.
/// </para>
/// <para>
/// Both halves of that contract - the synthesized handler on the dialog side
/// (<c>FormMigrationPlanner.PlanDialogResultButtons</c>) and the hand-written
/// <c>DialogResult = ...</c> the rewriter translates - go through here, so they cannot drift
/// apart on what "OK" means.
/// </para>
/// </remarks>
public static class DialogResultCatalog
{
    /// <summary>
    /// Whether a designer-declared <c>DialogResult</c> closes the dialog as "accepted".
    /// </summary>
    /// <remarks>
    /// Total, unlike <see cref="TryGetBool"/>, and deliberately so. In WinForms *any* non-None
    /// DialogResult on a button closes the form, so a synthesized handler must always close -
    /// refusing an Abort/Retry/Ignore button would leave it doing nothing at all, which is a
    /// worse translation than losing the distinction between it and Cancel.
    /// </remarks>
    public static bool ClosesWithSuccess(string dialogResultMemberName) =>
        dialogResultMemberName is "OK" or "Yes";

    /// <summary>
    /// The faithful bool for a <c>DialogResult</c> value, when there is one. Used where the
    /// result has to survive a round trip - a hand-written <c>DialogResult = ...</c> that the
    /// caller reads back - so an Abort/Retry/Ignore value has no answer here rather than
    /// collapsing into "false".
    /// </summary>
    public static bool TryGetBool(string dialogResultMemberName, out bool accepted)
    {
        switch (dialogResultMemberName)
        {
            case "OK" or "Yes":
                accepted = true;
                return true;
            case "Cancel" or "No":
                accepted = false;
                return true;
            default:
                accepted = false;
                return false;
        }
    }
}
