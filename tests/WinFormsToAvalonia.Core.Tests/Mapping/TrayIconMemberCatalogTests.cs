using WinFormsToAvalonia.Core.Mapping;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

/// <summary>
/// The two tables that both state which Avalonia event a NotifyIcon's event becomes.
/// </summary>
/// <remarks>
/// They exist separately for a reason - the event registry drives the planner, the member catalog
/// is what Mapping.Tests can hold up against a real <c>TrayIcon</c>, since a
/// <c>SubscribeInCode</c> mapping does not name its declaring type. Two statements of one fact
/// need a test that they agree, exactly as <c>BindablePropertyCatalogTests</c> does for the
/// property side.
/// </remarks>
public class TrayIconMemberCatalogTests
{
    [Theory]
    [MemberData(nameof(EventEntries))]
    public void EventMapping_AgreesWithTheEventRegistry(string winFormsEventName, string avaloniaEventName)
    {
        var mapping = new EventMappingRegistry().ResolveControlEvent("NotifyIcon", winFormsEventName);

        Assert.Equal(avaloniaEventName, mapping.AvaloniaEventName);

        // A NotifyIcon has no element, so the subscription can only be written in the constructor.
        Assert.True(mapping.SubscribeInCode);
        Assert.Null(mapping.XamlAttributeName);
    }

    /// <summary>
    /// The events with no row must resolve to nothing, rather than falling through to the generic
    /// control table - which is what produced a translated handler nothing ever subscribed.
    /// </summary>
    [Theory]
    [InlineData("DoubleClick")]
    [InlineData("MouseDoubleClick")]
    [InlineData("MouseClick")]
    [InlineData("BalloonTipClicked")]
    public void UnmappableTrayEvent_ResolvesToNothingAndSaysWhy(string winFormsEventName)
    {
        var mapping = new EventMappingRegistry().ResolveControlEvent("NotifyIcon", winFormsEventName);

        Assert.Null(mapping.AvaloniaEventName);
        Assert.False(string.IsNullOrWhiteSpace(mapping.Guidance));
    }

    public static TheoryData<string, string> EventEntries()
    {
        var data = new TheoryData<string, string>();
        foreach (var (winFormsName, avaloniaName) in TrayIconMemberCatalog.AllEventEntries)
        {
            data.Add(winFormsName, avaloniaName);
        }

        return data;
    }
}
