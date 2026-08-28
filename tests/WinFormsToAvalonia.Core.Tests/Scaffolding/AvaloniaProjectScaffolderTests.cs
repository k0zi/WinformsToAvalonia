using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Scaffolding;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Scaffolding;

public class AvaloniaProjectScaffolderTests
{
    private static readonly string[] ExpectedFiles =
    [
        "App.axaml",
        "App.axaml.cs",
        "Program.cs",
        "ViewLocator.cs",
        "ViewModels/ViewModelBase.cs",
        "ViewModels/MainWindowViewModel.cs",
        "Views/MainWindowView.axaml",
        "Views/MainWindowView.axaml.cs",
        "app.manifest",
        "DemoApp.csproj",
    ];

    [Fact]
    public void BuildEmptySkeleton_EmitsExpectedFixedFileSet()
    {
        var scaffolder = new AvaloniaProjectScaffolder();

        var vfs = scaffolder.BuildEmptySkeleton("DemoApp");

        Assert.Equal(ExpectedFiles.OrderBy(f => f, StringComparer.Ordinal), vfs.RelativePaths);
    }

    [Fact]
    public void BuildEmptySkeleton_UsesProjectNameAsRootNamespaceEverywhere()
    {
        var scaffolder = new AvaloniaProjectScaffolder();

        var vfs = scaffolder.BuildEmptySkeleton("DemoApp");

        vfs.TryGetText("App.axaml.cs", out var appCode);
        vfs.TryGetText("Views/MainWindowView.axaml.cs", out var viewCode);
        vfs.TryGetText("ViewModels/MainWindowViewModel.cs", out var viewModelCode);

        Assert.Contains("namespace DemoApp;", appCode);
        Assert.Contains("namespace DemoApp.Views;", viewCode);
        Assert.Contains("namespace DemoApp.ViewModels;", viewModelCode);
    }

    [Fact]
    public void BuildEmptySkeleton_CsprojReferencesPinnedAvaloniaAndMvvmToolkitVersions()
    {
        var scaffolder = new AvaloniaProjectScaffolder();

        var vfs = scaffolder.BuildEmptySkeleton("DemoApp");
        vfs.TryGetText("DemoApp.csproj", out var csproj);

        Assert.Contains("""<PackageReference Include="Avalonia" Version="12.1.1" />""", csproj);
        Assert.Contains("""<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />""", csproj);
    }

    /// <summary>
    /// A control from a package outside core Avalonia brings its own theme, and App.axaml has to
    /// ask for it - referencing the package is not enough.
    /// </summary>
    /// <remarks>
    /// Without the include the control finds no ControlTheme, gets no template, and renders as
    /// nothing: the converted sample's two DataGrids were blank rectangles and its ColorDialog
    /// opened an empty window, while the project compiled and started cleanly. The third table
    /// an extra package has to appear in, after the mapper's RequiredNuGetPackage and
    /// ExtraPackageVersions.
    /// </remarks>
    [Fact]
    public void BuildProject_WithAPackagedControl_IncludesThatPackagesOwnTheme()
    {
        var vfs = new AvaloniaProjectScaffolder().BuildProject(
            "Demo",
            [new("", "MainView", "MainViewModel", "<Window />", "// view code-behind", "// view model")],
            extraNuGetPackages: new HashSet<string>(StringComparer.Ordinal) { "Avalonia.Controls.DataGrid" });

        vfs.TryGetText("App.axaml", out var app);

        Assert.Contains(
            "<StyleInclude Source=\"avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml\" />",
            app);
    }

    /// <summary>
    /// ...and only the ones actually used: an include for a package the csproj does not
    /// reference is a XAML load failure at startup, not a build error.
    /// </summary>
    [Fact]
    public void BuildProject_WithoutAPackagedControl_IncludesNoExtraTheme()
    {
        var vfs = new AvaloniaProjectScaffolder().BuildProject(
            "Demo",
            [new("", "MainView", "MainViewModel", "<Window />", "// view code-behind", "// view model")]);

        vfs.TryGetText("App.axaml", out var app);

        Assert.DoesNotContain("<StyleInclude", app);
    }

    /// <summary>Every package that ships a theme has to be one the csproj writer can emit.</summary>
    [Fact]
    public void PackageStyleIncludes_OnlyNamesPackagesTheCsprojCanReference()
    {
        Assert.All(
            AvaloniaProjectScaffolder.PackageStyleIncludes.Keys,
            package => Assert.Contains(package, AvaloniaProjectScaffolder.ExtraPackageVersions.Keys));
    }

    /// <summary>
    /// Un-migrated handler stubs report through this helper instead of throwing, because
    /// Avalonia invokes them from the framework - a TabControl selecting its first tab, a Window
    /// raising Loaded - so a throwing stub killed the generated app during XAML initialization,
    /// before its first window appeared.
    /// </summary>
    [Fact]
    public void BuildProject_Always_EmitsTheMigrationTodoHelperTheHandlerStubsCallInto()
    {
        var scaffolder = new AvaloniaProjectScaffolder();
        var forms = new List<ConvertedFormOutput>
        {
            new("", "MainView", "MainViewModel", "<Window />", "// view code-behind", "// view model"),
        };

        var vfs = scaffolder.BuildProject("Demo", forms);

        Assert.Contains("Generated/MigrationTodo.cs", vfs.RelativePaths);
        vfs.TryGetText("Generated/MigrationTodo.cs", out var helper);
        Assert.Contains("namespace Demo.Generated;", helper);
        Assert.Contains("public static void NotMigrated(string member, string originalWinFormsMember)", helper);
        // The strict behaviour stays available, just opt-in.
        Assert.Contains("public static bool ThrowOnUnmigratedCall", helper);
    }

