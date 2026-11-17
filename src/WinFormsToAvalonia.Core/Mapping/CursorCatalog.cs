namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// WinForms' stock <c>Cursors.X</c> and the Avalonia cursor that is the same shape.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has no static set of cursor objects: a stock cursor is <c>new Cursor(StandardCursorType.X)</c>,
/// and the arrow a control shows when nothing is set is <c>Cursor.Default</c>. So a write such as
/// <c>this.Cursor = Cursors.WaitCursor;</c> - the busy-indicator idiom around a long handler -
/// becomes <c>Cursor = new Cursor(StandardCursorType.Wait);</c>.
/// </para>
/// <para>
/// Only exact counterparts are listed. WinForms' splitter and diagonal-resize cursors
/// (<c>VSplit</c>, <c>SizeNWSE</c>, ...) have no equivalent of the same meaning, and the pan cursors
/// none at all, so a write naming one is refused rather than approximated.
/// <c>Mapping.Tests</c> checks every value against Avalonia's <c>StandardCursorType</c>.
/// </para>
/// </remarks>
public static class CursorCatalog
{
    /// <summary>The <c>Cursors</c> member that means "whatever the control shows by default".</summary>
    public const string DefaultCursorName = "Default";

    /// <summary>WinForms <c>Cursors</c> member → Avalonia <c>StandardCursorType</c> member.</summary>
    public static IReadOnlyDictionary<string, string> StandardCursors { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Arrow"] = "Arrow",
            ["AppStarting"] = "AppStarting",
            ["Cross"] = "Cross",
            ["Hand"] = "Hand",
            ["Help"] = "Help",
            ["IBeam"] = "Ibeam",
            ["No"] = "No",
            ["SizeAll"] = "SizeAll",
            ["SizeNS"] = "SizeNorthSouth",
            ["SizeWE"] = "SizeWestEast",
            ["UpArrow"] = "UpArrow",
            ["WaitCursor"] = "Wait",
        };

    /// <summary>The Avalonia expression for <c>Cursors.{winFormsName}</c>, or false when there is none.</summary>
    public static bool TryGetExpression(string winFormsName, out string expression)
    {
        if (winFormsName == DefaultCursorName)
        {
            expression = "Cursor.Default";
            return true;
        }

        if (StandardCursors.TryGetValue(winFormsName, out var standard))
        {
            expression = $"new Cursor(StandardCursorType.{standard})";
            return true;
        }

        expression = "";
        return false;
    }
}
