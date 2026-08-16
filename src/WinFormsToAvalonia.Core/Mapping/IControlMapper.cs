using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

public interface IControlMapper
{
    /// <summary>The WinForms CLR simple type name this mapper handles, e.g. "Button".</summary>
    string WinFormsTypeName { get; }

    MappedControl Map(ControlModel control);
}
