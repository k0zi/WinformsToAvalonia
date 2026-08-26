using System.Diagnostics;
using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Starts a converted app for real, on Avalonia's headless platform.
/// </summary>
/// <remarks>
/// <para>
/// The project's invariant is that a generated project always builds <em>and runs</em>, and every
/// other test here only ever checked the first half. That gap is not hypothetical: a Windows-only
/// component built in a field initializer compiled perfectly and then took the whole app down on
/// Linux from the View's constructor, which Avalonia calls out of
/// <c>OnFrameworkInitializationCompleted</c> - before any window exists.
/// </para>
/// <para>
/// The generated <c>Program.cs</c> is replaced rather than used, because it calls
/// <c>UsePlatformDetect()</c> and would need a display. Everything that matters still runs: the
/// App is constructed, <c>OnFrameworkInitializationCompleted</c> builds the main View (and with it
/// every field, initializer and event subscription this converter emitted), the AXAML is parsed by
/// <c>InitializeComponent</c>, and a forced render tick drives one frame through the visual tree.
/// </para>
/// </remarks>
public class GeneratedAppStartupTests
{
    private const string SuccessMarker = "W2A-SMOKE-OK";

    /// <summary>Must match the Avalonia version AvaloniaProjectScaffolder writes into the csproj.</summary>
    private const string HeadlessPackageVersion = "12.1.1";

    [Theory]
    // The regression this test exists for: Windows-only components on a non-Windows host.
    [InlineData("ComponentFieldApp")]
    // A DispatcherTimer wired in the constructor, plus a second Window the first one opens.
    [InlineData("DialogContractApp")]
    // The broadest fixture: many controls, so the most AXAML to parse and lay out.
    [InlineData("ComplexApp")]
    public async Task ConvertedApp_StartsOnTheHeadlessPlatform(string sampleAppName)
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", sampleAppName, $"{sampleAppName}.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-smoke-" + Guid.NewGuid());
        try
        {
            new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            InjectHeadlessHarness(outputDir);

            var run = await RunDotnetAsync("run", outputDir);

            Assert.True(
                run.ExitCode == 0 && run.StdOut.Contains(SuccessMarker, StringComparison.Ordinal),
                $"The converted '{sampleAppName}' did not start (exit code {run.ExitCode}).\n"
                + $"--- stdout ---\n{run.StdOut}\n--- stderr ---\n{run.StdErr}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Swaps the generated entry point for one that boots the same <c>App</c> headlessly, and adds
    /// the package that provides it. Both are test-side edits: nothing here changes what the
    /// converter emits.
    /// </summary>
    private static void InjectHeadlessHarness(string outputDir)
    {
        var programPath = Path.Combine(outputDir, "Program.cs");
        var rootNamespace = Regex.Match(File.ReadAllText(programPath), @"namespace\s+([\w.]+)\s*;").Groups[1].Value;
        Assert.False(rootNamespace.Length == 0, "Could not read the generated root namespace from Program.cs.");

        File.WriteAllText(programPath, $$"""
            using Avalonia;
            using Avalonia.Controls;
            using Avalonia.Controls.ApplicationLifetimes;
            using Avalonia.Headless;

            namespace {{rootNamespace}};

            internal sealed class Program
            {
                [STAThread]
                public static int Main(string[] args)
                {
                    var lifetime = new ClassicDesktopStyleApplicationLifetime
                    {
                        Args = args,
                        ShutdownMode = ShutdownMode.OnExplicitShutdown,
                    };

                    // Runs OnFrameworkInitializationCompleted, which constructs the main View.
                    AppBuilder.Configure<App>()
                        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                        .SetupWithLifetime(lifetime);

                    // ...and one frame through the visual tree the AXAML just produced.
                    lifetime.MainWindow?.Show();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                    Console.WriteLine("{{SuccessMarker}}");
                    return 0;
                }
            }

            """);

        var csprojPath = Directory.GetFiles(outputDir, "*.csproj").Single();
        var csproj = File.ReadAllText(csprojPath);
        File.WriteAllText(
            csprojPath,
            ReplaceFirst(
                csproj,
                "  </ItemGroup>",
                $"""    <PackageReference Include="Avalonia.Headless" Version="{HeadlessPackageVersion}" />{Environment.NewLine}  </ItemGroup>"""));
    }

    private static string ReplaceFirst(string text, string search, string replacement)
    {
        var index = text.IndexOf(search, StringComparison.Ordinal);
        Assert.True(index >= 0, $"'{search}' not found in the generated csproj.");
        return text[..index] + replacement + text[(index + search.Length)..];
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

        // The harness returns without running a message loop, so it cannot hang on its own - but a
        // converted app that blocks during startup would, and a hung test is worse than a failing one.
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return (-1, await stdOutTask, "Timed out waiting for the converted app to start.");
        }

        return (process.ExitCode, await stdOutTask, await stdErrTask);
    }
}
