using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
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
    // The regression this test exists for: Windows-only components on a non-Windows host - and
    // now also a project-defined component whose source this run copied in, whose constructor
    // runs at startup like any other field initializer.
    [InlineData("ComponentFieldApp")]
    // A DispatcherTimer wired in the constructor, plus a second Window the first one opens -
    // and the close-confirmation rewrite, whose guard field and `async void` closing handler are
    // wired to the Window's own Closing event as it is constructed.
    [InlineData("DialogContractApp")]
    // The broadest fixture: many controls, so the most AXAML to parse and lay out.
    [InlineData("ComplexApp")]
    // Bundled fallback templates, including one that pulls in a package of its own.
    [InlineData("HandlerMigrationApp")]
    // An app-level TrayIcon: built by App.axaml during Initialize, before any View exists, and
    // reached from a handler through the accessor the generated App declares for it.
    [InlineData("TrayIconApp")]
    public Task ConvertedApp_StartsOnTheHeadlessPlatform(string sampleAppName) =>
        AssertStarts(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", sampleAppName, $"{sampleAppName}.csproj"),
            sampleAppName);

    /// <summary>
    /// The all-in-one sample, which is not a fixture at all - it is the app this repo ships to be
    /// looked at, an order of magnitude broader than any fixture here.
    /// </summary>
    /// <remarks>
    /// It earned its place: a converted handler that Avalonia raised <em>during</em> XAML
    /// population took the sample down at startup, and every fixture above passed - because none
    /// of them happened to have a TabControl with a handler on it. Fixture coverage is only ever
    /// what someone thought to write down; this one is the whole thing.
    /// </remarks>
    [Fact]
    public Task ConvertedAllInOneSample_StartsOnTheHeadlessPlatform() =>
        AssertStarts(
            Path.Combine(
                RepositoryRoot(), "samples", "WinForms", "All-In-One-WinForms", "All-In-One-WinForms.csproj"),
            "All-In-One-WinForms");

    private static async Task AssertStarts(string sourceProject, string name)
    {
        Assert.True(File.Exists(sourceProject), $"Source project not found: {sourceProject}");

        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-smoke-" + Guid.NewGuid());
        try
        {
            new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            InjectHeadlessHarness(outputDir);

            var run = await DotnetRunner.RunAsync("run", outputDir);

            Assert.True(
                run.ExitCode == 0 && run.StdOut.Contains(SuccessMarker, StringComparison.Ordinal),
                $"The converted '{name}' did not start (exit code {run.ExitCode}).\n"
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

    /// <summary>
    /// The repository root, walked up from the test output - the samples are source, not fixtures,
    /// so they are not copied beside the test assembly.
    /// </summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WinFormsToAvalonia.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "Could not find the repository root from the test output directory.");
        return directory!.FullName;
    }

    private static string ReplaceFirst(string text, string search, string replacement)
    {
        var index = text.IndexOf(search, StringComparison.Ordinal);
        Assert.True(index >= 0, $"'{search}' not found in the generated csproj.");
        return text[..index] + replacement + text[(index + search.Length)..];
    }

}
