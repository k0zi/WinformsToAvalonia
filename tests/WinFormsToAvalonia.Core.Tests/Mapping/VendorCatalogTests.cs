using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Mapping.Vendors;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

/// <summary>
/// The vendor catalog's promises to the rest of the converter. Its promises about the vendors
/// themselves are WinFormsToAvalonia.Vendors.Tests', which reads their real assemblies.
/// </summary>
public class VendorCatalogTests
{
    private static readonly ControlMappingRegistry Registry = new();

    /// <summary>
    /// A vendor control is only ever converted as a WinForms control the registry already maps -
    /// that is what lets every table downstream answer for it unchanged, and what keeps this
    /// catalog from introducing an Avalonia element nothing has checked.
    /// </summary>
    [Fact]
    public void EveryTarget_IsAControlTheRegistryMaps()
    {
        foreach (var control in VendorCatalog.BuiltIn.Controls)
        {
            foreach (var target in control.PlacedAs.Values.Append(control.As).OfType<string>())
            {
                Assert.True(Registry.Mappers.ContainsKey(target), $"{control.FullName} targets '{target}', which the registry does not map.");
            }
        }
    }

    /// <summary>
    /// A vendor name the registry also maps would make a user's --mappings entry, keyed by simple
    /// name, ambiguous - and would mean the catalog shadows a WinForms control.
    /// </summary>
    [Fact]
    public void NoVendorName_IsAlsoAWinFormsName()
    {
        Assert.Empty(VendorCatalog.BuiltIn.Controls.Where(c => Registry.Mappers.ContainsKey(c.SimpleName)).Select(c => c.FullName));
    }

    /// <summary>
    /// A designer local has no field, so the element it becomes must not want an x:Name - or it is
    /// consumed before emission (its target is null, or it is an item whose text is read).
    /// </summary>
    [Fact]
    public void EveryTypeAllowedAsALocal_BecomesSomethingWithoutAName()
    {
        foreach (var control in VendorCatalog.BuiltIn.Controls.Where(c => c.AllowedAsLocal && c.As is not null))
        {
            var mapped = Registry.Map(new ControlModel { FieldName = "x", ClrTypeName = control.As! });
            Assert.False(mapped.SupportsName, $"{control.FullName} may be a designer local, but {control.As} takes an x:Name.");
        }
    }

    [Theory]
    [InlineData("DevExpress.XtraEditors.TextEdit", "DevExpress.XtraEditors.TextEdit")]
    [InlineData("global::DevExpress.XtraEditors.TextEdit", "DevExpress.XtraEditors.TextEdit")]
    [InlineData("TextEdit", "DevExpress.XtraEditors.TextEdit")]
    [InlineData("Some.Other.TextEdit", null)]
    public void Resolve_MatchesTheWrittenName(string written, string? expected)
    {
        Assert.Equal(expected, VendorCatalog.BuiltIn.Resolve(written, new HashSet<string>())?.FullName);
    }

    /// <summary>LayoutControlItem is both vendors' - unqualified, only the file's usings can say whose.</summary>
    [Fact]
    public void Resolve_AnAmbiguousSimpleName_NeedsTheUsing()
    {
        Assert.Null(VendorCatalog.BuiltIn.Resolve("LayoutControlItem", new HashSet<string>()));
        Assert.Equal(
            "Telerik.WinControls.UI.LayoutControlItem",
            VendorCatalog.BuiltIn.Resolve("LayoutControlItem", new HashSet<string> { "Telerik.WinControls.UI" })?.FullName);
    }

    /// <summary>A user's --mappings entry for a vendor type replaces the built-in one, base types included.</summary>
    [Fact]
    public void Without_DropsWhatTheUserMapped()
    {
        var catalog = VendorCatalog.BuiltIn.Without(["TextEdit", "XtraForm"]);

        Assert.Null(catalog.Resolve("DevExpress.XtraEditors.TextEdit", new HashSet<string>()));
        Assert.Null(catalog.BaseTypeNamed("XtraForm"));
        Assert.NotNull(catalog.BaseTypeNamed("RadForm"));
    }

    [Fact]
    public void Retype_RenamesConvertsAndDropsTheIgnorable()
    {
        Assert.True(VendorCatalog.BuiltIn.TryGet("DevExpress.XtraEditors.CheckEdit", out var entry));
        var control = new ControlModel { FieldName = "agree", ClrTypeName = "CheckEdit" };
        control.Properties["Properties.Caption"] = new PropertyValue.Literal("I agree");
        control.Properties["EditValue"] = new PropertyValue.Literal(true);
        control.Properties["Properties.Appearance.Options.UseFont"] = new PropertyValue.Literal(true);
        control.Events.Add(new EventHandlerBinding("CheckedChanged", "agree_CheckedChanged", null));

        entry.Retype(control, entry.As!);

        Assert.Equal("CheckBox", control.ClrTypeName);
        Assert.Equal("CheckEdit", control.DeclaredClrTypeName);
        Assert.Equal(new PropertyValue.Literal("I agree"), control.Properties.Peek("Text"));
        Assert.Equal(new PropertyValue.Literal(true), control.Properties.Peek("Checked"));
        Assert.False(control.Properties.ContainsKey("Properties.Appearance.Options.UseFont"));
        Assert.Equal("CheckedChanged", Assert.Single(control.Events).EventName);
        Assert.Equal("Checked", control.VendorMembers!["Checked"]);
    }

    /// <summary>A value the vendor spells differently declines when it has no WinForms spelling, and is reported under the vendor's name.</summary>
    [Fact]
    public void Retype_AValueThatDoesNotConvert_KeepsItsVendorName()
    {
        Assert.True(VendorCatalog.BuiltIn.TryGet("Telerik.WinControls.UI.RadCheckBox", out var entry));
        var control = new ControlModel { FieldName = "agree", ClrTypeName = "RadCheckBox" };
        control.Properties["CheckState"] = new PropertyValue.EnumMembers(["Indeterminate"]);

        entry.Retype(control, entry.As!);

        Assert.True(control.Properties.ContainsKey("CheckState"));
        Assert.False(control.Properties.ContainsKey("Checked"));
    }
}
