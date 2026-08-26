using System.Diagnostics;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Non-visual components that are really plain .NET types, emitted as real fields. Everything
/// this touches is invisible to the tool's own build: the NuGet package has to resolve, the
/// generated `using`s have to be right, and the platform analyser has to stay quiet - so the
/// converted project's build is the only thing that proves any of it.
/// </summary>
public class ComponentFieldConversionTests
{
    [Fact]
    public async Task ConvertedComponentFieldApp_DeclaresWiresAndUsesTheComponents()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "ComponentFieldApp", "ComponentFieldApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-components-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind));

            // A real field of the same, unchanged .NET type.
            Assert.Contains("private readonly BackgroundWorker backgroundWorker1 = new();", codeBehind);
            Assert.Contains("private readonly Process process1 = new();", codeBehind);

            // Designer literals reproduced, and the events *subscribed* - which closes the old
            // "the handler is emitted but nothing subscribes it" gap for these components.
            Assert.Contains("backgroundWorker1.WorkerReportsProgress = true;", codeBehind);
            Assert.Contains("backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;", codeBehind);
            Assert.Contains("fileSystemWatcher1.Filter = \"*.txt\";", codeBehind);
            Assert.Contains("fileSystemWatcher1.Changed += fileSystemWatcher1_Changed;", codeBehind);

            // The handler is declared with the real .NET args type, not the "unknown" fallback.
            Assert.Contains("private void backgroundWorker1_ProgressChanged(object? sender, ProgressChangedEventArgs e)", codeBehind);
            Assert.Contains("progressBar1.Value = e.ProgressPercentage;", codeBehind);

            // Handler bodies can name the component now, nested paths included.
            Assert.Contains("backgroundWorker1.RunWorkerAsync();", codeBehind);
            Assert.Contains("process1.StartInfo.FileName = \"dotnet\";", codeBehind);
            Assert.Contains("fileSystemWatcher1.Path = Path.GetTempPath();", codeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(launchButton_Click)", codeBehind);

            // Evidence-driven: a component nothing wires and nothing names gets no field.
            Assert.DoesNotContain("unusedWatcher", codeBehind.Split("ORIGINAL WINFORMS CODE-BEHIND")[0]);

            // Windows-only: declared and used, the analyser silenced for this file only, and the
            // constraint reported rather than lost.
            Assert.Contains("#pragma warning disable CA1416", codeBehind);
            Assert.Contains("#pragma warning restore CA1416", codeBehind);
            Assert.Contains("eventLog1.WriteEntry(\"Component field demo\");", codeBehind);

            // ...and built lazily. The View's constructor runs before the first window exists, so
            // a `new EventLog()` there takes the whole app down at startup on Linux instead of
            // failing where the original code used it.
            Assert.Contains("private EventLog? _eventLog1;", codeBehind);
            var constructorBody = codeBehind
                .Split("public MainView()")[1]
                .Split("\n    private ")[0];
            Assert.DoesNotContain("EventLog", constructorBody);
            Assert.Contains(result.Report.Warnings, w => w.Contains("eventLog1") && w.Contains("Windows-only"));

            // The package has to be allowlisted in both places or the csproj silently drops it.
            var csprojPath = Assert.Single(result.Vfs.RelativePaths, p => p.EndsWith(".csproj", StringComparison.Ordinal));
            Assert.True(result.Vfs.TryGetText(csprojPath, out var csproj));
            Assert.Contains("System.Diagnostics.EventLog", csproj);

            var buildResult = await RunDotnetAsync("build", outputDir);
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

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunDotnetAsync(string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await stdOutTask, await stdErrTask);
    }
}
