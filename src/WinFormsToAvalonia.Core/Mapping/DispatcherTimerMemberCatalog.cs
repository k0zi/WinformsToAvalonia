namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// What a handler body may say about a <c>System.Windows.Forms.Timer</c> that this conversion
/// turned into an <c>Avalonia.Threading.DispatcherTimer</c> field.
/// </summary>
/// <remarks>
/// This table exists because the converter creates that field itself
/// (<c>FormMigrationPlanner.PlanTimers</c>) and then has to be able to talk about it: without an
/// entry here, <c>clockTimer.Enabled = false;</c> blocks the whole handler on a field the
/// conversion's own output declared.
/// </remarks>
public static class DispatcherTimerMemberCatalog
{
    /// <summary>Zero-argument methods that mean the same thing in both frameworks.</summary>
    private static readonly IReadOnlySet<string> Methods =
        new HashSet<string>(StringComparer.Ordinal) { "Start", "Stop" };

    /// <summary>
    /// <c>Enabled</c> is the only property that survives in both directions: WinForms' bool and
    /// Avalonia's <c>IsEnabled</c> bool mean the same thing, and starting/stopping through it
    /// works the same way.
    /// </summary>
    private const string EnabledProperty = "IsEnabled";

    public static bool TryGetMethod(string methodName, out string avaloniaMethodName)
    {
        avaloniaMethodName = Methods.Contains(methodName) ? methodName : "";
        return avaloniaMethodName.Length > 0;
    }

    /// <summary>Reading a timer member. Only <c>Enabled</c>, and see <see cref="TryGetWrite"/> for why.</summary>
    public static bool TryGetRead(string propertyName, out string text)
    {
        text = propertyName == "Enabled" ? EnabledProperty : "";
        return text.Length > 0;
    }

    /// <summary>
    /// Writing a timer member, as a whole statement rather than a left-hand side, because
    /// <c>Interval</c> changes type: WinForms counts milliseconds in an <c>int</c>, Avalonia holds
    /// a <c>TimeSpan</c>.
    /// </summary>
    /// <remarks>
    /// That type change is also why <c>Interval</c> is write-only here. A write can be wrapped
    /// faithfully (<c>TimeSpan.FromMilliseconds(n)</c>); a read cannot - <c>if (t.Interval &gt; 500)</c>
    /// would compile against a TimeSpan and quietly mean something else. Wherever the two
    /// frameworks disagree about a type, this converter refuses rather than guesses.
    /// </remarks>
    public static bool TryGetWrite(string fieldName, string propertyName, string valueText, out string statement)
    {
        switch (propertyName)
        {
            case "Enabled":
                statement = $"{fieldName}.{EnabledProperty} = {valueText};";
                return true;
            case "Interval":
                statement = $"{fieldName}.Interval = TimeSpan.FromMilliseconds({valueText});";
                return true;
            default:
                statement = "";
                return false;
        }
    }
}
