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