    [Fact]
    public void BuildEmptySkeleton_NoNotifyIcons_AppAxamlHasNoTrayIconBlock()
    {
        var scaffolder = new AvaloniaProjectScaffolder();

        var vfs = scaffolder.BuildEmptySkeleton("DemoApp");
        vfs.TryGetText("App.axaml", out var appAxaml);

        Assert.DoesNotContain("TrayIcon", appAxaml);
    }

    [Fact]
    public void BuildProject_WithNotifyIcons_EmitsTrayIconIconsBlockInAppAxaml()
    {
        var scaffolder = new AvaloniaProjectScaffolder();
        var forms = new List<ConvertedFormOutput>
        {
            new("", "MainView", "MainViewModel", "<Window />", "// view code-behind", "// view model"),
        };
        var notifyIcons = new List<NotifyIconInfo> { new("notifyIcon1", "Assets/notifyIcon1.ico", "My App") };

        var vfs = scaffolder.BuildProject("Demo", forms, notifyIcons: notifyIcons);
        vfs.TryGetText("App.axaml", out var appAxaml);

        Assert.Contains("<TrayIcon.Icons>", appAxaml);
        Assert.Contains("<TrayIcon Icon=\"/Assets/notifyIcon1.ico\" ToolTipText=\"My App\" />", appAxaml);
    }

    /// <summary>
    /// Avalonia resolves TrayIcon.Icon at run time, so a live reference to an asset the
    /// conversion never produced builds fine and then throws FileNotFoundException out of
    /// App.Initialize(), before any window opens - it used to take the whole generated app down.
    /// </summary>
    [Fact]
    public void BuildProject_NotifyIconWithoutAResolvedIconFile_EmitsTheTrayIconBlockCommentedOut()
    {
        var scaffolder = new AvaloniaProjectScaffolder();
        var forms = new List<ConvertedFormOutput>
        {
            new("", "MainView", "MainViewModel", "<Window />", "// view code-behind", "// view model"),
        };
        var notifyIcons = new List<NotifyIconInfo> { new("notifyIcon1", null, "My App") };

        var vfs = scaffolder.BuildProject("Demo", forms, notifyIcons: notifyIcons);
        vfs.TryGetText("App.axaml", out var appAxaml);

        Assert.Contains("TODO(Winforms2Avalonia)", appAxaml);
        Assert.Contains("notifyIcon1", appAxaml);

        // Everything TrayIcon-related must sit inside the comment, or the app still crashes.
        var commentStart = appAxaml.IndexOf("<!--", StringComparison.Ordinal);
        var commentEnd = appAxaml.IndexOf("-->", StringComparison.Ordinal);
        Assert.InRange(commentStart, 0, int.MaxValue);
        Assert.InRange(appAxaml.IndexOf("TrayIcon", StringComparison.Ordinal), commentStart, commentEnd);
        Assert.DoesNotContain("TrayIcon", appAxaml[(commentEnd + 3)..]);
    }

    [Fact]
    public void BuildProject_MixedNotifyIcons_EmitsOnlyTheResolvedOneLive()
    {
        var scaffolder = new AvaloniaProjectScaffolder();
        var forms = new List<ConvertedFormOutput>
        {
            new("", "MainView", "MainViewModel", "<Window />", "// view code-behind", "// view model"),
        };
        var notifyIcons = new List<NotifyIconInfo>
        {
            new("resolvedIcon", "Assets/app.ico", null),
            new("unresolvedIcon", null, null),
        };

        var vfs = scaffolder.BuildProject("Demo", forms, notifyIcons: notifyIcons);
        vfs.TryGetText("App.axaml", out var appAxaml);

        var liveSection = appAxaml[..appAxaml.IndexOf("<!--", StringComparison.Ordinal)];
        Assert.Contains("<TrayIcon Icon=\"/Assets/app.ico\" />", liveSection);
        Assert.DoesNotContain("unresolvedIcon", liveSection);
        Assert.Contains("unresolvedIcon", appAxaml);
    }

    [Fact]
    public void BuildProject_NoNotifyIcons_AppAxamlByteIdenticalToPreFeatureOutput()
    {
        var scaffolder = new AvaloniaProjectScaffolder();
        var forms = new List<ConvertedFormOutput>
        {
            new("", "MainView", "MainViewModel", "<Window />", "// view code-behind", "// view model"),
        };

        var vfs = scaffolder.BuildProject("Demo", forms);
        vfs.TryGetText("App.axaml", out var appAxaml);

        Assert.Equal(
            """
            <Application xmlns="https://github.com/avaloniaui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         x:Class="Demo.App"
                         xmlns:local="using:Demo"
                         RequestedThemeVariant="Light">

                <Application.DataTemplates>
                    <local:ViewLocator />
                </Application.DataTemplates>

                <Application.Styles>
                    <SimpleTheme />
                </Application.Styles>
            </Application>
            """,
            appAxaml);
    }

    [Fact]
    public void BuildProject_MainFormInSubfolder_QualifiesMainWindowWiringWithNestedNamespace()
    {
        var scaffolder = new AvaloniaProjectScaffolder();
        var forms = new List<ConvertedFormOutput>
        {
            new("Forms", "MainFormView", "MainFormViewModel", "<Window />", "// view code-behind", "// view model"),
        };

        var vfs = scaffolder.BuildProject("Demo", forms);
        vfs.TryGetText("App.axaml.cs", out var appCode);

        Assert.Contains("new Demo.Views.Forms.MainFormView()", appCode);
        // The ViewModel is no longer wired here: each generated View sets its own DataContext.
        Assert.DoesNotContain("DataContext =", appCode);
        Assert.DoesNotContain("MainFormViewModel", appCode);
    }
}
