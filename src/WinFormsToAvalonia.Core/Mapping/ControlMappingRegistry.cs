using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

public sealed class ControlMappingRegistry
{
    private readonly Dictionary<string, IControlMapper> _mappersByTypeName;

    public ControlMappingRegistry()
        : this(DefaultControlMappers.All)
    {
    }

    public ControlMappingRegistry(IEnumerable<IControlMapper> mappers)
    {
        _mappersByTypeName = mappers.ToDictionary(m => m.WinFormsTypeName, StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, IControlMapper> Mappers => _mappersByTypeName;

    public MappedControl Map(ControlModel control)
    {
        if (_mappersByTypeName.TryGetValue(control.ClrTypeName, out var mapper))
        {
            return mapper.Map(control);
        }

        return new MappedControl(
            control.ClrTypeName,
            MappingStatus.Unsupported,
            null,
            new Dictionary<string, string>(),
            null,
            [$"No mapping registered for WinForms control type '{control.ClrTypeName}'."]);
    }
}
