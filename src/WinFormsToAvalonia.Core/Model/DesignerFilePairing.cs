namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A discovered WinForms class (Form/UserControl/Component/Other) together with the files
/// that make it up: the hand-written partial (if any), the *.Designer.cs partial (if any),
/// and a paired *.resx (if any).
/// </summary>
/// <param name="UnresolvedBaseTypes">
/// Set only when <paramref name="Kind"/> is <see cref="WinFormsArtifactKind.Other"/>: the base-list
/// names that could not be traced to a WinForms base type anywhere in this project (typically a base
/// class that lives in a referenced assembly). Lets the pipeline report a skipped designer artifact
/// instead of dropping it silently.
/// </param>
/// <param name="BaseTypeNames">
/// The simple names in the class's own base list, ordinal-sorted. What lets the pipeline follow
/// visual inheritance to a base Form whose designer has to run first.
/// </param>
public sealed record DesignerFilePairing(
    string ClassName,
    string? Namespace,
    WinFormsArtifactKind Kind,
    string? PrimaryFilePath,
    string? DesignerFilePath,
    string? ResxFilePath,
    IReadOnlyList<string>? UnresolvedBaseTypes = null,
    IReadOnlyList<string>? BaseTypeNames = null)
{
    public IReadOnlyList<string> UnresolvedBaseTypes { get; } = UnresolvedBaseTypes ?? [];

    public IReadOnlyList<string> BaseTypeNames { get; } = BaseTypeNames ?? [];

    public string FullyQualifiedName => Namespace is null ? ClassName : $"{Namespace}.{ClassName}";
}
