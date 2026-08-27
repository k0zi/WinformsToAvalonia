namespace WinFormsToAvalonia.Core.Model;

/// <param name="MigratedStatements">Real C# statements, in original order, ready to emit.</param>
/// <param name="RemainingBody">
/// The suffix of the original body that was not migrated, verbatim - still emitted as a comment.
/// Empty only when the whole body was migrated.
/// </param>
/// <summary>
/// Where the un-migrated remainder of a <em>whole-body</em> rewrite goes, when appending it to the
/// end of the method would be wrong.
/// </summary>
/// <remarks>
/// Only the close-confirmation produces one. Its shape runs the handler's tail on two paths - the
/// one that asks and the one that does not - so a tail that could not be translated has to live
/// in one place both of them call, or a human would fix it on one path and silently leave the
/// other broken. A local function is that place: it keeps the remainder inside the method it came
/// from, and there is exactly one of it to edit.
/// </remarks>
/// <param name="LocalFunctionName">What the emitted statements call, and what the emitter declares.</param>
/// <param name="MigratedStatements">The part of the tail that did translate, if any.</param>
public sealed record BodyRemainder(string LocalFunctionName, IReadOnlyList<string> MigratedStatements);

public sealed record RewrittenBody(
    IReadOnlyList<string> MigratedStatements,
    string RemainingBody,
    int TotalStatementCount,
    IReadOnlySet<string> RequiredUsings,
    IReadOnlySet<string> RequiredFallbackKeys,
    bool RequiresAsync,
    IReadOnlySet<string>? InlinedDialogFields = null,
    bool RequiresCloseGuard = false,
    int? MigratedStatementCountOverride = null,
    BodyRemainder? Remainder = null)
{
    /// <summary>
    /// Whether the View has to declare the <c>w2aForceClose</c> field this body's close
    /// confirmation reads - see <c>HandlerBodyRewriter.TryMatchCloseConfirmation</c>.
    /// </summary>
    public bool RequiresCloseGuard { get; } = RequiresCloseGuard;

    /// <summary>
    /// Where <see cref="RemainingBody"/> is rendered, when it is not simply appended to the end of
    /// the method. Null for every ordinary prefix rewrite.
    /// </summary>
    public BodyRemainder? Remainder { get; } = Remainder;

    /// <summary>File dialogs this body opens inline, so no separate method is generated for them.</summary>
    public IReadOnlySet<string> InlinedDialogFields { get; } = InlinedDialogFields ?? EmptySet;

    /// <summary>
    /// A body this converter wrote itself rather than translated - there is no original to keep,
    /// so it is complete by construction and needs no TODO marker.
    /// </summary>
    public static RewrittenBody Synthesized(params string[] statements) =>
        new(statements, "", statements.Length, EmptySet, EmptySet, false);

    /// <summary>Nothing understood - the pre-Track-3 behaviour, and still the common case.</summary>
    public static RewrittenBody NothingMigrated(string body, int totalStatementCount = 0) =>
        new([], body, totalStatementCount, EmptySet, EmptySet, false);

    private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// How many of the original's statements came across. Normally one entry per statement - but
    /// a body rewritten as a *whole* (the close confirmation) is one entry standing for all of
    /// them, and counting entries there would report a migration rate that means nothing.
    /// </summary>
    public int MigratedStatementCount => MigratedStatementCountOverride ?? MigratedStatements.Count;

    /// <summary>True when nothing is left to migrate by hand - the method needs no TODO marker.</summary>
    public bool IsComplete => RemainingBody.Length == 0 && TotalStatementCount > 0;
}
