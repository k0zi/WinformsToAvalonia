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
    IReadOnlyList<string> ResourceFiles)
{
    public string ProjectDirectory => Path.GetDirectoryName(ProjectFilePath)!;
}
