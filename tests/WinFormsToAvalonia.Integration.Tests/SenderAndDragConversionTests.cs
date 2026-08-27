using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Three translations whose Avalonia side is nothing like a rename: `sender` resolved through
/// the mapping rather than cast, the clipboard reached through the TopLevel, and a drag payload
/// asked with Avalonia 12's reworked data-transfer API. Only a build proves any of them - the
/// tool's own compilation never sees these types.
/// </summary>
public class SenderAndDragConversionTests
{
    [Fact]
    public async Task ConvertedSenderAndDragApp_ResolvesSenderAndTheDragPayloadAndStillBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "SenderAndDragApp", "SenderAndDragApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-sender-drag-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

            // `sender` in a single-control handler provably *is* that control, so the local
            // becomes another name for its field and the cast disappears entirely.
            Assert.Contains(
                """
                    private void renameButton_Click(object? sender, RoutedEventArgs e)
                    {
                        renameButton.Content = "Renamed";
                        renameButton.IsEnabled = false;
                    }
                """,
                codeBehind.Replace("\r\n", "\n"));

            // Two controls share this one, so there is no single answer for what `sender` is.
            Assert.Contains("MigrationTodo.NotMigrated(nameof(sharedClick)", codeBehind);

            // The clipboard hangs off the TopLevel and is async, so the handler turns async -
            // and the handler must stay in code-behind for that to be possible at all.
            Assert.Contains("private async void copyButton_Click", codeBehind);
            Assert.Contains("await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(", codeBehind);

            // Avalonia 12 renamed the property, changed its type and replaced the format
            // constants - so the whole shape is translated at once or not at all.
            Assert.Contains("e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;", codeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(dropPanel_DragEnter)", codeBehind);

            // Reading the payload is a change of shape, not of spelling.
            Assert.Contains("MigrationTodo.NotMigrated(nameof(dropPanel_DragDrop)", codeBehind);

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
