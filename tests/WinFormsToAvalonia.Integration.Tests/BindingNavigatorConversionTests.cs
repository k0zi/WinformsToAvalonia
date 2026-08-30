using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// What a <c>BindingNavigator</c> bound to a <c>BindingSource</c> becomes.
/// </summary>
/// <remarks>
/// <c>bindingNavigator1.BindingSource = this.bindingSource1;</c> used to be dropped without a
/// word: a fallback mapper emits the property mappings it declares and silently ignores the rest,
/// so the navigator rendered its buttons and navigated nothing. The runtime half - that clicking
/// those buttons really moves the grid - is <c>GeneratedAppStartupTests</c>' job; this pins the
/// text, which is where the reasoning is visible.
/// </remarks>
public class BindingNavigatorConversionTests
{
    [Fact]
    public void ConvertedBindingNavigatorApp_SharesOnePositionBetweenTheNavigatorAndTheGrid()
    {
        var sourceProject = Path.Combine(
            AppContext.BaseDirectory, "SampleApps", "BindingNavigatorApp", "BindingNavigatorApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-navigator-" + Guid.NewGuid());

        var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir, DryRun: true));

        Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));
        Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));
        Assert.True(result.Vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel));

        // BindingSource.Position was one number two controls showed, so it becomes one ViewModel
        // property that both bind to. Moving either moves the other, which is what the pair did.
        Assert.Contains(
            "Position=\"{Binding BindingNavigator1Position, Mode=TwoWay}\"", axaml);
        Assert.Contains(
            "SelectedIndex=\"{Binding BindingNavigator1Position, Mode=TwoWay}\"", axaml);
        Assert.Contains("public partial int BindingNavigator1Position { get; set; }", viewModel);

        // Count is a path into the collection itself - no ViewModel property of its own, and
        // ObservableCollection raises PropertyChanged for it, so it follows the rows.
        Assert.Contains("Count=\"{Binding TracksGridItems.Count}\"", axaml);

        // The designer records which button had which role, so nothing is inferred from a name or
        // a caption. The clamping lives in the bundled template, not in the generated code.
        Assert.Contains(
            "bindingNavigatorMoveFirstItem.Click += (_, _) => bindingNavigator1.MoveFirst();", codeBehind);
        Assert.Contains(
            "bindingNavigatorMovePreviousItem.Click += (_, _) => bindingNavigator1.MovePrevious();", codeBehind);
        Assert.Contains(
            "bindingNavigatorMoveNextItem.Click += (_, _) => bindingNavigator1.MoveNext();", codeBehind);
        Assert.Contains(
            "bindingNavigatorMoveLastItem.Click += (_, _) => bindingNavigator1.MoveLast();", codeBehind);

        // AddNewItem is emitted as a button and left unwired: adding a row means constructing the
        // element type, which a navigator knows nothing about. Reported rather than guessed at.
        Assert.DoesNotContain("bindingNavigatorAddNewItem.Click +=", codeBehind);
        Assert.Contains(
            result.Report.Warnings,
            w => w.Contains("AddNewItem", StringComparison.Ordinal)
                && w.Contains("not wired", StringComparison.Ordinal));
    }
}
