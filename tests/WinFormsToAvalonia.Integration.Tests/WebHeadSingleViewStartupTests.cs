using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Core.Scaffolding;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// The other half of "the web head works": that the main View really can stand on its own as a
/// <b>view</b> rather than a window, which is the only thing Avalonia's browser backend can show.
/// </summary>
/// <remarks>
/// <para>
/// The type side of this is already settled by the build: <c>App.axaml.cs</c>'s single-view
/// branch assigns the main View to <c>ISingleViewApplicationLifetime.MainView</c>, so the shared
/// library only compiles if that View is a <c>Control</c> and not a <c>Window</c> - see
/// <see cref="WebHeadConversionBuildTests"/>. What the build cannot say is whether the thing
/// then constructs and renders with no Window of its own, and that is what this runs.
/// </para>
/// <para>
/// On Avalonia's headless platform rather than in a browser: the WebAssembly runtime is not what
/// is being tested, and a browser cannot be driven from here anyway. The lifetime itself is not
/// faked - Avalonia 12 makes <c>ISingleViewApplicationLifetime</c> unimplementable outside the
/// framework - so the harness does what the browser backend would do with the result instead.
/// </para>
/// </remarks>
public class WebHeadSingleViewStartupTests
{
    private static readonly string SampleAppsRoot = Path.Combine(AppContext.BaseDirectory, "SampleApps");

    private const string SuccessMarker = "W2A-SINGLEVIEW-OK";

    /// <summary>Must match the Avalonia version AvaloniaProjectScaffolder writes into the csproj.</summary>
    private const string HeadlessPackageVersion = AvaloniaProjectScaffolder.AvaloniaVersion;

    [Theory]
    [InlineData("ModernNetApp", "ModernNetApp.csproj", "MainView")]
    // The one whose main View reaches for a Window it no longer is - so its constructor, its
    // field initializers and its AXAML all have to survive having no Window above them.
    [InlineData("DialogContractApp", "DialogContractApp.csproj", "MainView")]
    public async Task ConvertedWithWeb_MainViewRendersWithoutAWindowOfItsOwn(
        string appFolder, string csprojName, string mainViewClassName)
    {
        var sourceProject = Path.Combine(SampleAppsRoot, appFolder, csprojName);
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-singleview-" + Guid.NewGuid());
        var projectName = Path.GetFileNameWithoutExtension(outputDir).Replace("-", "_");

        try
        {
            new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir, WithWeb: true));

            var headDir = Path.Combine(outputDir, $"{projectName}.Desktop");
            InjectSingleViewHarness(headDir, projectName, mainViewClassName);

            var run = await DotnetRunner.RunAsync("run", headDir);

            Assert.True(
                run.ExitCode == 0 && run.StdOut.Contains(SuccessMarker, StringComparison.Ordinal),
                $"The converted '{appFolder}' main view did not render on its own "
                + $"(exit code {run.ExitCode}).\n"
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
    /// Replaces the desktop head's entry point with one that constructs the main View the way a
    /// single-view host would - as a plain Control with nothing above it but the page.
    /// </summary>
    private static void InjectSingleViewHarness(
        string headDirectory, string projectName, string mainViewClassName)
    {
        File.WriteAllText(Path.Combine(headDirectory, "Program.cs"), $$"""
            using Avalonia;
            using Avalonia.Controls;
            using Avalonia.Controls.Primitives;
            using Avalonia.Headless;
            using Avalonia.LogicalTree;
            using {{projectName}};
            using {{projectName}}.Views;

            namespace {{projectName}}.Desktop;

            internal sealed class Program
            {
                [STAThread]
                public static int Main(string[] args)
                {
                    // Initializes App - styles, the ViewLocator, App.axaml itself - without
                    // starting any lifetime, which is all a single-view host does before it
                    // hands the view to the page.
                    AppBuilder.Configure<App>()
                        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                        .SetupWithoutStarting();

                    // The claim --with-web rests on: a browser has no Window, so the main View
                    // must not be one. If it were, App.axaml.cs would not have compiled - this
                    // says so out loud anyway, because it is the reason for everything else.
                    if (typeof({{mainViewClassName}}).IsSubclassOf(typeof(Window)))
                    {
                        Console.Error.WriteLine(
                            "{{mainViewClassName}} is a Window, which a browser cannot instantiate at all.");
                        return 1;
                    }

                    Control view = new {{mainViewClassName}}();

                    // A view still needs a top level to lay out in; in the browser that is the
                    // page, and here the only one a headless platform has.
                    var host = new Window { Content = view };
                    host.Show();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                    var untemplated = Descendants(view)
                        .OfType<TemplatedControl>()
                        .Where(c => c.IsVisible && c.Template is null)
                        .Select(c => c.GetType().Name)
                        .Distinct()
                        .ToList();

                    if (untemplated.Count > 0)
                    {
                        Console.Error.WriteLine(
                            $"These controls got no template: {string.Join(", ", untemplated)}. "
                            + "See AvaloniaProjectScaffolder.PackageStyleIncludes.");
                        return 1;
                    }

                    Console.WriteLine("{{SuccessMarker}}");
                    return 0;
                }

                private static IEnumerable<Control> Descendants(Control root)
                {
                    yield return root;
                    foreach (var child in root.GetLogicalChildren().OfType<Control>())
                    {
                        foreach (var descendant in Descendants(child))
                        {
                            yield return descendant;
                        }
                    }
                }
            }
            """);

        var csprojPath = Path.Combine(headDirectory, $"{projectName}.Desktop.csproj");
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
