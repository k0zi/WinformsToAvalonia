namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Thrown by <see cref="ConversionPipeline.Run"/> when the source project contains no
/// WinForms Form, UserControl, or Component - there is nothing worth converting, so callers
/// should short-circuit instead of scaffolding an empty Avalonia project.
/// </summary>
/// <param name="unresolvedBaseTypes">
/// Base classes of designer classes that could not be traced to WinForms - a vendor's
/// <c>MetroForm</c>, say. Non-empty means there <em>is</em> something to convert, once a
/// <c>--mappings</c> file says what those bases are.
/// </param>
public sealed class NoConvertibleArtifactsException(string projectFilePath, IReadOnlyList<string>? unresolvedBaseTypes = null)
    : Exception($"'{projectFilePath}' contains no WinForms Form, UserControl, or Component to convert.")
{
    public string ProjectFilePath { get; } = projectFilePath;

    public IReadOnlyList<string> UnresolvedBaseTypes { get; } = unresolvedBaseTypes ?? [];
}
