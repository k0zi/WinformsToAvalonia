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
    /// <summary>
    /// Whether <see cref="ConstructorExtraStatements"/> can run in the generated View's own
    /// (parameterless) constructor and mean what they meant: they all come from a single,
    /// parameterless instance constructor - or there is no constructor at all. Statements of a
    /// constructor taking arguments, or of one of several, would run where they never did.
    /// </summary>
    public bool ConstructorStatementsMovable { get; init; } = true;

    /// <summary>
    /// Handlers that were <c>protected override void OnX(...)</c> methods in the original
    /// (<c>OverrideHandlerLifter</c>), each with the override's name - which is the method the
    /// semantic model knows, since it compiled the original source.
    /// </summary>
    public IReadOnlyDictionary<string, string> LiftedOverrides { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>The method of the original source that <paramref name="methodName"/>'s body came from.</summary>
    public string SourceMethodName(string methodName) =>
        LiftedOverrides.TryGetValue(methodName, out var original) ? original : methodName;

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
            ConstructorStatementsMovable = ConstructorStatementsMovable && baseModel.ConstructorStatementsMovable,
            RuntimeEventSubscriptions = [.. baseModel.RuntimeEventSubscriptions, .. RuntimeEventSubscriptions],
        };
    }

    public HandlerMethodModel? FindHandler(string methodName) =>
        HandlerMethods.FirstOrDefault(h => string.Equals(h.MethodName, methodName, StringComparison.Ordinal));
}
