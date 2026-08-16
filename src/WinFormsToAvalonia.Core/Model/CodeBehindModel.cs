namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// The analyzed (not merely captured) contents of a Form's non-designer .cs file:
/// its event handlers with the facts needed to classify them, its remaining members, and any
/// extra constructor statements. Produced by CodeBehindAnalyzer, consumed by
/// FormMigrationPlanner. <see cref="RawCodeBehind"/> stays the verbatim-text counterpart used
/// for the leftover comment block.
/// </summary>
public sealed record CodeBehindModel(
    string OriginalFilePath,
    IReadOnlyList<HandlerMethodModel> HandlerMethods,
    IReadOnlyList<HelperMemberModel> HelperMembers,
    IReadOnlyList<string> ConstructorExtraStatements,
    IReadOnlyList<RuntimeEventSubscription> RuntimeEventSubscriptions)
{
    public static CodeBehindModel Empty(string originalFilePath = "") =>
        new(originalFilePath, [], [], [], []);

    public HandlerMethodModel? FindHandler(string methodName) =>
        HandlerMethods.FirstOrDefault(h => string.Equals(h.MethodName, methodName, StringComparison.Ordinal));
}
