using WinFormsToAvalonia.Core.Mapping;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// <see cref="WinFormsMnemonicCatalog"/>'s two halves, held up against each other and against
/// the mappers they describe.
/// </summary>
/// <remarks>
/// The catalog says both "this WinForms control's Text is a caption" and "the element it becomes
/// can render an access key". The second half is the one that can drift: change a mapper's target
/// element and the catalog still claims the old one's behaviour, which shows up as a stray
/// underscore in front of a caption or a swallowed first letter - never as an error.
/// </remarks>
public class MnemonicCatalogTests
{
    [Theory]
    [MemberData(nameof(Entries))]
    public void Handling_MatchesWhatTheTargetElementCanRender(string winFormsTypeName, MnemonicHandling handling)
    {
        var (targetName, _) = TargetOf(winFormsTypeName);

        Assert.Equal(handling == MnemonicHandling.AccessKey, AvaloniaAccessKeySupport.Consumes(targetName));
    }

    /// <summary>An entry only means anything on a mapper that actually carries <c>Text</c> across.</summary>
    [Theory]
    [MemberData(nameof(Entries))]
    public void Handling_IsDeclaredOnAMapperThatMapsText(string winFormsTypeName, MnemonicHandling handling)
    {
        Assert.NotEqual(MnemonicHandling.None, handling);

        var (_, attributes) = TargetOf(winFormsTypeName);

        Assert.Contains(attributes, a => a.WinFormsProperty == "Text");
    }

    /// <summary>
    /// The element (or bundled template) a mapper emits, and what it carries across - the two
    /// mapper kinds that can map <c>Text</c>, answered uniformly.
    /// </summary>
    private static (string TargetName, IReadOnlyList<(string? WinFormsProperty, string AvaloniaAttribute)> Attributes)
        TargetOf(string winFormsTypeName)
    {
        foreach (var mapper in DefaultControlMappers.All)
        {
            switch (mapper)
            {
                case SimplePropertyMapper simple when simple.WinFormsTypeName == winFormsTypeName:
                    return (simple.AvaloniaElementName, simple.DeclaredAttributes);
                case FallbackControlMapper fallback when fallback.WinFormsTypeName == winFormsTypeName:
                    return (
                        fallback.FallbackTemplateKey,
                        [.. fallback.DeclaredAttributes.Select(a => ((string?)a.WinFormsProperty, a.AvaloniaAttribute))]);
            }
        }

        Assert.Fail($"'{winFormsTypeName}' has a mnemonic entry but no mapper that could apply it.");
        return default;
    }

    /// <summary>
    /// The reverse direction: a control whose <c>Text</c> becomes a caption must have decided
    /// what an ampersand in it means, rather than silently keeping one.
    /// </summary>
    [Theory]
    [MemberData(nameof(CaptionMappers))]
    public void CaptionCarryingMapper_HasAMnemonicDecision(string winFormsTypeName)
    {
        if (NotAMnemonicCaption.TryGetValue(winFormsTypeName, out var reason))
        {
            Assert.Equal(MnemonicHandling.None, WinFormsMnemonicCatalog.For(winFormsTypeName));
            Assert.False(string.IsNullOrWhiteSpace(reason));
            return;
        }

        Assert.NotEqual(MnemonicHandling.None, WinFormsMnemonicCatalog.For(winFormsTypeName));
    }

    /// <summary>
    /// Captions whose ampersand really is an ampersand, with the reason written down rather than
    /// remembered.
    /// </summary>
    private static IReadOnlyDictionary<string, string> NotAMnemonicCaption { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ColumnHeader"] = "a ListView column heading is not focusable and WinForms does not "
                + "read a mnemonic out of it - an ampersand there is part of the heading",
        };

    /// <summary>Every element named as access-key-capable has to be one Avalonia really defines.</summary>
    [Theory]
    [MemberData(nameof(AccessKeyElements))]
    public void AccessKeyElement_Exists(string elementName)
    {
        Assert.True(
            AvaloniaMetadata.FindElement(elementName) is not null,
            $"AvaloniaAccessKeySupport names '{elementName}', which Avalonia does not define.");
    }

    public static TheoryData<string, MnemonicHandling> Entries()
    {
        var data = new TheoryData<string, MnemonicHandling>();
        foreach (var (typeName, handling) in WinFormsMnemonicCatalog.All.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            data.Add(typeName, handling);
        }

        return data;
    }

    public static TheoryData<string> AccessKeyElements()
    {
        var data = new TheoryData<string>();
        foreach (var name in AvaloniaAccessKeySupport.All.Order(StringComparer.Ordinal))
        {
            data.Add(name);
        }

        return data;
    }

    /// <summary>
    /// The mappers that turn a WinForms <c>Text</c> into a caption - <c>Content</c> or
    /// <c>Header</c> - as opposed to into text a user typed.
    /// </summary>
    public static TheoryData<string> CaptionMappers()
    {
        var data = new TheoryData<string>();

        var carriers = DefaultControlMappers.All
            .OfType<SimplePropertyMapper>()
            .Select(m => (m.WinFormsTypeName, Attributes: m.DeclaredAttributes.Select(a => (a.WinFormsProperty, a.AvaloniaAttribute))))
            .Concat(DefaultControlMappers.All
                .OfType<FallbackControlMapper>()
                .Select(m => (m.WinFormsTypeName, Attributes: m.DeclaredAttributes.Select(a => ((string?)a.WinFormsProperty, a.AvaloniaAttribute)))));

        foreach (var (typeName, _) in carriers
            .Where(m => m.Attributes.Any(a => a.Item1 == "Text" && a.Item2 is "Content" or "Header"))
            .OrderBy(m => m.WinFormsTypeName, StringComparer.Ordinal))
        {
            data.Add(typeName);
        }

        return data;
    }
}
