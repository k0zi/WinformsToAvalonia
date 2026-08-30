using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.FallbackControls;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// What a bundled template claims to expose, against what it really has.
/// </summary>
/// <remarks>
/// <para>
/// The table's whole justification is that these templates ship in this repo, so what they expose
/// is "a known fact rather than a guess". A member is real if the template declares it itself or
/// inherits it - and the inherited half is a fact about <em>Avalonia</em>, which nothing here
/// could check until this project existed.
/// </para>
/// <para>
/// The reverse direction matters just as much, and cost more: a template can declare a property
/// that nobody registers, and then every handler line touching it refuses for no reason at all.
/// Five did.
/// </para>
/// </remarks>
public class FallbackControlMemberSupportTests
{
    [Theory]
    [MemberData(nameof(Claims))]
    public void Template_ReallyExposesWhatItClaims(string templateKey, string memberName)
    {
        if (DeclaredProperties(templateKey).Contains(memberName))
        {
            return;
        }

        var baseTypeName = BaseTypeOf(templateKey);
        var baseType = AvaloniaMetadata.FindElement(baseTypeName);
        Assert.True(baseType is not null, $"'{templateKey}' derives from '{baseTypeName}', which Avalonia does not define.");

        Assert.True(
            AvaloniaMetadata.FindProperty(baseType!, memberName) is not null
            || AvaloniaMetadata.FindMethod(baseType!, memberName, 0) is not null
            || AvaloniaMetadata.FindMethod(baseType!, memberName, 1) is not null,
            $"FallbackControlMemberSupport says '{templateKey}' exposes '{memberName}', but the template "
            + $"does not declare it and its base '{baseTypeName}' has no such member - a body written "
            + "against it would not compile.");
    }

    /// <summary>
    /// The other direction: a property a template declares must be registered, or listed as
    /// deliberately out of reach with the reason next to it.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeclaredTemplateProperties))]
    public void DeclaredProperty_IsRegisteredOrDeliberatelyExcluded(string templateKey, string propertyName)
    {
        if (DeliberatelyUnreachable.TryGetValue((templateKey, propertyName), out var reason))
        {
            Assert.False(
                FallbackControlMemberSupport.Exposes(templateKey, propertyName),
                $"'{templateKey}.{propertyName}' is registered, but this test lists it as out of reach: {reason}");
            return;
        }

        Assert.True(
            FallbackControlMemberSupport.Exposes(templateKey, propertyName),
            $"'{templateKey}' declares '{propertyName}' and nothing registers it, so every handler line "
            + "touching it refuses. Register it, or add it to DeliberatelyUnreachable with the reason.");
    }

    /// <summary>
    /// Properties a template really has that a converted body still may not name, each with the
    /// reason - written down here rather than remembered.
    /// </summary>
    private static IReadOnlyDictionary<(string Template, string Property), string> DeliberatelyUnreachable { get; } =
        new Dictionary<(string, string), string>
        {
            // WinForms' WebBrowser.Url is a Uri and the template's is a string, so this is a change
            // of value shape rather than a rename. `new Uri("literal")` could be written out, but
            // reading it back could not, and half a pair is worse than none.
            [("WebBrowserFallback", "Url")] = "WinForms' Url is a Uri, the template's a string",

            // BindingNavigatorFallback's Position and Count used to sit here, on the grounds that
            // no WinForms BindingNavigator has either. Still true of a *handler body* - and beside
            // the point, because the conversion binds them itself now. This table is also what the
            // AXAML emitter consults before writing a binding onto a fallback, and an unlisted
            // property is dropped there without a word.

            // Its value is a PrintDocument, a WinForms type the converted code cannot produce.
            [("PrintPreviewControlFallback", "Document")] = "a PrintDocument cannot be constructed on this side",
        };

    public static TheoryData<string, string> Claims()
    {
        var data = new TheoryData<string, string>();

        foreach (var (templateKey, memberName) in FallbackControlMemberSupport.AllEntries
            .OrderBy(e => e.TemplateKey, StringComparer.Ordinal)
            .ThenBy(e => e.MemberName, StringComparer.Ordinal))
        {
            data.Add(templateKey, memberName);
        }

        return data;
    }

    public static TheoryData<string, string> DeclaredTemplateProperties()
    {
        var data = new TheoryData<string, string>();

        foreach (var templateKey in FallbackControlCatalog.All.Keys.Order(StringComparer.Ordinal))
        {
            foreach (var propertyName in DeclaredProperties(templateKey))
            {
                data.Add(templateKey, propertyName);
            }
        }

        return data;
    }

    /// <summary>
    /// The properties a template declares itself, read from the source that is copied verbatim
    /// into the generated project.
    /// </summary>
    /// <remarks>
    /// Only the <c>StyledProperty</c>-backed ones: those are the control properties a designer
    /// value or a handler line would set. A template's structural members - the panels a
    /// ToolStripContainer exposes, the static helpers on MessageBoxFallback - are reached by
    /// bespoke rules in the emitter and the rewriter rather than through this table.
    /// </remarks>
    private static IReadOnlySet<string> DeclaredProperties(string templateKey) =>
        DeclaredPropertyCache.TryGetValue(templateKey, out var names)
            ? names
            : new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> DeclaredPropertyCache { get; } =
        FallbackControlCatalog.All.Keys.ToDictionary(
            key => key,
            key => (IReadOnlySet<string>)Regex
                .Matches(
                    FallbackControlCatalog.ReadTemplateSource(FallbackControlCatalog.All[key].ResourceLogicalName),
                    @"StyledProperty<[^>]+>\s+(\w+)Property\b")
                .Select(m => m.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);

    /// <summary>
    /// The class a template derives from, read from its own source - the same answer the C#
    /// compiler in the generated project will get.
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
