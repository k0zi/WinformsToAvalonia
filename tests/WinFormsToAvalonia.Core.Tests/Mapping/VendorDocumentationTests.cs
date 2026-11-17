using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Mapping.Vendors;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

/// <summary>
/// <c>docs/ThirdPartyControls.md</c> against the catalog it describes, in both directions - the
/// same check <c>ControlsDocumentationTests</c> makes for the WinForms registry, for the same reason:
/// a hand-written table quietly drifts, and the code wins.
/// </summary>
public partial class VendorDocumentationTests
{
    [Fact]
    public void EveryCatalogEntry_HasARow_AndEveryRow_AnEntry()
    {
        var rows = DocumentedRows();
        var catalog = VendorCatalog.BuiltIn.Controls.Select(c => c.FullName).ToHashSet(StringComparer.Ordinal);

        Assert.Empty(catalog.Where(c => !rows.ContainsKey(c)).Order(StringComparer.Ordinal));
        Assert.Empty(rows.Keys.Where(r => !catalog.Contains(r)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryRow_SaysWhatTheEntryIsConvertedAs()
    {
        foreach (var (fullName, documented) in DocumentedRows())
        {
            Assert.True(VendorCatalog.BuiltIn.TryGet(fullName, out var entry));
            var expected = entry.As is { } target ? target
                : entry.PlacedAs.Count > 0 ? string.Join(" / ", entry.PlacedAs.Values.Distinct())
                : "—";
            Assert.True(expected == documented, $"docs/ThirdPartyControls.md says {fullName} is '{documented}', the catalog '{expected}'.");
        }
    }

    [Fact]
    public void EveryBaseType_IsNamed()
    {
        var text = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "ThirdPartyControls.md"));
        Assert.Empty(VendorCatalog.BuiltIn.BaseTypes.Where(b => !text.Contains($"`{b.SimpleName}`", StringComparison.Ordinal)).Select(b => b.FullName));
    }

    private static Dictionary<string, string> DocumentedRows() =>
        Row().Matches(File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "ThirdPartyControls.md")))
            .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["target"].Value.Trim(), StringComparer.Ordinal);

    [GeneratedRegex(@"^\| `(?<name>[A-Za-z.]+)` \| (?<target>[^|]+) \|", RegexOptions.Multiline)]
    private static partial Regex Row();

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WinFormsToAvalonia.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found above the test assembly.");
    }
}
