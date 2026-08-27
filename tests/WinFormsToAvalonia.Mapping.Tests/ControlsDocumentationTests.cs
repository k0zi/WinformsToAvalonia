using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// <c>docs/Controls.md</c> against the registry it describes.
/// </summary>
/// <remarks>
/// The doc says so itself: it is a hand-written snapshot, and if it drifts from
/// <c>DefaultControlMappers.cs</c> the code wins and the doc gets fixed. That is the same
/// "hand-maintained thing that can quietly lie" this project exists for - it just happened to be
/// prose rather than a table, so nothing checked it.
/// </remarks>
public class ControlsDocumentationTests
{
    [Theory]
    [MemberData(nameof(DocumentedRows))]
    public void DocumentedRow_MatchesTheRegistry(string winFormsTypeName, string status, string targets)
    {
        var registry = new ControlMappingRegistry();
        var hasMapper = registry.Mappers.ContainsKey(winFormsTypeName);

        // A base class is never instantiated by designer code, so it must have no entry at all -
        // an entry for one would mean the doc is describing a mapping that can never be reached.
        if (status == BaseClassStatus)
        {
            Assert.False(
                hasMapper,
                $"The doc calls '{winFormsTypeName}' a base class with no registry entry, but the "
                + "registry maps it.");
            return;
        }

        // Form and UserControl are conversion roots, not table entries - the doc says so, and the
        // registry agrees by not having one.
        if (status == ConversionRootStatus)
        {
            return;
        }

        Assert.True(hasMapper, $"The doc gives '{winFormsTypeName}' a status of '{status}', but the registry has no mapper for it.");

        var mapped = registry.Map(new ControlModel { FieldName = "field1", ClrTypeName = winFormsTypeName });
        var expected = ExpectedStatus(status);

        Assert.True(
            mapped.Status == expected,
            $"The doc says '{winFormsTypeName}' is {status}, but the registry maps it as {mapped.Status}.");

        // An Unsupported entry produces no element, so the doc's target column is empty for it.
        if (expected == MappingStatus.Unsupported)
        {
            return;
        }

        var actual = expected == MappingStatus.Fallback ? mapped.FallbackTemplateKey : mapped.AvaloniaElementName;

        // A per-instance mapper (ListView) legitimately has more than one target, and the doc
        // lists all of them - a probe only ever observes the one this control model chose.
        Assert.True(
            targets.Split('/', StringSplitOptions.TrimEntries).Contains(actual, StringComparer.Ordinal),
            $"The doc says '{winFormsTypeName}' maps to '{targets}', but the registry produced '{actual}'.");
    }

    /// <summary>
    /// The other direction: nothing the registry maps may be missing from the doc. A drifting
    /// table is bad; a silently absent row is worse, because there is nothing to notice.
    /// </summary>
    [Theory]
    [MemberData(nameof(RegisteredTypeNames))]
    public void RegisteredType_IsDocumented(string winFormsTypeName)
    {
        Assert.True(
            DocumentedTypeNames.Contains(winFormsTypeName),
            $"The registry maps '{winFormsTypeName}', but docs/Controls.md has no row for it.");
    }

    /// <summary>
    /// The summary line at the top, against the rows it summarises.
    /// </summary>
    /// <remarks>
    /// Added after finding it already wrong - 45/30 where the table said 46/34 - which is the
    /// whole argument for checking a number a reader is asked to believe.
    /// </remarks>
    [Fact]
    public void SummaryLine_CountsTheRowsBelowIt()
    {
        var counts = ParsedRows
            .GroupBy(r => r.Status, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var direct = counts.GetValueOrDefault("✅ Direct");
        var fallback = counts.GetValueOrDefault("✅ Fallback");
        var unsupported = counts.GetValueOrDefault("❌ Unsupported");
        var baseClasses = counts.GetValueOrDefault(BaseClassStatus);

        var summary = File.ReadAllLines(Path.Combine(RepositoryRoot(), "docs", "Controls.md"))
            .First(l => l.StartsWith("**Summary**:", StringComparison.Ordinal));

        Assert.Equal(
            $"**Summary**: {direct} Direct, {fallback} Fallback ({direct + fallback} mapped) · "
            + $"{unsupported} Unsupported (not mapped, guidance-only) ·",
            summary);

        Assert.Equal(10, baseClasses);
    }

    public static TheoryData<string, string, string> DocumentedRows()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var (typeName, status, targets) in ParsedRows)
        {
            data.Add(typeName, status, targets);
        }

        return data;
    }

    public static TheoryData<string> RegisteredTypeNames()
    {
        var data = new TheoryData<string>();

        foreach (var typeName in new ControlMappingRegistry().Mappers.Keys.Order(StringComparer.Ordinal))
        {
            data.Add(typeName);
        }

        return data;
    }

    private const string BaseClassStatus = "—";
    private const string ConversionRootStatus = "✅ Converted";

    private static MappingStatus ExpectedStatus(string documented) => documented switch
    {
        "✅ Direct" => MappingStatus.Direct,
        "✅ Fallback" => MappingStatus.Fallback,
        "❌ Unsupported" => MappingStatus.Unsupported,
        _ => throw new InvalidOperationException($"docs/Controls.md uses a status this test does not know: '{documented}'."),
    };

    /// <summary>
    /// Every `| `Type` | status | `target` | notes |` row. Parsed rather than duplicated, so the
    /// doc stays the single place these are written down.
    /// </summary>
    private static IReadOnlyList<(string TypeName, string Status, string Targets)> ParsedRows { get; } =
    [
        .. Regex.Matches(
                File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "Controls.md")),
                // Whitespace-tolerant on purpose: an Unsupported row's target column is empty,
                // so the cell is a single space rather than ` something `.
                @"^\|\s*`(?<type>[\w.]+)`\s*\|(?<status>[^|]*)\|(?<target>[^|]*)\|",
                RegexOptions.Multiline)
            .Select(m => (
                m.Groups["type"].Value,
                m.Groups["status"].Value.Trim(),
                string.Join(" / ", Regex.Matches(m.Groups["target"].Value, "`([^`]+)`").Select(t => t.Groups[1].Value)))),
    ];

    /// <remarks>
    /// After <c>ParsedRows</c>, which it reads: static initializers run in source order.
    /// </remarks>
    private static IReadOnlySet<string> DocumentedTypeNames { get; } =
        ParsedRows.Select(r => r.TypeName).ToHashSet(StringComparer.Ordinal);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WinFormsToAvalonia.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "Could not find the repository root from the test output directory.");
        return directory!.FullName;
    }
}
