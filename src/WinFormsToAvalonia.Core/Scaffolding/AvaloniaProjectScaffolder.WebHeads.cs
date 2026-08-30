namespace WinFormsToAvalonia.Core.Scaffolding;

/// <summary>
/// The `--with-web` half of the scaffolder: turns the single self-contained desktop project into
/// the cross-platform three-project layout - a shared library plus a desktop and a browser head.
/// </summary>
/// <remarks>
/// <para>
/// This runs as a <b>post-processing pass over the finished <see cref="VirtualFileSystem"/></b>
/// rather than as a branch threaded through <see cref="BuildProject"/>, and that is deliberate.
/// The pipeline keeps adding to the VFS long after the scaffolder is done - carried components,
/// binary assets, <c>MIGRATION.md</c>, the resolved fallback templates - and every one of those
/// call sites would otherwise have to learn where the project root moved to. Re-rooting once, at
/// the end, leaves all of them untouched.
/// </para>
/// <para>
/// It also means the flag cannot change what a converted View looks like: without it the emitted
/// bytes are what they have always been, which is what keeps the golden tests honest.
/// </para>
/// </remarks>
public sealed partial class AvaloniaProjectScaffolder
{
    /// <summary>
    /// Avalonia's browser backend. Pinned to <see cref="AvaloniaVersion"/> like the desktop one:
    /// it is scaffolding, not a package a mapper asked for, so it is not in ExtraPackageVersions.
    /// </summary>
    private const string BrowserPackage = "Avalonia.Browser";

    /// <summary>The div the browser head renders into; shared by index.html and Program.cs.</summary>
    private const string BrowserRootElementId = "out";

    /// <summary>
    /// The extra packages that restore and compile for a browser head but cannot work in one.
    /// </summary>
    /// <remarks>
    /// They are `net10.0` libraries, so a `net10.0-browser` project references them happily and
    /// the build says nothing; the failure is a <c>PlatformNotSupportedException</c> the first
    /// time the converted code touches one. Nothing here can be fixed by the conversion - a
    /// serial port and the Windows event log genuinely are not in a browser - so this reports
    /// rather than blocks. The Avalonia extras are absent on purpose: those work fine.
    /// </remarks>
    public static readonly IReadOnlySet<string> BrowserUnsupportedPackages = new HashSet<string>(StringComparer.Ordinal)
    {
        "System.Diagnostics.EventLog",
        "System.Diagnostics.PerformanceCounter",
        "System.IO.Ports",
        "System.ServiceProcess.ServiceController",
        "System.Windows.Extensions",
    };

    /// <summary>The folder name suffix of each head, relative to the output root.</summary>
    public static string DesktopHeadFolder(string projectName) => $"{projectName}.Desktop";

    /// <summary>The folder name suffix of each head, relative to the output root.</summary>
    public static string BrowserHeadFolder(string projectName) => $"{projectName}.Browser";

