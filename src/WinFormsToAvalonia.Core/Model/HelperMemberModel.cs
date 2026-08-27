namespace WinFormsToAvalonia.Core.Model;

public enum HelperMemberKind
{
    Field,
    Property,
    Method,
    Other,
}

/// <summary>
/// A non-handler member of the original Form class (a private helper method like
/// <c>SetBusy</c>, a backing field, a property). Preserved verbatim so the generated
/// code-behind can carry it alongside the handlers that call it, instead of losing it in a
/// file-level comment.
/// </summary>
/// <param name="Signature">
/// For a method, the parts a translation needs: the return type, the parameter list and the body,
/// each as written. Null for every other member kind - and for a method whose shape puts it out
/// of reach before anything is even attempted (generic, or with ref/out parameters).
/// </param>
/// <param name="Facts">
/// For a method, what its body touches - the same analysis a handler's body gets, so a handler
/// that calls it can answer for the helper's requirements as if they were its own.
/// </param>
public sealed record HelperMemberModel(
    string Name,
    HelperMemberKind Kind,
    string SourceText,
    HelperMethodSignature? Signature = null,
    HelperFieldInfo? Field = null,
    HandlerMethodModel? Facts = null,
    HelperPropertyInfo? Property = null);

/// <summary>
/// A property of the original Form or UserControl. Null unless its shape is one the conversion
/// could reproduce: an accessor with a real body, not an auto-property (a field-shaped property
/// is the field-promotion path, not this one).
/// </summary>
/// <remarks>
/// Both accessor bodies are normalised to statements, so an expression-bodied property, an
/// expression-bodied accessor and a block accessor all reach the planner in one shape - it is
/// the property's *body* that decides whether it can come across, never how it was spelled.
/// </remarks>
/// <param name="ModifiersText">As written, e.g. <c>"public"</c>.</param>
/// <param name="TypeText">As written.</param>
/// <param name="GetterBodyText">The getter's statements, dedented like a helper's; null if it has none.</param>
/// <param name="SetterBodyText">The setter's statements, with <c>value</c> in scope; null if it has none.</param>
public sealed record HelperPropertyInfo(
    string ModifiersText,
    string TypeText,
    string? GetterBodyText,
    string? SetterBodyText);

/// <summary>
/// A private backing field of the original Form. Null unless the field's shape is one the
/// conversion can reproduce verbatim.
/// </summary>
/// <param name="ModifiersText">As written, minus the accessibility, e.g. <c>"readonly"</c>.</param>
/// <param name="TypeText">A keyword type only - see <c>FormMigrationPlanner.PlanHelperFields</c>.</param>
/// <param name="InitializerText">The declared initializer, as written, or null.</param>
public sealed record HelperFieldInfo(string ModifiersText, string TypeText, string? InitializerText);

/// <param name="ReturnTypeText">As written - `void`, `int`, `string`.</param>
/// <param name="ParameterListText">As written, including the parentheses: `(bool busy)`.</param>
/// <param name="ParameterNames">Just the names, which is what the translation needs in scope.</param>
/// <param name="BodyText">The statements between the braces, dedented like a handler's.</param>
/// <param name="IsAsync">Whether the original already carried the modifier.</param>
public sealed record HelperMethodSignature(
    string ReturnTypeText,
    string ParameterListText,
    IReadOnlyList<string> ParameterNames,
    string BodyText,
    bool IsAsync);

/// <summary>
/// What a translated body needs to know before it may call a helper: that the helper became real
/// code at all, how many arguments it takes, and whether the call has to be awaited.
/// </summary>
public sealed record HelperCallInfo(int ParameterCount, bool IsAsync);
