namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// The result of loading and evaluating a WinForms .csproj: which style it is, and the
/// resolved set of source/resource files that belong to it.
/// </summary>
public sealed record WinFormsProjectModel(
    string ProjectFilePath,
    bool IsLegacyStyle,
    string RootNamespace,
    string AssemblyName,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> CompileFiles,
    IReadOnlyList<string> ResourceFiles,
    IReadOnlyList<string> ProjectReferences,
    string? OutputType = null,
    bool NullableEnabled = false,
    IReadOnlyList<NuGetPackageReference>? PackageReferences = null)
{
    /// <summary>The NuGet packages the project declares itself (not the ones they bring along).</summary>
    public IReadOnlyList<NuGetPackageReference> Packages => PackageReferences ?? [];

    public string ProjectDirectory => Path.GetDirectoryName(ProjectFilePath)!;

    /// <summary>
    /// Declared a class library - <c>&lt;OutputType&gt;Library&lt;/OutputType&gt;</c>. Only the explicit
    /// value counts: an SDK-style project with no OutputType is a library to MSBuild too, but every
    /// WinForms application template writes <c>WinExe</c>, and guessing from an absence would turn a
    /// hand-written app csproj into a library.
    /// </summary>
    public bool IsLibrary => string.Equals(OutputType, "Library", StringComparison.OrdinalIgnoreCase);
}
