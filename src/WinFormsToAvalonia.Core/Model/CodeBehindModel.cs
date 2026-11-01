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

    /// <summary>
    /// This class's code-behind with a base class's folded in, for a Form whose designer was
    /// flattened over its base's. Whatever this class declares itself wins - an override or a
    /// <c>new</c> member hides the base's, as it does in C# - and the base's constructor work
    /// comes first, as it runs first.
    /// </summary>
    public CodeBehindModel WithInherited(CodeBehindModel baseModel)
    {
        var ownHandlers = HandlerMethods.Select(h => h.MethodName).ToHashSet(StringComparer.Ordinal);
        var ownMembers = HelperMembers.Select(m => m.Name).Concat(ownHandlers).ToHashSet(StringComparer.Ordinal);

        return this with
        {
            HandlerMethods = [.. HandlerMethods, .. baseModel.HandlerMethods.Where(h => !ownMembers.Contains(h.MethodName))],
            HelperMembers = [.. HelperMembers, .. baseModel.HelperMembers.Where(m => !ownMembers.Contains(m.Name))],
            ConstructorExtraStatements = [.. baseModel.ConstructorExtraStatements, .. ConstructorExtraStatements],
            RuntimeEventSubscriptions = [.. baseModel.RuntimeEventSubscriptions, .. RuntimeEventSubscriptions],
        };
    }

    public HandlerMethodModel? FindHandler(string methodName) =>
        HandlerMethods.FirstOrDefault(h => string.Equals(h.MethodName, methodName, StringComparison.Ordinal));
}
