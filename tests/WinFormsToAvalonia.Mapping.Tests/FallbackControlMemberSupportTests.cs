using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.FallbackControls;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// What a bundled template claims to expose, checked against the Avalonia class it derives from.
/// </summary>
/// <remarks>
/// The table's whole justification is that these templates ship in this repo, so what they expose
/// is "a known fact rather than a guess" - but the members are <em>inherited</em>, and inheritance
/// is a fact about Avalonia. The templates are embedded text, never compiled here, so the claim
/// was previously checked by nothing at all.
/// </remarks>
public class FallbackControlMemberSupportTests
{
    [Theory]
    [MemberData(nameof(Claims))]
    public void Template_ReallyExposesWhatItClaims(string templateKey, string memberName, string baseTypeName)
    {
        var baseType = AvaloniaMetadata.FindElement(baseTypeName);
        Assert.True(baseType is not null, $"'{templateKey}' derives from '{baseTypeName}', which Avalonia does not define.");

        Assert.True(
            AvaloniaMetadata.FindProperty(baseType!, memberName) is not null
            || AvaloniaMetadata.FindMethod(baseType!, memberName, 0) is not null
            || AvaloniaMetadata.FindMethod(baseType!, memberName, 1) is not null,
            $"FallbackControlMemberSupport says '{templateKey}' exposes '{memberName}', but its base "
            + $"'{baseTypeName}' has no such member - a body written against it would not compile.");
    }

    public static TheoryData<string, string, string> Claims()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var (templateKey, memberName) in FallbackControlMemberSupport.AllEntries
            .OrderBy(e => e.TemplateKey, StringComparer.Ordinal)
            .ThenBy(e => e.MemberName, StringComparer.Ordinal))
        {
            data.Add(templateKey, memberName, BaseTypeOf(templateKey));
        }

        return data;
    }

    /// <summary>
    /// The class a template derives from, read out of the template's own source - the source that
    /// is copied verbatim into the generated project, so it is the same answer the C# compiler
    /// there will get.
    /// </summary>
    private static string BaseTypeOf(string templateKey)
    {
        var definition = FallbackControlCatalog.All[templateKey];
        var source = FallbackControlCatalog.ReadTemplateSource(definition.ResourceLogicalName);

        var declaration = Regex.Match(source, $@"class\s+{Regex.Escape(templateKey)}\s*:\s*([\w.]+)");
        Assert.True(declaration.Success, $"Could not find '{templateKey}'s declaration in its own source.");

        var baseName = declaration.Groups[1].Value;
        return baseName[(baseName.LastIndexOf('.') + 1)..];
    }
}
