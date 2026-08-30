using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class FallbackControlConversionTests
{
    [Fact]
    public async Task ConvertedGroupBoxApp_EmitsRealGroupBoxWithChildrenInACanvasAndBuildsSuccessfully()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "GroupBoxApp", "GroupBoxApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-groupbox-" + Guid.NewGuid());
        try
        {
            var pipeline = new ConversionPipeline();
            var options = new ConversionOptions(SourceProjectPath: sourceProject, OutputDirectory: outputDir);

            var vfs = pipeline.Run(options).Vfs;

            // Avalonia 12 ships a real GroupBox, so no bundled Fallback control is involved -
            // GroupBoxFallback was deleted as superseded, exactly like MenuStripFallback before it.
            Assert.DoesNotContain("Controls/GroupBoxFallback.cs", vfs.RelativePaths);

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<GroupBox x:Name=\"groupBox1\"", axaml);
            Assert.Contains("Header=\"Options\"", axaml);
            Assert.DoesNotContain("GroupBoxFallback", axaml);

            // A GroupBox holds content, not positioned children, so the children go into a
            // wrapper Canvas and keep the absolute layout every other container gets.
            var groupBox = axaml.IndexOf("<GroupBox ", StringComparison.Ordinal);
            var canvas = axaml.IndexOf("<Canvas>", groupBox, StringComparison.Ordinal);
            var button = axaml.IndexOf("<Button ", StringComparison.Ordinal);
            Assert.True(groupBox < canvas && canvas < button, $"Expected the child inside a wrapper Canvas.\n{axaml}");

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
    public async Task ConvertedDomainUpDownApp_CopiesRewrittenFallbackControlAndCarriesItsItemsAndBuilds()
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

            // The template's Items is a get-only AvaloniaList<string>, so its entries are bare
            // strings inside a property element rather than item elements with a Content
            // attribute. The xmlns they need is declared on the root: Avalonia's XAML compiler
            // rejects an attribute on a property element, which is where it would otherwise go.
            Assert.Contains("xmlns:sys=\"using:System\"", axaml);
            Assert.Contains("<controls:DomainUpDownFallback.Items>", axaml);
            Assert.Contains("<sys:String>Monday</sys:String>", axaml);

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

            var result = pipeline.Run(options);
            var vfs = result.Vfs;

            // ToolStripContainerFallback.cs references ToolStripPanelFallback/
            // ToolStripContentPanelFallback as types, even though only ToolStripContainer
            // was actually mapped from the WinForms source - proves the dependency-closure
            // resolution in FallbackControlResolver actually copies all three.
            Assert.Contains("Controls/ToolStripContainerFallback.cs", vfs.RelativePaths);
            Assert.Contains("Controls/ToolStripPanelFallback.cs", vfs.RelativePaths);
            Assert.Contains("Controls/ToolStripContentPanelFallback.cs", vfs.RelativePaths);

            vfs.TryGetText("Views/MainView.axaml", out var axaml);
            Assert.Contains("<controls:ToolStripContainerFallback", axaml);

            // A control added to one of the container's nested regions cannot be placed - the
            // regions are not slots this converter models. It used to disappear from the AXAML
            // *and* from the report, which made it the one wholly silent loss in the converter.
            Assert.DoesNotContain("contentLabel", axaml);
            Assert.Contains(
                result.Report.Warnings,
                w => w.Contains("contentLabel", StringComparison.Ordinal)
                    && w.Contains("ContentPanel", StringComparison.Ordinal));

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
