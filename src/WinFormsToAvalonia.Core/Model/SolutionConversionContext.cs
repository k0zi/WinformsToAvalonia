namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One UserControl a <em>different</em> project of the same solution defines, resolved to the
/// View that project's own conversion emits for it.
/// </summary>
/// <remarks>
/// It carries no xmlns prefix: prefixes are positional and only mean anything within a single
/// generated document, so the hosting run assigns them alongside its own.
/// </remarks>
/// <param name="AssemblyName">The generated assembly the View lands in - always another one.</param>
public sealed record ExternalUserControl(
    string WinFormsTypeName,
    string ViewClassName,
    string ViewNamespace,
    string AssemblyName);

/// <summary>
/// What the rest of the solution contributes to one project's conversion. Null for the ordinary
/// single-project run, which is why this is a parameter of <c>ConversionPipeline.Run</c> rather
/// than a field on ConversionOptions: options are the user's intent, this is orchestration
/// context that only <see cref="Pipeline.SolutionConversionPipeline"/> can know.
/// </summary>
/// <param name="ExternalUserControls">
/// UserControls from the projects this one references. They come from the referenced set rather
/// than from every project in the solution, because that is exactly the set whose types the
/// source project could legally have named - the generated project inherits the same graph.
/// </param>
/// <param name="ProjectReferences">
/// The generated csproj files those projects produce, as paths relative to this project's own
/// folder, e.g. <c>../Widgets/Widgets.csproj</c>.
/// </param>
public sealed record SolutionConversionContext(
    IReadOnlyList<ExternalUserControl> ExternalUserControls,
    IReadOnlyList<string> ProjectReferences);
