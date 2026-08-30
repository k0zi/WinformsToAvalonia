using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Emission;

/// <summary>
/// A Form emitted as a <c>UserControl</c> - the shape <c>--with-web</c> needs, because Avalonia's
/// browser backend has no windowing platform and a Window cannot be instantiated there at all.
/// </summary>
/// <remarks>
/// The point of every test here is that <b>only the root changes</b>. The artifact is still a
/// Form, so it still takes its size from <c>ClientSize</c>, and everything inside the document is
/// emitted exactly as it always was.
/// </remarks>
public class ViewRootKindEmissionTests
{
    private static readonly string FixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DesignerCs");

    private static FormModel LoadForm()
    {
        var designerPath = Path.Combine(FixturesRoot, "DirectMappedTree.designer.cs");
        var walkResult = new DesignerSyntaxWalker().Walk(
            File.ReadAllText(designerPath), designerPath, "DirectMappedTreeForm", "Demo");

        return new ControlGraphBuilder().Build(walkResult);
    }

    private static string EmitAs(ViewRootKind? rootKind) =>
        new AxamlEmitter(new ControlMappingRegistry())
            .EmitView(
                LoadForm(), "Demo", "DirectMappedTreeView", "DirectMappedTreeViewModel",
                rootKind: rootKind)
            .Axaml;

    [Fact]
    public void EmitView_FormAsUserControl_RootsAtUserControlWithoutATitle()
    {
        var axaml = EmitAs(ViewRootKind.UserControl);

        Assert.StartsWith("<UserControl ", axaml);
        Assert.Contains("<UserControl.Styles>", axaml);
        Assert.DoesNotContain("Title=", axaml);
        Assert.DoesNotContain("<Window", axaml);
    }

    /// <summary>
    /// The size source follows the WinForms artifact, not the emitted element: a Form records
    /// ClientSize, and that is what its window was, whichever element now carries it.
    /// </summary>
    [Fact]
    public void EmitView_FormAsUserControl_StillTakesItsSizeFromClientSize()
    {
        var asWindow = EmitAs(null);
        var asUserControl = EmitAs(ViewRootKind.UserControl);

        Assert.Contains(@"Width=""230"" Height=""160""", asWindow);
        Assert.Contains(@"Width=""230"" Height=""160""", asUserControl);
    }

    /// <summary>Same document below the root - the split must not disturb the conversion.</summary>
    [Fact]
    public void EmitView_FormAsUserControl_EmitsTheSameBodyAsTheWindowRootedOne()
    {
        // Between the first Canvas and the root's own closing tag, which is the one line that is
        // allowed to differ.
        static string Body(string axaml) => axaml[
            axaml.IndexOf("<Canvas", StringComparison.Ordinal)
            ..axaml.LastIndexOf("</Canvas>", StringComparison.Ordinal)];

        Assert.Equal(Body(EmitAs(null)), Body(EmitAs(ViewRootKind.UserControl)));
    }

    [Fact]
    public void EmitView_NoRootKindGiven_IsExactlyWhatItAlwaysWas()
    {
        var emitter = new AxamlEmitter(new ControlMappingRegistry());
        var explicitWindow = emitter
            .EmitView(LoadForm(), "Demo", "DirectMappedTreeView", "DirectMappedTreeViewModel",
                rootKind: ViewRootKind.Window)
            .Axaml;

        Assert.Equal(EmitAs(null), explicitWindow);
    }

    [Fact]
    public void EmitViewCodeBehind_FormAsUserControl_DeclaresTheMatchingBaseType()
    {
        var emitter = new ViewCodeBehindEmitter();

        var asWindow = emitter.EmitViewCodeBehind(
            "Demo", "", "MainView", "MainViewModel", FormMigrationPlan.Empty, null);
        var asUserControl = emitter.EmitViewCodeBehind(
            "Demo", "", "MainView", "MainViewModel", FormMigrationPlan.Empty, null,
            rootKind: ViewRootKind.UserControl);

        // A base type disagreeing with the .axaml root is an AVLN2000 in the generated project,
        // which is the only place it would ever be noticed.
        Assert.Contains("public partial class MainView : Window", asWindow);
        Assert.Contains("public partial class MainView : UserControl", asUserControl);
    }
}
