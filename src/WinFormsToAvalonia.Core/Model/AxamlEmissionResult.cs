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
    IReadOnlyList<string> Warnings,
    /// <summary>
    /// Form-level event attributes this document could not carry because it is rooted at a
    /// <c>UserControl</c> - see <see cref="Mapping.WindowOnlyEventCatalog"/>. Empty for every
    /// Window-rooted View, which is all of them unless <c>--with-web</c> asked otherwise; the
    /// generated wrapper Window takes these and forwards them into the View.
    /// </summary>
    IReadOnlyList<(string AttributeName, string HandlerMethodName)>? DeferredWindowEvents = null,
    /// <summary>
    /// Controls that emitted no element because their feature lives elsewhere - counted and
    /// reported apart from the ones nothing handles. See <c>ConversionReport</c>.
    /// </summary>
    int ConvertedElsewhereCount = 0,
    IReadOnlyList<string>? ConvertedElsewhereNotes = null)
{
    public IReadOnlyList<string> ConvertedElsewhereNotes { get; } = ConvertedElsewhereNotes ?? [];
}
