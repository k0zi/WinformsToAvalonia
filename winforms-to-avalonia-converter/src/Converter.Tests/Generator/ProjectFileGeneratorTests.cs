using Converter.Generator.Project;

namespace Converter.Tests.Generator;

public class ProjectFileGeneratorTests
{
    [Fact]
    public void GenerateAvaloniaProject_DoesNotReferenceAvaloniaReactiveUI()
    {
        var csproj = new ProjectFileGenerator().GenerateAvaloniaProject("SampleApp");

        Assert.DoesNotContain("Avalonia.ReactiveUI", csproj);
    }

    [Fact]
    public void PackageVersions_Avalonia_DefaultsToTwelveZero()
    {
        // Deliberate tripwire: Avalonia 12 is the current default target (11.x remains a
        // fully-supported opt-in via projectGeneration.avaloniaVersion). If this const ever
        // reverts to an 11.x value, this test should fail loudly rather than silently.
        Assert.Equal("12.0.0", ProjectFileGenerator.PackageVersions.Avalonia);
    }

    [Fact]
    public void GenerateAvaloniaProject_PackageVersionsTraceToCentralConstants()
    {
        var csproj = new ProjectFileGenerator().GenerateAvaloniaProject("SampleApp");

        Assert.Contains($"Include=\"Avalonia\" Version=\"{ProjectFileGenerator.PackageVersions.Avalonia}\"", csproj);
        Assert.Contains($"Include=\"Avalonia.Desktop\" Version=\"{ProjectFileGenerator.PackageVersions.Avalonia}\"", csproj);
        Assert.Contains($"Include=\"Avalonia.Themes.Fluent\" Version=\"{ProjectFileGenerator.PackageVersions.Avalonia}\"", csproj);
        Assert.Contains($"Include=\"Avalonia.Fonts.Inter\" Version=\"{ProjectFileGenerator.PackageVersions.Avalonia}\"", csproj);
        Assert.Contains(
            $"Include=\"CommunityToolkit.Mvvm\" Version=\"{ProjectFileGenerator.PackageVersions.CommunityToolkitMvvm}\"",
            csproj);
    }

    [Fact]
    public void GenerateAvaloniaProject_ExplicitVersions_OverrideDefaults()
    {
        var csproj = new ProjectFileGenerator().GenerateAvaloniaProject(
            "SampleApp",
            targetFramework: "net10.0",
            avaloniaVersion: "11.9.9",
            communityToolkitMvvmVersion: "9.9.9");

        Assert.Contains("Include=\"Avalonia\" Version=\"11.9.9\"", csproj);
        Assert.Contains("Include=\"Avalonia.Desktop\" Version=\"11.9.9\"", csproj);
        Assert.Contains("Include=\"Avalonia.Themes.Fluent\" Version=\"11.9.9\"", csproj);
        Assert.Contains("Include=\"Avalonia.Fonts.Inter\" Version=\"11.9.9\"", csproj);
        Assert.Contains("Include=\"CommunityToolkit.Mvvm\" Version=\"9.9.9\"", csproj);
        Assert.DoesNotContain($"Version=\"{ProjectFileGenerator.PackageVersions.Avalonia}\"", csproj);
    }
}
