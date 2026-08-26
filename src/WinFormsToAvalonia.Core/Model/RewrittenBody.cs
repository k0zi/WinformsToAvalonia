namespace WinFormsToAvalonia.Core.Model;

/// <param name="MigratedStatements">Real C# statements, in original order, ready to emit.</param>
/// <param name="RemainingBody">
/// The suffix of the original body that was not migrated, verbatim - still emitted as a comment.
/// Empty only when the whole body was migrated.
/// </param>
public sealed record RewrittenBody(
    IReadOnlyList<string> MigratedStatements,
    string RemainingBody,
    int TotalStatementCount,
    IReadOnlySet<string> RequiredUsings,
    IReadOnlySet<string> RequiredFallbackKeys,
    bool RequiresAsync)
{
    /// <summary>Nothing understood - the pre-Track-3 behaviour, and still the common case.</summary>
    public static RewrittenBody NothingMigrated(string body, int totalStatementCount = 0) =>
        new([], body, totalStatementCount, EmptySet, EmptySet, false);

    private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>(StringComparer.Ordinal);

    public int MigratedStatementCount => MigratedStatements.Count;

    /// <summary>True when nothing is left to migrate by hand - the method needs no TODO marker.</summary>
    public bool IsComplete => RemainingBody.Length == 0 && TotalStatementCount > 0;
}
