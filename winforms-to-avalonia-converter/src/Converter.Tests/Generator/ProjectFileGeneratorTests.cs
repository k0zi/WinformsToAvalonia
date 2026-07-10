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
}
