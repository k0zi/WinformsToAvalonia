using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Which WinForms controls a translated body may create at run time - <c>new Button()</c>,
/// <c>new ToolStripStatusLabel()</c> - and the Avalonia element that construction becomes.
/// </summary>
/// <remarks>
/// <para>
/// Not a table: the answer is read off the registry, by mapping a control the designer never
/// configured. A type qualifies only when that mapping is the bare element - <c>Direct</c>, no
/// attribute, no wrapper or nested element, no extra package, no warning - because then
/// <c>new Element()</c> is exactly what the AXAML would have declared for it, and every property the
/// body goes on to set is translated by the same catalog a designer control's is.
/// </para>
/// <para>
/// Everything else refuses: a fallback template (its constructor is ours, but its member surface is
/// what <c>FallbackControlMemberSupport</c> lists, and a runtime instance would bypass the emitter's
/// checks), a mapper that needs the designer's properties to choose an element, and an unsupported
/// type.
/// </para>
/// </remarks>
public static class RuntimeControlSupport
{
    public static bool TryGetElement(ControlMappingRegistry registry, string winFormsTypeName, out string avaloniaElementName)
    {
        avaloniaElementName = "";
        if (!registry.Mappers.ContainsKey(winFormsTypeName))
        {
            return false;
        }

        var mapped = registry.Map(new ControlModel { FieldName = "w2aRuntimeProbe", ClrTypeName = winFormsTypeName });
        if (mapped is not { Status: MappingStatus.Direct, AvaloniaElementName: { } element }
            || mapped.Attributes.Count > 0
            || mapped.Warnings.Count > 0
            || mapped.RequiredNuGetPackage is not null
            || mapped.NestedElements is { Count: > 0 }
            || mapped.ChildWrapperElementNames is { Count: > 0 })
        {
            return false;
        }

        avaloniaElementName = element;
        return true;
    }
}
