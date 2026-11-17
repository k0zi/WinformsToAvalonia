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
/// <param name="AssemblyName">
/// The generated assembly this View lands in, when it is <em>not</em> the one being emitted -
/// a UserControl coming from another project of the same solution. Null for the ordinary case.
/// </param>
public sealed record UserControlViewInfo(
    string WinFormsTypeName,
    string ViewClassName,
    string ViewNamespace,
    string XmlnsPrefix,
    string? AssemblyName = null)
{
    /// <summary>The prefixed element name to emit, e.g. "uc0:DemoUserControlView".</summary>
    public string ElementName => $"{XmlnsPrefix}:{ViewClassName}";

    /// <summary>
    /// The xmlns value for <see cref="ViewNamespace"/>.
    /// </summary>
    /// <remarks>
    /// Avalonia's short <c>using:</c> form can only name a namespace in the assembly being
    /// compiled. A View from another project needs the full
    /// <c>clr-namespace:...;assembly=...</c> form - the same distinction XAML has always drawn,
    /// and the reason this is a property rather than one format string at the emitter.
    /// </remarks>
    public string XmlnsValue => AssemblyName is null
        ? $"using:{ViewNamespace}"
        : $"clr-namespace:{ViewNamespace};assembly={AssemblyName}";
}
