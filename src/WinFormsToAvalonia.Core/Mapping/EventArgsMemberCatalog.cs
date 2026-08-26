namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="Format">
/// How to write the member, as a format string: <c>{0}</c> is the handler's EventArgs parameter,
/// <c>{1}</c> the field of the control that raised the event.
/// </param>
/// <param name="NeedsSourceControl">
/// True when <c>{1}</c> is used, so the caller knows the translation is only possible for a
/// handler wired to exactly one control.
/// </param>
public readonly record struct EventArgsMember(string Format, bool NeedsSourceControl = false);

/// <summary>
/// What a WinForms handler's <c>EventArgs</c> members mean on the Avalonia side.
/// </summary>
/// <remarks>
/// <para>
/// The member-level counterpart of <see cref="EventMappingRegistry"/>, which maps the *event*.
/// Three cases show up in practice, and only the first two are safe enough to translate:
/// a member Avalonia's own args type happens to spell identically (<c>Cancel</c>,
/// <c>NewValue</c>); a member of an args type that is plain .NET and survived the conversion
/// untouched (<c>FileSystemEventArgs</c>, the BackgroundWorker ones); and a member whose
/// Avalonia equivalent has a genuinely different shape.
/// </para>
/// <para>
/// Of the third kind only the pointer position is translated, because it has an exact answer:
/// WinForms' <c>e.X</c>/<c>e.Y</c> are relative to the control that raised the event, which is
/// precisely what <c>GetPosition(control)</c> takes. Something like
/// <c>DataGridViewCellEventArgs.RowIndex</c> has no exact answer - Avalonia reports the cell
/// through an object rather than an index pair - so it is left for a human.
/// </para>
/// </remarks>
public static class EventArgsMemberCatalog
{
    /// <summary>
    /// Args types that are plain .NET rather than Avalonia's, and therefore reach the generated
    /// project unchanged - every member of them passes through.
    /// </summary>
    /// <remarks>
    /// Plain <c>EventArgs</c> is deliberately absent. It is what
    /// <c>FormMigrationPlanner.PlanCodeBehindHandler</c> falls back to when an event has no
    /// Avalonia equivalent, so it means "the type is unknown", not "the type is EventArgs" - and
    /// the original body will be reaching for members of the richer WinForms args type it was
    /// written against. Treating it as unchanged emitted `e.ProgressPercentage` on a parameter
    /// declared `EventArgs`, which does not compile.
    /// </remarks>
    private static readonly HashSet<string> UnchangedArgsTypes = new(StringComparer.Ordinal)
    {
        "FileSystemEventArgs",
        "RenamedEventArgs",
        "ErrorEventArgs",
        "DoWorkEventArgs",
        "ProgressChangedEventArgs",
        "RunWorkerCompletedEventArgs",
        "SerialDataReceivedEventArgs",
    };

    private static readonly IReadOnlyDictionary<(string ArgsType, string Member), EventArgsMember> ByArgsTypeAndMember =
        new Dictionary<(string, string), EventArgsMember>
        {
            // Avalonia spells these exactly as WinForms did.
            [("WindowClosingEventArgs", "Cancel")] = new("{0}.Cancel"),
            [("ScrollEventArgs", "NewValue")] = new("{0}.NewValue"),
            [("RangeBaseValueChangedEventArgs", "NewValue")] = new("{0}.NewValue"),
            [("RangeBaseValueChangedEventArgs", "OldValue")] = new("{0}.OldValue"),

            // WinForms' pointer coordinates are relative to the control that raised the event -
            // which is exactly the argument Avalonia's GetPosition takes.
            [("PointerPressedEventArgs", "X")] = new("{0}.GetPosition({1}).X", NeedsSourceControl: true),
            [("PointerPressedEventArgs", "Y")] = new("{0}.GetPosition({1}).Y", NeedsSourceControl: true),
            [("PointerEventArgs", "X")] = new("{0}.GetPosition({1}).X", NeedsSourceControl: true),
            [("PointerEventArgs", "Y")] = new("{0}.GetPosition({1}).Y", NeedsSourceControl: true),
            [("PointerReleasedEventArgs", "X")] = new("{0}.GetPosition({1}).X", NeedsSourceControl: true),
            [("PointerReleasedEventArgs", "Y")] = new("{0}.GetPosition({1}).Y", NeedsSourceControl: true),
        };

    public static bool TryGet(string avaloniaArgsTypeName, string memberName, out EventArgsMember member)
    {
        if (ByArgsTypeAndMember.TryGetValue((avaloniaArgsTypeName, memberName), out member))
        {
            return true;
        }

        if (UnchangedArgsTypes.Contains(avaloniaArgsTypeName))
        {
            member = new EventArgsMember("{0}." + memberName);
            return true;
        }

        member = default;
        return false;
    }
}
