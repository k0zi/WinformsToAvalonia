namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A summary of one `convert` run: what was discovered, how controls were mapped, and how
/// long it took - what the CLI renders as tables/a summary panel, and what --log-file
/// serializes to JSON.
/// </summary>
/// <param name="FormCount">Forms converted into Avalonia Windows.</param>
/// <param name="UserControlCount">UserControls converted into Avalonia UserControls.</param>
public sealed record ConversionReport(
    bool IsLegacyStyle,
    IReadOnlyList<string> TargetFrameworks,
    int FormCount,
    int UserControlCount,
    int DirectControlCount,
    int FallbackControlCount,
    int UnsupportedControlCount,
    IReadOnlyList<string> UsedFallbackKeys,
    IReadOnlyList<string> RequiredNuGetPackages,
    IReadOnlyList<string> Warnings,
    TimeSpan Elapsed);
