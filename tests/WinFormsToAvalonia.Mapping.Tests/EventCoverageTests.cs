using WinFormsToAvalonia.Core.Mapping;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// Every event a WinForms <c>Control</c> or <c>Form</c> declares, against the registry.
/// </summary>
/// <remarks>
/// <para>
/// These are the events a designer can wire on <em>anything</em>, so they are the finite,
/// complete set the converter owes an answer for. Each must be classified by name: either mapped
/// to a real Avalonia event, or given guidance that says why not and what to do instead. The
/// generic "no equivalent is registered" fallback does not count - it is true, it explains
/// nothing, and nothing used to say which events landed in it.
/// </para>
/// <para>
/// Type-specific events are deliberately out of scope: DataGridView alone declares 126, more than
/// Control and Form together, and Avalonia's DataGrid is a different shape almost throughout.
/// Those get mappings one at a time, when a real one can be proven.
/// </para>
/// </remarks>
public class EventCoverageTests
{
    [Theory]
    [MemberData(nameof(UniversalEvents))]
    public void UniversalEvent_IsClassifiedByName(string declaringType, string eventName)
    {
        Assert.True(
            Classified.Contains(eventName),
            $"WinForms {declaringType} declares '{eventName}', and the registry has no entry for it - "
            + "a form wiring it gets a generic 'no equivalent registered' with no explanation. Map it, "
            + "or give it guidance saying why there is none.");
    }

    /// <summary>
    /// The WinForms being read has to be the one the generated projects target, or this is
    /// measuring a different API than the tables describe.
    /// </summary>
    [Fact]
    public void WinFormsMetadata_MatchesTheTargetFramework()
    {
        Assert.Equal(10, WinFormsMetadata.MajorVersion);
    }

    public static TheoryData<string, string> UniversalEvents()
    {
        var data = new TheoryData<string, string>();

        foreach (var typeName in new[] { "Control", "Form" })
        {
            var type = WinFormsMetadata.FindType(typeName);
            Assert.True(type is not null, $"WinForms has no '{typeName}' type - something is very wrong.");

            foreach (var eventName in WinFormsMetadata.DeclaredEventNames(type!))
            {
                data.Add(typeName, eventName);
            }
        }

        return data;
    }

    private static IReadOnlySet<string> Classified { get; } =
        EventMappingRegistry.ClassifiedEventNames.ToHashSet(StringComparer.Ordinal);
}
