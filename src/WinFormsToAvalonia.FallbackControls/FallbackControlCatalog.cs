using System.Reflection;

namespace WinFormsToAvalonia.FallbackControls;

/// <param name="DependsOnKeys">
/// Other catalog keys this template's source references as types (e.g.
/// ToolStripContainerFallback composes ToolStripPanelFallback/ToolStripContentPanelFallback
/// instances internally). <see cref="FallbackControlResolver"/> must copy these too even when
/// no WinForms control was itself mapped to them, or the generated project won't compile.
/// </param>
/// <param name="RequiredNuGetPackage">
/// A package the template's source needs, when it is not one the generated project already
/// references. Like a mapper's <c>RequiredNuGetPackage</c>, this is only half the story: the
/// package must *also* appear in <c>AvaloniaProjectScaffolder.ExtraPackageVersions</c>, because
/// the csproj writer emits only what is listed there.
/// </param>
public sealed record FallbackTemplateDefinition(
    string Key,
    string OutputFileName,
    string ResourceLogicalName,
    IReadOnlyList<string>? DependsOnKeys = null,
    string? RequiredNuGetPackage = null)
{
    public IReadOnlyList<string> DependsOnKeys { get; } = DependsOnKeys ?? [];
}

/// <summary>
/// The fixed, versioned set of fallback control templates shipped with the tool. Baked
/// into this assembly's embedded resources at build time, so it can't drift from what was
/// tested - reused identically across every conversion run.
/// </summary>
public static class FallbackControlCatalog
{
    public static IReadOnlyDictionary<string, FallbackTemplateDefinition> All { get; } =
        new Dictionary<string, FallbackTemplateDefinition>(StringComparer.Ordinal)
        {
            // The only entry pulled in by a converted handler body rather than by the AXAML -
            // see HandlerBodyRewriter's MessageBox.Show translation.
            ["MessageBoxFallback"] = new("MessageBoxFallback", "MessageBoxFallback.cs", "MessageBoxFallback.cs"),

            // The other two pulled in by a handler body: the dialogs WinForms has and Avalonia
            // does not. ColorDialogFallback wraps Avalonia's real ColorView, which ships
            // separately - hence the package.
            ["ColorDialogFallback"] = new(
                "ColorDialogFallback", "ColorDialogFallback.cs", "ColorDialogFallback.cs",
                RequiredNuGetPackage: "Avalonia.Controls.ColorPicker"),
            ["FontDialogFallback"] = new("FontDialogFallback", "FontDialogFallback.cs", "FontDialogFallback.cs"),
            ["StatusStripFallback"] = new("StatusStripFallback", "StatusStripFallback.cs", "StatusStripFallback.cs"),
            ["ToolStripFallback"] = new("ToolStripFallback", "ToolStripFallback.cs", "ToolStripFallback.cs"),
            ["RichTextBoxFallback"] = new("RichTextBoxFallback", "RichTextBoxFallback.cs", "RichTextBoxFallback.cs"),
            ["ErrorProviderFallback"] = new("ErrorProviderFallback", "ErrorProviderFallback.cs", "ErrorProviderFallback.cs"),
            ["DomainUpDownFallback"] = new("DomainUpDownFallback", "DomainUpDownFallback.cs", "DomainUpDownFallback.cs"),
            ["ToolStripContentPanelFallback"] = new("ToolStripContentPanelFallback", "ToolStripContentPanelFallback.cs", "ToolStripContentPanelFallback.cs"),
            ["ToolStripPanelFallback"] = new("ToolStripPanelFallback", "ToolStripPanelFallback.cs", "ToolStripPanelFallback.cs"),
            ["PropertyGridFallback"] = new("PropertyGridFallback", "PropertyGridFallback.cs", "PropertyGridFallback.cs"),
            ["BindingNavigatorFallback"] = new("BindingNavigatorFallback", "BindingNavigatorFallback.cs", "BindingNavigatorFallback.cs"),
            ["WebBrowserFallback"] = new("WebBrowserFallback", "WebBrowserFallback.cs", "WebBrowserFallback.cs"),
            ["PaintSurfaceFallback"] = new("PaintSurfaceFallback", "PaintSurfaceFallback.cs", "PaintSurfaceFallback.cs"),
            ["PrintPreviewControlFallback"] = new("PrintPreviewControlFallback", "PrintPreviewControlFallback.cs", "PrintPreviewControlFallback.cs"),
            ["ToolStripContainerFallback"] = new(
                "ToolStripContainerFallback", "ToolStripContainerFallback.cs", "ToolStripContainerFallback.cs",
                DependsOnKeys: ["ToolStripPanelFallback", "ToolStripContentPanelFallback"]),
        };

    public static string ReadTemplateSource(string resourceLogicalName)
    {
        var assembly = typeof(FallbackControlCatalog).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceLogicalName)
            ?? throw new InvalidOperationException($"Embedded fallback template resource '{resourceLogicalName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
