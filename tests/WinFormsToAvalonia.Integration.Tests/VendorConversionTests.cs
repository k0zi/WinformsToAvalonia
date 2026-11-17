using WinFormsToAvalonia.Core.Mapping.Vendors;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// DevExpress and Telerik, converted with no mappings file. The build and startup rows prove the
/// output compiles and runs; these say what it is made of.
/// </summary>
public class VendorConversionTests
{
    private static ConversionRunResult Convert(string app) =>
        new ConversionPipeline().Run(new ConversionOptions(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", app, $"{app}.csproj"),
            Path.Combine(Path.GetTempPath(), "w2a-vendor-" + Guid.NewGuid(), app)) { DryRun = true });

    private static string Text(ConversionRunResult result, string path)
    {
        Assert.True(result.Vfs.TryGetText(path, out var text), $"'{path}' was not generated");
        return text;
    }

    [Fact]
    public void DevExpress_FormsOnVendorBases_AreConverted()
    {
        var result = Convert("DevExpressApp");

        // XtraForm, RibbonForm, XtraUserControl.
        Assert.Equal(2, result.Report.FormCount);
        Assert.Equal(1, result.Report.UserControlCount);
        Assert.Contains("Views/ContactCardView.axaml", result.Vfs.RelativePaths);
        Assert.Equal(0, result.Report.UnsupportedControlCount);
    }

    [Fact]
    public void DevExpress_GridView_FoldsIntoTheGrid()
    {
        var axaml = Text(Convert("DevExpressApp"), "Views/MainView.axaml");

        // VisibleIndex order (Name, Active, Age), the check editor's column as a check column, the
        // hidden Id column left out, and Editable = false as IsReadOnly.
        var name = axaml.IndexOf("Header=\"Name\"", StringComparison.Ordinal);
        var active = axaml.IndexOf("<DataGridCheckBoxColumn Header=\"Active\"", StringComparison.Ordinal);
        var age = axaml.IndexOf("Header=\"Age\"", StringComparison.Ordinal);
        Assert.True(name >= 0 && name < active && active < age, axaml);
        Assert.DoesNotContain("Header=\"Id\"", axaml);
        Assert.Contains("x:Name=\"peopleGrid\"", axaml);
        Assert.Contains("IsReadOnly=\"True\"", axaml);
    }

    [Fact]
    public void DevExpress_Bars_BecomeAMenuAToolbarAndAStatusBar()
    {
        var axaml = Text(Convert("DevExpressApp"), "Views/MainView.axaml");

        Assert.Contains("<Menu x:Name=\"menuBar\" Canvas.Left=\"0\" Canvas.Top=\"0\"", axaml);
        Assert.Contains("<MenuItem x:Name=\"fileSubItem\" Header=\"_File\">", axaml);
        Assert.Contains("<controls:ToolStripFallback x:Name=\"toolsBar\"", axaml);
        Assert.Contains("<controls:StatusStripFallback x:Name=\"statusBar\" Canvas.Left=\"0\" Canvas.Top=\"458\"", axaml);

        // Linked into the File menu and the toolbar: both run the handler, one carries the name.
        Assert.Contains("<MenuItem x:Name=\"openItem\" Header=\"Open\" Click=\"openItem_ItemClick\" />", axaml);
        Assert.Contains("<Button Content=\"Open\" Click=\"openItem_ItemClick\" />", axaml);
        Assert.DoesNotContain("barDockControl", axaml);
    }

    [Fact]
    public void DevExpress_RibbonLayoutAndRadioGroup_BecomeMenusLabelsAndRadioButtons()
    {
        var result = Convert("DevExpressApp");
        var details = Text(result, "Views/DetailsView.axaml");
        Assert.Contains("<MenuItem x:Name=\"homePage\" Header=\"Home\">", details);
        Assert.Contains("<MenuItem x:Name=\"saveItem\" Header=\"Save\"", details);
        Assert.Contains("<TextBlock x:Name=\"cityItem\"", details);
        Assert.Contains("Text=\"City\"", details);
        Assert.Contains("<Grid x:Name=\"split\"", details);

        var main = Text(result, "Views/MainView.axaml");
        Assert.Contains("Content=\"Medium\" IsChecked=\"True\"", main);
    }

    /// <summary>The vendor member names a body uses are read as the WinForms ones they are proved to be.</summary>
    [Fact]
    public void DevExpress_HandlerBodies_TranslateThroughTheVendorsMemberNames()
    {
        var result = Convert("DevExpressApp");
        var view = Text(result, "Views/MainView.axaml.cs").Split("ORIGINAL WINFORMS MEMBERS")[0];

        Assert.Contains("statusItem.Text = \"Bye\";", view);
        Assert.Contains("progress.Value = (nameEdit.Text ?? string.Empty).Length;", view);
        Assert.Contains("(darkToggle.IsChecked ?? false) ? \"Dark\" : \"Light\"", view);
        Assert.Contains("await MessageBoxFallback.ShowYesNoAsync(this, \"Save the changes?\", \"Confirm\")", view);
        Assert.Equal(result.Report.HandlerStatementCount, result.Report.MigratedStatementCount);
    }

    /// <summary>The suite is on nuget.org and hundreds of megabytes - refused on sight, never downloaded.</summary>
    [Fact]
    public void DevExpress_PackageReference_IsRefusedWithoutADownload()
    {
        Assert.Contains(
            Convert("DevExpressApp").Report.Warnings,
            w => w.Contains("'DevExpress.Win'", StringComparison.Ordinal) && w.Contains("WinForms control suite", StringComparison.Ordinal));
    }

