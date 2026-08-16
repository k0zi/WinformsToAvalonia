namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One View's emitted AXAML text, plus which bundled fallback control templates (by
/// FallbackControlCatalog key) it actually referenced - so the pipeline knows which ones
/// FallbackControlResolver needs to copy into Controls/ - which extra official Avalonia
/// NuGet packages (e.g. Avalonia.Controls.DataGrid) the emitted elements require, and
/// per-status control counts/warnings for the CLI's conversion report.
/// </summary>
public sealed record AxamlEmissionResult(
    string Axaml,
    IReadOnlySet<string> UsedFallbackKeys,
    IReadOnlySet<string> RequiredNuGetPackages,
    int DirectControlCount,
    int FallbackControlCount,
    int UnsupportedControlCount,
    IReadOnlyList<string> Warnings);
