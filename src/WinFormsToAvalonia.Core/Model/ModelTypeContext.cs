namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One type this run lifted out of a Form/UserControl into the generated project's
/// <c>Models/</c> folder, and the properties a handler body is allowed to set on it.
/// </summary>
/// <param name="TypeName">The type's own name, unchanged by the lift.</param>
/// <param name="Namespace">The namespace it was lifted into - the one thing that has to reach
/// both emitters and the rewriter, so none of them reconstructs it differently.</param>
/// <param name="SettablePropertyNames">
/// Its settable auto-properties, in declaration order. This list is the *entire* vocabulary
/// <see cref="Pipeline.HandlerBodyRewriter"/> may use inside an object initializer for this
/// type - the same "finite proven vocabulary" rule <c>BindablePropertyCatalog</c> applies to
/// controls, applied to a carried-over model type.
/// </param>
public sealed record ModelTypeInfo(
    string TypeName, string Namespace, IReadOnlyList<string> SettablePropertyNames);

/// <summary>
/// The model types available to *every* artifact of one conversion, discovered before any of
/// them is planned.
/// </summary>
/// <remarks>
/// Same shape and same reason as <see cref="ViewSurfaceContext"/>: a handler body may name a
/// type declared in a Form that has not been planned yet, and the ViewModel collection a
/// <c>BindingSource</c> becomes needs its element type at *planning* time. Ordering alone
/// cannot settle that, so discovery is hoisted into the pipeline's parse pass and handed back
/// in as a parameter.
/// </remarks>
public sealed record ModelTypeContext(IReadOnlyDictionary<string, ModelTypeInfo> ByTypeName)
{
    public static ModelTypeContext None { get; } =
        new(new Dictionary<string, ModelTypeInfo>(StringComparer.Ordinal));

    public bool TryGet(string typeName, out ModelTypeInfo info) =>
        ByTypeName.TryGetValue(typeName, out info!);
}
