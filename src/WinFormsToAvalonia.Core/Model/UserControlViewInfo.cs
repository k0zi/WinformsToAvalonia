namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One UserControl the source project defines, resolved to the Avalonia View generated from
/// it. Produced by ConversionPipeline once per run and shared by the two stages that need it:
/// ControlMappingRegistry (so the type resolves to an element instead of a TODO comment) and
/// AxamlEmitter (so <see cref="XmlnsPrefix"/> is declared on every View's root element).
/// </summary>
/// <param name="WinFormsTypeName">The UserControl's simple class name, e.g. "DemoUserControl".</param>
/// <param name="ViewClassName">Its generated View class name, e.g. "DemoUserControlView".</param>
/// <param name="ViewNamespace">
/// The View's full CLR namespace. It mirrors the source file's subfolder, so a UserControl
/// under Controls/ lands in {Project}.Views.Controls - which is exactly why the prefix can't
/// just be the one shared "views" one.
/// </param>
/// <param name="XmlnsPrefix">The AXAML namespace prefix declared for <paramref name="ViewNamespace"/>, e.g. "uc0".</param>
public sealed record UserControlViewInfo(
    string WinFormsTypeName,
    string ViewClassName,
    string ViewNamespace,
    string XmlnsPrefix)
{
    /// <summary>The prefixed element name to emit, e.g. "uc0:DemoUserControlView".</summary>
    public string ElementName => $"{XmlnsPrefix}:{ViewClassName}";
}
