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


    /// <summary>
    /// Injected only for the fixture that has a paint surface, because it names the generated
    /// template type - which does not exist in an app that has none.
    /// </summary>
    /// <remarks>
    /// This is the one thing building cannot tell you. Avalonia draws by calling
    /// <c>Render(DrawingContext)</c> during the render pass; if that never reached the surface -
    /// a zero-sized control, a visual never attached - the handler would simply not run, the app
    /// would start perfectly, and the drawing would be absent. Exactly the "compiles, starts,
    /// renders as nothing" shape this suite exists for.
    /// </remarks>
    private const string PaintReportSource = """
                    private static void ReportPaintSurfaces(Window? window)
                    {
                        if (window is null)
                        {
                            return;
                        }

                        foreach (var surface in Descendants(window).OfType<__ROOT__.Controls.PaintSurfaceFallback>())
                        {
                            var painted = 0;
                            surface.Paint += (_, _) => painted++;

                            window.UpdateLayout();
                            surface.InvalidateVisual();

                            // Renders a real frame, which is the only thing that makes Avalonia
                            // walk the tree calling Render on each visual.
                            using (window.CaptureRenderedFrame())
                            {
                            }

                            Console.WriteLine($"w2a-paint:{surface.Name}:{painted}");
                        }
                    }
        """;


    /// <summary>
    /// Injected only for the printing fixture, because it names the generated document type.
    /// </summary>
    /// <remarks>
    /// Proves the half that has no other witness: <c>RenderFirstPage</c> creates a
    /// <c>RenderTargetBitmap</c>, opens a drawing context and raises <c>PrintPage</c> on it. If the
    /// page size were wrong, the context unavailable, or the handler never subscribed, the app
    /// would still start and still build - and no page would ever be drawn.
    /// </remarks>
    private const string PrintReportSource = """
                    private static void ReportPrintDocuments(Window? window)
                    {
                        if (window is null)
                        {
                            return;
                        }

                        foreach (var field in window.GetType().GetFields(
                                     BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                        {
                            if (field.GetValue(window) is not __ROOT__.Controls.PrintDocumentFallback document)
                            {
                                continue;
                            }

                            var drawn = 0;
                            document.PrintPage += (_, _) => drawn++;

                            using var page = document.RenderFirstPage();
                            Console.WriteLine($"w2a-print:{document.DocumentName}:{drawn}:{page.PixelSize.Width}x{page.PixelSize.Height}");
                        }
                    }
        """;

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
    // Images extracted out of an ImageList and referenced from the AXAML by asset path. Building
    // proves nothing here: an asset the conversion named but never wrote is not a compile error,
    // it is Avalonia failing to load the Image while the View is being constructed.
    [InlineData("ImageListApp")]
    // Two controls Avalonia 12 added that this run maps directly rather than falling back to a
    // bundled template. Building says nothing about either: a control whose theme the app never
    // included gets no template and renders as *nothing* - which is exactly what the walk below
    // checks, and the only reason to trust the promotion.
    [InlineData("GroupBoxApp")]
    // Literal items inside a bundled template's collection property, as <sys:String> elements.
    // That form compiles and can still fail to *load* - the item type is resolved at run time.
    [InlineData("DomainUpDownApp")]
    public Task ConvertedApp_StartsOnTheHeadlessPlatform(string sampleAppName) =>
        AssertStarts(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", sampleAppName, $"{sampleAppName}.csproj"),
            sampleAppName,
            clickButtons: true);

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
    /// <remarks>
    /// <para>
    /// Startup only - this is the one app whose buttons are not clicked, and that is a real gap
    /// rather than an oversight. Its handlers open a serial port and write to the OS event log:
    /// dependencies the conversion neither introduced nor can remove, whose absence on a test
    /// machine says nothing about whether the conversion was right. Clicking here would trade a
    /// sharp assertion for a growing list of exceptions to forgive.
    /// </para>
    /// <para>
    /// The fixtures are where a handler body is exercised, and they are hand-written to reach
    /// nothing but themselves.
    /// </para>
    /// </remarks>
    [Fact]
    public Task ConvertedAllInOneSample_StartsOnTheHeadlessPlatform() =>
        AssertStarts(
            Path.Combine(
                RepositoryRoot(), "samples", "WinForms", "All-In-One-WinForms", "All-In-One-WinForms.csproj"),
            "All-In-One-WinForms",
            clickButtons: false,
            reportPaint: false,
            reportPrint: false,
            // Row counts only: both grids live on TabItems that are never selected, so nothing
            // under them is ever laid out and no cell is realized. The counts are what matters
            // here anyway - they prove MainForm_Load really ran and really populated both
            // collections. The cell text is proved by DataGridViewColumnsApp, whose grids are at
            // the root.
            "w2a-grid:dataGridView1:2:",
            "w2a-grid:itemsListView:2:");

    /// <summary>
    /// The grids: a fixture whose DataGrids sit at the root, so their cells really lay out.
    /// </summary>
    /// <remarks>
    /// Clicked, because the handler that fills both is a Click handler. The all-in-one sample
    /// proves the row *counts* but not the cell text - its grids live on TabItems that are never
    /// selected, so nothing under them is ever laid out.
    /// </remarks>
    [Fact]
    public Task ConvertedDataGridViewColumnsApp_ShowsTheRowsItsHandlerAdded() =>
        AssertStarts(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", "DataGridViewColumnsApp", "DataGridViewColumnsApp.csproj"),
            "DataGridViewColumnsApp",
            clickButtons: true,
            reportPaint: false,
            reportPrint: false,
            "w2a-grid:detailsListView:1:notes.txt");

    /// <summary>
    /// A BindingNavigator wired to a BindingSource: the buttons move the grid's selection.
    /// </summary>
    /// <remarks>
    /// The harness clicks every button in tree order - MoveFirst, MovePrevious, MoveNext,
    /// MoveLast - so the position ends on the last row. That is the assertion: three rows loaded
    /// by the Load handler, and a selection the navigator moved to the end of them.
    /// </remarks>
    [Fact]
    public Task ConvertedBindingNavigatorApp_NavigatesTheBoundGrid() =>
        AssertStarts(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", "BindingNavigatorApp", "BindingNavigatorApp.csproj"),
            "BindingNavigatorApp",
            clickButtons: true,
            reportPaint: false,
            reportPrint: false,
            "w2a-grid:tracksGrid:3:First selected=2");

    /// <summary>
    /// A Paint handler on the bundled surface: the drawing really reaches Avalonia.
    /// </summary>
    /// <remarks>
    /// Its own test rather than a row above, because the assertion needs the harness to name the
    /// generated template type - and because "the handler ran" is the whole claim. The count is 1:
    /// the report subscribes, invalidates, and forces exactly one render pass.
    /// </remarks>
    [Fact]
    public Task ConvertedCodeBehindMigrationApp_ReallyRendersItsPaintHandler() =>
        AssertStarts(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", "CodeBehindMigrationApp", "CodeBehindMigrationApp.csproj"),
            "CodeBehindMigrationApp",
            clickButtons: false,
            reportPaint: true,
            reportPrint: false,
            "w2a-paint:canvasPanel:1");

    /// <summary>
    /// A converted CheckedListBox: the tick renders, and a handler can move it.
    /// </summary>
    /// <remarks>
    /// The button is clicked, so <c>SetItemChecked(1, true)</c> really runs. Both halves matter:
    /// "Logging" proves the ItemTemplate realized a CheckBox at all, and "Telemetry" being True
    /// proves the row raised a change notification the binding heard - a plain POCO would render
    /// identically and never move.
    /// </remarks>
    [Fact]
    public Task ConvertedCheckedListApp_RendersAndMovesItsTicks() =>
        AssertStarts(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", "CheckedListApp", "CheckedListApp.csproj"),
            "CheckedListApp",
            clickButtons: true,
            reportPaint: false,
            reportPrint: false,
            "w2a-check:Logging:False",
            "w2a-check:Telemetry:True");

    /// <summary>
    /// A converted ToolStripContainer: both nested regions really hold their controls.
    /// </summary>
    /// <remarks>
    /// The regions are filled through XAML property-element syntax onto settable properties, and
    /// the setter rebuilds the container's children. Both happen at load time - so building says
    /// nothing here, and an app whose controls silently vanished would still start.
    /// </remarks>
    [Fact]
    public Task ConvertedToolStripContainerApp_PutsItsControlsInTheRightRegions() =>
        AssertStarts(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", "ToolStripContainerApp", "ToolStripContainerApp.csproj"),
            "ToolStripContainerApp",
            clickButtons: false,
            reportPaint: false,
            reportPrint: false,
            "w2a-region:toolStripContainer1:contentLabel,dockedStrip");

    /// <summary>
    /// A converted PrintDocument: the page is really drawn.
    /// </summary>
    /// <remarks>
    /// The document is not in the visual tree and nothing on screen shows it, so this is the only
    /// way to see whether it works. Two counts: the handler ran once, and the page came out the
    /// size the document says it is.
    /// </remarks>
    [Fact]
    public Task ConvertedPrintingApp_ReallyDrawsItsPage() =>
        AssertStarts(
            Path.Combine(AppContext.BaseDirectory, "SampleApps", "PrintingApp", "PrintingApp.csproj"),
            "PrintingApp",
            clickButtons: false,
            reportPaint: false,
            reportPrint: true,
            "w2a-print:Sample report:1:816x1056");

    private static async Task AssertStarts(
        string sourceProject, string name, bool clickButtons, bool reportPaint = false, bool reportPrint = false,
        params string[] expectedGrids)
    {
        Assert.True(File.Exists(sourceProject), $"Source project not found: {sourceProject}");

        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-smoke-" + Guid.NewGuid());
        try
        {
            new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            InjectHeadlessHarness(outputDir, clickButtons, reportPaint, reportPrint);

            var run = await DotnetRunner.RunAsync("run", outputDir);

            Assert.True(
                run.ExitCode == 0 && run.StdOut.Contains(SuccessMarker, StringComparison.Ordinal),
                $"The converted '{name}' did not start (exit code {run.ExitCode}).\n"
                + $"--- stdout ---\n{run.StdOut}\n--- stderr ---\n{run.StdErr}");

            // `{grid}:{rows}:{first cell text}`. Both halves matter: the count proves the handler
            // populated the collection, the text proves the column bindings resolve - a grid can
            // hold rows and still show nothing, which is what a Binding-less column always did.
            foreach (var expected in expectedGrids)
            {
                Assert.True(
                    run.StdOut.Contains(expected, StringComparison.Ordinal),
                    $"The converted '{name}' did not report '{expected}'.\n"
                    + $"--- stdout ---\n{run.StdOut}");
            }
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
    private static void InjectHeadlessHarness(string outputDir, bool clickButtons, bool reportPaint, bool reportPrint)
    {
        var programPath = Path.Combine(outputDir, "Program.cs");
        var rootNamespace = Regex.Match(File.ReadAllText(programPath), @"namespace\s+([\w.]+)\s*;").Groups[1].Value;
        Assert.False(rootNamespace.Length == 0, "Could not read the generated root namespace from Program.cs.");

        var paintReport = reportPaint ? PaintReportSource.Replace("__ROOT__", rootNamespace, StringComparison.Ordinal) : "";
        var printReport = reportPrint ? PrintReportSource.Replace("__ROOT__", rootNamespace, StringComparison.Ordinal) : "";

        File.WriteAllText(programPath, $$"""
            using System.Reflection;
            using Avalonia;
            using Avalonia.Controls;
            using Avalonia.Controls.ApplicationLifetimes;
            using Avalonia.Controls.Primitives;
            using Avalonia.Headless;
            using Avalonia.Interactivity;
            using Avalonia.LogicalTree;
            using Avalonia.VisualTree;
            using CommunityToolkit.Mvvm.Input;

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
                        // UseHeadlessDrawing short-circuits drawing entirely, which is fine for
                        // everything else here and useless for proving a Render override runs -
                        // so the paint fixture asks for the real Skia backend instead.
                        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = {{(reportPaint || reportPrint ? "false" : "true")}} })
                        {{(reportPaint || reportPrint ? ".UseSkia()" : "")}}
                        .SetupWithLifetime(lifetime);

                    // ...and one frame through the visual tree the AXAML just produced.
                    lifetime.MainWindow?.Show();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                    AssertEveryControlGotItsTemplate(lifetime.MainWindow);
                    ExecutePromotedCommands(lifetime.MainWindow?.DataContext);
                    {{(clickButtons ? "ClickEveryButton(lifetime.MainWindow);" : "")}}
                    ReportGrids(lifetime.MainWindow);
                    ReportCheckBoxes(lifetime.MainWindow);
                    ReportContainerRegions(lifetime.MainWindow);
                    {{(reportPaint ? "ReportPaintSurfaces(lifetime.MainWindow);" : "")}}
                    {{(reportPrint ? "ReportPrintDocuments(lifetime.MainWindow);" : "")}}

                    Console.WriteLine("{{SuccessMarker}}");
                    return 0;
                }

                /// <summary>
                /// Raises Click on every button in the window, so the translated handler bodies
                /// run rather than only the constructor.
                /// </summary>
                /// <remarks>
                /// <para>
                /// Through the tree rather than by calling the methods, so `sender` and the args
                /// are the real ones a user would produce. An async handler returns at its first
                /// await - it does not finish here, and is not meant to.
                /// </para>
                /// <para>
                /// PlatformNotSupportedException is the one exception allowed through, and it is
                /// not a hole in the assertion - it is the contract. A Windows-only component is
                /// emitted as a lazily built field precisely so the app starts everywhere and
                /// only *touching* it fails off Windows; a handler that writes to the EventLog is
                /// supposed to throw here, on Linux, exactly as the WinForms original would have.
                /// Everything else still fails the run.
                /// </para>
                /// </remarks>
                private static void ClickEveryButton(Window? window)
                {
                    if (window is null)
                    {
                        return;
                    }

                    foreach (var button in Descendants(window).OfType<Button>().ToList())
                    {
                        try
                        {
                            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        catch (Exception e) when (IsPlatformGate(e))
                        {
                            Console.WriteLine($"platform-gated: {button.Name}");
                        }
                    }
                }

                private static bool IsPlatformGate(Exception? e)
                {
                    for (; e is not null; e = e.InnerException)
                    {
                        if (e is PlatformNotSupportedException)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                /// <summary>
                /// Every templated control in the window has to have found a theme.
                /// </summary>
                /// <remarks>
                /// <para>
                /// Avalonia resolves a ControlTheme by the control's concrete type, and when it
                /// finds none the control gets no template and draws *nothing* - not an unstyled
                /// box, nothing. Two separate causes produced exactly that in this converter: a
                /// bundled fallback subclassing TextBox without a StyleKeyOverride, and a control
                /// from a package whose own theme App.axaml never included. Both compiled, both
                /// started, both passed every test in this suite, and both were invisible.
                /// </para>
                /// <para>
                /// This is the assertion that catches the class rather than the two instances.
                /// It runs after the first render tick, so anything reachable has been templated
                /// by now.
                /// </para>
                /// </remarks>
                private static void AssertEveryControlGotItsTemplate(Window? window)
                {
                    if (window is null)
                    {
                        return;
                    }

                    var untemplated = Descendants(window)
                        .OfType<TemplatedControl>()
                        .Where(c => c.IsVisible && c.Template is null)
                        .Select(c => $"{c.GetType().Name} '{c.Name}'")
                        .Distinct()
                        .ToList();

                    if (untemplated.Count > 0)
                    {
                        throw new InvalidOperationException(
                            "These controls found no ControlTheme and would render as nothing: "
                            + string.Join(", ", untemplated)
                            + ". Either the type needs a StyleKeyOverride, or its package's theme "
                            + "is missing from App.axaml (AvaloniaProjectScaffolder.PackageStyleIncludes).");
                    }
                }

                /// <summary>
                /// Prints every DataGrid's row count and first realized cell text.
                /// </summary>
                /// <remarks>
                /// <para>
                /// The cell text is the half that matters. A row count on its own would pass even
                /// if the column bindings resolved to nothing - which is the exact
                /// "compiles, starts, renders as nothing" failure this suite exists to catch, and
                /// the one a Binding-less DataGridTextColumn used to produce every time.
                /// </para>
                /// <para>
                /// Through reflection rather than a typed `DataGrid`, because the type lives in an
                /// optional package: naming it would stop this harness compiling for every
                /// converted app that has no grid.
                /// </para>
                /// </remarks>
                private static void ReportGrids(Window? window)
                {
                    if (window is null)
                    {
                        return;
                    }

                    // A DataGrid realizes its cells during layout, not on construction, so the
                    // cell text below is only there after the tree has actually been laid out.
                    for (var pass = 0; pass < 5; pass++)
                    {
                        window.UpdateLayout();
                        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    }

                    foreach (var grid in Descendants(window).Where(c => c.GetType().Name == "DataGrid"))
                    {
                        var source = grid.GetType().GetProperty("ItemsSource")?.GetValue(grid);
                        var rows = source is System.Collections.IEnumerable items
                            ? items.Cast<object>().Count()
                            : 0;

                        // Through DataGridCell, not any TextBlock under the grid: the column
                        // headers are TextBlocks too, and they render whether or not a single
                        // binding resolved.
                        var cell = grid.GetVisualDescendants()
                            .Where(c => c.GetType().Name == "DataGridCell")
                            .SelectMany(c => c.GetVisualDescendants())
                            .OfType<TextBlock>()
                            .Select(t => t.Text)
                            .FirstOrDefault(t => !string.IsNullOrEmpty(t)) ?? "";

                        // Appended rather than inserted, so the existing prefix assertions still
                        // match. This is what proves a BindingNavigator really drives the grid:
                        // its buttons move a ViewModel property that is the grid's SelectedIndex.
                        var selected = grid.GetType().GetProperty("SelectedIndex")?.GetValue(grid);

                        Console.WriteLine($"w2a-grid:{grid.Name}:{rows}:{cell} selected={selected}");
                    }
                }

                {{paintReport}}

                {{printReport}}

                /// <summary>
                /// Every realized CheckBox and whether it is ticked.
                /// </summary>
                /// <remarks>
                /// The proof for a converted CheckedListBox: its tick lives in an ItemTemplate
                /// now, so the box only exists once the ListBox has realized a container for the
                /// row - and it only *moves* when a handler writes the row and the row raises a
                /// change notification. Neither of those is visible from the generated text.
                /// Through the visual tree, because a templated item is not a logical child.
                /// </remarks>
                private static void ReportCheckBoxes(Window? window)
                {
                    if (window is null)
                    {
                        return;
                    }

                    window.UpdateLayout();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                    foreach (var box in window.GetVisualDescendants().OfType<CheckBox>())
                    {
                        Console.WriteLine($"w2a-check:{box.Content}:{box.IsChecked}");
                    }
                }

                /// <summary>
                /// What actually ended up inside a converted ToolStripContainer.
                /// </summary>
                /// <remarks>
                /// The regions are filled through XAML property-element syntax onto settable
                /// properties, and the setter rebuilds the container's children. Both of those
                /// happen at *load* time, not compile time: a loader that rejected the syntax, or
                /// a rebuild that dropped a panel, would produce an app that starts with the
                /// controls simply absent. Reflected rather than typed, because the container is
                /// a generated type that only some apps have.
                /// </remarks>
                private static void ReportContainerRegions(Window? window)
                {
                    if (window is null)
                    {
                        return;
                    }

                    foreach (var container in window.GetVisualDescendants()
                                 .OfType<Control>()
                                 .Where(c => c.GetType().Name == "ToolStripContainerFallback"))
                    {
                        var named = string.Join(
                            ",",
                            container.GetVisualDescendants()
                                .OfType<Control>()
                                .Select(c => c.Name)
                                .Where(n => !string.IsNullOrEmpty(n))
                                .OrderBy(n => n, StringComparer.Ordinal));

                        Console.WriteLine($"w2a-region:{container.Name}:{named}");
                    }
                }

                private static IEnumerable<Control> Descendants(Control root)
                {
                    foreach (var child in root.GetLogicalChildren().OfType<Control>())
                    {
                        yield return child;

                        foreach (var nested in Descendants(child))
                        {
                            yield return nested;
                        }
                    }
                }

                /// <summary>
                /// Runs every generated [RelayCommand]. Nothing else in the test suite ever calls
                /// one, so their translated bodies had never been executed at all.
                /// </summary>
                /// <remarks>
                /// Safe by construction rather than by luck: a handler is only promoted to a
                /// command when its body touches nothing but two-way-bindable control properties,
                /// which the ViewModel holds as [ObservableProperty]s. No TopLevel, no dialog,
                /// no I/O - so there is nothing here that needs a real window to succeed.
                /// </remarks>
                private static void ExecutePromotedCommands(object? viewModel)
                {
                    if (viewModel is null)
                    {
                        return;
                    }

                    foreach (var property in viewModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (!typeof(IRelayCommand).IsAssignableFrom(property.PropertyType))
                        {
                            continue;
                        }

                        var command = (IRelayCommand?)property.GetValue(viewModel);
                        if (command is not null && command.CanExecute(null))
                        {
                            command.Execute(null);
                        }
                    }
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
                $"""    <PackageReference Include="Avalonia.Headless" Version="{HeadlessPackageVersion}" />{Environment.NewLine}"""
                + (reportPaint || reportPrint
                    ? $"""    <PackageReference Include="Avalonia.Skia" Version="{HeadlessPackageVersion}" />{Environment.NewLine}"""
                    : "")
                + "  </ItemGroup>"));
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
