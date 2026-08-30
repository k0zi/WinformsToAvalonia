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
    public void DocumentedRow_MatchesTheRegistry(string winFormsTypeName, string status, string targets, string why)
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

        // An Unsupported entry produces no element, so the doc's target column is empty for it -
        // but its "Why not" cell has to say which of the three kinds of "no element" it is.
        if (expected == MappingStatus.Unsupported)
        {
            var mapper = Assert.IsType<UnsupportedControlMapper>(registry.Mappers[winFormsTypeName]);

            Assert.True(
                ExpectedDisposition(why) == mapper.Disposition,
                $"The doc calls '{winFormsTypeName}' {why}, but the registry classifies it as {mapper.Disposition}.");

            return;
        }

        // The other direction, so a stray glyph on a mapped row cannot sit there unnoticed.
        Assert.True(
            why.Length == 0,
            $"'{winFormsTypeName}' is {status}, so its 'Why not' cell must be empty - it said '{why}'.");

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

        var why = ParsedRows
            .Where(r => r.Status == "❌ Unsupported")
            .GroupBy(r => r.Why, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var summary = File.ReadAllLines(Path.Combine(RepositoryRoot(), "docs", "Controls.md"))
            .First(l => l.StartsWith("**Summary**:", StringComparison.Ordinal));

        // The headline numbers group by *disposition*, not by status. "33 Unsupported" was true
        // of the emitter and false of the conversion: 20 of those types are converted, just not
        // as an element, and reading the total as a failure count is exactly what it invited.
        var elsewhere = why.GetValueOrDefault("🟡 Elsewhere");
        var unreachable = why.GetValueOrDefault("⚪ Unreachable");
        var noApi = why.GetValueOrDefault("❌ No API");
        Assert.Equal(unsupported, elsewhere + unreachable + noApi);

        Assert.Equal(
            $"**Summary**: {direct} Direct, {fallback} Fallback ({direct + fallback} mapped) · "
            + $"{elsewhere} converted without an element ·",
            summary);

        var breakdown = File.ReadAllLines(Path.Combine(RepositoryRoot(), "docs", "Controls.md"))
            .SkipWhile(l => !l.StartsWith("**Summary**:", StringComparison.Ordinal))
            .Skip(1)
            .First();

        Assert.Equal(
            $"{unreachable + noApi} not converted ({unreachable} unreachable from designer code, "
            + $"{noApi} no Avalonia API) ·",
            breakdown);

        Assert.Equal(10, baseClasses);
    }

    /// <summary>
    /// Every type appears exactly once.
    /// </summary>
    /// <remarks>
    /// The summary counts <em>rows</em>, so a type listed twice inflates it and the count still
    /// agrees with the table - which is exactly what happened: `LinkLabel` and
    /// `PrintPreviewDialog` each carried a second, cross-referencing row, and the header claimed
    /// two more types than the registry has. Nothing could notice, because the only two things
    /// checking each other were both counting the same duplicate.
    /// </remarks>
    [Fact]
    public void EachType_IsListedOnce()
    {
        var duplicated = ParsedRows
            .GroupBy(r => r.TypeName, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(
            duplicated.Count == 0,
            $"docs/Controls.md lists these types more than once: {string.Join(", ", duplicated)}. "
            + "Cross-reference them in prose under the table instead - a second row inflates the "
            + "summary counts without disagreeing with them.");
    }

    /// <summary>
    /// Every kind of "no Avalonia element" is actually in use.
    /// </summary>
    /// <remarks>
    /// A disposition nobody ever assigns is a distinction that reads as meaningful and is not -
    /// the same failure the single undifferentiated status had, one level down.
    /// </remarks>
    [Fact]
    public void EveryDisposition_IsUsedByAtLeastOneEntry()
    {
        var used = new ControlMappingRegistry().Mappers.Values
            .OfType<UnsupportedControlMapper>()
            .Select(m => m.Disposition)
            .ToHashSet();

        Assert.Equal(Enum.GetValues<UnsupportedDisposition>().ToHashSet(), used);
    }

    public static TheoryData<string, string, string, string> DocumentedRows()
    {
        var data = new TheoryData<string, string, string, string>();

        foreach (var (typeName, status, targets, why) in ParsedRows)
        {
            data.Add(typeName, status, targets, why);
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

    private static UnsupportedDisposition ExpectedDisposition(string documented) => documented switch
    {
        "🟡 Elsewhere" => UnsupportedDisposition.FeatureElsewhere,
        "⚪ Unreachable" => UnsupportedDisposition.Unreachable,
        "❌ No API" => UnsupportedDisposition.NoAvaloniaApi,
        _ => throw new InvalidOperationException(
            $"docs/Controls.md uses a 'Why not' value this test does not know: '{documented}'."),
    };

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
    private static IReadOnlyList<(string TypeName, string Status, string Targets, string Why)> ParsedRows { get; } =
    [
        .. Regex.Matches(
                File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "Controls.md")),
                // Whitespace-tolerant on purpose: an Unsupported row's target column is empty,
                // so the cell is a single space rather than ` something `. The disposition cell
                // comes *after* the target - putting it before would silently rebind the target
                // group to the glyph and every row would compare against the wrong thing.
                @"^\|\s*`(?<type>[\w.]+)`\s*\|(?<status>[^|]*)\|(?<target>[^|]*)\|(?<why>[^|]*)\|",
                RegexOptions.Multiline)
            .Select(m => (
                m.Groups["type"].Value,
                m.Groups["status"].Value.Trim(),
                string.Join(" / ", Regex.Matches(m.Groups["target"].Value, "`([^`]+)`").Select(t => t.Groups[1].Value)),
                m.Groups["why"].Value.Trim())),
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
