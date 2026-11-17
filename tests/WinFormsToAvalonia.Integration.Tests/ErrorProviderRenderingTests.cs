using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Core.Scaffolding;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// That a converted <c>errorProvider1.SetError(control, "...")</c> actually puts something on
/// the screen.
/// </summary>
/// <remarks>
/// <para>
/// Building proves nothing here and neither does booting: the bundled template used to store the
/// message on an attached property nobody read, so the generated code compiled, ran, reported
/// the call as successfully translated, and displayed <em>nothing</em>. It was the one place this
/// converter told the user something untrue.
/// </para>
/// <para>
/// So this boots the generated app headlessly, raises the Click that calls SetError, and looks
/// for the indicator in the window's <b>visual</b> tree - an adorner lives in the window's adorner
/// layer, not among the adorned control's logical children.
/// </para>
/// </remarks>
public class ErrorProviderRenderingTests
{
    private const string SuccessMarker = "W2A-ERRORPROVIDER-OK";

    /// <summary>Must match the Avalonia version AvaloniaProjectScaffolder writes into the csproj.</summary>
    private const string HeadlessPackageVersion = AvaloniaProjectScaffolder.AvaloniaVersion;

    [Fact]
    public async Task ConvertedSetError_PutsAnIndicatorOnScreenAndClearingItTakesItAway()
    {
        var sourceProject = Path.Combine(
            AppContext.BaseDirectory, "SampleApps", "HandlerMigrationApp", "HandlerMigrationApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-errorprovider-" + Guid.NewGuid());

        try
        {
            new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            InjectHarness(outputDir);

            var run = await DotnetRunner.RunAsync("run", outputDir);

            Assert.True(
                run.ExitCode == 0 && run.StdOut.Contains(SuccessMarker, StringComparison.Ordinal),
                $"The converted ErrorProvider did not render (exit code {run.ExitCode}).\n"
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

    private static void InjectHarness(string outputDir)
    {
        var programPath = Path.Combine(outputDir, "Program.cs");
        var rootNamespace = System.Text.RegularExpressions.Regex
            .Match(File.ReadAllText(programPath), @"namespace\s+([\w.]+)\s*;")
            .Groups[1].Value;

        File.WriteAllText(programPath, $$"""
            using Avalonia;
            using Avalonia.Controls;
            using Avalonia.Controls.ApplicationLifetimes;
            using Avalonia.Headless;
            using Avalonia.Interactivity;
            using Avalonia.VisualTree;
            using {{rootNamespace}}.Controls;

            namespace {{rootNamespace}};

            internal sealed class Program
            {
                private const string Message = "A name is required.";

                [STAThread]
                public static int Main(string[] args)
                {
                    var lifetime = new ClassicDesktopStyleApplicationLifetime
                    {
                        Args = args,
                        ShutdownMode = ShutdownMode.OnExplicitShutdown,
                    };

                    AppBuilder.Configure<App>()
                        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                        .SetupWithLifetime(lifetime);

                    var window = lifetime.MainWindow!;
                    window.Show();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                    // The handler that calls errorProvider1.SetError(nameTextBox, Message).
                    var flagButton = window.GetVisualDescendants().OfType<Button>()
                        .First(b => b.Name == "flagButton");
                    flagButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                    if (!HasIndicator(window))
                    {
                        Console.Error.WriteLine(
                            "SetError produced no visible indicator. The attached property was set and "
                            + "nothing rendered it - which is exactly the bug this test exists for.");
                        return 1;
                    }

                    // ...and WinForms clears an error by setting an empty message.
                    var textBox = window.GetVisualDescendants().OfType<TextBox>()
                        .First(t => t.Name == "nameTextBox");
                    ErrorProviderFallback.SetError(textBox, string.Empty);
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                    if (HasIndicator(window))
                    {
                        Console.Error.WriteLine("Clearing the error left its indicator on screen.");
                        return 1;
                    }

                    Console.WriteLine("{{SuccessMarker}}");
                    return 0;
                }

                /// <summary>The indicator is an adorner, so it is in the window's visual tree.</summary>
                private static bool HasIndicator(Window window) =>
                    window.GetVisualDescendants()
                        .OfType<Control>()
                        .Any(c => ToolTip.GetTip(c) as string == Message);
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
}
