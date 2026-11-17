using System.Text.RegularExpressions;
using WinFormsToAvalonia.FallbackControls;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// Structural rules the bundled templates have to follow to be visible at all.
/// </summary>
public class FallbackControlTemplateTests
{
    /// <summary>
    /// Avalonia resolves a control's theme by its concrete type, so a subclass of a templated
    /// control finds no theme, gets no template, and renders as <em>nothing</em>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the failure mode this whole test project exists for, in its purest form: a
    /// converted app whose MaskedTextBox and RichTextBox were simply absent from the window,
    /// while the project compiled, started, and passed every test in the suite. Nothing in the
    /// generated code is wrong - the missing piece is a fact about Avalonia that only this
    /// project can check.
    /// </para>
    /// <para>
    /// Panel-derived templates are exempt because a Panel has no template to lose; a
    /// <c>UserControl</c> subclass was measured to render fine without the override, but is
    /// still required to declare it so the rule has no exceptions to remember.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(TemplatesDerivedFromTemplatedControls))]
    public void TemplatedControlSubclass_OverridesItsStyleKey(string templateKey, string baseTypeName)
    {
        var source = SourceOf(templateKey);

        Assert.Matches(
            $@"protected override Type StyleKeyOverride => typeof\({Regex.Escape(baseTypeName)}\);",
            source);
    }

    public static TheoryData<string, string> TemplatesDerivedFromTemplatedControls()
    {
        var templatedControl = AvaloniaMetadata.FindElement("TemplatedControl");
        Assert.True(templatedControl is not null, "Avalonia does not define TemplatedControl.");

        var data = new TheoryData<string, string>();

        foreach (var templateKey in FallbackControlCatalog.All.Keys.Order(StringComparer.Ordinal))
        {
            // A few templates are static helpers (MessageBoxFallback, the dialog wrappers) with
            // no base type at all - nothing to theme, nothing to check.
            if (BaseTypeOf(templateKey) is not { } baseTypeName)
            {
                continue;
            }

            var baseType = AvaloniaMetadata.FindElement(baseTypeName);
            if (baseType is not null && DerivesFrom(baseType, templatedControl!))
            {
                data.Add(templateKey, baseTypeName);
            }
        }

        Assert.True(data.Count > 0, "No template derives from a templated control - the rule is not being checked.");
        return data;
    }

    private static bool DerivesFrom(Type type, Type candidateBase)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.FullName == candidateBase.FullName)
            {
                return true;
            }
        }

        return false;
    }

    private static string SourceOf(string templateKey) =>
        FallbackControlCatalog.ReadTemplateSource(FallbackControlCatalog.All[templateKey].ResourceLogicalName);

    /// <summary>The class a template derives from, or null when it declares no base type.</summary>
    private static string? BaseTypeOf(string templateKey)
    {
        var declaration = Regex.Match(SourceOf(templateKey), $@"class\s+{Regex.Escape(templateKey)}\s*:\s*([\w.]+)");
        if (!declaration.Success)
        {
            return null;
        }

        var baseName = declaration.Groups[1].Value;
        return baseName[(baseName.LastIndexOf('.') + 1)..];
    }
}
