namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One property the converted View really exposes - the public surface a handler in another
/// artifact is allowed to name.
/// </summary>
/// <param name="TypeText">As declared, e.g. <c>"string"</c>. Only keyword types get this far.</param>
/// <param name="HasSetter">Whether it can be written to as well as read.</param>
public sealed record ViewPropertyInfo(string Name, string TypeText, bool HasSetter);

/// <summary>
/// What the project's converted Views expose to each other, resolved before any handler body is
/// translated.
/// </summary>
/// <remarks>
/// The same problem <c>ConversionPipeline.BuildFormViews</c> solves for Forms, one level down: a
/// handler that says <c>dialog.EnteredText</c> names a member of a View that may not be planned
/// yet, so ordering alone cannot fix it. Hence a pass over every artifact first, and this as its
/// result.
/// </remarks>
/// <param name="Own">
/// This artifact's own promoted properties, with their translated bodies - what the emitter
/// writes out. Planned in that same pass, so it is never planned twice.
/// </param>
/// <param name="ByType">
/// Every artifact's, keyed by the original WinForms type name (<c>"DialogForm"</c>). This one is
/// only ever *consulted*: it says what a name means, not what to emit.
/// </param>
public sealed record ViewSurfaceContext(
    IReadOnlyList<PromotedPropertyPlan> Own,
    IReadOnlyDictionary<string, IReadOnlyList<ViewPropertyInfo>> ByType)
{
    public static ViewSurfaceContext None { get; } =
        new([], new Dictionary<string, IReadOnlyList<ViewPropertyInfo>>(StringComparer.Ordinal));
}
