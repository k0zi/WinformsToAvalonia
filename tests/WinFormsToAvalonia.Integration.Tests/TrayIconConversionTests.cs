using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// A NotifyIcon is the one component with no per-View existence at all: Avalonia's tray support
/// is app-level, declared in App.axaml. So a handler saying <c>notifyIcon1.Visible = false</c> -
/// which is most of what a WinForms app does with one - had nothing to name.
/// </summary>
public class TrayIconConversionTests
{
    [Fact]
    public async Task ConvertedTrayIconApp_ReachesTheTrayIconFromAHandlerAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "TrayIconApp", "TrayIconApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-tray-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            var vfs = result.Vfs;

            // The icon file resolved, so the TrayIcon is emitted live rather than commented out.
            vfs.TryGetText("App.axaml", out var appAxaml);
            Assert.Contains("<TrayIcon Icon=\"/Assets/app.ico\" ToolTipText=\"Tray demo\">", appAxaml);

            // The NotifyIcon's ContextMenuStrip becomes TrayIcon.Menu - a *native* menu, drawn by
            // the OS, so it carries a caption, an enabled flag and a submenu and nothing else.
            // The `&` mnemonic is stripped rather than converted: there is no AccessText to
            // render one, so an underscore would just be a stray character in the caption.
            Assert.Contains("<TrayIcon.Menu>", appAxaml);
            Assert.Contains("<NativeMenuItem Header=\"Open\" />", appAxaml);
            Assert.Contains("<NativeMenuItemSeparator />", appAxaml);
            Assert.Contains("<NativeMenuItem Header=\"Settings...\" IsEnabled=\"False\" />", appAxaml);
            Assert.Contains("<NativeMenuItem.Menu>", appAxaml);

            // NativeMenuItem raises Click as an event, which XAML cannot point at a method - so
            // the item is emitted and the handler is reported rather than silently dropped.
            Assert.Contains(
                result.Report.Warnings,
                w => w.Contains("openMenuItem", StringComparison.Ordinal)
                    && w.Contains("NativeMenuItem", StringComparison.Ordinal));

            // ...and the App exposes it, named after the WinForms field it came from.
            vfs.TryGetText("App.axaml.cs", out var appCodeBehind);
            Assert.Contains("public static TrayIcon NotifyIcon1 =>", appCodeBehind);

            // The View reaches it without a using: App is in the root namespace and the View in a
            // child of it, so the enclosing-namespace lookup finds it.
            vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind);
            Assert.Contains("App.NotifyIcon1.IsVisible = false;", codeBehind);
            Assert.Contains("App.NotifyIcon1.ToolTipText = \"Hidden\";", codeBehind);
            Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(hideButton_Click)", codeBehind);

            // A designer-wired tray event is subscribed from the constructor, since there is no
            // element to put an attribute on. This used to be emitted as a translated method that
            // nothing subscribed and nothing reported - alive to read, dead to run.
            Assert.Contains("App.NotifyIcon1.Clicked += notifyIcon1_Click;", codeBehind);

            // The same Click on the icon that did NOT resolve is suppressed, because App.axaml
            // emitted its TrayIcon commented out - there is no accessor, so the constructor line
            // would not compile. This is the case the suppression pass exists for.
            Assert.DoesNotContain("NotifyIcon2.", codeBehind);
            Assert.Contains(
                result.Report.Warnings,
                w => w.Contains("notifyIcon2", StringComparison.Ordinal)
                    && w.Contains("icon could not be resolved", StringComparison.Ordinal));

            // ...and an event Avalonia's TrayIcon simply does not have is refused, by name.
            Assert.Contains(
                result.Report.Warnings,
                w => w.Contains("notifyIcon1_DoubleClick", StringComparison.Ordinal)
                    && w.Contains("never subscribed", StringComparison.Ordinal));

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

    /// <summary>
    /// The negative half, and the one that matters. The same fixture's second NotifyIcon has no
    /// icon the conversion can resolve - the common real-world case, since the icon is usually a
    /// resx resource - so App.axaml emits nothing live for it. A handler naming it must stay a
    /// comment rather than be given an accessor to something that is not there.
    /// </summary>
    [Fact]
    public void ConvertedTrayIconApp_UnresolvableIcon_GetsNoAccessorAndNoTranslation()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "TrayIconApp", "TrayIconApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-tray-none-" + Guid.NewGuid());

        var result = new ConversionPipeline().Run(
            new ConversionOptions(sourceProject, outputDir) { DryRun = true });

        result.Vfs.TryGetText("App.axaml.cs", out var appCodeBehind);
        Assert.Contains("public static TrayIcon NotifyIcon1 =>", appCodeBehind);
        Assert.DoesNotContain("NotifyIcon2", appCodeBehind);

        result.Vfs.TryGetText("Views/MainView.axaml.cs", out var codeBehind);
        Assert.DoesNotContain("App.NotifyIcon2", codeBehind);
        Assert.Contains("MigrationTodo.NotMigrated(nameof(otherButton_Click)", codeBehind);
    }
}
