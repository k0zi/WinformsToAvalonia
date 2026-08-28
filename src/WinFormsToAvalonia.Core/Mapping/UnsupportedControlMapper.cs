using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Why a type has no mapping - the three very different things a single <c>Unsupported</c> status
/// has been covering.
/// </summary>
/// <remarks>
/// <para>
/// <c>Unsupported</c> means "produces no Avalonia element", which is a fact about the emitter and
/// says nothing about whether the type is converted. Most of the entries in this registry are
/// converted, thoroughly, somewhere else - a <c>Timer</c> becomes a <c>DispatcherTimer</c> field, a
/// <c>ToolTip</c>'s <c>SetToolTip</c> calls become attributes on their targets, an
/// <c>ImageList</c>'s images become files. Reading the registry, or the doc, gave no way to tell
/// those apart from a type nothing will ever do anything with.
/// </para>
/// <para>
/// Worth distinguishing because the three want different things from a reader: one needs nothing,
/// one can never need anything, and one is a permanent manual item. The checklist and the
/// <c>list-mappings</c> table can group by it rather than presenting 33 equal-looking failures.
/// </para>
/// </remarks>
public enum UnsupportedDisposition
{
    /// <summary>
    /// Converted, just not as an element. The guidance says where the feature went.
    /// </summary>
    FeatureElsewhere,

    /// <summary>
    /// Designer code never instantiates one, so the entry can never be reached. It exists so that
    /// a hand-written or unusual input reports rather than falling through to the generic "no
    /// mapping registered" message.
    /// </summary>
    Unreachable,

    /// <summary>
    /// Avalonia has nothing to map to. Permanently manual, and no amount of work here changes it.
    /// </summary>
    NoAvaloniaApi,
}

/// <summary>Records a WinForms control/component with no automatic mapping at all - guidance-only, flagged for manual migration.</summary>
public sealed class UnsupportedControlMapper : IControlMapper
{
    private readonly string _guidance;

    /// <param name="disposition">
    /// Required rather than defaulted, on purpose: an entry added without deciding which of the
    /// three kinds it is would be back to the single undifferentiated status this replaces, and
    /// the compiler is the only thing that can insist.
    /// </param>
    public UnsupportedControlMapper(string winFormsTypeName, UnsupportedDisposition disposition, string guidance)
    {
        WinFormsTypeName = winFormsTypeName;
        Disposition = disposition;
        _guidance = guidance;
    }

    public string WinFormsTypeName { get; }

    public UnsupportedDisposition Disposition { get; }

    public MappedControl Map(ControlModel control) => new(
        control.ClrTypeName,
        MappingStatus.Unsupported,
        null,
        new Dictionary<string, string>(),
        null,
        [_guidance]);
}
