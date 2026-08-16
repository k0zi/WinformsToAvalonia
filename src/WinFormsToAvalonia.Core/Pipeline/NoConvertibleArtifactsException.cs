namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Thrown by <see cref="ConversionPipeline.Run"/> when the source project contains no
/// WinForms Form, UserControl, or Component - there is nothing worth converting, so callers
/// should short-circuit instead of scaffolding an empty Avalonia project.
/// </summary>
public sealed class NoConvertibleArtifactsException(string projectFilePath)
    : Exception($"'{projectFilePath}' contains no WinForms Form, UserControl, or Component to convert.")
{
    public string ProjectFilePath { get; } = projectFilePath;
}