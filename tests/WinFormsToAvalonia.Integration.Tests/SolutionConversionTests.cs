using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// Converting a whole solution rather than one project at a time - the limit
/// <c>samples/convert.sh</c> exists to work around.
/// </summary>
public class SolutionConversionTests
{
    [Fact]
    public async Task ConvertedSolution_ConvertsEveryWinFormsProjectAndBuilds()
    {
        var solutionPath = Path.Combine(AppContext.BaseDirectory, "SampleApps", "MultiProjectSolution", "MultiProjectSolution.slnx");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-solution-" + Guid.NewGuid());
        try
        {
            var result = new SolutionConversionPipeline().Run(new ConversionOptions(solutionPath, outputDir));

            Assert.Equal(
                ["Shell.csproj", "Widgets.csproj"],
                result.Converted.Select(c => Path.GetFileName(c.SourceProjectPath)).OrderBy(n => n, StringComparer.Ordinal));

            // Each project lands in its own folder, and one generated solution ties them together.
            Assert.Equal("MultiProjectSolution.slnx", result.SolutionFileName);
            var solutionFile = Path.Combine(outputDir, result.SolutionFileName);
            Assert.True(File.Exists(solutionFile));

            var solutionText = await File.ReadAllTextAsync(solutionFile);
            Assert.Contains("Shell/Shell.csproj", solutionText);
            Assert.Contains("Widgets/Widgets.csproj", solutionText);

            // The hard half: Shell's Form hosts a UserControl that Widgets defines. It has to
            // resolve to an element - through the assembly-qualified xmlns form, since `using:`
            // can only name the assembly being compiled - and Shell has to reference Widgets.
            var shell = result.Converted.Single(c => c.SourceProjectPath.EndsWith("Shell.csproj", StringComparison.Ordinal));
            Assert.DoesNotContain(shell.Result.Report.Warnings, w => w.Contains("sharedPanel1"));

            var mainView = await File.ReadAllTextAsync(Path.Combine(shell.OutputDirectory, "Views", "MainView.axaml"));
            Assert.Contains("xmlns:uc0=\"clr-namespace:Widgets.Views.Controls;assembly=Widgets\"", mainView);
            Assert.Contains("<uc0:SharedPanelView", mainView);

            var shellCsproj = await File.ReadAllTextAsync(Path.Combine(shell.OutputDirectory, "Shell.csproj"));
            Assert.Contains("<ProjectReference Include=\"..\\Widgets\\Widgets.csproj\" />", shellCsproj);

            // ...and not the other way round: Widgets names nothing of Shell's.
            var widgets = result.Converted.Single(c => c.SourceProjectPath.EndsWith("Widgets.csproj", StringComparison.Ordinal));
            var widgetsCsproj = await File.ReadAllTextAsync(Path.Combine(widgets.OutputDirectory, "Widgets.csproj"));
            Assert.DoesNotContain("ProjectReference", widgetsCsproj);

            // Widgets is a class library, and stays one: no entry point, no App, no stand-in window.
            Assert.DoesNotContain("<OutputType>", widgetsCsproj);
            Assert.DoesNotContain("Avalonia.Desktop", widgetsCsproj);
            Assert.False(File.Exists(Path.Combine(widgets.OutputDirectory, "Program.cs")));
            Assert.False(File.Exists(Path.Combine(widgets.OutputDirectory, "App.axaml")));
            Assert.False(File.Exists(Path.Combine(widgets.OutputDirectory, "Views", "MainWindowView.axaml")));
            Assert.True(File.Exists(Path.Combine(widgets.OutputDirectory, "Views", "Controls", "SharedPanelView.axaml")));

            // The point of the whole thing: `dotnet build` on the *generated solution*.
            var buildResult = await DotnetRunner.RunAsync($"build {result.SolutionFileName}", outputDir);
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

    /// <summary>Nothing reaches disk, the generated solution file included.</summary>
    [Fact]
    public void ConvertedSolution_WritesNothingOnADryRun()
    {
        var solutionPath = Path.Combine(AppContext.BaseDirectory, "SampleApps", "MultiProjectSolution", "MultiProjectSolution.slnx");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-solution-dry-" + Guid.NewGuid());

        var result = new SolutionConversionPipeline().Run(new ConversionOptions(solutionPath, outputDir) { DryRun = true });

        Assert.Equal(2, result.Converted.Count);
        Assert.False(Directory.Exists(outputDir));
    }

    /// <summary>
    /// Two projects called <c>App.csproj</c> in different folders used to be written into the
    /// same output folder, and a malformed csproj aborted the whole run - after the projects before
    /// it were already on disk, and without the solution file.
    /// </summary>
    [Fact]
    public async Task ConvertedSolution_SameNamedProjectsGetTheirOwnFoldersAndABrokenOneIsOnlySkipped()
    {
        var sourceDir = Path.Combine(Path.GetTempPath(), "w2a-solution-src-" + Guid.NewGuid());
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-solution-out-" + Guid.NewGuid());
        try
        {
            var widgets = Path.Combine(AppContext.BaseDirectory, "SampleApps", "MultiProjectSolution", "Widgets");
            foreach (var folder in new[] { "Client", "Server" })
            {
                CopyDirectory(widgets, Path.Combine(sourceDir, folder));
                File.Move(Path.Combine(sourceDir, folder, "Widgets.csproj"), Path.Combine(sourceDir, folder, "App.csproj"));
            }

            Directory.CreateDirectory(Path.Combine(sourceDir, "Broken"));
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "Broken", "Broken.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>");

            var solutionPath = Path.Combine(sourceDir, "Mixed.slnx");
            await File.WriteAllTextAsync(
                solutionPath,
                """
                <Solution>
                  <Project Path="Client/App.csproj" />
                  <Project Path="Broken/Broken.csproj" />
                  <Project Path="Server/App.csproj" />
                </Solution>
                """);

            var result = new SolutionConversionPipeline().Run(new ConversionOptions(solutionPath, outputDir));

            Assert.Equal(
                [Path.Combine(outputDir, "App"), Path.Combine(outputDir, "App2")],
                result.Converted.Select(c => c.OutputDirectory).Order(StringComparer.Ordinal));
            var skipped = Assert.Single(result.Skipped);
            Assert.EndsWith("Broken.csproj", skipped.SourceProjectPath, StringComparison.Ordinal);
            Assert.StartsWith("the conversion failed", skipped.Reason, StringComparison.Ordinal);

            var solutionText = await File.ReadAllTextAsync(Path.Combine(outputDir, "Mixed.slnx"));
            Assert.Contains("App/App.csproj", solutionText);
            Assert.Contains("App2/App2.csproj", solutionText);
            Assert.True(File.Exists(Path.Combine(outputDir, "App2", "App2.csproj")));
        }
        finally
        {
            foreach (var dir in new[] { sourceDir, outputDir })
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
        }
    }

    /// <summary>
    /// A UserControl deriving from a vendor base class the mappings file names as a UserControl is
    /// converted in its own project - and has to be predicted as one by the solution-wide
    /// discovery too, or the Form hosting it in another project finds no View to name.
    /// </summary>
    [Fact]
    public void ConvertedSolution_DiscoveryHonoursTheMappingsFile()
    {
        var sourceDir = Path.Combine(Path.GetTempPath(), "w2a-solution-map-" + Guid.NewGuid());
        try
        {
            CopyDirectory(Path.Combine(AppContext.BaseDirectory, "SampleApps", "MultiProjectSolution"), sourceDir);
            var panel = Path.Combine(sourceDir, "Widgets", "Controls", "SharedPanel.cs");
            File.WriteAllText(panel, File.ReadAllText(panel).Replace("SharedPanel : UserControl", "SharedPanel : Vendor.PanelBase", StringComparison.Ordinal));
            var mappings = Path.Combine(sourceDir, "mappings.json");
            File.WriteAllText(mappings, """{ "controls": { "PanelBase": { "as": "UserControl" } } }""");

            var result = new SolutionConversionPipeline().Run(
                new ConversionOptions(Path.Combine(sourceDir, "MultiProjectSolution.slnx"), Path.Combine(sourceDir, "out"))
                {
                    DryRun = true,
                    MappingsFile = mappings,
                });

            var shell = result.Converted.Single(c => c.SourceProjectPath.EndsWith("Shell.csproj", StringComparison.Ordinal));
            Assert.DoesNotContain(shell.Result.Report.Warnings, w => w.Contains("sharedPanel1", StringComparison.Ordinal));
            Assert.True(shell.Result.Vfs.TryGetText("Views/MainView.axaml", out var mainView));
            Assert.Contains("<uc0:SharedPanelView", mainView);
        }
        finally
        {
            if (Directory.Exists(sourceDir))
            {
                Directory.Delete(sourceDir, recursive: true);
            }
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.Split(Path.DirectorySeparatorChar).Any(s => s is "bin" or "obj"))
            {
                continue;
            }

            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }
}
