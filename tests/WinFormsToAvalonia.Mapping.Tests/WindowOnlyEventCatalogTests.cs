using WinFormsToAvalonia.Core.Mapping;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// <see cref="WindowOnlyEventCatalog"/> decides which Form-level event attributes a
/// UserControl-rooted View has to hand to its wrapper Window, so it has to be right in both
/// directions - and Avalonia's own metadata is the only thing that can say.
/// </summary>
/// <remarks>
/// A false entry moves an event onto the wrapper that the View could have kept, and the View's
/// handler stops being raised in the browser for no reason. A missing one leaves an
/// <c>Opened=</c> on a <c>UserControl</c>, which is an AVLN2000 in the generated project - the
/// failure this whole suite exists to catch.
/// </remarks>
public class WindowOnlyEventCatalogTests
{
    public static TheoryData<string> WindowOnlyEvents()
    {
        var data = new TheoryData<string>();
        foreach (var name in WindowOnlyEventCatalog.All)
        {
            data.Add(name);
        }

        return data;
    }

    /// <summary>Every Avalonia event a Form subscription can map to, from the registry itself.</summary>
    public static TheoryData<string> MappedFormEvents()
    {
        var data = new TheoryData<string>();
        foreach (var name in EventMappingRegistry.AllFormEventTargets)
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(WindowOnlyEvents))]
    public void ListedEvent_IsDeclaredByWindowAndNotByUserControl(string avaloniaEventName)
    {
        var window = AvaloniaMetadata.FindElement("Window");
        var userControl = AvaloniaMetadata.FindElement("UserControl");
        Assert.True(window is not null && userControl is not null, "Avalonia has no Window/UserControl.");

        Assert.True(
            AvaloniaMetadata.FindEvent(window!, avaloniaEventName) is not null,
            $"'{avaloniaEventName}' is listed as Window-only, but Avalonia's Window does not raise it - "
            + "so the generated wrapper Window would not compile either.");

        Assert.True(
            AvaloniaMetadata.FindEvent(userControl!, avaloniaEventName) is null,
            $"'{avaloniaEventName}' is listed as Window-only, but Avalonia's UserControl raises it too - "
            + "so it should stay on the View, where it keeps working in the browser.");
    }

    [Theory]
    [MemberData(nameof(MappedFormEvents))]
    public void EveryMappedFormEvent_IsEitherWindowOnlyOrCarriedByUserControl(string avaloniaEventName)
    {
        var userControl = AvaloniaMetadata.FindElement("UserControl");
        Assert.True(userControl is not null, "Avalonia has no UserControl type.");

        if (WindowOnlyEventCatalog.IsWindowOnly(avaloniaEventName))
        {
            return;
        }

        Assert.True(
            AvaloniaMetadata.FindEvent(userControl!, avaloniaEventName) is not null,
            $"A Form subscription maps to '{avaloniaEventName}', which Avalonia's UserControl does not "
            + "raise and WindowOnlyEventCatalog does not list. Under --with-web that attribute would be "
            + "emitted on a UserControl root and fail to compile - add it to the catalog.");
    }
}
