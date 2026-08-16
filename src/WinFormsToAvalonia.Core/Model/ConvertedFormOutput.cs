namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One converted Form or UserControl's emitted View, ready to be merged into the generated
/// project by AvaloniaProjectScaffolder. <see cref="RelativeFolder"/> mirrors the original
/// WinForms source file's subfolder (relative to the project directory, "" for root-level
/// artifacts, "/"-separated for nested ones) under both Views/ and ViewModels/.
/// <see cref="Kind"/> is what tells the scaffolder which of these can be the application's
/// MainWindow: only a <see cref="WinFormsArtifactKind.Form"/> becomes an Avalonia Window, a
/// <see cref="WinFormsArtifactKind.UserControl"/> becomes an Avalonia UserControl and can
/// only be hosted inside one.
/// </summary>
public sealed record ConvertedFormOutput(
    string RelativeFolder,
    string ViewClassName,
    string ViewModelClassName,
    string AxamlContent,
    string ViewCodeBehindContent,
    string ViewModelContent,
    WinFormsArtifactKind Kind = WinFormsArtifactKind.Form);
