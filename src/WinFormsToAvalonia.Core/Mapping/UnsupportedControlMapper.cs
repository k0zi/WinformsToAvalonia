using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>Records a WinForms control/component with no automatic mapping at all - guidance-only, flagged for manual migration.</summary>
public sealed class UnsupportedControlMapper : IControlMapper
{
    private readonly string _guidance;

    public UnsupportedControlMapper(string winFormsTypeName, string guidance)
    {
        WinFormsTypeName = winFormsTypeName;
        _guidance = guidance;
    }

    public string WinFormsTypeName { get; }

    public MappedControl Map(ControlModel control) => new(
        control.ClrTypeName,
        MappingStatus.Unsupported,
        null,
        new Dictionary<string, string>(),
        null,
        [_guidance]);
}