    /// <summary>
    /// Rewrites <paramref name="single"/> - a complete single-project output - into the shared
    /// library plus the two heads, and adds the solution that ties them together.
    /// </summary>
    public VirtualFileSystem SplitIntoHeads(
        VirtualFileSystem single,
        string projectName,
        IReadOnlySet<string>? extraNuGetPackages = null,
        IReadOnlyList<string>? projectReferences = null)
    {
        var packages = extraNuGetPackages ?? (IReadOnlySet<string>)new HashSet<string>();
        var references = projectReferences ?? [];

        var split = new VirtualFileSystem();

        // Everything the conversion produced belongs to the library, except the two files that
        // only ever made sense for a desktop executable - they move to the desktop head below.
        foreach (var (path, content) in single.Files)
        {
            if (path is "Program.cs" or "app.manifest")
            {
                continue;
            }

            split.AddText($"{projectName}/{path}", content);
        }

        foreach (var (path, content) in single.BinaryFiles)
        {
            split.AddBinary($"{projectName}/{path}", content);
        }

        split.AddText($"{projectName}/{projectName}.csproj", BuildCsproj(projectName, packages, references, asLibrary: true));
        split.AddText($"{projectName}/Generated/ViewWindow.cs", BuildViewWindow(projectName));

        var desktop = DesktopHeadFolder(projectName);
        split.AddText($"{desktop}/{desktop}.csproj", BuildDesktopHeadCsproj(projectName));
        split.AddText($"{desktop}/Program.cs", BuildDesktopHeadProgram(projectName));
        split.AddText($"{desktop}/app.manifest", AppManifest);

        var browser = BrowserHeadFolder(projectName);
        split.AddText($"{browser}/{browser}.csproj", BuildBrowserHeadCsproj(projectName));
        split.AddText($"{browser}/Program.cs", BuildBrowserHeadProgram(projectName));
        split.AddText($"{browser}/wwwroot/index.html", BuildBrowserIndexHtml(projectName));
        split.AddText($"{browser}/wwwroot/main.js", BrowserMainJs);

        split.AddText($"{projectName}.slnx", BuildHeadsSolution(projectName));

        return split;
    }

    /// <summary>
    /// How a View that is not itself a Window names the one hosting it. Only the split main View
    /// needs it - see <c>FormMigrationPlanner.GeneratedWindowAccessor</c>, which emits the calls.
    /// </summary>
    private static string BuildViewWindow(string projectName) => $$"""
        using Avalonia;
        using Avalonia.Controls;

        namespace {{projectName}}.Generated;

        /// <summary>
        /// The Window hosting a View that is not one itself.
        /// </summary>
        /// <remarks>
        /// The main View is a UserControl so it can be shown under the browser's single-view
        /// lifetime, which leaves it without the Window members its WinForms Form had - Close,
        /// Activate, Title, and being a dialog's owner. On the desktop head a generated Window
        /// hosts it and this walks up to it.
        /// <para>
        /// In the browser there is no Window at all, so every call through here throws. That is
        /// the honest outcome: the browser has no second window to open and no chrome to close.
        /// </para>
        /// </remarks>
        internal static class ViewWindow
        {
            internal static Window Of(Visual view) =>
                TopLevel.GetTopLevel(view) as Window
                ?? throw new InvalidOperationException(
                    "TODO(Winforms2Avalonia): this view is not hosted in a Window. On the browser head "
                    + "Avalonia has no windowing platform, so there is nothing to close, activate or own "
                    + "a dialog - rework this interaction for a single-view shell.");
        }
        """;

    private static string BuildDesktopHeadCsproj(string projectName) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>WinExe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <ApplicationManifest>app.manifest</ApplicationManifest>
            <RootNamespace>{projectName}.Desktop</RootNamespace>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Avalonia.Desktop" Version="{AvaloniaVersion}" />
          </ItemGroup>

          <ItemGroup>
            <ProjectReference Include="..\{projectName}\{projectName}.csproj" />
          </ItemGroup>
        </Project>
        """;

    /// <remarks>
    /// <para>
    /// <c>net10.0-browser</c> is what Avalonia.Browser ships for - its lib folder is
    /// <c>net10.0-browser1.0</c>. Building it needs the `wasm-tools` workload.
    /// </para>
    /// <para>
    /// The two easily-missed items are load-bearing, and both fail <b>silently</b>: without
    /// <c>RuntimeIdentifier</c> the WebAssembly SDK targets never run at all - the project
    /// compiles to plain IL, reports success, and produces no <c>AppBundle</c> to serve - and
    /// without <c>WasmExtraFilesToDeploy</c> the bundle is built but has no <c>index.html</c>,
    /// so there is nothing for a browser to open.
    /// </para>
    /// </remarks>
    private static string BuildBrowserHeadCsproj(string projectName) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0-browser</TargetFramework>
            <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <RootNamespace>{projectName}.Browser</RootNamespace>
            <WasmMainJSPath>wwwroot/main.js</WasmMainJSPath>
          </PropertyGroup>

          <ItemGroup>
            <WasmExtraFilesToDeploy Include="wwwroot\**" />
          </ItemGroup>

          <ItemGroup>
            <PackageReference Include="{BrowserPackage}" Version="{AvaloniaVersion}" />
          </ItemGroup>

          <ItemGroup>
            <ProjectReference Include="..\{projectName}\{projectName}.csproj" />
          </ItemGroup>
        </Project>
        """;

