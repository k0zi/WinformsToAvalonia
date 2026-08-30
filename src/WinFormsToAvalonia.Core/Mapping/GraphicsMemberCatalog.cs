namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>What the first argument of a <c>Graphics</c> drawing call is.</summary>
public enum GraphicsStrokeKind
{
    /// <summary>A <c>Pen</c> - an outline. Avalonia wants an <c>IPen</c>.</summary>
    Pen,

    /// <summary>A <c>Brush</c> - a fill. Avalonia wants an <c>IBrush</c>.</summary>
    Brush,
}

/// <summary>
/// One <c>System.Drawing.Graphics</c> drawing call and the <c>DrawingContext</c> call that means
/// the same thing.
/// </summary>
/// <param name="ArgumentCount">
/// Exact arity, including the leading pen/brush. WinForms overloads the same name over
/// <c>Rectangle</c>, <c>RectangleF</c>, point arrays and float/int coordinates; only the
/// four-coordinate form is listed, and anything else refuses rather than being guessed at.
/// </param>
/// <param name="Format">
/// A composite format for the <c>DrawingContext</c> call: <c>{0}</c> is the translated pen or
/// brush, <c>{1}</c>..<c>{4}</c> the translated coordinates.
/// </param>
public sealed record GraphicsCall(GraphicsStrokeKind Stroke, int ArgumentCount, string Format);

/// <summary>
/// The drawing calls a WinForms <c>Paint</c> handler may use, and their Avalonia equivalents.
/// </summary>
/// <remarks>
/// <para>
/// The member-level counterpart of <see cref="ControlMethodCatalog"/>, for the one place a
/// converted body gets a drawing surface at all: a handler on the bundled
/// <c>PaintSurfaceFallback</c>. Deliberately tiny, and geometric only.
/// </para>
/// <para>
/// <c>DrawString</c> is the notable absence. Avalonia's <c>DrawText</c> takes a
/// <c>FormattedText</c>, which needs a <c>Typeface</c> and an em size where WinForms passed one
/// <c>Font</c> object - and the WinForms <c>Font</c> in a handler is usually
/// <c>this.Font</c> or a control's, neither of which survives as a single value. Splitting one
/// argument into two that this converter cannot read is exactly the kind of guess it does not
/// make, so the statement refuses and the prefix rule leaves the rest of the handler to a human.
/// </para>
/// <para>
/// The shapes themselves are not symmetrical between the two frameworks and the mapping says so:
/// WinForms names the operation twice (<c>DrawEllipse</c> outlines, <c>FillEllipse</c> fills)
/// while Avalonia has one method taking both a brush and a pen, either of which may be null.
/// </para>
/// </remarks>
public static class GraphicsMemberCatalog
{
    private static readonly IReadOnlyDictionary<string, GraphicsCall> Calls =
        new Dictionary<string, GraphicsCall>(StringComparer.Ordinal)
        {
            ["DrawLine"] = new(GraphicsStrokeKind.Pen, 5, "DrawLine({0}, new Point({1}, {2}), new Point({3}, {4}))"),
            ["DrawRectangle"] = new(GraphicsStrokeKind.Pen, 5, "DrawRectangle(null, {0}, new Rect({1}, {2}, {3}, {4}))"),
            ["FillRectangle"] = new(GraphicsStrokeKind.Brush, 5, "DrawRectangle({0}, null, new Rect({1}, {2}, {3}, {4}))"),
            ["DrawEllipse"] = new(GraphicsStrokeKind.Pen, 5, "DrawEllipse(null, {0}, new Rect({1}, {2}, {3}, {4}))"),
            ["FillEllipse"] = new(GraphicsStrokeKind.Brush, 5, "DrawEllipse({0}, null, new Rect({1}, {2}, {3}, {4}))"),
        };

    public static bool TryGet(string methodName, out GraphicsCall call) => Calls.TryGetValue(methodName, out call!);

    /// <summary>Every entry, for the test that checks each one against Avalonia's DrawingContext.</summary>
    public static IEnumerable<(string MethodName, GraphicsCall Call)> All =>
        Calls.Select(entry => (entry.Key, entry.Value));
}
