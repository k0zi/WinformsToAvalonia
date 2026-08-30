using WinFormsToAvalonia.Core.Scaffolding;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Scaffolding;

/// <summary>
/// The <c>--with-web</c> split: one finished single-project output re-rooted into a shared
/// library plus a desktop and a browser head.
/// </summary>
public class AvaloniaProjectScaffolderWebTests
{
    private static VirtualFileSystem Split()
    {
        var scaffolder = new AvaloniaProjectScaffolder();
        return scaffolder.SplitIntoHeads(scaffolder.BuildEmptySkeleton("DemoApp"), "DemoApp");
    }

    private static readonly string[] ExpectedFiles =
    [
        "DemoApp.Browser/DemoApp.Browser.csproj",
        "DemoApp.Browser/Program.cs",
        "DemoApp.Browser/wwwroot/index.html",
        "DemoApp.Browser/wwwroot/main.js",
        "DemoApp.Desktop/DemoApp.Desktop.csproj",
        "DemoApp.Desktop/Program.cs",
        "DemoApp.Desktop/app.manifest",
        "DemoApp.slnx",
        "DemoApp/App.axaml",
        "DemoApp/App.axaml.cs",
        "DemoApp/DemoApp.csproj",
        "DemoApp/Generated/ViewWindow.cs",
        "DemoApp/ViewLocator.cs",
        "DemoApp/ViewModels/MainWindowViewModel.cs",
        "DemoApp/ViewModels/ViewModelBase.cs",
        "DemoApp/Views/MainWindowView.axaml",
        "DemoApp/Views/MainWindowView.axaml.cs",
    ];

    [Fact]
    public void SplitIntoHeads_EmitsExpectedFixedFileSet()
    {
        Assert.Equal(ExpectedFiles.OrderBy(f => f, StringComparer.Ordinal), Split().RelativePaths);
    }

    /// <summary>
    /// The two files that only ever made sense for an executable move rather than being copied -
    /// a library carrying a Main and a Windows manifest would be wrong in both directions.
    /// </summary>
    [Fact]
    public void SplitIntoHeads_MovesTheEntryPointAndManifestOutOfTheLibrary()
    {
        var paths = Split().RelativePaths.ToList();

        Assert.DoesNotContain("DemoApp/Program.cs", paths);
        Assert.DoesNotContain("DemoApp/app.manifest", paths);
        Assert.Contains("DemoApp.Desktop/Program.cs", paths);
        Assert.Contains("DemoApp.Desktop/app.manifest", paths);
    }

    [Fact]
    public void SplitIntoHeads_LibraryCsprojHasNoExecutableOrDesktopBackendBits()
    {
        Assert.True(Split().TryGetText("DemoApp/DemoApp.csproj", out var csproj));

        Assert.DoesNotContain("<OutputType>", csproj);
        Assert.DoesNotContain("ApplicationManifest", csproj);
        Assert.DoesNotContain("Avalonia.Desktop", csproj);

        // ...but everything a View needs is still here.
        Assert.Contains($"""<PackageReference Include="Avalonia" Version="{AvaloniaProjectScaffolder.AvaloniaVersion}" />""", csproj);
        Assert.Contains("Avalonia.Themes.Simple", csproj);
        Assert.Contains("<AvaloniaResource Include=\"Assets\\**\" />", csproj);
    }

    [Fact]
    public void SplitIntoHeads_DesktopHeadCarriesTheBackendAndReferencesTheLibrary()
    {
        Assert.True(Split().TryGetText("DemoApp.Desktop/DemoApp.Desktop.csproj", out var csproj));

        Assert.Contains("<OutputType>WinExe</OutputType>", csproj);
        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", csproj);
        Assert.Contains($"""<PackageReference Include="Avalonia.Desktop" Version="{AvaloniaProjectScaffolder.AvaloniaVersion}" />""", csproj);
        Assert.Contains(@"<ProjectReference Include=""..\DemoApp\DemoApp.csproj"" />", csproj);
    }

    /// <summary>
    /// Both of these fail silently when missing: without the RuntimeIdentifier the WebAssembly
    /// SDK targets never run and no AppBundle is produced at all, and without the deploy item the
    /// bundle has no index.html to open. Neither shows up as a build error.
    /// </summary>
    [Fact]
    public void SplitIntoHeads_BrowserHeadCarriesTheWasmBuildInputs()
    {
        Assert.True(Split().TryGetText("DemoApp.Browser/DemoApp.Browser.csproj", out var csproj));

        Assert.Contains("<TargetFramework>net10.0-browser</TargetFramework>", csproj);
        Assert.Contains("<RuntimeIdentifier>browser-wasm</RuntimeIdentifier>", csproj);
        Assert.Contains("<WasmMainJSPath>wwwroot/main.js</WasmMainJSPath>", csproj);
        Assert.Contains(@"<WasmExtraFilesToDeploy Include=""wwwroot\**"" />", csproj);
        Assert.Contains($"""<PackageReference Include="Avalonia.Browser" Version="{AvaloniaProjectScaffolder.AvaloniaVersion}" />""", csproj);
    }

    /// <summary>The div id is one fact spelled in two files, so they have to agree.</summary>
    [Fact]
    public void SplitIntoHeads_BrowserProgramRendersIntoTheDivIndexHtmlDeclares()
    {
        var vfs = Split();
        Assert.True(vfs.TryGetText("DemoApp.Browser/Program.cs", out var program));
        Assert.True(vfs.TryGetText("DemoApp.Browser/wwwroot/index.html", out var html));

        Assert.Contains("""StartBrowserAppAsync("out")""", program);
        Assert.Contains("""<div id="out"></div>""", html);

        // The browser backend is the only one that can be configured here - UsePlatformDetect
        // would drag in the desktop one, which does not exist for this target framework.
        Assert.DoesNotContain("UsePlatformDetect", program);
        Assert.Contains("[assembly: SupportedOSPlatform(\"browser\")]", program);
    }

    [Fact]
    public void SplitIntoHeads_HeadProgramsLiveInTheirOwnNamespaces()
    {
        var vfs = Split();
        Assert.True(vfs.TryGetText("DemoApp.Desktop/Program.cs", out var desktop));
        Assert.True(vfs.TryGetText("DemoApp.Browser/Program.cs", out var browser));

        Assert.Contains("namespace DemoApp.Desktop;", desktop);
        Assert.Contains("namespace DemoApp.Browser;", browser);
    }

    [Fact]
    public void SplitIntoHeads_SolutionListsAllThreeProjects()
    {
        Assert.True(Split().TryGetText("DemoApp.slnx", out var slnx));

        Assert.Equal(
            """
            <Solution>
              <Project Path="DemoApp.Browser/DemoApp.Browser.csproj" />
              <Project Path="DemoApp.Desktop/DemoApp.Desktop.csproj" />
              <Project Path="DemoApp/DemoApp.csproj" />
            </Solution>

            """.Replace("\r\n", "\n"),
            slnx);
    }

    /// <summary>
    /// The whole reason the split is a post-processing pass: with the flag off nothing about the
    /// output may change, which is what keeps every golden test in this suite honest.
    /// </summary>
    [Fact]
    public void WithoutTheSplit_TheSkeletonIsUntouched()
    {
        var scaffolder = new AvaloniaProjectScaffolder();
        var single = scaffolder.BuildEmptySkeleton("DemoApp");

        Assert.Contains("DemoApp.csproj", single.RelativePaths);
        Assert.Contains("Program.cs", single.RelativePaths);
        Assert.DoesNotContain("DemoApp/DemoApp.csproj", single.RelativePaths);
    }
}
