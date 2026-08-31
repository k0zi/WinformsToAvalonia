namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// What a WinForms <c>StringFormat</c> can say that an Avalonia <c>FormattedText</c> can say back.
/// </summary>
/// <remarks>
/// <para>
/// A <c>StringFormat</c> is the layout half of <c>Graphics.DrawString</c>, and most of it has no
/// counterpart: <c>LineAlignment</c> is vertical placement, which a FormattedText does not do at
/// all (<c>MaxTextHeight</c> clips, it does not centre); <c>FormatFlags</c> carries a dozen
/// behaviours with no single Avalonia setting; and the <c>Generic*</c> statics are opaque - a
/// converted body cannot read what is in one. Those are refused rather than half-translated.
/// </para>
/// <para>
/// The two below are exact, and both are meaningless without a width to lay out inside. That is
/// why the rewriter only accepts a <c>StringFormat</c> on the <c>RectangleF</c> overload: aligning
/// or trimming against nothing would emit a setting that silently does nothing, which is worse
/// than refusing.
/// </para>
/// </remarks>
public static class TextFormatCatalog
{
    /// <summary>
    /// <c>StringAlignment</c> to <c>TextAlignment</c>. WinForms names the ends of the line by
    /// reading direction, Avalonia by side - the same three positions.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> Alignments =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Near"] = "Left",
            ["Center"] = "Center",
            ["Far"] = "Right",
        };

    /// <summary>
    /// <c>StringTrimming</c> to <c>TextTrimming</c>.
    /// </summary>
    /// <remarks>
    /// <c>Character</c> and <c>Word</c> are deliberately absent: they cut the text off with no
    /// ellipsis, and Avalonia has no such mode - every one of its trimming values but
    /// <c>None</c> appends one. Emitting an ellipsis where the original showed none is a visible
    /// difference, so those refuse.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, string> Trimmings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["None"] = "None",
            ["EllipsisCharacter"] = "CharacterEllipsis",
            ["EllipsisWord"] = "WordEllipsis",
            ["EllipsisPath"] = "PathSegmentEllipsis",
        };

    public static bool TryGetAlignment(string stringAlignmentMember, out string textAlignmentMember) =>
        Alignments.TryGetValue(stringAlignmentMember, out textAlignmentMember!);

    public static bool TryGetTrimming(string stringTrimmingMember, out string textTrimmingMember) =>
        Trimmings.TryGetValue(stringTrimmingMember, out textTrimmingMember!);

    /// <summary>Every mapped pair, for the test that checks each against Avalonia's own enums.</summary>
    public static IEnumerable<(string WinFormsEnum, string WinFormsMember, string AvaloniaType, string AvaloniaMember)> All =>
        Alignments.Select(e => ("StringAlignment", e.Key, "TextAlignment", e.Value))
            .Concat(Trimmings.Select(e => ("StringTrimming", e.Key, "TextTrimming", e.Value)));
}
