using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// The <c>--with-web</c> output, held to the same standard as every other generated project: it
/// has to build. All three projects of it.
/// </summary>
/// <remarks>
/// <para>
/// Building the browser head needs the <c>wasm-tools</c> workload
/// (<c>dotnet workload install wasm-tools</c>), which is therefore a prerequisite of this suite.
/// </para>
/// <para>
/// Each project is built on its own rather than through the generated solution: a
/// <c>dotnet build</c> of a <i>solution</i> is exactly what once hung this suite forever - see
/// <see cref="DotnetRunner"/> - and building them in dependency order gives a far better failure
/// message anyway, because the first thing to break is named.
/// </para>
/// </remarks>
public class WebHeadConversionBuildTests
{
    private static readonly string SampleAppsRoot = Path.Combine(AppContext.BaseDirectory, "SampleApps");

    /// <summary>The WebAssembly SDK's native link step blows straight through the default.</summary>
    private static readonly TimeSpan BrowserBuildTimeout = TimeSpan.FromMinutes(20);

    [Theory]
    // The plain case: one Form, one View, nothing that reaches for a Window.
    [InlineData("ModernNetApp", "ModernNetApp.csproj")]
    // The hard one: a second Form opened with ShowDialog, a close-confirmation rewrite that has
    // to Close() the window it no longer is, and Title/WindowState reads - every path that goes
    // through the generated ViewWindow helper.
    [InlineData("DialogContractApp", "DialogContractApp.csproj")]
    public Task ConvertedWithWeb_AllThreeProjectsBuild(string appFolder, string csprojName) =>
        AssertAllThreeBuild(Path.Combine(SampleAppsRoot, appFolder, csprojName));

    /// <summary>
    /// The all-in-one sample, and this is the row that matters most: it is the only input that
    /// carries the packages a browser cannot support, a TrayIcon, and file dialogs inlined into
    /// handler bodies. The last of those broke the shared library while every fixture here built
    /// fine - `StorageProvider` was emitted bare from the rewriter, which only resolves on a View
    /// that is itself the TopLevel.
    /// </summary>
    [Fact]
    public Task ConvertedAllInOneSampleWithWeb_AllThreeProjectsBuild() =>
        AssertAllThreeBuild(Path.Combine(
            RepositoryRoot(), "samples", "WinForms", "All-In-One-WinForms", "All-In-One-WinForms.csproj"));

    private static async Task AssertAllThreeBuild(string sourceProject)
    {
        Assert.True(File.Exists(sourceProject), $"Source project not found: {sourceProject}");

        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-web-" + Guid.NewGuid());
        var projectName = Path.GetFileNameWithoutExtension(outputDir).Replace("-", "_");

        try
        {
            new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir, WithWeb: true));

            await AssertBuilds(Path.Combine(outputDir, projectName), $"{projectName}.csproj", "shared library");
            await AssertBuilds(
                Path.Combine(outputDir, $"{projectName}.Desktop"), $"{projectName}.Desktop.csproj", "desktop head");

            var browserDir = Path.Combine(outputDir, $"{projectName}.Browser");
            await AssertBuilds(browserDir, $"{projectName}.Browser.csproj", "browser head", BrowserBuildTimeout);

            // A browser head that "builds" while producing no bundle is the failure this asserts
            // against: without the RuntimeIdentifier the WebAssembly targets never run, and
            // without the deploy item the bundle has no page to open. Both exit zero.
            var bundle = Path.Combine(browserDir, "bin", "Debug", "net10.0-browser", "browser-wasm", "AppBundle");
            Assert.True(Directory.Exists(bundle), $"The browser head built but produced no AppBundle at '{bundle}'.");
            Assert.True(File.Exists(Path.Combine(bundle, "index.html")), "The AppBundle has no index.html to open.");
            Assert.True(
                File.Exists(Path.Combine(bundle, "_framework", "dotnet.js")),
                "The AppBundle has no _framework/dotnet.js - the WebAssembly SDK targets did not run.");
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
    /// The repository root, walked up from the test output - the samples are source, not
    /// fixtures, so they are not copied beside the test assembly.
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

    private static async Task AssertBuilds(
        string projectDirectory, string csprojName, string what, TimeSpan? timeout = null)
    {
        Assert.True(
            File.Exists(Path.Combine(projectDirectory, csprojName)),
            $"The {what} was not generated: '{Path.Combine(projectDirectory, csprojName)}' does not exist.");

        var build = await DotnetRunner.RunAsync($"build \"{csprojName}\"", projectDirectory, timeout);

        Assert.True(
            build.ExitCode == 0,
            $"The generated {what} did not build (exit code {build.ExitCode}).\n"
            + $"--- stdout ---\n{build.StdOut}\n--- stderr ---\n{build.StdErr}");
    }
}
