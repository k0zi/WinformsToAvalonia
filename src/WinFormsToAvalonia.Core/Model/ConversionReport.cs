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
    TimeSpan Elapsed,
    IReadOnlyList<string>? PreservedFiles = null,
    int MigratedStatementCount = 0,
    int HandlerStatementCount = 0,
    /// <summary>
    /// Controls that produce no Avalonia element because their feature was converted somewhere
    /// else - a <c>Timer</c> into a <c>DispatcherTimer</c> field, a <c>ToolTip</c> into attributes
    /// on its targets, a <c>NotifyIcon</c> into App.axaml.
    /// </summary>
    /// <remarks>
    /// Counted apart from <see cref="UnsupportedControlCount"/> because they are not the same
    /// news. These need nothing from the reader; lumping them together is what made a converted
    /// project report dozens of red "unsupported" controls while every one of them worked.
    /// </remarks>
    int ConvertedElsewhereCount = 0,
    IReadOnlyList<string>? ConvertedElsewhereNotes = null)
{
    /// <summary>Where each <see cref="ConvertedElsewhereCount"/> feature went, for the checklist.</summary>
    public IReadOnlyList<string> ConvertedElsewhereNotes { get; } = ConvertedElsewhereNotes ?? [];

    /// <summary>
    /// Output files that already existed with different content and were therefore left alone;
    /// the generated version sits beside each one as `*.w2a-new`. Empty on a first conversion,
    /// and whenever --overwrite-all was passed.
    /// </summary>
    public IReadOnlyList<string> PreservedFiles { get; } = PreservedFiles ?? [];
}
