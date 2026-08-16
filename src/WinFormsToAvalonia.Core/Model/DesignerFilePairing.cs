namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A discovered WinForms class (Form/UserControl/Component/Other) together with the files
/// that make it up: the hand-written partial (if any), the *.Designer.cs partial (if any),
/// and a paired *.resx (if any).
/// </summary>
public sealed record DesignerFilePairing(
    string ClassName,
    string? Namespace,
    WinFormsArtifactKind Kind,
    string? PrimaryFilePath,
    string? DesignerFilePath,
    string? ResxFilePath)
{
    public string FullyQualifiedName => Namespace is null ? ClassName : $"{Namespace}.{ClassName}";
}