    /// <summary>
    /// The same entry point the single-project output has, moved into its own head. The App it
    /// configures now lives in the referenced library, which the enclosing namespace resolves.
    /// </summary>
    private static string BuildDesktopHeadProgram(string projectName) => $$"""
        using Avalonia;

        namespace {{projectName}}.Desktop;

        internal sealed class Program
        {
            // Initialization code. Don't use any Avalonia, third-party APIs or any
            // SynchronizationContext-reliant code before AppMain is called: things aren't
            // initialized yet and stuff might break.
            [STAThread]
            public static void Main(string[] args) => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            public static AppBuilder BuildAvaloniaApp()
                => AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .LogToTrace();
        }
        """;

    /// <remarks>
    /// <c>StartBrowserAppAsync</c> creates a <b>single-view</b> lifetime - the browser has no
    /// windowing platform at all - which is why App.axaml.cs carries an
    /// <c>ISingleViewApplicationLifetime</c> branch and the main Form is emitted as a UserControl.
    /// </remarks>
    private static string BuildBrowserHeadProgram(string projectName) => $$"""
        using System.Runtime.Versioning;
        using Avalonia;
        using Avalonia.Browser;

        [assembly: SupportedOSPlatform("browser")]

        namespace {{projectName}}.Browser;

        internal sealed class Program
        {
            public static Task Main(string[] args) => BuildAvaloniaApp()
                .WithInterFont()
                .StartBrowserAppAsync("{{BrowserRootElementId}}");

            public static AppBuilder BuildAvaloniaApp()
                => AppBuilder.Configure<App>();
        }
        """;

    private static string BuildBrowserIndexHtml(string projectName) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=no" />
            <title>{{projectName}}</title>
            <style>
                html, body { margin: 0; padding: 0; width: 100%; height: 100%; overflow: hidden; }
                #{{BrowserRootElementId}} { width: 100%; height: 100%; }
            </style>
        </head>
        <body>
            <div id="{{BrowserRootElementId}}"></div>
            <script type="module" src="./main.js"></script>
        </body>
        </html>
        """;

    private const string BrowserMainJs = """
        import { dotnet } from './_framework/dotnet.js'

        const is_browser = typeof window != "undefined";
        if (!is_browser) throw new Error(`Expected to be running in a browser`);

        const dotnetRuntime = await dotnet
            .withDiagnosticTracing(false)
            .withApplicationArgumentsFromQuery()
            .create();

        const config = dotnetRuntime.getConfig();

        await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
        """;

    /// <summary>
    /// Ordinal-ordered so a re-run produces the same file; `.slnx` for the same reason
    /// SolutionConversionPipeline chose it - no GUIDs to invent and keep stable.
    /// </summary>
    private static string BuildHeadsSolution(string projectName)
    {
        var paths = new[]
        {
            $"{projectName}/{projectName}.csproj",
            $"{BrowserHeadFolder(projectName)}/{BrowserHeadFolder(projectName)}.csproj",
            $"{DesktopHeadFolder(projectName)}/{DesktopHeadFolder(projectName)}.csproj",
        }.OrderBy(p => p, StringComparer.Ordinal);

        return "<Solution>\n"
            + string.Concat(paths.Select(p => $"  <Project Path=\"{p}\" />\n"))
            + "</Solution>\n";
    }
}
