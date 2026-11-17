using WinFormsToAvalonia.Core.Mapping.Vendors;

namespace WinFormsToAvalonia.Vendors.Tests;

/// <summary>
/// Every claim the built-in vendor catalog makes, held up against the vendors' real assemblies and
/// against WinForms. A failure here means the catalog is wrong, not the test.
/// </summary>
public class VendorCatalogAgainstMetadataTests
{
    public static TheoryData<string> Controls() => [.. VendorCatalog.BuiltIn.Controls.Select(c => c.FullName).Order(StringComparer.Ordinal)];

    public static TheoryData<string> BaseTypes() => [.. VendorCatalog.BuiltIn.BaseTypes.Select(b => b.FullName).Order(StringComparer.Ordinal)];

    private static VendorControl Entry(string fullName) =>
        VendorCatalog.BuiltIn.TryGet(fullName, out var entry) ? entry : throw new InvalidOperationException(fullName);

    private static Type VendorType(string fullName) =>
        VendorMetadata.VendorType(fullName) ?? throw new Xunit.Sdk.XunitException($"'{fullName}' is not a type in the vendor's assemblies.");

    /// <summary>
    /// The WinForms controls the entry's claims are compared with: its target, or - for an item its
    /// place decides - every control it can become. A claim has to hold for each of them.
    /// </summary>
    private static IReadOnlyList<Type> WinFormsTargets(VendorControl entry) =>
    [
        .. (entry.As is { } target ? [target] : entry.PlacedAs.Values.Distinct())
            .Select(name => VendorMetadata.WinFormsType(name) ?? throw new Xunit.Sdk.XunitException($"'{name}' is not a WinForms type.")),
    ];

    [Theory]
    [MemberData(nameof(Controls))]
    public void EveryControl_IsARealVendorType(string fullName)
    {
        Assert.NotNull(VendorMetadata.VendorType(fullName));
    }

    [Theory]
    [MemberData(nameof(BaseTypes))]
    public void EveryBaseType_IsTheVendorsFormOrUserControl(string fullName)
    {
        var baseType = VendorCatalog.BuiltIn.BaseTypes.Single(b => b.FullName == fullName);
        Assert.True(
            VendorMetadata.DerivesFrom(VendorType(fullName), $"System.Windows.Forms.{baseType.As}"),
            $"'{fullName}' does not derive from System.Windows.Forms.{baseType.As}.");
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void EveryTarget_IsARealWinFormsControl(string fullName)
    {
        // Resolving them is the assertion: an unknown name throws with the name in the message.
        Assert.NotNull(WinFormsTargets(Entry(fullName)));
    }

    /// <summary>
    /// A renamed designer property must exist under the vendor's name, dotted path and all - a
    /// misspelt one is simply never renamed - and under the WinForms name on the target.
    /// </summary>
    [Theory]
    [MemberData(nameof(Controls))]
    public void EveryRenamedProperty_ExistsOnBothSides(string fullName)
    {
        var entry = Entry(fullName);
        var vendor = VendorType(fullName);
        var targets = WinFormsTargets(entry);

        foreach (var property in entry.Properties)
        {
            Assert.True(VendorMetadata.TryResolvePath(vendor, property.From, out _), $"{entry.SimpleName}.{property.From} does not exist.");
            foreach (var winForms in targets)
            {
                Assert.True(
                    VendorMetadata.TryResolvePath(winForms, property.To, out _),
                    $"{entry.SimpleName}.{property.From} is renamed to {winForms.Name}.{property.To}, which does not exist.");
            }
        }

        foreach (var (key, _) in entry.Synthesized)
        {
            Assert.True(targets.Count > 0, $"{entry.SimpleName} implies {key} but has no WinForms target.");
            foreach (var winForms in targets)
            {
                Assert.True(VendorMetadata.TryResolvePath(winForms, key, out _), $"{entry.SimpleName} implies {key}, which {winForms.Name} does not have.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void EveryRenamedEvent_ExistsOnBothSides(string fullName)
    {
        var entry = Entry(fullName);
        var vendor = VendorType(fullName);

        foreach (var (from, to) in entry.Events)
        {
            Assert.True(VendorMetadata.HasEvent(vendor, from), $"{entry.SimpleName} has no event {from}.");
            foreach (var winForms in WinFormsTargets(entry))
            {
                Assert.True(VendorMetadata.HasEvent(winForms, to), $"{entry.SimpleName}.{from} is renamed to {winForms.Name}.{to}, which does not exist.");
            }
        }
    }

    /// <summary>
    /// The whole safety of letting a handler body name a vendor member rests on this: the vendor
    /// member and the WinForms one it is read as have the same type, so a translated statement
    /// means what the original meant - and never compiles to a conversion the original never had.
    /// </summary>
    [Theory]
    [MemberData(nameof(Controls))]
    public void EveryMemberABodyMayName_HasTheSameTypeOnBothSides(string fullName)
    {
        var entry = Entry(fullName);
        if (entry.Members.Count == 0)
        {
            return;
        }

        var vendor = VendorType(fullName);
        var targets = WinFormsTargets(entry);
        Assert.True(targets.Count > 0, $"{entry.SimpleName} names members but has no WinForms target.");

        foreach (var (from, to) in entry.Members)
        {
            var vendorMember = VendorMetadata.MemberType(vendor, from);
            Assert.True(vendorMember is not null, $"{entry.SimpleName}.{from} does not exist.");
            foreach (var winForms in targets)
            {
                var winFormsMember = VendorMetadata.MemberType(winForms, to);
                Assert.True(winFormsMember is not null, $"{winForms.Name}.{to} does not exist.");
                Assert.True(
                    vendorMember!.FullName == winFormsMember!.FullName,
                    $"{entry.SimpleName}.{from} is {vendorMember.FullName}, but {winForms.Name}.{to} is {winFormsMember.FullName}.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void EveryCollection_ExistsOnTheVendorType(string fullName)
    {
        var entry = Entry(fullName);
        var vendor = VendorType(fullName);

        foreach (var collection in entry.Collections)
        {
            Assert.True(VendorMetadata.TryResolvePath(vendor, collection.Path, out _), $"{entry.SimpleName}.{collection.Path} does not exist.");
        }
    }

    /// <summary>
    /// The message boxes a handler body may call in place of WinForms' own: they have to exist, and
    /// have WinForms' text/caption/buttons overload, or the alias claims a meaning they do not have.
    /// </summary>
    [Theory]
    [InlineData("DevExpress.XtraEditors.XtraMessageBox")]
    [InlineData("Telerik.WinControls.RadMessageBox")]
    public void EveryMessageBoxAlias_HasWinFormsOverloads(string fullName)
    {
        var type = VendorType(fullName);
        var shows = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == "Show")
            .Select(m => string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName)))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("System.String", shows);
        Assert.Contains("System.String,System.String", shows);
        Assert.Contains("System.String,System.String,System.Windows.Forms.MessageBoxButtons", shows);
    }
}
