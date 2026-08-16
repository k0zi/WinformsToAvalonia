namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A NotifyIcon component collected across all of a project's Forms, aggregated by
/// ConversionPipeline.Run for AvaloniaProjectScaffolder to emit into App.axaml's
/// TrayIcon.Icons - App-level, not per-View, so it doesn't fit ConvertedFormOutput's
/// per-form shape.
/// </summary>
/// <param name="IconAssetPath">
/// The generated project's asset path for the icon (e.g. "Assets/app.ico"), set only when the
/// icon file was actually resolved from Designer.cs <em>and</em> copied into the output.
/// <see langword="null"/> otherwise - which is the common case, since real Designer.cs rarely
/// assigns a literal icon path (it is usually a resx resource or a computed Icon).
/// Avalonia's TrayIcon resolves its Icon at run time, so emitting a path to a file that was
/// never produced is a startup FileNotFoundException, not a build error - hence the null.
/// </param>
public sealed record NotifyIconInfo(string FieldName, string? IconAssetPath, string? TooltipText);
