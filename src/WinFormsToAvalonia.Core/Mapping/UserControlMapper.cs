using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Maps a UserControl defined by the project being converted onto the Avalonia UserControl
/// View generated from it, so `this.demoUserControl1 = new DemoUserControl();` in a Form's
/// Designer.cs emits a real `&lt;uc0:DemoUserControlView /&gt;` element instead of a
/// "no mapping registered" TODO comment.
/// </summary>
/// <remarks>
/// Unlike every other mapper this one is per-conversion-run, not part of
/// <see cref="DefaultControlMappers.All"/>: which UserControls exist is a fact about the
/// source project. ConversionPipeline builds one per discovered UserControl and composes them
/// with the built-in set through the <see cref="ControlMappingRegistry"/>'s mapper-sequence
/// constructor.
/// </remarks>
public sealed class UserControlMapper : IControlMapper
{
    private readonly string _elementName;

    /// <param name="winFormsTypeName">The UserControl's simple class name, e.g. "DemoUserControl".</param>
    /// <param name="elementName">
    /// The prefixed AXAML element name for its generated View, e.g. "uc0:DemoUserControlView" -
    /// the prefix is declared on the root element by AxamlEmitter.
    /// </param>
    public UserControlMapper(string winFormsTypeName, string elementName)
    {
        WinFormsTypeName = winFormsTypeName;
        _elementName = elementName;
    }

    public string WinFormsTypeName { get; }

    public MappedControl Map(ControlModel control) => new(
        control.ClrTypeName,
        MappingStatus.Direct,
        _elementName,
        new Dictionary<string, string>(StringComparer.Ordinal),
        FallbackTemplateKey: null,
        Warnings: []);
}
