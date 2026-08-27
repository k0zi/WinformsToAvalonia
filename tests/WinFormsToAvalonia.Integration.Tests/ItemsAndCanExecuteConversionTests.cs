using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Designer-declared list entries, and the WinForms "enable the button when the input is valid"
/// idiom turned into a real <c>CanExecute</c> guard.
/// </summary>
public class ItemsAndCanExecuteConversionTests
{
    [Fact]
    public async Task ConvertedItemsAndCanExecuteApp_EmitsItemsAndAGuardAndBuilds()
    {
        var sourceProject = Path.Combine(
            AppContext.BaseDirectory, "SampleApps", "ItemsAndCanExecuteApp", "ItemsAndCanExecuteApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-items-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml", out var axaml));
            Assert.True(result.Vfs.TryGetText("ViewModels/MainViewModel.cs", out var viewModel));

            // Designer-declared entries become real item elements, in order.
            Assert.Contains("<ComboBoxItem Content=\"Hardware\" />", axaml);
            Assert.Contains("<ComboBoxItem Content=\"Services\" />", axaml);
            Assert.Contains("<ListBoxItem Content=\"Urgent\" />", axaml);

            // The TextChanged handler is gone: it existed only to maintain the button's state.
            // (Its original source still appears in the preserved code-behind comment block -
            // what must be absent is a *generated* method and the subscription that called it.)
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));
            Assert.DoesNotContain("private void nameTextBox_TextChanged(object? sender", codeBehind);
            Assert.DoesNotContain("TextChanged=", axaml);

            // It became the command's guard, re-evaluated whenever the bound property changes.
            Assert.Contains("[RelayCommand(CanExecute = nameof(CanSubmitButton))]", viewModel);
            Assert.Contains("private bool CanSubmitButton() => NameTextBoxText.Length > 0;", viewModel);
            Assert.Contains("[NotifyCanExecuteChangedFor(nameof(SubmitButtonCommand))]", viewModel);

            // CanExecute owns the button's enabled state now - a second binding would fight it,
            // and would never be updated once the handler is gone.
            Assert.DoesNotContain("IsEnabled=\"{Binding", axaml);
            Assert.DoesNotContain("SubmitButtonIsEnabled", viewModel);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);
            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
            Assert.DoesNotContain(": warning ", buildResult.StdOut);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

}
