namespace WinFormsToAvalonia.Core.Model;

public enum MappingStatus
{
    /// <summary>A built-in Avalonia control covers this WinForms control directly.</summary>
    Direct,

    /// <summary>No built-in Avalonia control matches; the tool's own bundled fallback control is used instead.</summary>
    Fallback,

    /// <summary>No mapping exists at all - flagged for manual migration.</summary>
    Unsupported,
}