    [Fact]
    public void Telerik_FormsLocalsAndStructures_AreConverted()
    {
        var result = Convert("TelerikApp");
        Assert.Equal(2, result.Report.FormCount);

        var main = Text(result, "Views/MainView.axaml");

        // Grid columns are designer locals: they come across, the hidden one does not, none is named.
        Assert.Contains("<DataGridTextColumn Header=\"Customer\" Binding=\"{ReflectionBinding Customer}\" />", main);
        Assert.Contains("<DataGridCheckBoxColumn Header=\"Paid\"", main);
        Assert.DoesNotContain("Total", main);
        Assert.DoesNotContain("@MainForm", main);

        // List items are designer locals too, read for their text.
        Assert.Contains("<ComboBoxItem Content=\"Red\" />", main);

        // The command bar's rows and strips fold away; its items are the toolbar's.
        Assert.Contains("<controls:ToolStripFallback x:Name=\"radCommandBar1\"", main);
        Assert.Contains("<Button x:Name=\"helpButton\" Content=\"Help\" />", main);
        Assert.DoesNotContain("commandBarStripElement1", main);

        // ToggleState/CheckState is Checked.
        Assert.Contains("x:Name=\"fastRadio\" Canvas.Left=\"10\" Canvas.Top=\"24\" Width=\"80\" Height=\"18\" Content=\"Fast\" IsChecked=\"True\"", main);

        var editor = Text(result, "Views/EditorView.axaml");
        Assert.Contains("<Grid x:Name=\"radSplitContainer1\"", editor);
        Assert.Contains("<TextBlock x:Name=\"layoutControlItem1\" Canvas.Left=\"0\"", editor);
        Assert.Contains("<MenuItem x:Name=\"copyButton\" Header=\"Copy\"", editor);

        Assert.Contains(result.Report.Warnings, w => w.Contains("element tree", StringComparison.Ordinal));

        // A nested type deriving from a Telerik type compiles in no generated project - refused, said.
        Assert.DoesNotContain("Models/ReorderBehavior.cs", result.Vfs.RelativePaths);
        Assert.Contains(result.Report.Warnings, w => w.Contains("'ReorderBehavior'", StringComparison.Ordinal)
            && w.Contains("does not compile without the WinForms and vendor assemblies", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Report.Warnings, w => w.Contains("not emitted either", StringComparison.Ordinal));
    }

    [Fact]
    public void Telerik_HandlerBodies_TranslateThroughTheVendorsMemberNames()
    {
        var result = Convert("TelerikApp");
        Assert.Equal(result.Report.HandlerStatementCount, result.Report.MigratedStatementCount);
        Assert.Contains("MessageBoxFallback.ShowAsync", Text(result, "Views/MainView.axaml.cs"));
    }

    /// <summary>
    /// The guarantee a WinForms project rests on: with no vendor control in it, the walk is exactly
    /// what it was before vendors were understood - every fixture, property for property.
    /// </summary>
    [Fact]
    public void EveryWinFormsFixture_WalksTheSameWithTheVendorCatalog()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "SampleApps");
        var walker = new DesignerSyntaxWalker();
        var walked = 0;
        foreach (var designer in Directory.EnumerateFiles(root, "*.Designer.cs", SearchOption.AllDirectories)
                     // The three fixtures that do contain vendor controls - ThirdPartyApp's are mapped by
                     // a --mappings file, which wins over the catalog (ThirdPartyMappingsConversionTests).
                     .Where(f => !new[] { "DevExpressApp", "TelerikApp", "ThirdPartyApp" }.Any(v => f.Contains(v, StringComparison.Ordinal)))
                     .Order(StringComparer.Ordinal))
        {
            var text = File.ReadAllText(designer);
            var className = Path.GetFileName(designer)[..^".Designer.cs".Length];

            var plain = walker.Walk(text, designer, className, null);
            var withVendors = walker.Walk(text, designer, className, null, vendors: VendorCatalog.BuiltIn);

            Assert.Equal(Describe(plain), Describe(withVendors));
            walked++;
        }

        Assert.True(walked > 50, $"Only {walked} designer files were found under {root}.");
    }

    private static string Describe(DesignerWalkResult walk) => string.Join(
        "\n",
        walk.Form.Controls.Values.OrderBy(c => c.FieldName, StringComparer.Ordinal)
            .Select(c => $"{c.FieldName}:{c.ClrTypeName}:{c.VendorTypeName}:"
                + string.Join(",", c.Properties.Keys.Order(StringComparer.Ordinal).Select(k => $"{k}={c.Properties.Peek(k)}"))
                + ":" + string.Join(",", c.LiteralItems)
                + ":" + string.Join(",", c.Events.Select(e => $"{e.EventName}={e.HandlerMethodName}")))
            .Concat(walk.Form.FormProperties.Keys.Order(StringComparer.Ordinal).Select(k => $"form.{k}={walk.Form.FormProperties.Peek(k)}"))
            .Concat(walk.Edges.Select(e => $"{e.ParentFieldName}->{e.ChildFieldName}"))
            .Concat(walk.Warnings)
            .Concat(walk.VendorLinks.Select(l => l.ToString())));
}
