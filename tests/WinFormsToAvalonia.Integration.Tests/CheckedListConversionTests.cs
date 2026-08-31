using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// What a <c>CheckedListBox</c> becomes.
/// </summary>
/// <remarks>
/// The tick used to be approximated by <c>SelectionMode="Multiple"</c> - defensible only while it
/// had nowhere else to live. Avalonia has no checkbox list, but it has an <c>ItemTemplate</c>, so
/// the tick now has a row object of its own. The runtime half - that the box renders and a handler
/// can move it - is <c>GeneratedAppStartupTests</c>' job.
/// </remarks>
public class CheckedListConversionTests
{
    [Fact]
    public void ConvertedCheckedListApp_GivesEveryRowItsOwnTick()
    {
        var sourceProject = Path.Combine(
            AppContext.BaseDirectory, "SampleApps", "CheckedListApp", "CheckedListApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-checkedlist-" + Guid.NewGuid());

        var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir, DryRun: true));

        Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));
        Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));
        Assert.True(result.Vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel));

        var modelPath = Assert.Single(result.Vfs.RelativePaths, p => p.StartsWith("Models/", StringComparison.Ordinal));
        Assert.True(result.Vfs.TryGetText(modelPath, out var model));

        // The element stays a ListBox; the tick moves into a template.
        Assert.Contains("<ListBox x:Name=\"optionsList\"", axaml);
        Assert.Contains("ItemsSource=\"{Binding OptionsListItems}\"", axaml);
        Assert.Contains("<CheckBox IsChecked=\"{Binding IsChecked, Mode=TwoWay}\" Content=\"{Binding Text}\" />", axaml);

        // The approximation is gone: WinForms tracked checked and selected separately, and so does
        // this now, so a converted list selects the way the original did.
        Assert.DoesNotContain("SelectionMode", axaml);

        // Designer items become the collection's contents, not literal item elements - a templated
        // ListBox binds its rows rather than hosting them.
        Assert.DoesNotContain("<ListBoxItem", axaml);
        Assert.Contains("new() { Text = \"Logging\" },", viewModel);
        Assert.Contains("ObservableCollection<OptionsListItem> OptionsListItems", viewModel);

        // An ObservableObject, and that is load-bearing rather than idiom: a handler writing the
        // tick from code has to move the box on screen, which a plain POCO would not.
        Assert.Equal("Models/OptionsListItem.cs", modelPath);
        Assert.Contains(": ObservableObject", model);
        Assert.Contains("public partial bool IsChecked { get; set; }", model);

        // SetItemChecked named an index and a bool, and so does the translation.
        Assert.Contains("w2aViewModel.OptionsListItems[1].IsChecked = true;", codeBehind);
        Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(tickButton_Click)", codeBehind);
    }
}
