using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class FallbackControlConversionTests
{
    [Fact]
    public async Task ConvertedGroupBoxApp_CopiesRewrittenFallbackControlAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "GroupBoxApp", "GroupBoxApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-groupbox-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            Assert.Contains("Controls/GroupBoxFallback.cs", vfs.RelativePaths);

            var fallbackFilePath = Path.Combine(outputDir, "Controls", "GroupBoxFallback.cs");
            Assert.True(File.Exists(fallbackFilePath));
            var fallbackSource = File.ReadAllText(fallbackFilePath);
            Assert.DoesNotContain("__TARGET_NAMESPACE__", fallbackSource);
            Assert.Contains("namespace w2a_groupbox_", fallbackSource); // project-name-derived namespace, guid suffix varies

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<controls:GroupBoxFallback", axaml);
            Assert.Contains("Header=\"Options\"", axaml);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);

            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ConvertedMenuStripApp_EmitsRealMenuWithNestedMenuItemAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "MenuStripApp", "MenuStripApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-menustrip-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            // MenuStrip is Direct-mapped to Avalonia's real Menu now - no bundled Fallback
            // control involved at all (MenuStripFallback was deleted as superseded).
            Assert.DoesNotContain("Controls/MenuStripFallback.cs", vfs.RelativePaths);

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<Menu x:Name=\"menuStrip1\"", axaml);
            Assert.Contains("<MenuItem x:Name=\"fileMenuItem\" Header=\"File\">", axaml);
            // The designer's `exitMenuItem.Click += ...` now becomes a real subscription, handled
            // by a generated method on the View (MenuItem maps to a control that has Click).
            Assert.Contains("<MenuItem x:Name=\"exitMenuItem\" Header=\"Exit\" Click=\"exitMenuItem_Click\" />", axaml);

            vfs.TryGetText("Views/MainView.axaml.cs", out var viewCodeBehind);
            Assert.Contains("private void exitMenuItem_Click(object? sender, RoutedEventArgs e)", viewCodeBehind);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);

            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ConvertedDomainUpDownApp_CopiesRewrittenFallbackControlAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "DomainUpDownApp", "DomainUpDownApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-domainupdown-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            Assert.Contains("Controls/DomainUpDownFallback.cs", vfs.RelativePaths);

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<controls:DomainUpDownFallback", axaml);
            Assert.Contains("Wrap=\"True\"", axaml);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);

            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ConvertedToolStripContainerApp_CopiesTransitiveFallbackDependenciesAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "ToolStripContainerApp", "ToolStripContainerApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-toolstripcontainer-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            // ToolStripContainerFallback.cs references ToolStripPanelFallback/
            // ToolStripContentPanelFallback as types, even though only ToolStripContainer
            // was actually mapped from the WinForms source - proves the dependency-closure
            // resolution in FallbackControlResolver actually copies all three.
            Assert.Contains("Controls/ToolStripContainerFallback.cs", vfs.RelativePaths);
            Assert.Contains("Controls/ToolStripPanelFallback.cs", vfs.RelativePaths);
            Assert.Contains("Controls/ToolStripContentPanelFallback.cs", vfs.RelativePaths);

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<controls:ToolStripContainerFallback", axaml);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);

            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

}
